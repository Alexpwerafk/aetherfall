using UnityEngine;
using UnityEngine.Rendering;

namespace Aetherfall.Core
{
    /// <summary>
    /// Art Bible single color source: Clair-Obscur Astral Diorama.
    /// Every material, light, particle, and UI element pulls strictly from this palette.
    /// </summary>
    public static class Palette
    {
        // Architecture & Shadows
        public static readonly Color ObsidianFloor = Hex("#1C202E");
        public static readonly Color MarbleTrim = Hex("#353C52");
        public static readonly Color ObsidianArmor = Hex("#171A26");
        public static readonly Color AbyssShadow = Hex("#10131E");
        public static readonly Color DeepVoidSky = Hex("#0D101B");

        // Hero & Filigree
        public static readonly Color HeroCrimson = Hex("#D94854");
        public static readonly Color FiligreeGold = Hex("#F5C26B");
        public static readonly Color IvoryCloth = Hex("#EAE3D2");

        // Enemies & Boss
        public static readonly Color EnemyVanguard = Hex("#4F6D5A");
        public static readonly Color EnemyAlchemist = Hex("#7A4E9E");
        public static readonly Color BossEclipse = Hex("#B8336A");

        // Elements & VFX Accents
        public static readonly Color FireAmber = Hex("#FF6E27");
        public static readonly Color WaterAzure = Hex("#2880FF");
        public static readonly Color LightningCyan = Hex("#2CE8F5");
        public static readonly Color FrostIce = Hex("#8CF4FF");
        public static readonly Color OilDark = Hex("#221D1A");
        public static readonly Color HealEmerald = Hex("#3CD070");

        // Lighting
        public static readonly Color KeyLightWarm = Hex("#FFE8C8");
        public static readonly Color RimLightCool = Hex("#8BB4FF");
        public static readonly Color AmbientSky = Hex("#4A5D96");
        public static readonly Color AmbientEquator = Hex("#313B61");
        public static readonly Color AmbientGround = Hex("#191D30");

        // UI Surfaces
        public static readonly Color UiPanelBg = new Color(0.08f, 0.09f, 0.14f, 0.92f);
        public static readonly Color UiPanelInner = new Color(0.13f, 0.15f, 0.22f, 0.95f);
        public static readonly Color UiScrim = new Color(0.06f, 0.07f, 0.12f, 0.78f);

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
