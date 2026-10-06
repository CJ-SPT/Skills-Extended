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

    internal static GameObject InstantiateVisual(Transform parent)
    {
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
        var clone = UnityEngine.Object.Instantiate(prefab, parent, false);
        // The olive case is authored in Unity's Y-up axes.
        clone.transform.localRotation = Quaternion.identity;
        return clone;
    }

    public Transform Transform => _root ? _root.transform : null;

    public static SignalsCase Create(
        GameWorld world,
        SignalManifest manifest,
        string inventory = null
    )
    {
        if (
            !manifest.PlacementResolved
            || manifest.Placement?.Position?.IsFinite != true
            || !SignalPoint.Finite(manifest.Placement.Yaw)
        )
            throw new InvalidOperationException(
                "Signal placement has not been resolved by the authority."
            );
        var position = Vector(manifest.Placement.Position);
        var result = new SignalsCase { _world = world, _root = new GameObject("Signal cache") };
        try
        {
            result._root.SetActive(false);
            result._root.transform.SetPositionAndRotation(
                position,
                Quaternion.Euler(0, manifest.Placement.Yaw, 0)
            );
            var clone = InstantiateVisual(result._root.transform);
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
                        : SignalsInventoryCodec.Deserialize(inventory).Deserialize()
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
            if (manifest.Config?.ShowCacheArrow != false)
            {
                var beacon = new GameObject("Signal cache arrow");
                beacon.transform.SetParent(result._root.transform, false);
                // The arrow's origin is its downward tip, just above the closed lid.
                var bounds = SignalsPlacement.Geometry().Body;
                beacon.transform.localPosition = new Vector3(
                    bounds.Center.X,
                    bounds.Center.Y + bounds.Extents.Y + .12f,
                    bounds.Center.Z
                );
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
            }
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
            : SignalsInventoryCodec.Serialize(
                ItemBinarySerializer.SerializeItem(Container.ItemOwner.RootItem, null)
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
