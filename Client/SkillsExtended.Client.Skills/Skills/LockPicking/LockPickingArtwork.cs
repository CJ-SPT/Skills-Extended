using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace SkillsExtended.Skills.LockPicking;

/// <summary>Purchased art is embedded in the plugin; no source FBX or loose vendor textures are shipped.</summary>
internal sealed class LockPickingArtwork : IDisposable
{
    // Large model coordinates preserve submillimetre detail in the remote render studio.
    private const float StudioScale = 64;
    private static readonly Dictionary<string, Mesh> Meshes = new();
    private static Texture2D _albedo,
        _metal,
        _normal;
    private readonly GameObject _root;
    private readonly Camera _camera;
    private readonly RenderTexture _target;
    private readonly Material _material;
    private readonly Material _backingMaterial;
    private readonly Cubemap _reflection;
    private readonly Transform _cylinder,
        _pick,
        _tension;
    private readonly Quaternion _pickRest,
        _tensionRest;

    public static void Prepare()
    {
        if (_albedo)
            return;
        using var stream =
            Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream("SkillsExtended.LockPicking.assets")
            ?? throw new InvalidDataException("Missing embedded lock-picking art.");
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        foreach (
            var name in new[]
            {
                "Housing_01",
                "Housing_02",
                "Lock_01",
                "Lock_02",
                "Pick_01",
                "Pick_02",
                "Pick_03",
                "Pick_04",
                "Screwdriver",
            }
        )
        {
            using var input = archive.GetEntry(name + ".mesh").Open();
            using var reader = new BinaryReader(input);
            var vertices = reader.ReadInt32();
            var indices = reader.ReadInt32();
            if (vertices < 3 || vertices > 10000 || indices < 3 || indices > 30000)
                throw new InvalidDataException("Invalid picking mesh.");
            var positions = new Vector3[vertices];
            var normals = new Vector3[vertices];
            var uv = new Vector2[vertices];
            for (var i = 0; i < vertices; i++)
            {
                positions[i] = new Vector3(
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    -reader.ReadSingle()
                );
                normals[i] = new Vector3(
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    -reader.ReadSingle()
                );
                uv[i] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            }
            var triangles = new int[indices];
            for (var i = 0; i < indices; i += 3)
            {
                triangles[i] = reader.ReadInt32();
                triangles[i + 2] = reader.ReadInt32();
                triangles[i + 1] = reader.ReadInt32();
            }
            var mesh = new Mesh
            {
                name = "LockPicking/" + name,
                vertices = positions,
                normals = normals,
                uv = uv,
                triangles = triangles,
            };
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            Meshes[name] = mesh;
        }
        Texture2D Texture(string name, bool linear)
        {
            using var input = archive.GetEntry(name + ".png").Open();
            using var buffer = new MemoryStream();
            input.CopyTo(buffer);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true, linear)
            {
                name = "LockPicking/" + name,
                anisoLevel = 8,
                filterMode = FilterMode.Trilinear,
            };
            if (!texture.LoadImage(buffer.ToArray(), true))
                throw new InvalidDataException("Invalid picking texture.");
            return texture;
        }
        _metal = Texture("MS", true);
        _normal = Texture("N", true);
        _albedo = Texture("AT", false);
    }

    public LockPickingArtwork(RawImage image, int tier)
    {
        Prepare();
        var shader = Shader.Find("Standard");
        if (!shader)
            throw new InvalidOperationException(
                "Standard material shader is unavailable for lock-picking artwork."
            );
        _material = new Material(shader) { name = "LockPicking metal" };
        _material.SetTexture("_MainTex", _albedo);
        _material.SetTexture("_MetallicGlossMap", _metal);
        _material.SetTexture("_BumpMap", _normal);
        _material.SetFloat("_GlossMapScale", .48f);
        _material.SetFloat("_BumpScale", .55f);
        _material.EnableKeyword("_METALLICGLOSSMAP");
        _material.EnableKeyword("_NORMALMAP");
        _root = new GameObject("LockPicking isolated studio");
        _root.transform.position = new Vector3(10000, 10000, 10000);
        _root.transform.localScale = Vector3.one * StudioScale;
        var cameraObject = Child("Camera");
        _camera = cameraObject.AddComponent<Camera>();
        _camera.enabled = false;
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = new Color(.042f, .049f, .052f, 1);
        _camera.orthographic = true;
        _camera.orthographicSize = .135f * StudioScale;
        _camera.nearClipPlane = .01f * StudioScale;
        _camera.farClipPlane = 2 * StudioScale;
        _camera.cullingMask = 1 << 30;
        _camera.renderingPath = RenderingPath.Forward;
        cameraObject.transform.localPosition = new Vector3(0, -.035f, -.65f);
        _target = new RenderTexture(2000, 620, 24)
        {
            antiAliasing = 4,
            name = "LockPicking close-up",
        };
        _target.Create();
        _camera.targetTexture = _target;
        image.texture = _target;
        _reflection = StudioReflection();
        var probe = Child("Local material reflections").AddComponent<ReflectionProbe>();
        probe.transform.localScale = Vector3.one / StudioScale;
        probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Custom;
        probe.customBakedTexture = _reflection;
        probe.size = Vector3.one * 3 * StudioScale;
        probe.blendDistance = 0;
        probe.intensity = .8f;
        Light("Soft key", new Vector3(-.22f, .2f, -.3f), new Color(1, .94f, .85f), 1.05f);
        Light("Soft fill", new Vector3(.22f, -.02f, -.25f), new Color(.80f, .89f, 1), .55f);
        _backingMaterial = new Material(shader) { color = new Color(.075f, .09f, .095f) };
        _backingMaterial.SetFloat("_Metallic", .35f);
        _backingMaterial.SetFloat("_Glossiness", .22f);
        var backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backing.name = "Door surface";
        backing.layer = 30;
        backing.transform.SetParent(_root.transform, false);
        backing.transform.localPosition = new Vector3(0, -.035f, .038f);
        backing.transform.localScale = new Vector3(.7f, .45f, .015f);
        UnityEngine.Object.Destroy(backing.GetComponent<Collider>());
        backing.GetComponent<MeshRenderer>().sharedMaterial = _backingMaterial;
        var variant = tier >= 4 ? "02" : "01";
        Model("Housing_" + variant, _root.transform);
        _cylinder = Model("Lock_" + variant, _root.transform);
        _pick = Child("Pick pivot").transform;
        var pickMesh = Model("Pick_04", _pick);
        pickMesh.localPosition = Vector3.zero;
        pickMesh.localScale = Vector3.one * .32f;
        _pick.localPosition = new Vector3(-.0015f, .006f, -.045f);
        _pickRest = Quaternion.Euler(0, 0, 123);
        _pick.localRotation = _pickRest;
        _tension = Child("Tension pivot").transform;
        var tensionMesh = Model("Screwdriver", _tension);
        tensionMesh.localPosition = Vector3.zero;
        tensionMesh.localScale = Vector3.one * .25f;
        _tension.localPosition = new Vector3(.002f, -.01f, -.037f);
        _tensionRest = Quaternion.Euler(0, 0, 236) * Quaternion.Euler(-10, 0, 0);
        _tension.localRotation = _tensionRest;
    }

    private GameObject Child(string name)
    {
        var go = new GameObject(name) { layer = 30 };
        go.transform.SetParent(_root.transform, false);
        return go;
    }

    private Transform Model(string name, Transform parent)
    {
        var go = Child(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = Meshes[name];
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = _material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Simple;
        return go.transform;
    }

    private void Light(string name, Vector3 position, Color color, float intensity)
    {
        var go = Child(name);
        go.transform.localPosition = position;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = StudioScale;
        light.color = color;
        light.intensity = intensity;
        light.cullingMask = 1 << 30;
    }

    public void Render(
        float lift,
        float depth,
        float strain,
        bool tension,
        bool unlocked,
        bool broken,
        bool reducedMotion
    )
    {
        var shake = reducedMotion ? 0 : Mathf.Sin(Time.unscaledTime * 43) * strain * 1.2f;
        // The handle projects toward the camera. Positive X tilt put it inside the lock face.
        _pick.localRotation = _pickRest * Quaternion.Euler(-12 - lift * 5, 0, lift * 6 + shake);
        _pick.localPosition = new Vector3(
            -.0015f + depth * .001f,
            .006f + lift * .001f,
            -.045f + depth * .002f
        );
        _pick.gameObject.SetActive(!broken);
        _tension.localRotation = _tensionRest * Quaternion.Euler(0, 0, tension ? -4 : 0);
        var rotation =
            unlocked ? 90
            : tension ? 2
            : 0;
        _cylinder.localRotation = Quaternion.Slerp(
            _cylinder.localRotation,
            Quaternion.Euler(0, 0, rotation),
            1 - Mathf.Exp(-12 * Time.unscaledDeltaTime)
        );
        // Pull the tools clear before showing the released cylinder.
        _pick.gameObject.SetActive(!broken && !unlocked);
        _tension.gameObject.SetActive(!unlocked);
        _camera.Render();
    }

    public void Dispose()
    {
        if (_camera)
            _camera.targetTexture = null;
        if (_target)
        {
            _target.Release();
            UnityEngine.Object.Destroy(_target);
        }
        if (_root)
            UnityEngine.Object.Destroy(_root);
        if (_material)
            UnityEngine.Object.Destroy(_material);
        if (_backingMaterial)
            UnityEngine.Object.Destroy(_backingMaterial);
        if (_reflection)
            UnityEngine.Object.Destroy(_reflection);
    }

    private static Cubemap StudioReflection()
    {
        const int size = 64;
        var cube = new Cubemap(size, TextureFormat.RGBAHalf, true)
        {
            name = "Picking softbox reflections",
        };
        foreach (
            CubemapFace face in new[]
            {
                CubemapFace.PositiveX,
                CubemapFace.NegativeX,
                CubemapFace.PositiveY,
                CubemapFace.NegativeY,
                CubemapFace.PositiveZ,
                CubemapFace.NegativeZ,
            }
        )
        {
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var u = (x + .5f) / size * 2 - 1;
                var v = (y + .5f) / size * 2 - 1;
                var d = face switch
                {
                    CubemapFace.PositiveX => new Vector3(1, -v, -u),
                    CubemapFace.NegativeX => new Vector3(-1, -v, u),
                    CubemapFace.PositiveY => new Vector3(u, 1, v),
                    CubemapFace.NegativeY => new Vector3(u, -1, -v),
                    CubemapFace.PositiveZ => new Vector3(u, -v, 1),
                    _ => new Vector3(-u, -v, -1),
                };
                d.Normalize();
                var color = Color.Lerp(
                    new Color(.045f, .05f, .055f),
                    new Color(.22f, .25f, .28f),
                    d.y * .5f + .5f
                );
                var softbox = Mathf.Pow(
                    Mathf.Max(0, Vector3.Dot(d, new Vector3(-.5f, .6f, -.6f).normalized)),
                    18
                );
                var fill = Mathf.Pow(
                    Mathf.Max(0, Vector3.Dot(d, new Vector3(.8f, .1f, -.6f).normalized)),
                    26
                );
                color += new Color(1.15f, 1.08f, .94f) * softbox + new Color(.4f, .48f, .6f) * fill;
                color.a = 1;
                pixels[y * size + x] = color;
            }
            cube.SetPixels(pixels, face);
        }
        cube.Apply(true, true);
        return cube;
    }
}
