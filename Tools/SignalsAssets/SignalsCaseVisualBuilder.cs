// Authoring only. Copy this file into
// CJ-SDK/Assets/Mods/SkillsExtended.Assets/Editor before running Unity.
//
// This builder deliberately contains no EFT references. Native EFT components
// are grafted onto the compiled visual bundle by the separate UnityPy step.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class SignalsCaseVisualBuilder
{
    private const string Root = "Assets/Mods/SkillsExtended.Assets/SignalsCase";
    private const string Source = Root + "/Source";
    private const string Generated = Root + "/Generated";
    private const string SourcePrefab = Source + "/prefabs/ammo_box_01.prefab";
    private const string SourceMesh = Source + "/meshes/ammo_box.FBX";
    private const string SourceMaterial = Source + "/materials/ammo_box_01.mat";
    private const string AlbedoTexture = Source + "/textures/ammo_box_01_albedo.tga";
    private const string NormalTexture = Source + "/textures/ammo_box_normal.tga";
    private const string PackedTexture =
        Source + "/textures/ammo_box_MetallicOcclusionSmoothness.tga";
    private const string GeneratedPrefab = Generated + "/signal-case.prefab";
    private const string BundleName = "signal_case_visual.bundle";
    private const string PrefabAddress = "signal-case.prefab";
    private const string ValidationName = "visual-validation.json";

    // These are the native scene layer indices observed on the donor case.
    // The target project owns the layer names; this builder must not edit its
    // TagManager just to author a visual asset.
    private const int BallisticLayer = 12;
    private const int PhysicalLayer = 18;
    private const int InteractionLayer = 22;

    // The source hinge is at the rear (-Z). A negative X rotation raises the
    // front (+Z), matching the native container's opening direction.
    private const float LidOpenAngleDegrees = -105f;
    private static readonly Vector3 LidPivot = new(0f, 0.21615717f, -0.2890697f);
    private static readonly Vector3 IdentityScale = Vector3.one;

    [MenuItem("SDK/Skills Extended/Build Signals Case Visual")]
    public static void Build()
    {
        RequireUnityVersion();
        EnsureInputLayout();
        ConfigureTextureImporters();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        ValidateSourceAssets();

        Directory.CreateDirectory(AbsoluteProjectPath(Generated));
        var generated = BuildPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        ValidateGeneratedPrefab(generated);

        var output = ResolveOutputDirectory();
        Directory.CreateDirectory(output);
        var manifest = BuildBundle(output);
        if (!manifest || manifest.GetAllDependencies(BundleName).Length != 0)
            throw new InvalidOperationException("Signals case visual bundle has dependencies.");

        var bundlePath = Path.Combine(output, BundleName);
        var bundle = AssetBundle.LoadFromFile(bundlePath);
        if (!bundle)
            throw new InvalidOperationException("Could not reload signals case visual bundle.");

        try
        {
            var built = bundle.LoadAsset<GameObject>(PrefabAddress);
            if (!built)
                throw new InvalidOperationException(
                    "Bundle address did not resolve: " + PrefabAddress
                );

            ValidateBundledPrefab(built);
            WritePreviewsAndValidation(built, output);
        }
        finally
        {
            bundle.Unload(true);
        }

        Debug.Log("Signals case visual bundle validated: " + bundlePath);
    }

    // Execute this after the UnityPy native graft. The SDK does not contain
    // the native EFT scripts, so this validates the visual graph and renders
    // the final bundle without attempting to instantiate those script types.
    [MenuItem("SDK/Skills Extended/Validate Packaged Signals Case")]
    public static void ValidatePackaged()
    {
        RequireUnityVersion();
        var output = ResolveOutputDirectory();
        var bundlePath = Path.Combine(output, "signal_case.bundle");
        if (!File.Exists(bundlePath))
            throw new FileNotFoundException("Final grafted signals bundle is missing.", bundlePath);

        var bundle = AssetBundle.LoadFromFile(bundlePath);
        if (!bundle)
            throw new InvalidOperationException("Could not load final grafted signals bundle.");

        try
        {
            var prefab = bundle.LoadAsset<GameObject>(PrefabAddress);
            if (!prefab)
                throw new InvalidOperationException(
                    "Final grafted bundle does not expose " + PrefabAddress + "."
                );
            ValidateBundledPrefab(prefab);
            WritePreviewsAndValidation(prefab, output);
        }
        finally
        {
            bundle.Unload(true);
        }

        File.WriteAllText(
            Path.Combine(output, "packaged-visual-validation.txt"),
            "Unity "
                + Application.unityVersion
                + "\nFinal grafted bundle graph/materials/meshes/colliders: passed\n"
                + "Closed/open previews rendered with cap.localRotation (native scripts intentionally not required in SDK).\n"
        );
        Debug.Log("Final grafted signals case bundle validated: " + bundlePath);
    }

    private static void RequireUnityVersion()
    {
        if (Application.unityVersion != "2022.3.43f1")
            throw new InvalidOperationException(
                "Use Unity 2022.3.43f1 for EFT bundles; found " + Application.unityVersion + "."
            );
    }

    private static void EnsureInputLayout()
    {
        var required = new[]
        {
            SourcePrefab,
            SourceMesh,
            SourceMaterial,
            AlbedoTexture,
            NormalTexture,
            PackedTexture,
        };
        var missing = required.Where(path => !File.Exists(AbsoluteProjectPath(path))).ToArray();
        if (missing.Length != 0)
            throw new FileNotFoundException(
                "Signals case source input(s) are missing: " + string.Join(", ", missing)
            );
    }

    private static void ConfigureTextureImporters()
    {
        ConfigureTextureImporter(AlbedoTexture, TextureImporterType.Default, true);
        ConfigureTextureImporter(NormalTexture, TextureImporterType.NormalMap, false);
        ConfigureTextureImporter(PackedTexture, TextureImporterType.Default, false);
    }

    private static void ConfigureTextureImporter(
        string path,
        TextureImporterType type,
        bool srgb
    )
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!importer)
            throw new InvalidOperationException("Texture importer is unavailable: " + path);

        // Keep the authored 4K maps intact. Compression may be chosen by the
        // runtime platform later, but authoring validation must see all texels.
        importer.textureType = type;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.crunchedCompression = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.sRGBTexture = srgb;
        importer.mipmapEnabled = true;
        importer.isReadable = false;
        importer.filterMode = FilterMode.Trilinear;
        importer.SaveAndReimport();
    }

    private static void ValidateSourceAssets()
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefab);
        if (!source)
            throw new InvalidOperationException("Could not load source prefab: " + SourcePrefab);

        var filters = source.GetComponentsInChildren<MeshFilter>(true);
        if (filters.Length != 4)
            throw new InvalidOperationException(
                "Expected the source prefab to contain four visual mesh filters; found "
                    + filters.Length
            );

        foreach (var filter in filters)
        {
            if (!filter.sharedMesh || filter.sharedMesh.vertexCount == 0)
                throw new InvalidOperationException(
                    "Source mesh is missing or empty on " + filter.name
                );

            if (filter.sharedMesh.subMeshCount == 0)
                throw new InvalidOperationException("Source mesh has no submeshes: " + filter.name);
        }

        var renderers = source.GetComponentsInChildren<MeshRenderer>(true);
        if (renderers.Length != filters.Length)
            throw new InvalidOperationException("Source visual mesh/renderers are not one-to-one.");

        var materials = renderers
            .SelectMany(renderer => renderer.sharedMaterials)
            .Where(material => material)
            .Distinct()
            .ToArray();
        if (materials.Length != 1)
            throw new InvalidOperationException(
                "Expected exactly one olive source material; found " + materials.Length
            );

        var materialPath = Normalize(AssetDatabase.GetAssetPath(materials[0]));
        if (materialPath != SourceMaterial)
            throw new InvalidOperationException(
                "Source prefab uses an unexpected material: " + materialPath
            );

        ValidateMaterial(materials[0]);
        ValidateTexture(AlbedoTexture, "albedo", 4096, 4096);
        ValidateTexture(NormalTexture, "normal", 4096, 4096);
        ValidateTexture(PackedTexture, "metallic/occlusion/smoothness", 4096, 4096);

        var importedMeshes = AssetDatabase
            .LoadAllAssetsAtPath(SourceMesh)
            .OfType<Mesh>()
            .Where(mesh => mesh)
            .ToArray();
        if (importedMeshes.Length == 0 || importedMeshes.Any(mesh => mesh.vertexCount == 0))
            throw new InvalidOperationException("The staged FBX has no non-empty meshes.");

        var dependencyPaths = AssetDatabase.GetDependencies(SourcePrefab, true).Select(Normalize);
        var forbidden = dependencyPaths
            .Where(path =>
                path.IndexOf("ammo_box_02", StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("ammo_box_03", StringComparison.OrdinalIgnoreCase) >= 0
            )
            .ToArray();
        if (forbidden.Length != 0)
            throw new InvalidOperationException(
                "Olive source prefab has alternate-color dependencies: "
                    + string.Join(", ", forbidden)
            );
    }

    private static void ValidateMaterial(Material material, bool requireSourcePaths = true)
    {
        if (!material.shader || !material.shader.isSupported)
            throw new InvalidOperationException(
                "Source material shader is missing or unsupported: " + material.name
            );

        var shaderName = material.shader.name ?? string.Empty;
        if (shaderName.IndexOf("standard", StringComparison.OrdinalIgnoreCase) < 0)
            throw new InvalidOperationException(
                "Expected a supported Standard-compatible source shader; found " + shaderName
            );

        ValidateMaterialTexture(
            material,
            "_MainTex",
            AlbedoTexture,
            "albedo",
            requireSourcePaths
        );
        ValidateMaterialTexture(
            material,
            "_BumpMap",
            NormalTexture,
            "normal",
            requireSourcePaths
        );
        ValidateMaterialTexture(
            material,
            "_MetallicGlossMap",
            PackedTexture,
            "packed map",
            requireSourcePaths
        );
        ValidateMaterialTexture(
            material,
            "_OcclusionMap",
            PackedTexture,
            "occlusion map",
            requireSourcePaths
        );
    }

    private static void ValidateMaterialTexture(
        Material material,
        string property,
        string expectedPath,
        string label,
        bool requireSourcePath
    )
    {
        if (!material.HasProperty(property))
            throw new InvalidOperationException(
                "Source material does not expose its " + label + " property: " + property
            );

        var texture = material.GetTexture(property);
        if (!texture)
            throw new InvalidOperationException(
                "Source material " + material.name + " is missing its " + label + " texture."
            );

        if (texture is Texture2D map && (map.width != 4096 || map.height != 4096))
            throw new InvalidOperationException(
                "Source material " + material.name + " has a non-4K " + label + " texture."
            );

        var path = texture ? Normalize(AssetDatabase.GetAssetPath(texture)) : string.Empty;
        if (requireSourcePath && path != expectedPath)
            throw new InvalidOperationException(
                "Source material "
                    + material.name
                    + " has unexpected "
                    + label
                    + " texture: "
                    + path
            );
    }

    private static void ValidateTexture(string path, string label, int width, int height)
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (!texture || texture.width != width || texture.height != height)
            throw new InvalidOperationException(
                "Source "
                    + label
                    + " texture must be "
                    + width
                    + "x"
                    + height
                    + ": "
                    + path
            );

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (
            !importer
            || importer.maxTextureSize < width
            || importer.textureCompression != TextureImporterCompression.Uncompressed
        )
            throw new InvalidOperationException("Source " + label + " texture was downsampled/compressed.");
    }

    private static GameObject BuildPrefab()
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefab);
        var instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (!instance)
            throw new InvalidOperationException("Could not instantiate source prefab.");

        try
        {
            PrefabUtility.UnpackPrefabInstance(
                instance,
                PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction
            );

            instance.name = "signal-case";
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = IdentityScale;

            var cap = FindUnique(instance.transform, "cap");
            cap.localPosition = LidPivot;
            cap.localRotation = Quaternion.identity;
            cap.localScale = IdentityScale;
            // The native interaction component is grafted onto this exact
            // GameObject. Keep its collider on cap rather than on a proxy.
            cap.gameObject.layer = InteractionLayer;

            // Source transforms are authored as identity children of the body
            // and lid. Normalize their tiny FBX epsilon rotations so the
            // generated root and hinge are deterministic.
            foreach (
                var visual in instance
                    .GetComponentsInChildren<MeshFilter>(true)
                    .Select(filter => filter.transform)
            )
            {
                if (visual != cap)
                {
                    visual.localPosition = Vector3.zero;
                    visual.localRotation = Quaternion.identity;
                    visual.localScale = IdentityScale;
                }
            }

            foreach (var body in instance.GetComponentsInChildren<Rigidbody>(true))
                UnityEngine.Object.DestroyImmediate(body);
            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider);

            var visualFilters = instance.GetComponentsInChildren<MeshFilter>(true).ToArray();
            if (visualFilters.Length != 4)
                throw new InvalidOperationException("Source visual mesh count changed during build.");

            AddBallisticColliders(visualFilters);
            AddPhysicalColliders(instance.transform, cap, visualFilters);
            AddInteractionCollider(cap, visualFilters);

            Directory.CreateDirectory(AbsoluteProjectPath(Generated));
            var saved = PrefabUtility.SaveAsPrefabAsset(instance, GeneratedPrefab);
            if (!saved)
                throw new InvalidOperationException("Unity did not save " + GeneratedPrefab);
            return saved;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static void AddBallisticColliders(IReadOnlyList<MeshFilter> visualFilters)
    {
        foreach (var filter in visualFilters)
        {
            var ballistic = new GameObject(filter.gameObject.name + "_BALLISTIC");
            // Keep each collider beneath the visual it mirrors. In particular,
            // cap/cap_metallic ballistic meshes must rotate with the hinge.
            ballistic.transform.SetParent(filter.transform, false);
            ballistic.transform.localPosition = Vector3.zero;
            ballistic.transform.localRotation = Quaternion.identity;
            ballistic.transform.localScale = IdentityScale;
            ballistic.layer = BallisticLayer;

            var meshCollider = ballistic.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = filter.sharedMesh;
            meshCollider.convex = false;
            meshCollider.isTrigger = false;
            meshCollider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation
                | MeshColliderCookingOptions.EnableMeshCleaning
                | MeshColliderCookingOptions.WeldColocatedVertices
                | MeshColliderCookingOptions.UseFastMidphase;
        }
    }

    private static void AddPhysicalColliders(
        Transform root,
        Transform cap,
        IReadOnlyList<MeshFilter> visualFilters
    )
    {
        var bodyFilters = visualFilters.Where(filter => !filter.transform.IsChildOf(cap)).ToArray();
        var lidFilters = visualFilters.Where(filter => filter.transform.IsChildOf(cap)).ToArray();
        if (bodyFilters.Length != 2 || lidFilters.Length != 2)
            throw new InvalidOperationException("Expected two body and two lid visual meshes.");

        var bodyBounds = BoundsInSpace(bodyFilters, root);
        var bodyPhysical = NewChild(root, "BodyPhysical", PhysicalLayer);
        AddShellBoxes(bodyPhysical, bodyBounds);

        var lidBounds = BoundsInSpace(lidFilters, cap);
        var lidPhysical = NewChild(cap, "LidPhysical", PhysicalLayer);
        var lidCollider = lidPhysical.AddComponent<BoxCollider>();
        lidCollider.center = lidBounds.center;
        lidCollider.size = InflateMinimum(lidBounds.size, 0.002f);
        lidCollider.isTrigger = false;
    }

    private static void AddShellBoxes(GameObject parent, Bounds bounds)
    {
        var size = bounds.size;
        var wall = Mathf.Max(0.008f, Mathf.Min(size.x, size.z) * 0.075f);
        var floor = Mathf.Max(0.008f, size.y * 0.12f);
        var sideX = new Vector3(wall, size.y, Mathf.Max(0.001f, size.z - wall * 2f));
        var sideZ = new Vector3(Mathf.Max(0.001f, size.x - wall * 2f), size.y, wall);

        AddBox(
            parent.transform,
            "Floor",
            new Vector3(bounds.center.x, bounds.min.y + floor * 0.5f, bounds.center.z),
            new Vector3(size.x, floor, size.z)
        );
        AddBox(
            parent.transform,
            "LeftWall",
            new Vector3(bounds.min.x + wall * 0.5f, bounds.center.y, bounds.center.z),
            sideX
        );
        AddBox(
            parent.transform,
            "RightWall",
            new Vector3(bounds.max.x - wall * 0.5f, bounds.center.y, bounds.center.z),
            sideX
        );
        AddBox(
            parent.transform,
            "FrontWall",
            new Vector3(bounds.center.x, bounds.center.y, bounds.max.z - wall * 0.5f),
            sideZ
        );
        AddBox(
            parent.transform,
            "RearWall",
            new Vector3(bounds.center.x, bounds.center.y, bounds.min.z + wall * 0.5f),
            sideZ
        );
    }

    private static void AddInteractionCollider(Transform cap, IReadOnlyList<MeshFilter> visualFilters)
    {
        var lidFilters = visualFilters.Where(filter => filter.transform.IsChildOf(cap)).ToArray();
        var bounds = BoundsInSpace(lidFilters, cap);
        var collider = cap.gameObject.AddComponent<BoxCollider>();
        collider.center = bounds.center;
        collider.size = InflateMinimum(bounds.size, 0.004f);
        collider.isTrigger = false;
    }

    private static AssetBundleManifest BuildBundle(string output)
    {
        var manifest = BuildPipeline.BuildAssetBundles(
            output,
            new[]
            {
                new AssetBundleBuild
                {
                    assetBundleName = BundleName,
                    assetNames = new[] { GeneratedPrefab },
                    addressableNames = new[] { PrefabAddress },
                },
            },
            BuildAssetBundleOptions.ChunkBasedCompression
                | BuildAssetBundleOptions.ForceRebuildAssetBundle,
            BuildTarget.StandaloneWindows64
        );
        return manifest;
    }

    private static void ValidateGeneratedPrefab(GameObject prefab, bool requireSourcePaths = true)
    {
        if (!prefab || prefab.name != "signal-case")
            throw new InvalidOperationException("Generated prefab root is not signal-case.");

        if (
            prefab.transform.localPosition != Vector3.zero
            || Quaternion.Angle(prefab.transform.localRotation, Quaternion.identity) > 0.001f
            || prefab.transform.localScale != IdentityScale
        )
            throw new InvalidOperationException("Generated prefab root is not identity.");

        var cap = FindUnique(prefab.transform, "cap");
        if (
            Vector3.Distance(cap.localPosition, LidPivot) > 0.00001f
            || Quaternion.Angle(cap.localRotation, Quaternion.identity) > 0.001f
        )
            throw new InvalidOperationException("Generated lid hinge pivot is not deterministic.");

        var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
        var renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
        if (filters.Length != 4 || renderers.Length != 4)
            throw new InvalidOperationException("Generated prefab lost visual geometry.");
        if (filters.Any(filter => !filter.sharedMesh || filter.sharedMesh.vertexCount == 0))
            throw new InvalidOperationException("Generated prefab contains an empty visual mesh.");

        var ballistic = prefab
            .GetComponentsInChildren<MeshCollider>(true)
            .Where(collider => collider.gameObject.name.EndsWith("_BALLISTIC", StringComparison.Ordinal))
            .ToArray();
        if (
            ballistic.Length != 4
            || ballistic.Any(
                collider =>
                    collider.gameObject.layer != BallisticLayer
                    || collider.convex
                    || collider.isTrigger
                    || !collider.sharedMesh
                    || collider.sharedMesh.vertexCount == 0
            )
        )
            throw new InvalidOperationException(
                "Generated prefab must contain four nonconvex *_BALLISTIC mesh colliders on layer 12."
            );

        var physical = prefab
            .GetComponentsInChildren<Collider>(true)
            .Where(collider => collider.gameObject.layer == PhysicalLayer)
            .ToArray();
        if (physical.Length == 0 || physical.Any(collider => collider.isTrigger))
            throw new InvalidOperationException("Generated physical colliders are missing/triggered.");

        var interaction = prefab
            .GetComponentsInChildren<BoxCollider>(true)
            .Where(collider => collider.gameObject.layer == InteractionLayer)
            .ToArray();
        if (interaction.Length != 1 || interaction[0].isTrigger)
            throw new InvalidOperationException("Generated lid interaction collider is missing/triggered.");

        if (prefab.GetComponentsInChildren<Rigidbody>(true).Length != 0)
            throw new InvalidOperationException("Generated visual prefab contains a Rigidbody.");

        var materials = renderers
            .SelectMany(renderer => renderer.sharedMaterials)
            .Where(material => material)
            .Distinct()
            .ToArray();
        if (
            materials.Length != 1
            || (
                requireSourcePaths
                && Normalize(AssetDatabase.GetAssetPath(materials[0])) != SourceMaterial
            )
        )
            throw new InvalidOperationException("Generated prefab does not use only the olive source material.");
        ValidateMaterial(materials[0], requireSourcePaths);

        if (requireSourcePaths)
        {
            var dependencies = AssetDatabase
                .GetDependencies(GeneratedPrefab, true)
                .Select(Normalize)
                .ToArray();
            var forbidden = dependencies
                .Where(path =>
                    path.IndexOf("ammo_box_02", StringComparison.OrdinalIgnoreCase) >= 0
                    || path.IndexOf("ammo_box_03", StringComparison.OrdinalIgnoreCase) >= 0
                    || path.EndsWith(
                        "prefabs/ammo_box_01.prefab",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .ToArray();
            if (forbidden.Length != 0)
                throw new InvalidOperationException(
                    "Generated prefab has unneeded source/demo dependencies: "
                        + string.Join(", ", forbidden)
                );
        }
    }

    private static void ValidateBundledPrefab(GameObject prefab)
    {
        ValidateGeneratedPrefab(prefab, false);
        var ballistic = prefab
            .GetComponentsInChildren<MeshCollider>(true)
            .Count(collider => collider.gameObject.name.EndsWith("_BALLISTIC", StringComparison.Ordinal));
        if (ballistic != 4)
            throw new InvalidOperationException("Bundled prefab lost one or more ballistic colliders.");
    }

    private static void WritePreviewsAndValidation(GameObject prefab, string output)
    {
        var instance = UnityEngine.Object.Instantiate(prefab);
        instance.name = "signal-case-preview";
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        var cameraObject = new GameObject("SignalsCasePreviewCamera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.045f, 0.048f, 1f);
        camera.fieldOfView = 35f;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 100f;

        var keyObject = new GameObject("SignalsCasePreviewKey");
        var key = keyObject.AddComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 1.25f;
        key.color = new Color(1f, 0.93f, 0.8f);
        key.transform.rotation = Quaternion.Euler(35f, -35f, 0f);

        var fillObject = new GameObject("SignalsCasePreviewFill");
        var fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.55f;
        fill.color = new Color(0.72f, 0.84f, 1f);
        fill.transform.rotation = Quaternion.Euler(25f, 145f, 0f);

        var closedBounds = WorldVisualBounds(instance);
        var openBounds = closedBounds;
        try
        {
            RenderPreview(instance, camera, closedBounds, 0f, output, "signal-case-closed.png");
            var cap = FindUnique(instance.transform, "cap");
            cap.localRotation = Quaternion.Euler(LidOpenAngleDegrees, 0f, 0f);
            openBounds = WorldVisualBounds(instance);
            ValidateOpenPose(instance, closedBounds, openBounds);
            RenderPreview(instance, camera, openBounds, LidOpenAngleDegrees, output, "signal-case-open.png");

            cap.localRotation = Quaternion.identity;
            var metadata = CreateMetadata(instance, closedBounds, openBounds);
            File.WriteAllText(
                Path.Combine(output, ValidationName),
                JsonUtility.ToJson(metadata, true) + Environment.NewLine
            );
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(fillObject);
            UnityEngine.Object.DestroyImmediate(keyObject);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static void ValidateOpenPose(GameObject instance, Bounds closed, Bounds opened)
    {
        if (Mathf.Abs(LidOpenAngleDegrees) < 100f || Mathf.Abs(LidOpenAngleDegrees) > 110f)
            throw new InvalidOperationException("Lid opening must be between 100 and 110 degrees.");

        var cap = FindUnique(instance.transform, "cap");
        if (cap.localRotation == Quaternion.identity)
            throw new InvalidOperationException("Open preview did not rotate the lid.");

        var closedCap = closed;
        if (opened.size.y <= closedCap.size.y * 0.95f)
            throw new InvalidOperationException("Open lid pose did not raise the case silhouette.");

        // The negative sign is part of the geometry contract: the front of the
        // lid (positive Z from the rear hinge) must move above the body.
        var front = cap.TransformPoint(Vector3.forward * 0.1f);
        var hinge = cap.position;
        if (front.y <= hinge.y)
            throw new InvalidOperationException("Lid opening sign sends the front into the body.");
    }

    private static void RenderPreview(
        GameObject instance,
        Camera camera,
        Bounds bounds,
        float angle,
        string output,
        string fileName
    )
    {
        camera.transform.position = bounds.center
            + new Vector3(bounds.size.x * 1.35f, bounds.size.y * 1.1f, bounds.size.z * 1.75f);
        camera.transform.LookAt(bounds.center + Vector3.up * bounds.size.y * 0.08f);
        camera.aspect = 4f / 3f;

        var target = new RenderTexture(800, 600, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 1,
            useMipMap = false,
            autoGenerateMips = false,
        };
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(output, fileName), image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(target);
        }
    }

    private static ValidationMetadata CreateMetadata(
        GameObject instance,
        Bounds closedBounds,
        Bounds openBounds
    )
    {
        var filters = instance.GetComponentsInChildren<MeshFilter>(true);
        var renderers = instance.GetComponentsInChildren<MeshRenderer>(true);
        var colliders = instance.GetComponentsInChildren<Collider>(true);
        var cap = FindUnique(instance.transform, "cap");
        var capFilters = filters.Where(filter => filter.transform.IsChildOf(cap)).ToArray();
        var bodyFilters = filters.Where(filter => !filter.transform.IsChildOf(cap)).ToArray();
        var capBounds = BoundsInSpace(capFilters, instance.transform);
        var bodyBounds = BoundsInSpace(bodyFilters, instance.transform);

        return new ValidationMetadata
        {
            unityVersion = Application.unityVersion,
            prefab = PrefabAddress,
            bundle = BundleName,
            sourcePrefab = SourcePrefab,
            sourcePrefabGuid = AssetDatabase.AssetPathToGUID(SourcePrefab),
            material = SourceMaterial,
            variant = "ammo_box_01 (olive)",
            lidOpenAngleDegrees = LidOpenAngleDegrees,
            capPivot = ToVector(cap.localPosition),
            closedBounds = ToBounds(closedBounds),
            openBounds = ToBounds(openBounds),
            bodyBounds = ToBounds(bodyBounds),
            lidBounds = ToBounds(capBounds),
            visualMeshCount = filters.Length,
            visualRendererCount = renderers.Length,
            ballisticColliderCount = colliders.Count(
                collider => collider.gameObject.name.EndsWith("_BALLISTIC", StringComparison.Ordinal)
            ),
            physicalColliderCount = colliders.Count(
                collider => collider.gameObject.layer == PhysicalLayer
            ),
            interactionColliderCount = colliders.Count(
                collider => collider.gameObject.layer == InteractionLayer
            ),
            visualVertexCount = filters.Sum(filter => filter.sharedMesh ? filter.sharedMesh.vertexCount : 0),
            closedPreview = "signal-case-closed.png",
            openPreview = "signal-case-open.png",
        };
    }

    private static Bounds WorldVisualBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new InvalidOperationException("Cannot compute preview bounds without renderers.");

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static Bounds BoundsInSpace(IEnumerable<MeshFilter> filters, Transform space)
    {
        var found = false;
        var result = new Bounds();
        foreach (var filter in filters)
        {
            if (!filter.sharedMesh)
                continue;
            var meshBounds = filter.sharedMesh.bounds;
            for (var x = -1; x <= 1; x += 2)
            for (var y = -1; y <= 1; y += 2)
            for (var z = -1; z <= 1; z += 2)
            {
                var corner = meshBounds.center
                    + Vector3.Scale(meshBounds.extents, new Vector3(x, y, z));
                var point = space.InverseTransformPoint(filter.transform.TransformPoint(corner));
                if (!found)
                {
                    result = new Bounds(point, Vector3.zero);
                    found = true;
                }
                else
                {
                    result.Encapsulate(point);
                }
            }
        }

        if (!found)
            throw new InvalidOperationException("Could not compute bounds for visual meshes.");
        return result;
    }

    private static GameObject NewChild(Transform parent, string name, int layer)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.layer = layer;
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = IdentityScale;
        return child;
    }

    private static BoxCollider AddBox(Transform parent, string name, Vector3 center, Vector3 size)
    {
        var child = NewChild(parent, name, PhysicalLayer);
        var collider = child.AddComponent<BoxCollider>();
        collider.center = center;
        collider.size = InflateMinimum(size, 0.001f);
        collider.isTrigger = false;
        return collider;
    }

    private static Vector3 InflateMinimum(Vector3 size, float minimum)
    {
        return new Vector3(
            Mathf.Max(Mathf.Abs(size.x), minimum),
            Mathf.Max(Mathf.Abs(size.y), minimum),
            Mathf.Max(Mathf.Abs(size.z), minimum)
        );
    }

    private static Transform FindUnique(Transform root, string name)
    {
        var matches = root
            .GetComponentsInChildren<Transform>(true)
            .Where(transform => transform.name == name)
            .ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException(
                "Expected one '" + name + "' transform; found " + matches.Length
            );
        return matches[0];
    }

    private static void CopyLocalTransform(Transform source, Transform destination)
    {
        destination.localPosition = source.localPosition;
        destination.localRotation = source.localRotation;
        destination.localScale = source.localScale;
    }

    private static BoundsMetadata ToBounds(Bounds bounds)
    {
        return new BoundsMetadata
        {
            min = ToVector(bounds.min),
            max = ToVector(bounds.max),
            center = ToVector(bounds.center),
            size = ToVector(bounds.size),
        };
    }

    private static VectorMetadata ToVector(Vector3 vector)
    {
        return new VectorMetadata { x = vector.x, y = vector.y, z = vector.z };
    }

    private static string ResolveOutputDirectory()
    {
        var args = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(args, "-signalsOutput");
        if (index >= 0)
        {
            if (index + 1 >= args.Length || string.IsNullOrWhiteSpace(args[index + 1]))
                throw new InvalidOperationException("-signalsOutput requires an output directory.");
            return Path.GetFullPath(args[index + 1]);
        }

        return Path.GetFullPath(Path.Combine("..", "Skills-Extended", "artifacts", "signals-case-visual"));
    }

    private static string AbsoluteProjectPath(string projectPath)
    {
        if (Path.IsPathRooted(projectPath))
            return projectPath;

        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        return projectRoot == null
            ? Path.GetFullPath(projectPath)
            : Path.GetFullPath(Path.Combine(projectRoot, projectPath));
    }

    private static string Normalize(string path) => path.Replace('\\', '/');

    [Serializable]
    private sealed class ValidationMetadata
    {
        public string unityVersion;
        public string prefab;
        public string bundle;
        public string sourcePrefab;
        public string sourcePrefabGuid;
        public string material;
        public string variant;
        public float lidOpenAngleDegrees;
        public VectorMetadata capPivot;
        public BoundsMetadata closedBounds;
        public BoundsMetadata openBounds;
        public BoundsMetadata bodyBounds;
        public BoundsMetadata lidBounds;
        public int visualMeshCount;
        public int visualRendererCount;
        public int ballisticColliderCount;
        public int physicalColliderCount;
        public int interactionColliderCount;
        public int visualVertexCount;
        public string closedPreview;
        public string openPreview;
    }

    [Serializable]
    private sealed class BoundsMetadata
    {
        public VectorMetadata min;
        public VectorMetadata max;
        public VectorMetadata center;
        public VectorMetadata size;
    }

    [Serializable]
    private sealed class VectorMetadata
    {
        public float x;
        public float y;
        public float z;
    }
}
