using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EFT;
using EFT.Game.Spawning;
using EFT.Interactive;
using SkillsExtended.Signals;
using UnityEngine;
using UnityEngine.AI;
using NVector = System.Numerics.Vector3;

namespace SkillsExtended.Skills.Signals;

internal sealed class SignalsPlacement : ISignalPlacementScene
{
    private static SignalCaseGeometry _geometry;
    private readonly Vector3[] _spawns;
    private readonly NavMeshPath _path = new();
    private readonly int _groundMask = LayerMask.GetMask(
        "Terrain",
        "LowPolyCollider",
        "HighPolyCollider"
    );

    private static NVector N(Vector3 p) => new(p.x, p.y, p.z);

    private static Vector3 U(NVector p) => new(p.X, p.Y, p.Z);

    private SignalsPlacement()
    {
        _spawns = LocationScene
            .GetAllObjectsAndWhenISayAllIActuallyMeanIt<SpawnPointMarker>()
            .Where(marker =>
                marker
                && marker.SpawnPoint != null
                && (marker.SpawnPoint.Sides & EPlayerSideMask.Pmc) != 0
                && (marker.SpawnPoint.Categories & ESpawnCategoryMask.Player) != 0
            )
            .Select(marker => marker.SpawnPoint.Position)
            .Select(p =>
                NavMesh.SamplePosition(p, out var hit, 1.4143f, NavMesh.AllAreas)
                && Bounded(p, hit.position)
                    ? (Vector3?)hit.position
                    : null
            )
            .Where(p => p.HasValue)
            .Select(p => p.Value)
            .Distinct()
            .ToArray();
    }

