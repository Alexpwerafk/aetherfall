using System.Collections.Generic;
using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using TMPro;
using Aetherfall.Audio;
using Aetherfall.CameraRig;
using Aetherfall.Core;
using Aetherfall.Feel;
using Aetherfall.Gameplay;
using Aetherfall.UI;

public static class BuildAetherfall
{
    public static string BuildAll()
    {
        var heroImp = AssetImporter.GetAtPath("Assets/Models/Hero_Expeditioner.fbx") as ModelImporter;
        if (heroImp != null && heroImp.bakeAxisConversion)
        {
            heroImp.bakeAxisConversion = false;
            heroImp.SaveAndReimport();
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        EnsureDirectories();
        ImportTMPEssentialsIfNeeded();
        ConfigureQualityAndURP();

        Dictionary<string, Material> mats = CreatePaletteMaterials();
        Material skyMat = CreatePaletteSkyboxMaterial();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SetupLightingAndSky(skyMat);
        SetupGlobalVolume();

        // Build 3-Chamber Sunlit 3D Cartoon Sky-Sanctum
        GameObject envRoot = new GameObject("SanctumEnvironment");
        BuildSkySeaAndFloatingIslands(envRoot.transform, mats);
        BuildChamberFloor(envRoot.transform, "Chamber1_Courtyard", 0f, -8f, 8f, -8f, 8f, mats);
        BuildBridge(envRoot.transform, "Bridge1", 9f, 19f, mats, out GameObject barrier1, Palette.HeroCrimson);
        BuildChamberFloor(envRoot.transform, "Chamber2_FloodedHall", 28f, 20f, 36f, -8f, 8f, mats);
        BuildBridge(envRoot.transform, "Bridge2", 37f, 47f, mats, out GameObject barrier2, Palette.LightningCyan);
        BuildChamberFloor(envRoot.transform, "Chamber3_EclipseThrone", 56f, 48f, 64f, -8f, 8f, mats);

        // Pillars, Cartoon Trees, Crystal Clusters & Torch Braziers
        PlaceChamberProps(envRoot.transform, mats);

        // Hero
        HeroController hero = SpawnHero(mats);

        // Enemies across 3 Chambers
        SpawnChamberEnemies(mats);

        // Interactive Elemental Props, Barrels, Chests, Traps & Switches
        GameObject propsRoot = new GameObject("SanctumInteractables");
        SpawnInteractables(propsRoot.transform, mats);

        // Cameras (Cinemachine 3.x Dual Rig)
        SetupCameras(hero.transform, out CinemachineCamera titleCam, out CinemachineCamera isoCam, out CinemachineCamera duelCam);

        // Core Managers
        GameObject managers = new GameObject("AetherfallManagers");
        GameLoop loop = managers.AddComponent<GameLoop>();
        managers.AddComponent<SynthAudioManager>();
        VfxPoolManager vfx = managers.AddComponent<VfxPoolManager>();
        managers.AddComponent<FeelManager>();
        managers.AddComponent<ElementalSurfaceManager>();
        managers.AddComponent<ExpeditionQTEController>();
        DungeonDirector director = managers.AddComponent<DungeonDirector>();
        director.Bridge1Barrier = barrier1;
        director.Bridge2Barrier = barrier2;

        TacticalCameraDirector camDir = managers.AddComponent<TacticalCameraDirector>();
        camDir.TitleOrbitCam = titleCam;
        camDir.IsoTacticalCam = isoCam;
        camDir.DuelActionCam = duelCam;
        camDir.OrbitCenter = hero.transform;
        camDir.HeroTarget = hero.transform;

        managers.AddComponent<GameShellUI>();

        // EventSystem for Input System UI
        GameObject esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<InputSystemUIInputModule>();

        // Save Scene & Build Settings
        string scenePath = "Assets/Scenes/AetherfallSanctum.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(scenePath, true)
        };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return $"Built Aetherfall Sanctum scene at '{scenePath}' with {mats.Count} palette materials, 3 chambers, Hero, 6 enemies (incl. Boss), Cinemachine 3.x dual rig, and URP ACES/Bloom Volume.";
    }

