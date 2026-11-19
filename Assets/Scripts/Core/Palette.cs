using UnityEngine;
using UnityEngine.Rendering;

namespace Aetherfall.Core
{
    /// <summary>
    /// Art Bible single color source: Sunlit 3D Cartoon Sky-Sanctum.
    /// Every material, light, particle, and UI element pulls strictly from this palette.
    /// </summary>
    public static class Palette
    {
        // Architecture & Shadows (Bright Cartoon Stone & Royal Cobalt)
        public static readonly Color ObsidianFloor = Hex("#F2E5C9");
        public static readonly Color MarbleTrim = Hex("#D8B888");
        public static readonly Color ObsidianArmor = Hex("#2C3A5E");
        public static readonly Color AbyssShadow = Hex("#52649E");
        public static readonly Color DeepVoidSky = Hex("#54C4FF");

        // Vibrant 3D Cartoon World & Foliage
        public static readonly Color GrassLush = Hex("#47D15C");
        public static readonly Color GrassLight = Hex("#7DF07E");
        public static readonly Color TreeTrunk = Hex("#9E6240");
        public static readonly Color CliffTerracotta = Hex("#C9845B");
        public static readonly Color CrystalMagenta = Hex("#FF4DB8");
        public static readonly Color RoyalRoofBlue = Hex("#2D8CFF");
        public static readonly Color SkySeaTurquoise = Hex("#30D5F2");

        // Hero & Filigree
        public static readonly Color HeroCrimson = Hex("#F53B4B");
        public static readonly Color FiligreeGold = Hex("#FFD23F");
        public static readonly Color IvoryCloth = Hex("#FFFDF5");

        // Enemies & Boss
        public static readonly Color EnemyVanguard = Hex("#34C77B");
        public static readonly Color EnemyAlchemist = Hex("#9B4DFF");
        public static readonly Color BossEclipse = Hex("#FF2E79");

        // Elements & VFX Accents
        public static readonly Color FireAmber = Hex("#FF6E27");
        public static readonly Color WaterAzure = Hex("#2880FF");
        public static readonly Color LightningCyan = Hex("#2CE8F5");
        public static readonly Color FrostIce = Hex("#8CF4FF");
        public static readonly Color OilDark = Hex("#2E2623");
        public static readonly Color HealEmerald = Hex("#3CD070");

        // Lighting
        public static readonly Color KeyLightWarm = Hex("#FFF6E5");
        public static readonly Color RimLightCool = Hex("#8CE0FF");
        public static readonly Color AmbientSky = Hex("#82D4FF");
        public static readonly Color AmbientEquator = Hex("#B8E2C8");
        public static readonly Color AmbientGround = Hex("#6E7EB5");

        // UI Surfaces
        public static readonly Color UiPanelBg = new Color(0.11f, 0.14f, 0.25f, 0.93f);
        public static readonly Color UiPanelInner = new Color(0.17f, 0.21f, 0.35f, 0.95f);
        public static readonly Color UiScrim = new Color(0.09f, 0.12f, 0.22f, 0.76f);

        public static Color Hex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color c))
                return c;
            return FiligreeGold;
        }

        public static Shader GetToonShader()
        {
            Shader s = Shader.Find("Aetherfall/CelToonURP");
            if (s != null) return s;

            if (GraphicsSettings.currentRenderPipeline != null &&
                GraphicsSettings.currentRenderPipeline.defaultShader != null)
            {
                return GraphicsSettings.currentRenderPipeline.defaultShader;
            }
            return Shader.Find("Universal Render Pipeline/Lit");
        }

        public static Material CreateToonMaterial(
            string matName,
            Color baseColor,
            Color? emission = null,
            float pulseSpeed = 0f,
            float rimStrength = 0.22f,
            Color? rimColor = null)
        {
            Shader shader = GetToonShader();
            Material mat = new Material(shader) { name = matName };
            mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_ShadowTint"))
                mat.SetColor("_ShadowTint", AbyssShadow);
            if (mat.HasProperty("_RimColor"))
                mat.SetColor("_RimColor", rimColor ?? LightningCyan);
            if (mat.HasProperty("_RimPower"))
                mat.SetFloat("_RimPower", 3.5f);
            if (mat.HasProperty("_RimStrength"))
                mat.SetFloat("_RimStrength", rimStrength);
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", emission ?? Color.black);
            if (mat.HasProperty("_PulseSpeed"))
                mat.SetFloat("_PulseSpeed", pulseSpeed);
            return mat;
        }
    }
}