    public static Task<SignalPlacementReport> Resolve(
        MonoBehaviour runner,
        IEnumerable<SignalPlacement> orderedLocations,
        uint seed,
        CancellationToken cancellation
    )
    {
        var completion = new TaskCompletionSource<SignalPlacementReport>();
        var scene = new SignalsPlacement();
        var attempts = SignalPlacementSearch
            .Search(orderedLocations, seed, Geometry(), scene, cancellation)
            .GetEnumerator();
        var registration = cancellation.Register(() => completion.TrySetCanceled());
        runner.StartCoroutine(Run());
        return completion.Task;

        IEnumerator Run()
        {
            var report = new SignalPlacementReport();
            try
            {
                while (!completion.Task.IsCompleted)
                {
                    var finished = false;
                    try
                    {
                        for (var i = 0; i < 4; i++)
                        {
                            if (!attempts.MoveNext())
                            {
                                finished = true;
                                break;
                            }
                            report.Add(attempts.Current);
                            if (report.Placement != null)
                            {
                                finished = true;
                                break;
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        completion.TrySetCanceled();
                    }
                    catch (Exception e)
                    {
                        completion.TrySetException(e);
                    }
                    if (finished)
                    {
                        SkillsExtendedPlugin.Log.LogInfo("Signals placement: " + report);
                        completion.TrySetResult(report);
                    }
                    if (!completion.Task.IsCompleted)
                        yield return null;
                }
            }
            finally
            {
                attempts.Dispose();
                registration.Dispose();
                completion.TrySetCanceled();
            }
        }
    }

    public static SignalCaseGeometry Geometry()
    {
        if (_geometry != null)
            return _geometry;
        var root = new GameObject("Signal geometry inspection");
        root.SetActive(false);
        try
        {
            var clone = SignalsCase.InstantiateVisual(root.transform);
            var container = clone.GetComponentInChildren<LootableContainer>(true);
            if (!container)
                throw new InvalidOperationException("Signal case interaction geometry is missing.");
            container.CurrentAngle = container.CloseAngle;
            container.transform.localPosition = container.ClosedPosition;
            var body = BoundsOf(clone.transform, root.transform);
            var interaction = BoundsOf(container.transform, root.transform);
            var approaches = new[] { container.interactPosition1, container.interactPosition2 }
                .Select(p =>
                    N(root.transform.InverseTransformPoint(container.transform.TransformPoint(p)))
                )
                .Distinct()
                .ToArray();
            var target = root.transform.InverseTransformPoint(
                container.transform.TransformPoint(container.viewTarget1)
            );
            var opening = interaction;
            // Sample native motion on an inactive disposable clone. Inflate the sweep
            // by 1 cm to cover the small arcs between samples (at most one degree).
            var steps = Math.Max(
                1,
                Mathf.CeilToInt(Mathf.Abs(container.OpenAngle - container.CloseAngle))
            );
            for (var i = 0; i <= steps; i++)
            {
                var t = i / (float)steps;
                container.CurrentAngle = Mathf.Lerp(container.CloseAngle, container.OpenAngle, t);
                container.transform.localPosition = Vector3.Lerp(
                    container.ClosedPosition,
                    container.OpenPosition,
                    t
                );
                opening.Encapsulate(BoundsOf(container.transform, root.transform));
            }
            opening.Expand(.02f);
            _geometry = new SignalCaseGeometry
            {
                Body = Volume(body),
                Opening = Volume(opening),
                Interaction = Volume(interaction),
                Approaches = approaches,
                ViewTarget = N(target),
            };
            return _geometry;
        }
        finally
        {
            UnityEngine.Object.Destroy(root);
        }
    }

    private static SignalVolume Volume(Bounds bounds) => new(N(bounds.center), N(bounds.extents));

    private static Bounds BoundsOf(Transform subtree, Transform root)
    {
        Bounds? combined = null;
        void Add(Transform transform, Bounds bounds)
        {
            for (var x = -1; x <= 1; x += 2)
            for (var y = -1; y <= 1; y += 2)
            for (var z = -1; z <= 1; z += 2)
            {
                var corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                var point = root.InverseTransformPoint(transform.TransformPoint(corner));
                var current = combined ?? new Bounds(point, Vector3.zero);
                current.Encapsulate(point);
                combined = current;
            }
        }
        foreach (var mesh in subtree.GetComponentsInChildren<MeshFilter>(true))
            if (mesh.sharedMesh)
                Add(mesh.transform, mesh.sharedMesh.bounds);
        foreach (var collider in subtree.GetComponentsInChildren<Collider>(true))
        {
            if (collider is BoxCollider box)
                Add(box.transform, new Bounds(box.center, box.size));
            else if (collider is MeshCollider mesh && mesh.sharedMesh)
                Add(mesh.transform, mesh.sharedMesh.bounds);
        }
        return combined ?? throw new InvalidOperationException("Signal case bounds are missing.");
    }

    private static bool Bounded(Vector3 a, Vector3 b) =>
        new Vector2(a.x - b.x, a.z - b.z).magnitude <= 1 && Mathf.Abs(a.y - b.y) <= 1;

    public bool Navigation(NVector near, out NVector position)
    {
        var found = NavMesh.SamplePosition(U(near), out var hit, 1.4143f, NavMesh.AllAreas);
        position = N(hit.position);
        return found && Bounded(U(near), hit.position);
    }

    public bool Ground(NVector near, out NVector point, out float slope)
    {
        // Ignore our already spawned case when validating or previewing during a raid.
        var hits = Physics
            .RaycastAll(
                U(near) + Vector3.up,
                Vector3.down,
                2.01f,
                _groundMask,
                QueryTriggerInteraction.Ignore
            )
            .OrderBy(h => h.distance);
        foreach (var hit in hits)
        {
            if (Ignored(hit.collider))
                continue;
            point = N(hit.point);
            slope = Vector3.Angle(hit.normal, Vector3.up);
            return true;
        }
        point = default;
        slope = 0;
        return false;
    }

    private static bool Ignored(Collider c) =>
        !c
        || c.GetComponentInParent<Player>()
        || c.GetComponentInParent<SpawnPointMarker>()
        || c.GetComponentInParent<SignalsPlacementPreview>()
        || (c.GetComponentInParent<LootableContainer>()?.Id?.StartsWith(SignalsIds.Prefix) == true)
        || (
            SignalsRuntime.Instance?.CaseTransform
            && c.transform.IsChildOf(SignalsRuntime.Instance.CaseTransform)
        );

    private static SignalPlacementFailure Restricted(Collider c)
    {
        if (
            c.GetComponentInParent<BorderZone>()
            || c.GetComponentInParent<ExfiltrationPoint>()
            || c.GetComponentInParent<BaseRestrictableZone>()
            || c.GetComponentInParent<FlameDamageTrigger>()
        )
            return SignalPlacementFailure.Hazard;
        if (
            c.GetComponentInParent<Door>()
            || c.GetComponentInParent<LootableContainer>()
            || c.GetComponentInParent<LootItem>()
        )
            return SignalPlacementFailure.Interaction;
        // Some scene hazards have only named trigger parents, not typed components.
        for (var t = c.transform; t != null; t = t.parent)
            if (
                t.name.IndexOf("minefield", StringComparison.OrdinalIgnoreCase) >= 0
                || t.name.IndexOf("exfil", StringComparison.OrdinalIgnoreCase) >= 0
                || t.name.IndexOf("sniper", StringComparison.OrdinalIgnoreCase) >= 0
            )
                return SignalPlacementFailure.Hazard;
        return SignalPlacementFailure.None;
    }

    public SignalPlacementFailure Clearance(SignalVolume volume)
    {
        foreach (
            var c in Physics.OverlapBox(
                U(volume.Center),
                U(volume.Extents),
                Quaternion.Euler(0, volume.Yaw, 0),
                ~0,
                QueryTriggerInteraction.Collide
            )
        )
        {
            if (Ignored(c))
                continue;
            var restricted = Restricted(c);
            if (restricted != SignalPlacementFailure.None)
                return restricted;
            if (!c.isTrigger)
                return SignalPlacementFailure.Solid;
        }
        return SignalPlacementFailure.None;
    }

    public bool Visible(NVector eye, NVector target)
    {
        var direction = U(target - eye);
        return !Physics
            .RaycastAll(
                U(eye),
                direction.normalized,
                direction.magnitude,
                ~0,
                QueryTriggerInteraction.Collide
            )
            .Any(h =>
                !Ignored(h.collider)
                && (!h.collider.isTrigger || Restricted(h.collider) != SignalPlacementFailure.None)
            );
    }

    public bool Route(NVector approach)
    {
        foreach (var spawn in _spawns.OrderBy(p => (p - U(approach)).sqrMagnitude))
        {
            if (
                !NavMesh.CalculatePath(spawn, U(approach), NavMesh.AllAreas, _path)
                || _path.status != NavMeshPathStatus.PathComplete
            )
                continue;
            var corners = _path.corners;
            if (
                corners.Length == 0
                || Vector3.Distance(corners[corners.Length - 1], U(approach)) > .25f
            )
                continue;
            var safe = true;
            for (var i = 1; i < corners.Length && safe; i++)
            {
                var from = corners[i - 1] + Vector3.up * .9f;
                var delta = corners[i] - corners[i - 1];
                // Check starting overlaps too; casts alone miss enclosing trigger volumes.
                safe =
                    !Physics
                        .OverlapSphere(from, .3f, ~0, QueryTriggerInteraction.Collide)
                        .Any(c => !Ignored(c) && Restricted(c) != SignalPlacementFailure.None)
                    && !Physics
                        .SphereCastAll(
                            from,
                            .3f,
                            delta.normalized,
                            delta.magnitude,
                            ~0,
                            QueryTriggerInteraction.Collide
                        )
                        .Any(h =>
                            !Ignored(h.collider)
                            && Restricted(h.collider) != SignalPlacementFailure.None
                        );
            }
            if (safe && corners.Length > 0)
                return true;
        }
        return false;
    }
}

internal sealed class SignalsPlacementPreview : MonoBehaviour { }
