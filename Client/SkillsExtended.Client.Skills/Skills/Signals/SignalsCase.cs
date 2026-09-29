using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkillsExtended.Signals;
using UnityEngine;
using UnityEngine.AI;

namespace SkillsExtended.Skills.Signals;

public sealed class SignalsCase : IDisposable
{
    private static AssetBundle _bundle;
    public LootableContainer Container { get; private set; }
    private GameObject _root;
    private GameWorld _world;
    private Material _beaconMaterial;
    private Mesh _beaconMesh;
    private World _registeredWorld;

    public static Vector3 Vector(SignalPoint p) => new(p.X, p.Y, p.Z);

    public static SignalPoint Point(Vector3 p) =>
        new()
        {
            X = p.x,
            Y = p.y,
            Z = p.z,
        };

    public static string ValidatePlacement(SignalPlacement placement, out Vector3 position)
    {
        position = Vector(placement.Position);
        if (!NavMesh.SamplePosition(position, out var nav, 4, NavMesh.AllAreas))
            return "No accessible navigation surface at the signal placement.";
        if (
            !Physics.Raycast(
                nav.position + Vector3.up * 2,
                Vector3.down,
                out var floor,
                5,
                LayerMask.GetMask("Terrain", "LowPolyCollider")
            )
        )
            return "No solid ground at the signal placement.";
        position = floor.point + Vector3.up * .02f;
        if (Vector3.Angle(floor.normal, Vector3.up) > 25)
            return "Signal placement is too steep.";
        foreach (var c in Physics.OverlapSphere(position, .8f))
        {
            var name = c.GetType().Name + " " + c.name;
            if (
                c.GetComponentInParent<LootableContainer>()
                || c.GetComponentInParent<Door>()
                || c.GetComponentInParent<Minefield>()
                || c.GetComponentInParent<ExfiltrationPoint>()
                || name.IndexOf("mine", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("exfil", StringComparison.OrdinalIgnoreCase) >= 0
            )
                return "Signal placement overlaps another interaction or hazardous area.";
        }
        if (
            Physics.CheckBox(
                position + Vector3.up * .2f,
                new Vector3(.36f, .19f, .21f),
                Quaternion.Euler(0, placement.Yaw, 0),
                LayerMask.GetMask("LowPolyCollider"),
                QueryTriggerInteraction.Ignore
            )
        )
            return "Signal placement overlaps solid geometry.";
        return null;
    }

    public static SignalsCase Create(
        GameWorld world,
        SignalManifest manifest,
        string inventory = null
    )
    {
        var error = ValidatePlacement(manifest.Placement, out var position);
        if (error != null)
            throw new InvalidOperationException(error);
        if (SignalsRuntime.IsAuthority())
            manifest.Placement.Position = Point(position);
        else
            position = Vector(manifest.Placement.Position);
        if (!_bundle)
            _bundle = AssetBundle.LoadFromFile(
                Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "bundles",
                    "signal_case.bundle"
                )
            );
        var prefab = _bundle ? _bundle.LoadAsset<GameObject>("signal-case.prefab") : null;
        if (!prefab)
            throw new InvalidOperationException("Signal case asset is missing.");
        var result = new SignalsCase { _world = world, _root = new GameObject("Signal cache") };
        try
        {
            result._root.SetActive(false);
            result._root.transform.SetPositionAndRotation(
                position,
                Quaternion.Euler(0, manifest.Placement.Yaw, 0)
            );
            var clone = UnityEngine.Object.Instantiate(prefab, result._root.transform, false);
            // The native toolbox is authored Z-up. Rotate its entire hierarchy so
            // the lid, interaction volume and ballistic colliders stay together.
            clone.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            var container = result.Container = clone.GetComponentInChildren<LootableContainer>(
                true
            );
            if (!container || container.Template != manifest.ContainerTemplate)
                throw new InvalidOperationException("Signal case template mismatch.");
            container.Id = manifest.ContainerId;
            container.ItemOwner = null;
            container.IsInitialized = false;
            container.KeyId = "";
            container.DoorState = EDoorState.Locked;
            var records = JArray.Parse(manifest.ItemsJson);
            var factory = Singleton<ItemFactory>.Instance;
            var root =
                (
                    inventory == null
                        ? factory.CreateItem(manifest.RootId, manifest.ContainerTemplate, null)
                        : JsonConvert
                            .DeserializeObject<ItemDescriptor>(
                                inventory,
                                EftJsonConverters.Converters
                            )
                            .Deserialize()
                ) as ContainerCollection;
            if (root == null)
                throw new InvalidOperationException("Signal case inventory unavailable.");
            root.SpawnedInSession = true;
            foreach (var record in records.Skip(inventory == null ? 1 : records.Count))
            {
                var item = factory.CreateItem((string)record["_id"], (string)record["_tpl"], null);
                item.SpawnedInSession = true;
                var grid = root
                    .Containers.OfType<Grid>()
                    .Single(g => g.ID == (string)record["slotId"]);
                var location = JsonConvert.DeserializeObject<LocationInGrid>(
                    record["location"].ToString()
                );
                var added = grid.Add(item, location, false);
                if (!added.Succeeded)
                    throw new InvalidOperationException("Signal item does not fit: " + added.Error);
            }
            LootItem.CreateLootContainer(container, root, "Signal cache", world, container.Id);
            container.DoorState = EDoorState.Locked;
            var beacon = new GameObject("Signal cache arrow");
            beacon.transform.SetParent(result._root.transform, false);
            // The arrow's origin is its downward tip, just above the closed lid.
            beacon.transform.localPosition = new Vector3(0, .5f, 0);
            result._beaconMesh = CreateArrowMesh();
            beacon.AddComponent<MeshFilter>().sharedMesh = result._beaconMesh;
            var renderer = beacon.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var shader = Shader.Find("Unlit/Color");
            result._beaconMaterial = shader
                ? new Material(shader)
                : new Material(clone.GetComponentInChildren<Renderer>(true).sharedMaterial);
            result._beaconMaterial.color = new Color(.15f, .8f, .72f);
            renderer.sharedMaterial = result._beaconMaterial;
            clone.SetActive(true);
            result._root.SetActive(true);
            result.RegisterInteraction(manifest);
            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }

    private static Mesh CreateArrowMesh()
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        void Face(params Vector3[] points)
        {
            var start = vertices.Count;
            vertices.AddRange(points);
            for (var i = 1; i < points.Length - 1; i++)
            {
                triangles.Add(start);
                triangles.Add(start + i);
                triangles.Add(start + i + 1);
            }
        }

        // Square pyramid head and narrow square shaft: readable from every approach.
        var tip = Vector3.zero;
        var a = new Vector3(-.1f, .12f, -.1f);
        var b = new Vector3(.1f, .12f, -.1f);
        var c = new Vector3(.1f, .12f, .1f);
        var d = new Vector3(-.1f, .12f, .1f);
        Face(tip, a, b);
        Face(tip, b, c);
        Face(tip, c, d);
        Face(tip, d, a);
        Face(d, c, b, a);
        a = new Vector3(-.025f, .12f, -.025f);
        b = new Vector3(.025f, .12f, -.025f);
        c = new Vector3(.025f, .12f, .025f);
        d = new Vector3(-.025f, .12f, .025f);
        var height = Vector3.up * .15f;
        Face(a, a + height, b + height, b);
        Face(b, b + height, c + height, c);
        Face(c, c + height, d + height, d);
        Face(d, d + height, a + height, a);
        Face(a + height, d + height, c + height, b + height);
        var mesh = new Mesh { name = "Signal cache downward arrow" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    public void RegisterInteraction(SignalManifest manifest)
    {
        var world = _world.World;
        if (!world)
        {
            if (SkillsExtendedInfo.IsFikaPresent)
                throw new InvalidOperationException("Native network world is unavailable.");
            return;
        }
        var objects = AccessTools.Field(typeof(World), "_interactiveObjectsDictionary");
        var registry = (Dictionary<string, WorldInteractiveObject>)objects.GetValue(world);
        if (registry == null)
            objects.SetValue(world, registry = new Dictionary<string, WorldInteractiveObject>());
        if (registry.TryGetValue(Container.Id, out var existing) && existing != Container)
            throw new InvalidOperationException(
                "Signal interaction identity is already registered."
            );
        if (SignalsRuntime.IsAuthority() && manifest.InteractionNetId == 0)
        {
            Container.NetId = 0; // Allocate through the native counter, after map interactables.
            manifest.InteractionNetId = Container.NetId;
        }
        else
        {
            if (manifest.InteractionNetId <= 0)
                throw new InvalidOperationException("Signal interaction identity is missing.");
            Container.NetId = manifest.InteractionNetId;
        }
        if (registry.Values.Any(obj => obj && obj != Container && obj.NetId == Container.NetId))
            throw new InvalidOperationException("Signal interaction network identity conflicts.");
        _registeredWorld = world;
        if (!existing)
            world.RegisterWorldInteractionObject(Container);
        var sync = AccessTools.Field(typeof(World), "_interactableObjectsForNetSync");
        var entries =
            (WorldInteractiveObject[])sync.GetValue(world) ?? Array.Empty<WorldInteractiveObject>();
        if (!entries.Contains(Container))
            sync.SetValue(world, entries.Concat(new[] { Container }).ToArray());
        var ids = AccessTools.Field(typeof(World), "_interactables");
        var map = (IDictionary<string, int>)ids.GetValue(world);
        if (map != null)
            map[Container.Id] = Container.NetId;
    }

    public void Unlock()
    {
        if (Container && Container.DoorState == EDoorState.Locked)
            Container.DoorState = EDoorState.Shut;
        if (_beaconMaterial)
            _beaconMaterial.color = new Color(.2f, .25f, .25f);
    }

    public string Snapshot() =>
        Container?.ItemOwner?.RootItem == null
            ? null
            : JsonConvert.SerializeObject(
                ItemBinarySerializer.SerializeItem(Container.ItemOwner.RootItem, null),
                EftJsonConverters.Converters
            );

    public void Dispose()
    {
        if (_registeredWorld && Container)
        {
            (
                (IDictionary<string, WorldInteractiveObject>)
                    AccessTools
                        .Field(typeof(World), "_interactiveObjectsDictionary")
                        .GetValue(_registeredWorld)
            )?.Remove(Container.Id);
            (
                (IDictionary<string, int>)
                    AccessTools.Field(typeof(World), "_interactables").GetValue(_registeredWorld)
            )?.Remove(Container.Id);
            var sync = AccessTools.Field(typeof(World), "_interactableObjectsForNetSync");
            if (sync.GetValue(_registeredWorld) is WorldInteractiveObject[] entries)
                sync.SetValue(_registeredWorld, entries.Where(obj => obj != Container).ToArray());
        }
        if (Container && _world)
        {
            _world.LootList.Remove(Container);
            if (Container.ItemOwner != null)
                _world.ItemOwners.Remove(Container.ItemOwner);
        }
        if (_root)
        {
            _root.SetActive(false);
            UnityEngine.Object.Destroy(_root);
        }
        if (_beaconMaterial)
            UnityEngine.Object.Destroy(_beaconMaterial);
        if (_beaconMesh)
            UnityEngine.Object.Destroy(_beaconMesh);
    }
}
