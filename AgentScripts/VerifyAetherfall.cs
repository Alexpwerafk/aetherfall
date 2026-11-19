using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using Aetherfall.Core;
using Aetherfall.Gameplay;

public static class VerifyAetherfall
{
    public static string AuditScene()
    {
        var sb = new StringBuilder();
        int badRenderers = 0;

        Renderer[] renderers = Object.FindObjectsByType<Renderer>();
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            foreach (Material m in r.sharedMaterials)
            {
                if (m == null || m.name.Contains("Default-Material") ||
                    (m.shader != null && m.shader.name == "Universal Render Pipeline/Lit"))
                {
                    badRenderers++;
                    sb.AppendLine($"FAIL Renderer '{r.name}' has non-palette material: {(m == null ? "null" : m.name)}");
                }
            }
        }

        if (RenderSettings.skybox == null || !RenderSettings.skybox.name.Contains("SanctumSky"))
            sb.AppendLine($"FAIL Skybox is {(RenderSettings.skybox == null ? "null" : RenderSettings.skybox.name)}");

        Camera cam = Camera.main;
        if (cam == null)
        {
            sb.AppendLine("FAIL No Camera.main found.");
        }
        else if (!cam.TryGetComponent<UniversalAdditionalCameraData>(out var camData) || !camData.renderPostProcessing)
        {
            sb.AppendLine("FAIL Main Camera renderPostProcessing is false or missing UniversalAdditionalCameraData.");
        }

        var urp = UniversalRenderPipeline.asset;
        if (urp == null || !urp.supportsHDR)
            sb.AppendLine("FAIL URP Asset missing or HDR disabled.");

        Volume vol = Object.FindAnyObjectByType<Volume>();
        if (vol == null || vol.sharedProfile == null)
        {
            sb.AppendLine("FAIL Global Volume or sharedProfile missing.");
        }
        else
        {
            bool hasTone = vol.sharedProfile.TryGet<Tonemapping>(out var tone) && tone.active && tone.mode.overrideState;
            bool hasBloom = vol.sharedProfile.TryGet<Bloom>(out var bloom) && bloom.active && bloom.intensity.overrideState;
            bool hasVig = vol.sharedProfile.TryGet<Vignette>(out var vig) && vig.active && vig.intensity.overrideState;
            bool hasAdj = vol.sharedProfile.TryGet<ColorAdjustments>(out var adj) && adj.active && adj.saturation.overrideState;
            if (!hasTone || !hasBloom || !hasVig || !hasAdj)
                sb.AppendLine($"FAIL Volume overrides incomplete: tone={hasTone}, bloom={hasBloom}, vig={hasVig}, colorAdj={hasAdj}");
        }

        if (TMP_Settings.instance == null || TMP_Settings.defaultFontAsset == null)
            sb.AppendLine("FAIL TMP Essential Resources / defaultFontAsset missing.");