    static void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            AssetDatabase.CreateFolder("Assets", "Settings");
    }

    static void ImportTMPEssentialsIfNeeded()
    {
        if (!AssetDatabase.IsValidFolder("Assets/TextMesh Pro") || TMP_Settings.instance == null)
        {
            TMP_PackageResourceImporter.ImportResources(true, false, false);
            AssetDatabase.Refresh();
        }
    }

    static void ConfigureQualityAndURP()
    {
        string[] names = QualitySettings.names;
        int pcIndex = System.Array.IndexOf(names, "PC");
        if (pcIndex < 0) pcIndex = names.Length - 1;
        QualitySettings.SetQualityLevel(pcIndex, true);

        UniversalRenderPipelineAsset urp = UniversalRenderPipeline.asset;
        if (urp != null)
        {
            urp.supportsHDR = true;
            urp.shadowDistance = 55f;
            EditorUtility.SetDirty(urp);
        }
    }

    static Dictionary<string, Material> CreatePaletteMaterials()
    {
        var specs = new (string name, Color baseCol, Color? emit, float pulse, float rim, Color? rimCol)[]
        {
            ("Mat_ObsidianFloor", Palette.ObsidianFloor, null, 0f, 0.12f, Palette.MarbleTrim),
            ("Mat_MarbleTrim", Palette.MarbleTrim, null, 0f, 0.22f, Palette.LightningCyan),
            ("Mat_ObsidianArmor", Palette.ObsidianArmor, null, 0f, 0.26f, Palette.LightningCyan),
            ("Mat_HeroCrimson", Palette.HeroCrimson, null, 0f, 0.30f, Palette.FiligreeGold),
            ("Mat_FiligreeGold", Palette.FiligreeGold, Palette.FiligreeGold * 0.28f, 0f, 0.35f, Palette.IvoryCloth),
            ("Mat_IvoryCloth", Palette.IvoryCloth, null, 0f, 0.22f, Palette.FiligreeGold),
            ("Mat_EnemyVanguard", Palette.EnemyVanguard, null, 0f, 0.28f, Palette.FireAmber),
            ("Mat_EnemyAlchemist", Palette.EnemyAlchemist, Palette.EnemyAlchemist * 0.2f, 0f, 0.35f, Palette.LightningCyan),
            ("Mat_BossEclipse", Palette.BossEclipse, Palette.BossEclipse * 0.32f, 2.0f, 0.45f, Palette.FireAmber),
            ("Mat_FireAmber", Palette.FireAmber, Palette.FireAmber * 2.1f, 5.0f, 0.4f, Palette.FiligreeGold),
            ("Mat_ArcaneCyan", Palette.LightningCyan, Palette.LightningCyan * 2.0f, 3.8f, 0.45f, Palette.FrostIce),
            ("Mat_FrostIce", Palette.FrostIce, Palette.FrostIce * 1.4f, 2.5f, 0.4f, Palette.WaterAzure),
            ("Mat_OilDark", Palette.OilDark, null, 0f, 0.18f, Palette.MarbleTrim),
            ("Mat_Surface_Oil", Palette.OilDark, Palette.OilDark * 0.12f, 0f, 0.35f, Palette.MarbleTrim),
            ("Mat_Surface_Water", Palette.WaterAzure, Palette.WaterAzure * 0.45f, 1.8f, 0.4f, Palette.FrostIce),
            ("Mat_GrassLush", Palette.GrassLush, null, 0f, 0.20f, Palette.GrassLight),
            ("Mat_GrassLight", Palette.GrassLight, null, 0f, 0.22f, Palette.IvoryCloth),
            ("Mat_TreeTrunk", Palette.TreeTrunk, null, 0f, 0.15f, Palette.MarbleTrim),
            ("Mat_CliffTerracotta", Palette.CliffTerracotta, null, 0f, 0.16f, Palette.MarbleTrim),
            ("Mat_CrystalMagenta", Palette.CrystalMagenta, Palette.CrystalMagenta * 0.85f, 3.2f, 0.45f, Palette.IvoryCloth),
            ("Mat_RoyalRoofBlue", Palette.RoyalRoofBlue, null, 0f, 0.25f, Palette.LightningCyan),
            ("Mat_SkySeaTurquoise", Palette.SkySeaTurquoise, Palette.SkySeaTurquoise * 0.25f, 1.2f, 0.1f, Palette.IvoryCloth),
            ("Mat_CloudWhite", Palette.IvoryCloth, Palette.IvoryCloth * 0.15f, 0f, 0.2f, Palette.DeepVoidSky),
            ("Mat_SandTileAlt", Color.Lerp(Palette.ObsidianFloor, Palette.MarbleTrim, 0.55f), null, 0f, 0.14f, Palette.FiligreeGold)
        };

        var dict = new Dictionary<string, Material>();
        foreach (var s in specs)
        {
            string path = $"Assets/Materials/{s.name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = Palette.CreateToonMaterial(s.name, s.baseCol, s.emit, s.pulse, s.rim, s.rimCol);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = Palette.GetToonShader();
                mat.SetColor("_BaseColor", s.baseCol);
                if (mat.HasProperty("_ShadowTint")) mat.SetColor("_ShadowTint", Palette.AbyssShadow);
                if (mat.HasProperty("_RimColor")) mat.SetColor("_RimColor", s.rimCol ?? Palette.LightningCyan);
                if (mat.HasProperty("_RimStrength")) mat.SetFloat("_RimStrength", s.rim);
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", s.emit ?? Color.black);
                if (mat.HasProperty("_PulseSpeed")) mat.SetFloat("_PulseSpeed", s.pulse);
                EditorUtility.SetDirty(mat);
            }
            dict[s.name] = mat;
        }
        AssetDatabase.SaveAssets();
        return dict;
    }

    static Material CreatePaletteSkyboxMaterial()
    {
        string path = "Assets/Materials/Mat_SanctumSky.mat";
        Material sky = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader proc = Shader.Find("Skybox/Procedural");
        if (proc == null) proc = Palette.GetToonShader();

        if (sky == null)
        {
            sky = new Material(proc) { name = "Mat_SanctumSky" };
            AssetDatabase.CreateAsset(sky, path);
        }

        if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", Palette.DeepVoidSky);
        if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", Palette.SkySeaTurquoise);
        if (sky.HasProperty("_AtmosphereThickness")) sky.SetFloat("_AtmosphereThickness", 0.85f);
        if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 1.45f);
        if (sky.HasProperty("_SunSize")) sky.SetFloat("_SunSize", 0.04f);
        if (sky.HasProperty("_BaseColor")) sky.SetColor("_BaseColor", Palette.DeepVoidSky);
        EditorUtility.SetDirty(sky);
        AssetDatabase.SaveAssets();
        return sky;
    }

    static void SetupLightingAndSky(Material skyMat)
    {
        RenderSettings.skybox = skyMat;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Palette.AmbientSky;
        RenderSettings.ambientEquatorColor = Palette.AmbientEquator;
        RenderSettings.ambientGroundColor = Palette.AmbientGround;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = Palette.DeepVoidSky;
        RenderSettings.fogDensity = 0.0032f;

        GameObject lightGo = new GameObject("Directional Key Light");
        Light key = lightGo.AddComponent<Light>();
        key.type = LightType.Directional;
        key.color = Palette.KeyLightWarm;
        key.intensity = 1.85f;
        key.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(46f, -35f, 0f);

        GameObject rimGo = new GameObject("Sky Bounce Fill Light");
        Light rim = rimGo.AddComponent<Light>();
        rim.type = LightType.Directional;
        rim.color = Palette.RimLightCool;
        rim.intensity = 0.72f;
        rim.shadows = LightShadows.None;
        rimGo.transform.rotation = Quaternion.Euler(30f, 145f, 0f);
    }

    static void SetupGlobalVolume()
    {
        string profilePath = "Assets/Settings/AetherfallVolumeProfile.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);
        }
        profile.components.Clear();

        Tonemapping tone = profile.Add<Tonemapping>(true);
        tone.mode.overrideState = true;
        tone.mode.value = TonemappingMode.Neutral;

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.threshold.overrideState = true;
        bloom.threshold.value = 1.15f;
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 0.34f;
        bloom.scatter.overrideState = true;
        bloom.scatter.value = 0.60f;

        Vignette vig = profile.Add<Vignette>(true);
        vig.intensity.overrideState = true;
        vig.intensity.value = 0.14f;
        vig.smoothness.overrideState = true;
        vig.smoothness.value = 0.45f;
        vig.color.overrideState = true;
        vig.color.value = Palette.AbyssShadow;

        ColorAdjustments colorAdj = profile.Add<ColorAdjustments>(true);
        colorAdj.postExposure.overrideState = true;
        colorAdj.postExposure.value = 0.22f;
        colorAdj.saturation.overrideState = true;
        colorAdj.saturation.value = 22f;
        colorAdj.contrast.overrideState = true;
        colorAdj.contrast.value = 10f;

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        GameObject volGo = new GameObject("Global Volume");
        Volume vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = profile;
    }

    static GameObject InstantiateFbx(string fbxName, Vector3 pos, Quaternion rot, Transform parent, Dictionary<string, Material> mats)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Models/{fbxName}");
        if (asset == null)
        {
            Debug.LogError($"[BuildAetherfall] Missing FBX model: Assets/Models/{fbxName}");
            return new GameObject(fbxName);
        }
        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        inst.transform.SetParent(parent, false);
        inst.transform.position = pos;
        inst.transform.rotation = rot;
        ApplyPaletteMaterials(inst, mats);
        return inst;
    }

    static void ApplyPaletteMaterials(GameObject root, Dictionary<string, Material> mats)
    {
        Material fallback = mats["Mat_MarbleTrim"];
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
        {
            Material[] current = r.sharedMaterials;
            Material[] next = new Material[current.Length];
            for (int i = 0; i < current.Length; i++)
            {
                string mName = current[i] != null ? current[i].name : "";
                Material resolved = fallback;
                foreach (var kv in mats)
                {
                    if (mName.Contains(kv.Key))
                    {
                        resolved = kv.Value;
                        break;
                    }
                }
                next[i] = resolved;
            }
            r.sharedMaterials = next;
        }
    }

    static void BuildSkySeaAndFloatingIslands(Transform parent, Dictionary<string, Material> mats)
    {
        // Bright Turquoise Sky-Sea far below the floating citadel
        GameObject skySea = GameObject.CreatePrimitive(PrimitiveType.Plane);
        skySea.name = "CartoonSkySea";
        skySea.transform.SetParent(parent, false);
        skySea.transform.position = new Vector3(0f, -7.8f, 28f);
        skySea.transform.localScale = new Vector3(32f, 1f, 32f);
        Object.DestroyImmediate(skySea.GetComponent<Collider>());
        skySea.GetComponent<Renderer>().sharedMaterial = mats["Mat_SkySeaTurquoise"];

        // Decorative Flanking Sky-Islands with Trees & Crystals
        var islands = new (Vector3 pos, float scale, bool hasTree, bool hasPillar)[]
        {
            (new Vector3(-16.5f, -0.8f, -4.0f), 7.5f, true, false),
            (new Vector3(17.0f, 0.4f, 4.5f), 8.2f, true, true),
            (new Vector3(-18.0f, 1.2f, 14.5f), 6.5f, false, true),
            (new Vector3(16.5f, -0.5f, 24.0f), 8.0f, true, false),
            (new Vector3(-17.0f, 0.8f, 33.0f), 7.5f, true, true),
            (new Vector3(18.5f, 1.4f, 44.0f), 6.8f, true, false),
            (new Vector3(-17.5f, 0.5f, 54.0f), 8.5f, true, true),
            (new Vector3(17.5f, 1.0f, 60.0f), 8.0f, true, false)
        };

        foreach (var isl in islands)
        {
            GameObject islandBase = InstantiateFbx("Env_FloatingIslandBase.fbx", isl.pos, Quaternion.Euler(0f, isl.pos.z * 7f, 0f), parent, mats);
            islandBase.transform.localScale = new Vector3(isl.scale, isl.scale * 0.55f, isl.scale);

            if (isl.hasTree)
            {
                GameObject tree = InstantiateFbx("Env_CartoonTree.fbx", isl.pos + new Vector3(-0.8f, 0.05f, 0.4f), Quaternion.Euler(0f, isl.pos.x * 13f, 0f), parent, mats);
                tree.transform.localScale = Vector3.one * 1.15f;
            }
            if (isl.hasPillar)
            {
                InstantiateFbx("Env_RuinedPillar.fbx", isl.pos + new Vector3(1.1f, 0.05f, -0.6f), Quaternion.identity, parent, mats);
            }
            InstantiateFbx("Env_CrystalCluster.fbx", isl.pos + new Vector3(0.4f, 0.05f, 1.0f), Quaternion.Euler(0f, isl.pos.z * 11f, 0f), parent, mats);
        }

        // Puffy Cartoon Clouds floating beneath the islands
        Vector3[] cloudSpots =
        {
            new Vector3(-11f, -4.4f, -10f), new Vector3(12f, -4.8f, -2f),
            new Vector3(-13f, -4.2f, 12f), new Vector3(13f, -4.5f, 20f),
            new Vector3(-12f, -4.6f, 38f), new Vector3(12f, -4.2f, 50f),
            new Vector3(0f, -5.0f, 70f)
        };
        foreach (Vector3 cPos in cloudSpots)
        {
            GameObject cloudRoot = new GameObject("CartoonCloud");
            cloudRoot.transform.SetParent(parent, false);
            cloudRoot.transform.position = cPos;
            for (int p = 0; p < 3; p++)
            {
                GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                puff.name = $"CloudPuff_{p}";
                puff.transform.SetParent(cloudRoot.transform, false);
                puff.transform.localPosition = new Vector3((p - 1) * 1.6f, p == 1 ? 0.35f : 0f, (p % 2) * 0.6f);
                puff.transform.localScale = new Vector3(3.0f, 1.6f, 2.5f);
                Object.DestroyImmediate(puff.GetComponent<Collider>());
                puff.GetComponent<Renderer>().sharedMaterial = mats["Mat_CloudWhite"];
            }
        }
    }

    static void BuildChamberFloor(Transform parent, string name, float centerZ, float minZ, float maxZ, float minX, float maxX, Dictionary<string, Material> mats)
    {
        GameObject chamber = new GameObject(name);
        chamber.transform.SetParent(parent, false);

        // Lush Grassy Island Skirt + Cliff Base underneath chamber
        GameObject island = InstantiateFbx("Env_FloatingIslandBase.fbx", new Vector3(0f, -0.08f, centerZ), Quaternion.identity, chamber.transform, mats);
        island.transform.localScale = new Vector3(20.5f, 4.0f, 20.5f);

        int ix = 0;
        for (float x = minX; x <= maxX; x += 2f)
        {
            int iz = 0;
            for (float z = minZ; z <= maxZ; z += 2f)
            {
                GameObject tile = InstantiateFbx("Env_SanctumTile.fbx", new Vector3(x, 0f, z), Quaternion.identity, chamber.transform, mats);
                bool isCenterCarpet = Mathf.Abs(x) < 0.5f;
                bool checkerAlt = ((ix + iz) % 2) == 1;
                Renderer stoneRend = FindChildRecursive(tile.transform, "StoneBase")?.GetComponent<Renderer>();
                if (stoneRend != null)
                {
                    if (isCenterCarpet)
                        stoneRend.sharedMaterial = mats["Mat_HeroCrimson"];
                    else if (checkerAlt)
                        stoneRend.sharedMaterial = mats["Mat_SandTileAlt"];
                }
                iz++;
            }
            ix++;
        }

        // Solid floor collider for smooth raycast & footing
        GameObject colGo = new GameObject($"{name}_FloorCollider");
        colGo.transform.SetParent(chamber.transform, false);
        colGo.transform.position = new Vector3(0f, -0.2f, centerZ);
        BoxCollider bc = colGo.AddComponent<BoxCollider>();
        bc.size = new Vector3((maxX - minX) + 2f, 0.4f, (maxZ - minZ) + 2f);
    }

    static void BuildBridge(Transform parent, string name, float minZ, float maxZ, Dictionary<string, Material> mats, out GameObject barrierOut, Color barrierColor)
    {
        GameObject bridge = new GameObject(name);
        bridge.transform.SetParent(parent, false);

        for (float x = -1f; x <= 1f; x += 2f)
        {
            for (float z = minZ; z <= maxZ; z += 2f)
            {
                InstantiateFbx("Env_SanctumTile.fbx", new Vector3(x, 0f, z), Quaternion.identity, bridge.transform, mats);
            }
        }

        // Ornate Cartoon Castle Archway at bridge entrance
        InstantiateFbx("Env_CastleArchway.fbx", new Vector3(0f, 0f, minZ + 0.4f), Quaternion.identity, bridge.transform, mats);

        GameObject colGo = new GameObject($"{name}_Collider");
        colGo.transform.SetParent(bridge.transform, false);
        float midZ = (minZ + maxZ) * 0.5f;
        colGo.transform.position = new Vector3(0f, -0.2f, midZ);
        BoxCollider bc = colGo.AddComponent<BoxCollider>();
        bc.size = new Vector3(4.2f, 0.4f, (maxZ - minZ) + 2f);

        // Energy Seal Barrier across bridge entrance
        barrierOut = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barrierOut.name = $"{name}_Barrier";
        barrierOut.transform.SetParent(bridge.transform, false);
        barrierOut.transform.position = new Vector3(0f, 1.05f, minZ + 0.5f);
        barrierOut.transform.localScale = new Vector3(4.0f, 0.42f, 0.22f);
        string bMatPath = $"Assets/Materials/{name}_BarrierMat.mat";
        Material bMat = AssetDatabase.LoadAssetAtPath<Material>(bMatPath);
        if (bMat == null)
        {
            bMat = Palette.CreateToonMaterial($"{name}_BarrierMat", barrierColor, barrierColor * 0.95f, 3.5f);
            AssetDatabase.CreateAsset(bMat, bMatPath);
        }
        else
        {
            bMat.SetColor("_BaseColor", barrierColor);
            if (bMat.HasProperty("_EmissionColor")) bMat.SetColor("_EmissionColor", barrierColor * 0.95f);
            EditorUtility.SetDirty(bMat);
        }
        barrierOut.GetComponent<Renderer>().sharedMaterial = bMat;
    }

    static void PlaceChamberProps(Transform parent, Dictionary<string, Material> mats)
    {
        float[] zCenters = { 0f, 28f, 56f };
        foreach (float zc in zCenters)
        {
            Vector3[] pillarOffsets =
            {
                new Vector3(-7.8f, 0f, zc - 7.8f),
                new Vector3(7.8f, 0f, zc - 7.8f),
                new Vector3(-7.8f, 0f, zc + 7.8f),
                new Vector3(7.8f, 0f, zc + 7.8f)
            };
            foreach (Vector3 p in pillarOffsets)
            {
                GameObject pil = InstantiateFbx("Env_RuinedPillar.fbx", p, Quaternion.identity, parent, mats);
                BoxCollider bc = pil.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, 1.8f, 0f);
                bc.size = new Vector3(1.2f, 3.6f, 1.2f);
            }

            // Lush Cartoon Trees & Glowing Crystal Clusters on chamber corners
            Vector3[] treeOffsets =
            {
                new Vector3(-9.2f, 0f, zc - 5.2f),
                new Vector3(9.2f, 0f, zc + 5.2f)
            };
            foreach (Vector3 tPos in treeOffsets)
            {
                GameObject tree = InstantiateFbx("Env_CartoonTree.fbx", tPos, Quaternion.Euler(0f, tPos.z * 9f, 0f), parent, mats);
                tree.transform.localScale = Vector3.one * 1.1f;
            }

            Vector3[] crystalOffsets =
            {
                new Vector3(8.9f, 0f, zc - 5.5f),
                new Vector3(-8.9f, 0f, zc + 5.5f)
            };
            foreach (Vector3 cPos in crystalOffsets)
            {
                InstantiateFbx("Env_CrystalCluster.fbx", cPos, Quaternion.Euler(0f, cPos.x * 11f, 0f), parent, mats);
            }

            Vector3[] brazierOffsets =
            {
                new Vector3(-6.0f, 0f, zc - 6.0f),
                new Vector3(6.0f, 0f, zc - 6.0f),
                new Vector3(-6.0f, 0f, zc + 6.0f),
                new Vector3(6.0f, 0f, zc + 6.0f)
            };
            foreach (Vector3 b in brazierOffsets)
            {
                GameObject brazier = InstantiateFbx("Env_ArcaneBrazier.fbx", b, Quaternion.identity, parent, mats);
                GameObject lightGo = new GameObject("BrazierPointLight");
                lightGo.transform.SetParent(brazier.transform, false);
                lightGo.transform.localPosition = new Vector3(0f, 1.45f, 0f);
                Light pt = lightGo.AddComponent<Light>();
                pt.type = LightType.Point;
                pt.color = Palette.FireAmber;
                pt.intensity = 6.0f;
                pt.range = 9.0f;
                pt.shadows = LightShadows.None;
            }
        }
    }

    static HeroController SpawnHero(Dictionary<string, Material> mats)
    {
        GameObject heroGo = InstantiateFbx("Hero_Expeditioner.fbx", new Vector3(0f, 0f, -5.5f), Quaternion.identity, null, mats);
        heroGo.name = "Hero_Expeditioner";

        CapsuleCollider col = heroGo.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 0.95f, 0f);
        col.radius = 0.45f;
        col.height = 1.9f;

        Rigidbody rb = heroGo.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        heroGo.AddComponent<ProceduralCharacterAnimator>();
        HeroController hero = heroGo.AddComponent<HeroController>();

        Transform mount = FindChildRecursive(heroGo.transform, "WeaponMount");
        if (mount == null)
        {
            GameObject mGo = new GameObject("WeaponMount");
            mGo.transform.SetParent(heroGo.transform, false);
            mGo.transform.localPosition = new Vector3(0.38f, 0.95f, 0.25f);
            mount = mGo.transform;
        }
        hero.WeaponMount = mount;

        Quaternion wRot = Quaternion.Euler(28f, 0f, 0f);
        hero.SolarGreatswordModel = InstantiateFbx("Weapon_SolarGreatsword.fbx", Vector3.zero, wRot, mount, mats);
        hero.SolarGreatswordModel.transform.localPosition = Vector3.zero;
        hero.SolarGreatswordModel.transform.localScale = Vector3.one * 0.85f;

        hero.StormHalberdModel = InstantiateFbx("Weapon_StormHalberd.fbx", Vector3.zero, wRot, mount, mats);
        hero.StormHalberdModel.transform.localPosition = Vector3.zero;
        hero.StormHalberdModel.transform.localScale = Vector3.one * 0.85f;

        hero.FrostRunebladeModel = InstantiateFbx("Weapon_FrostRuneblade.fbx", Vector3.zero, wRot, mount, mats);
        hero.FrostRunebladeModel.transform.localPosition = Vector3.zero;
        hero.FrostRunebladeModel.transform.localScale = Vector3.one * 0.85f;

        return hero;
    }

    static void SpawnChamberEnemies(Dictionary<string, Material> mats)
    {
        // Chamber 1: 2 Corrupted Vanguards (one near Abyss edge, one near Oil Barrel)
        SpawnEnemy("Enemy_CorruptedVanguard.fbx", "Corrupted Vanguard", EnemyArchetype.CorruptedVanguard, 1, new Vector3(-3.2f, 0f, 2.5f), 55, 16, 2, 45, mats);
        SpawnEnemy("Enemy_CorruptedVanguard.fbx", "Ledge Sentinel", EnemyArchetype.CorruptedVanguard, 1, new Vector3(5.8f, 0f, 3.0f), 55, 16, 2, 45, mats);

        // Chamber 2: 2 Arcane Alchemists + 1 Vanguard near Water canals
        SpawnEnemy("Enemy_CorruptedVanguard.fbx", "Sanctum Warden", EnemyArchetype.CorruptedVanguard, 2, new Vector3(0f, 0f, 25.5f), 75, 18, 3, 55, mats);
        SpawnEnemy("Enemy_ArcaneAlchemist.fbx", "Arcane Alchemist", EnemyArchetype.ArcaneAlchemist, 2, new Vector3(-4.2f, 0f, 30.5f), 60, 20, 2, 55, mats);
        SpawnEnemy("Enemy_ArcaneAlchemist.fbx", "Storm Alchemist", EnemyArchetype.ArcaneAlchemist, 2, new Vector3(4.2f, 0f, 30.5f), 60, 20, 2, 55, mats);

        // Chamber 3 (Boss): Chrono-Inquisitor Malakor + Guard
        SpawnEnemy("Enemy_ChronoInquisitorBoss.fbx", "Chrono-Inquisitor Malakor", EnemyArchetype.ChronoInquisitorBoss, 3, new Vector3(0f, 0f, 58.0f), 165, 24, 4, 150, mats);
        SpawnEnemy("Enemy_CorruptedVanguard.fbx", "Eclipse Guard", EnemyArchetype.CorruptedVanguard, 3, new Vector3(-4.5f, 0f, 54.0f), 70, 18, 2, 50, mats);
    }

    static void SpawnEnemy(string fbx, string displayName, EnemyArchetype type, int chamber, Vector3 pos, int hp, int dmg, int posture, int xp, Dictionary<string, Material> mats)
    {
        GameObject go = InstantiateFbx(fbx, pos, Quaternion.Euler(0f, 180f, 0f), null, mats);
        go.name = $"{displayName}_Ch{chamber}";

        CapsuleCollider col = go.AddComponent<CapsuleCollider>();
        bool isBoss = type == EnemyArchetype.ChronoInquisitorBoss;
        col.center = new Vector3(0f, isBoss ? 1.45f : 0.95f, 0f);
        col.radius = isBoss ? 0.85f : 0.58f;
        col.height = isBoss ? 2.9f : 1.95f;

        go.AddComponent<ProceduralCharacterAnimator>();
        EnemyAI ai = go.AddComponent<EnemyAI>();
        ai.Archetype = type;
        ai.DisplayName = displayName;
        ai.ChamberIndex = chamber;
        ai.MaxHP = hp;
        ai.CurrentHP = hp;
        ai.AttackDamage = dmg;
        ai.MaxPosture = posture;
        ai.CurrentPosture = posture;
        ai.XPReward = xp;
    }

    static void SpawnInteractables(Transform parent, Dictionary<string, Material> mats)
    {
        // Chamber 1: Oil puddle + Explosive Barrel + Golden Chest (Storm Halberd)
        SpawnSurface(parent, new Vector3(-3.0f, 0.03f, 2.2f), SurfaceType.Oil, 2.8f, 6, mats);
        SpawnBarrel(parent, new Vector3(-1.8f, 0f, 3.6f), mats);
        SpawnBarrel(parent, new Vector3(3.4f, 0f, 1.2f), mats);
        SpawnChest(parent, new Vector3(-6.2f, 0f, -2.5f), WeaponType.StormHalberd, "Stormcaller Halberd [Lightning]", mats);

        // Chamber 2: Water puddles + Spike Traps + 2 Rune Crystal Switches + Frost Chest
        SpawnSurface(parent, new Vector3(-4.0f, 0.03f, 30.0f), SurfaceType.Water, 3.0f, 8, mats);
        SpawnSurface(parent, new Vector3(4.0f, 0.03f, 30.0f), SurfaceType.Water, 3.0f, 8, mats);
        SpawnTrap(parent, new Vector3(-2.5f, 0f, 26.5f), mats);
        SpawnTrap(parent, new Vector3(2.5f, 0f, 26.5f), mats);
        SpawnCrystalSwitch(parent, new Vector3(-6.2f, 0f, 34.0f), 0, mats);
        SpawnCrystalSwitch(parent, new Vector3(6.2f, 0f, 34.0f), 1, mats);
        SpawnChest(parent, new Vector3(6.4f, 0f, 23.0f), WeaponType.FrostRuneblade, "Frost Runeblade [Frost]", mats);

        // Chamber 3 (Boss Arena): Barrels + Oil & Water surfaces
        SpawnSurface(parent, new Vector3(-4.0f, 0.03f, 55.5f), SurfaceType.Oil, 2.8f, 8, mats);
        SpawnSurface(parent, new Vector3(4.0f, 0.03f, 55.5f), SurfaceType.Water, 2.8f, 8, mats);
        SpawnBarrel(parent, new Vector3(-3.2f, 0f, 56.5f), mats);
        SpawnBarrel(parent, new Vector3(3.6f, 0f, 57.0f), mats);
    }

    static void SpawnSurface(Transform parent, Vector3 pos, SurfaceType type, float radius, int turns, Dictionary<string, Material> mats)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = $"PreplacedSurface_{type}";
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        string matKey = type == SurfaceType.Oil ? "Mat_Surface_Oil" : "Mat_Surface_Water";
        go.GetComponent<Renderer>().sharedMaterial = mats[matKey];
        ElementalSurface surf = go.AddComponent<ElementalSurface>();
        surf.Init(type, radius, turns);
    }

    static void SpawnBarrel(Transform parent, Vector3 pos, Dictionary<string, Material> mats)
    {
        GameObject go = InstantiateFbx("Env_ExplosiveBarrel.fbx", pos, Quaternion.identity, parent, mats);
        CapsuleCollider col = go.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 0.55f, 0f);
        col.radius = 0.52f;
        col.height = 1.15f;
        go.AddComponent<ExplosiveBarrel>();
    }

    static void SpawnChest(Transform parent, Vector3 pos, WeaponType weapon, string title, Dictionary<string, Material> mats)
    {
        GameObject go = InstantiateFbx("Env_GoldenChest.fbx", pos, Quaternion.Euler(0f, 35f, 0f), parent, mats);
        BoxCollider col = go.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.45f, 0f);
        col.size = new Vector3(1.25f, 0.9f, 0.9f);
        GoldenChest chest = go.AddComponent<GoldenChest>();
        chest.RewardWeapon = weapon;
        chest.WeaponTitle = title;
        chest.LidTransform = FindChildRecursive(go.transform, "ChestLid");
    }

    static void SpawnCrystalSwitch(Transform parent, Vector3 pos, int idx, Dictionary<string, Material> mats)
    {
        GameObject go = InstantiateFbx("Env_RuneCrystalSwitch.fbx", pos, Quaternion.identity, parent, mats);
        CapsuleCollider col = go.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 0.95f, 0f);
        col.radius = 0.55f;
        col.height = 1.9f;
        RuneCrystalSwitch sw = go.AddComponent<RuneCrystalSwitch>();
        sw.SwitchIndex = idx;
        sw.CrystalVisual = FindChildRecursive(go.transform, "CrystalSpin");
    }

    static void SpawnTrap(Transform parent, Vector3 pos, Dictionary<string, Material> mats)
    {
        GameObject go = InstantiateFbx("Env_SpikeTrap.fbx", pos, Quaternion.identity, parent, mats);
        SpikeTrap trap = go.AddComponent<SpikeTrap>();
        trap.SpikesTransform = FindChildRecursive(go.transform, "Spikes");
    }

    static void SetupCameras(Transform heroTransform, out CinemachineCamera titleCam, out CinemachineCamera isoCam, out CinemachineCamera duelCam)
    {
        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 48f;
        cam.nearClipPlane = 0.2f;
        cam.farClipPlane = 180f;
        camGo.transform.position = heroTransform.position + new Vector3(-7.5f, 10.5f, -7.5f);
        camGo.transform.LookAt(heroTransform.position + Vector3.up * 0.8f);

        camGo.AddComponent<AudioListener>();
        UniversalAdditionalCameraData camData = camGo.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;
        camData.volumeLayerMask = -1;

        CinemachineBrain brain = camGo.AddComponent<CinemachineBrain>();
        brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.25f);
        camGo.AddComponent<CinemachineImpulseListener>();

        // 1. Title Orbit Camera
        GameObject tGo = new GameObject("TitleOrbitCam");
        titleCam = tGo.AddComponent<CinemachineCamera>();
        titleCam.Priority = 40;
        titleCam.Lens.FieldOfView = 48f;

        // 2. Isometric Tactical Camera
        GameObject isoGo = new GameObject("IsoTacticalCam");
        isoCam = isoGo.AddComponent<CinemachineCamera>();
        isoCam.Priority = 25;
        isoCam.Lens.FieldOfView = 46f;
        CinemachineFollow isoFollow = isoGo.AddComponent<CinemachineFollow>();
        isoFollow.FollowOffset = new Vector3(-7.5f, 10.8f, -7.5f);
        isoFollow.TrackerSettings.BindingMode = Unity.Cinemachine.TargetTracking.BindingMode.WorldSpace;
        isoFollow.TrackerSettings.PositionDamping = new Vector3(0.45f, 0.45f, 0.45f);
        CinemachineRotationComposer isoAim = isoGo.AddComponent<CinemachineRotationComposer>();
        isoAim.CenterOnActivate = true;

        // 3. Duel Action Camera (Expedition 33 Over-the-shoulder QTE/Parry Framing)
        GameObject duelGo = new GameObject("DuelActionCam");
        duelCam = duelGo.AddComponent<CinemachineCamera>();
        duelCam.Priority = 10;
        duelCam.Lens.FieldOfView = 45f;
    }

    static Transform FindChildRecursive(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform res = FindChildRecursive(root.GetChild(i), name);
            if (res != null) return res;
        }
        return null;
    }
}