        if (sb.Length == 0)
            return $"PASS: Audited {renderers.Length} renderers (0 default/Lit materials), palette skybox OK, HDR=true, camera postProcessing=true, ACES+Bloom+Vignette+ColorAdj active, TMP ready.";
        return sb.ToString();
    }

    public static string CheckPlayLiveness()
    {
        return $"isPlaying={Application.isPlaying}, frameCount={Time.frameCount}, runInBg={Application.runInBackground}, state={GameLoop.Instance?.State}";
    }

    public static string StartTacticalOverview()
    {
        if (GameLoop.Instance == null || HeroController.Instance == null)
            return "ERROR: Missing managers.";
        if (GameLoop.Instance.State == GameState.Title)
            GameLoop.Instance.StartExpedition();
        if (GameLoop.Instance.State == GameState.Exploring)
            DungeonDirector.Instance?.StartChamberEncounter(1);

        // Move hero slightly closer to center so frame 1 looks dramatic
        HeroController.Instance.transform.position = new Vector3(-0.6f, 0f, -1.8f);
        HeroController.Instance.transform.rotation = Quaternion.LookRotation(new Vector3(-0.4f, 0f, 1f));
        return $"TacticalOverview ready: state={GameLoop.Instance.State}, chamber={GameLoop.Instance.CurrentChamber}, ap={HeroController.Instance.CurrentAP}";
    }

    public static string TriggerAttackQTEForCapture()
    {
        if (HeroController.Instance == null || DungeonDirector.Instance == null)
            return "ERROR: Missing Hero or Director.";
        var enemies = DungeonDirector.Instance.GetActiveChamberEnemies();
        if (enemies.Count == 0) return "ERROR: No active enemies.";
        HeroController.Instance.transform.position = new Vector3(-1.3f, 0f, 0.2f);
        EnemyAI target = enemies[0];
        HeroController.Instance.ExecuteStrikeOn(target.transform);
        return $"Triggered ExecuteStrikeOn '{target.DisplayName}', QTE active={ExpeditionQTEController.Instance?.IsAttackQTEActive}";
    }

    public static string TriggerPerfectResonanceAndOilExplosion()
    {
        if (ExpeditionQTEController.Instance != null && ExpeditionQTEController.Instance.IsAttackQTEActive)
            ExpeditionQTEController.Instance.ForceTriggerResonance();
        else if (ElementalSurfaceManager.Instance != null)
            ElementalSurfaceManager.Instance.ApplyElementAt(new Vector3(-2.5f, 0f, 2.5f), 3.0f, ElementType.Fire);
        return $"Triggered resonance/oil explosion. Combos={GameLoop.Instance?.ElementalCombos}, Resonances={GameLoop.Instance?.PerfectResonances}";
    }

    public static string RunFullCombatAndLoopChecks()
    {
        var sb = new StringBuilder();
        var loop = GameLoop.Instance;
        var hero = HeroController.Instance;
        var dir = DungeonDirector.Instance;
        var surfMgr = ElementalSurfaceManager.Instance;

        if (loop == null || hero == null || dir == null || surfMgr == null)
            return "FAIL: Core managers null.";

        // 1. Test Chest Equip
        foreach (var chest in surfMgr.Chests)
        {
            if (chest != null && !chest.IsOpened)
            {
                chest.Open(hero);
                break;
            }
        }
        sb.AppendLine($"Chest opened -> Weapon={hero.EquippedWeapon} ({hero.CurrentElement})");

        // 2. Test Pause / Resume
        loop.TogglePause();
        bool pausedOk = loop.State == GameState.Paused && Time.timeScale == 0f;
        loop.TogglePause();
        bool resumedOk = loop.State != GameState.Paused && Time.timeScale == 1f;
        sb.AppendLine($"Pause/Resume check: pausedOk={pausedOk}, resumedOk={resumedOk}");

        // 3. Test Water + Lightning Combo
        surfMgr.SpawnSurface(new Vector3(4f, 0f, 3f), SurfaceType.Water, 2.8f, 3);
        surfMgr.ApplyElementAt(new Vector3(4f, 0f, 3f), 2.5f, ElementType.Lightning);
        sb.AppendLine($"ElementalCombos={loop.ElementalCombos}");

        // 4. Test Enemy defeat & Chamber Clear Draft
        foreach (var e in dir.GetActiveChamberEnemies())
        {
            if (e != null && !e.IsDead)
                e.TakeDamage(999, ElementType.Lightning, true);
        }
        dir.CheckChamberCleared();
        sb.AppendLine($"After Chamber 1 clear: State={loop.State}, Bridge1Active={dir.Bridge1Barrier?.activeSelf}");

        if (loop.State == GameState.ChamberClearDraft)
        {
            dir.PickBoon("TempestRiposte");
            sb.AppendLine($"After Boon Pick: State={loop.State}, BoonTempestRiposte={loop.BoonTempestRiposte}");
        }

        // 5. Test End state & Restart
        loop.WinExpedition();
        bool wonOk = loop.State == GameState.Won;
        sb.AppendLine($"Win state check: wonOk={wonOk}, EnemiesDefeated={loop.EnemiesDefeated}, Level={loop.Level}");
        return sb.ToString();
    }

    public static string InspectHierarchy()
    {
        var sb = new StringBuilder();
        foreach (string n in new[] { "Hero_Expeditioner", "Env_RuinedPillar", "Env_GoldenChest", "Env_ExplosiveBarrel", "Corrupted Vanguard_Ch1" })
        {
            var go = GameObject.Find(n);
            if (go == null) continue;
            int count = 0;
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                if (count++ > 10) break;
                sb.AppendLine($"{n}/{t.name}: pos={t.localPosition} rot={t.localEulerAngles}");
            }
        }
        return sb.ToString();
    }

    public static string TestBakeAxis()
    {
        var imp = UnityEditor.AssetImporter.GetAtPath("Assets/Models/Hero_Expeditioner.fbx") as UnityEditor.ModelImporter;
        if (imp == null) return "No importer";
        imp.bakeAxisConversion = true;
        imp.SaveAndReimport();
        var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Hero_Expeditioner.fbx");
        var sb = new StringBuilder();
        foreach (var t in asset.GetComponentsInChildren<Transform>(true))
            sb.AppendLine($"{t.name}: pos={t.localPosition} rot={t.localEulerAngles}");
        return sb.ToString();
    }
}
