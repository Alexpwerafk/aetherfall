using UnityEngine;

namespace Aetherfall.UI
{
    /// <summary>
    /// Procedural SDF vector-art sprite generator for Aetherfall's UI.
    /// Generates crisp, anti-aliased 9-sliced beveled frames, rounded pills,
    /// faceted AP diamonds, ornamental dividers, radial glows, QTE rings,
    /// and custom tactical skill/boon icons — zero external textures required.
    /// </summary>
    public static class UIArtFactory
    {
        public static Sprite Solid { get; private set; }
        public static Sprite ChamferPanel { get; private set; }
        public static Sprite ChamferPanelSoft { get; private set; }
        public static Sprite RoundedPill { get; private set; }
        public static Sprite RoundedPillOutline { get; private set; }
        public static Sprite DiamondGem { get; private set; }
        public static Sprite DiamondOutline { get; private set; }
        public static Sprite RadialGlow { get; private set; }
        public static Sprite GradientBar { get; private set; }
        public static Sprite GoldDivider { get; private set; }
        public static Sprite QteRing { get; private set; }
        public static Sprite QteTargetRing { get; private set; }
        public static Sprite VignetteTop { get; private set; }
        public static Sprite VignetteBottom { get; private set; }

        // Procedural Vector Icons
        public static Sprite IconSword { get; private set; }
        public static Sprite IconFlask { get; private set; }
        public static Sprite IconCrosshair { get; private set; }
        public static Sprite IconShove { get; private set; }
        public static Sprite IconHourglass { get; private set; }
        public static Sprite IconPause { get; private set; }
        public static Sprite IconLightning { get; private set; }
        public static Sprite IconChrono { get; private set; }
        public static Sprite IconCrown { get; private set; }

        static bool _initialized;

        public static void EnsureInitialized()
        {
            if (_initialized && Solid != null) return;
            _initialized = true;

            Solid = CreateSolidSprite();
            ChamferPanel = CreateChamferPanelSprite(128, 16f, 3.2f, true);
            ChamferPanelSoft = CreateChamferPanelSprite(128, 12f, 2.2f, false);
            RoundedPill = CreateRoundedRectSprite(64, 16f, 0f, false);
            RoundedPillOutline = CreateRoundedRectSprite(64, 16f, 2.5f, true);
            DiamondGem = CreateDiamondSprite(64, false);
            DiamondOutline = CreateDiamondSprite(64, true);
            RadialGlow = CreateRadialGlowSprite(128);
            GradientBar = CreateGradientBarSprite(64);
            GoldDivider = CreateDividerSprite(256, 20);
            QteRing = CreateRingSprite(256, 0.81f, 0.94f, false);
            QteTargetRing = CreateRingSprite(256, 0.83f, 0.92f, true);
            VignetteTop = CreateVerticalGradientSprite(32, 128, true);
            VignetteBottom = CreateVerticalGradientSprite(32, 128, false);

            IconSword = CreateIconSword(96);
            IconFlask = CreateIconFlask(96);
            IconCrosshair = CreateIconCrosshair(96);
            IconShove = CreateIconShove(96);
            IconHourglass = CreateIconHourglass(96);
            IconPause = CreateIconPause(96);
            IconLightning = CreateIconLightning(96);
            IconChrono = CreateIconChrono(96);
            IconCrown = CreateIconCrown(96);
        }

        static Sprite MakeSprite(Texture2D tex, Vector4 border = default)
        {
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        static Sprite CreateSolidSprite()
        {
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color[] cols = new Color[16];
            for (int i = 0; i < 16; i++) cols[i] = Color.white;
            tex.SetPixels(cols);
            return MakeSprite(tex);
        }

        /// <summary>
        /// Octagonal chamfered RPG frame with subtle vertical sheen and crisp double-bevel rim.
        /// White-tinted base so Image.color controls the fill hue cleanly, with bright rim highlight.
        /// </summary>
        static Sprite CreateChamferPanelSprite(int size, float chamfer, float borderThickness, bool ornateRim)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            float r = half - 2f;

            for (int y = 0; y < size; y++)
            {
                float py = y - half + 0.5f;
                float ny = y / (float)(size - 1); // 0 bottom, 1 top
                for (int x = 0; x < size; x++)
                {
                    float px = x - half + 0.5f;
                    float ax = Mathf.Abs(px);
                    float ay = Mathf.Abs(py);

                    // Signed distance to chamfered box (octagon)
                    float dBox = Mathf.Max(ax - r, ay - r);
                    float dChamfer = (ax + ay - (2f * r - chamfer)) * 0.70710678f;
                    float d = Mathf.Max(dBox, dChamfer);

                    float alpha = Mathf.Clamp01(0.5f - d);
                    if (alpha <= 0.001f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    // Subtle vertical depth gradient (brighter top, deeper bottom)
                    float grad = Mathf.Lerp(0.84f, 1.06f, ny);
                    Color c = new Color(grad, grad, grad, alpha);

                    // Subtle inner top bevel sheen
                    if (d > -(borderThickness + 3.5f) && d <= -borderThickness && py > 0f)
                    {
                        c = Color.Lerp(c, new Color(1.18f, 1.16f, 1.12f, alpha), 0.35f);
                    }

                    // Crisp outer rim
                    if (d > -borderThickness)
                    {
                        float rimFactor = ornateRim ? 1.0f : 0.72f;
                        Color rimCol = new Color(1f, 0.96f, 0.85f, alpha);
                        c = Color.Lerp(c, rimCol, rimFactor);
                    }
                    else if (ornateRim && d > -(borderThickness + 2.2f) && d <= -(borderThickness + 0.8f))
                    {
                        // Inner recessed dark hairline for tactile depth
                        c = new Color(c.r * 0.55f, c.g * 0.55f, c.b * 0.62f, alpha);
                    }

                    tex.SetPixel(x, y, c);
                }
            }

            float slice = chamfer + borderThickness + 6f;
            return MakeSprite(tex, new Vector4(slice, slice, slice, slice));
        }

        static Sprite CreateRoundedRectSprite(int size, float radius, float strokeWidth, bool outlineOnly)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            float b = half - radius - 1.5f;

            for (int y = 0; y < size; y++)
            {
                float py = Mathf.Abs(y - half + 0.5f);
                for (int x = 0; x < size; x++)
                {
                    float px = Mathf.Abs(x - half + 0.5f);
                    float qx = Mathf.Max(px - b, 0f);
                    float qy = Mathf.Max(py - b, 0f);
                    float d = Mathf.Sqrt(qx * qx + qy * qy) - radius;

                    float alpha;
                    if (outlineOnly)
                    {
                        float outer = Mathf.Clamp01(0.5f - d);
                        float inner = Mathf.Clamp01(0.5f - (d + strokeWidth));
                        alpha = Mathf.Clamp01(outer - inner);
                    }
                    else
                    {
                        alpha = Mathf.Clamp01(0.5f - d);
                    }

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            float slice = radius + 4f;
            return MakeSprite(tex, new Vector4(slice, slice, slice, slice));
        }

        static Sprite CreateDiamondSprite(int size, bool outlineOnly)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            float r = half - 4f;

            for (int y = 0; y < size; y++)
            {
                float py = y - half + 0.5f;
                for (int x = 0; x < size; x++)
                {
                    float px = x - half + 0.5f;
                    float d = (Mathf.Abs(px) + Mathf.Abs(py) - r) * 0.70710678f;

                    if (outlineOnly)
                    {
                        float outer = Mathf.Clamp01(0.5f - d);
                        float inner = Mathf.Clamp01(0.5f - (d + 3.0f));
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(outer - inner)));
                    }
                    else
                    {
                        float alpha = Mathf.Clamp01(0.5f - d);
                        if (alpha <= 0.001f)
                        {
                            tex.SetPixel(x, y, Color.clear);
                            continue;
                        }
                        // Faceted crystal shading: bright top-left facet, rich bottom-right
                        float facet = 0.85f;
                        if (py > 0f && px < 0f) facet = 1.18f;
                        else if (py > 0f && px >= 0f) facet = 1.02f;
                        else if (py <= 0f && px < 0f) facet = 0.90f;
                        else facet = 0.76f;

                        // Bright core glint
                        float centerDist = Mathf.Sqrt(px * px + py * py) / r;
                        facet += Mathf.Clamp01(1f - centerDist * 2.2f) * 0.25f;

                        tex.SetPixel(x, y, new Color(Mathf.Min(1f, facet), Mathf.Min(1f, facet), Mathf.Min(1f, facet), alpha));
                    }
                }
            }
            return MakeSprite(tex);
        }

        static Sprite CreateRadialGlowSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half + 0.5f) / half;
                    float dy = (y - half + 0.5f) / half;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - dist);
                    a = a * a * a; // smooth exponential falloff
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            return MakeSprite(tex);
        }

        static Sprite CreateGradientBarSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                float ny = y / (float)(size - 1);
                // Glossy RPG bar sheen: bright upper third, deep bottom edge
                float shade = Mathf.Lerp(0.74f, 1.0f, ny);
                if (ny > 0.58f && ny < 0.88f)
                    shade = Mathf.Lerp(shade, 1.18f, 0.55f);
                Color c = new Color(Mathf.Min(1f, shade), Mathf.Min(1f, shade), Mathf.Min(1f, shade), 1f);
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, c);
            }
            return MakeSprite(tex);
        }

        static Sprite CreateDividerSprite(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            float cx = w * 0.5f;
            float cy = h * 0.5f;

            for (int y = 0; y < h; y++)
            {
                float dy = Mathf.Abs(y - cy + 0.5f);
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x - cx + 0.5f);
                    float nx = dx / cx;

                    // Tapered horizontal filigree line
                    float lineHalfW = Mathf.Lerp(1.6f, 0.2f, nx);
                    float lineAlpha = Mathf.Clamp01(1f - Mathf.Max(0f, dy - lineHalfW)) * Mathf.Clamp01(1f - nx * nx);

                    // Center diamond jewel
                    float dDiamond = (dx + dy - (cy - 2f)) * 0.70710678f;
                    float diamondAlpha = Mathf.Clamp01(0.5f - dDiamond);

                    float alpha = Mathf.Max(lineAlpha * 0.85f, diamondAlpha);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            return MakeSprite(tex);
        }

        static Sprite CreateRingSprite(int size, float innerR, float outerR, bool withCardinalTicks)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half + 0.5f) / half;
                    float dy = (y - half + 0.5f) / half;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float aa = 0.025f;
                    float ringA = Mathf.SmoothStep(innerR - aa, innerR, dist) * (1f - Mathf.SmoothStep(outerR, outerR + aa, dist));

                    // Soft halo around ring
                    float mid = (innerR + outerR) * 0.5f;
                    float halo = Mathf.Clamp01(1f - Mathf.Abs(dist - mid) / 0.14f);
                    float alpha = Mathf.Max(ringA, halo * halo * 0.30f);

                    if (withCardinalTicks)
                    {
                        // 4 cardinal diamond ticks at N, S, E, W
                        float tickX = Mathf.Abs(dx);
                        float tickY = Mathf.Abs(dy);
                        float dN = (tickX + Mathf.Abs(tickY - mid) - 0.085f);
                        float dE = (Mathf.Abs(tickX - mid) + tickY - 0.085f);
                        float tickA = Mathf.Max(Mathf.Clamp01((0.015f - dN) / 0.02f), Mathf.Clamp01((0.015f - dE) / 0.02f));
                        alpha = Mathf.Max(alpha, tickA);
                    }

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)));
                }
            }
            return MakeSprite(tex);
        }

        static Sprite CreateVerticalGradientSprite(int w, int h, bool topDark)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                float ny = y / (float)(h - 1);
                float a = topDark ? ny : (1f - ny);
                a = a * a;
                Color c = new Color(1f, 1f, 1f, a);
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, c);
            }
            return MakeSprite(tex);
        }

        // --- Procedural Vector Icons (SDF Helpers) ---

        static float SdSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a;
            Vector2 ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(0.0001f, Vector2.Dot(ba, ba)));
            return (pa - ba * h).magnitude;
        }

        static Sprite CreateIconSword(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2((x / (float)size) * 2f - 1f, (y / (float)size) * 2f - 1f);
                    // Blade diagonal from (-0.25, -0.25) to (0.68, 0.68)
                    float dBlade = SdSegment(p, new Vector2(-0.22f, -0.22f), new Vector2(0.65f, 0.65f)) - 0.11f;
                    // Tapered tip
                    if (p.x + p.y > 1.15f) dBlade += (p.x + p.y - 1.15f) * 0.45f;
                    // Crossguard perpendicular
                    float dGuard = SdSegment(p, new Vector2(-0.52f, 0.08f), new Vector2(0.08f, -0.52f)) - 0.075f;
                    // Hilt
                    float dHilt = SdSegment(p, new Vector2(-0.62f, -0.62f), new Vector2(-0.22f, -0.22f)) - 0.06f;
                    // Pommel
                    float dPommel = (p - new Vector2(-0.66f, -0.66f)).magnitude - 0.11f;

                    float d = Mathf.Min(Mathf.Min(dBlade, dGuard), Mathf.Min(dHilt, dPommel));
                    float alpha = Mathf.Clamp01(0.04f - d) / 0.04f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            return MakeSprite(tex);
        }

        static Sprite CreateIconFlask(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2((x / (float)size) * 2f - 1f, (y / (float)size) * 2f - 1f);
                    // Round flask body
                    float dBody = (p - new Vector2(0f, -0.22f)).magnitude - 0.48f;
                    // Neck
                    float dNeck = SdSegment(p, new Vector2(0f, 0.18f), new Vector2(0f, 0.62f)) - 0.14f;
                    // Rim
                    float dRim = SdSegment(p, new Vector2(-0.22f, 0.64f), new Vector2(0.22f, 0.64f)) - 0.07f;

                    float dOuter = Mathf.Min(Mathf.Min(dBody, dNeck), dRim);
                    float alpha = Mathf.Clamp01(0.04f - dOuter) / 0.04f;

                    // Carve slight hollow upper-bulb so liquid reads nicely
                    float dHollow = (p - new Vector2(0f, -0.18f)).magnitude - 0.34f;
                    if (dHollow < 0f && p.y > -0.16f && p.y < 0.16f)
                        alpha *= 0.35f;

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            return MakeSprite(tex);
        }

        static Sprite CreateIconCrosshair(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2((x / (float)size) * 2f - 1f, (y / (float)size) * 2f - 1f);
                    float r = p.magnitude;
                    float dRing = Mathf.Abs(r - 0.54f) - 0.07f;
                    float dCenter = r - 0.14f;
                    float dN = SdSegment(p, new Vector2(0f, 0.34f), new Vector2(0f, 0.80f)) - 0.055f;
                    float dS = SdSegment(p, new Vector2(0f, -0.34f), new Vector2(0f, -0.80f)) - 0.055f;
                    float dE = SdSegment(p, new Vector2(0.34f, 0f), new Vector2(0.80f, 0f)) - 0.055f;
                    float dW = SdSegment(p, new Vector2(-0.34f, 0f), new Vector2(-0.80f, 0f)) - 0.055f;

                    float d = Mathf.Min(Mathf.Min(dRing, dCenter), Mathf.Min(Mathf.Min(dN, dS), Mathf.Min(dE, dW)));
                    float alpha = Mathf.Clamp01(0.04f - d) / 0.04f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            return MakeSprite(tex);
        }

        static Sprite CreateIconShove(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2((x / (float)size) * 2f - 1f, (y / (float)size) * 2f - 1f);
                    // Double kinetic push chevrons pointing right
                    float d1a = SdSegment(p, new Vector2(-0.48f, 0.48f), new Vector2(0.02f, 0f)) - 0.09f;
                    float d1b = SdSegment(p, new Vector2(-0.48f, -0.48f), new Vector2(0.02f, 0f)) - 0.09f;
                    float d2a = SdSegment(p, new Vector2(0.05f, 0.56f), new Vector2(0.62f, 0f)) - 0.10f;
                    float d2b = SdSegment(p, new Vector2(0.05f, -0.56f), new Vector2(0.62f, 0f)) - 0.10f;

                    float d = Mathf.Min(Mathf.Min(d1a, d1b), Mathf.Min(d2a, d2b));
                    float alpha = Mathf.Clamp01(0.04f - d) / 0.04f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            return MakeSprite(tex);
        }

        static Sprite CreateIconHourglass(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2((x / (float)size) * 2f - 1f, (y / (float)size) * 2f - 1f);
                    float dTop = SdSegment(p, new Vector2(-0.48f, 0.64f), new Vector2(0.48f, 0.64f)) - 0.075f;
                    float dBot = SdSegment(p, new Vector2(-0.48f, -0.64f), new Vector2(0.48f, -0.64f)) - 0.075f;
                    float dL1 = SdSegment(p, new Vector2(-0.38f, 0.60f), new Vector2(0.38f, -0.60f)) - 0.07f;
                    float dL2 = SdSegment(p, new Vector2(0.38f, 0.60f), new Vector2(-0.38f, -0.60f)) - 0.07f;
                    float d = Mathf.Min(Mathf.Min(dTop, dBot), Mathf.Min(dL1, dL2));
                    float alpha = Mathf.Clamp01(0.04f - d) / 0.04f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            return MakeSprite(tex);
        }

        static Sprite CreateIconPause(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2((x / (float)size) * 2f - 1f, (y / (float)size) * 2f - 1f);
                    float d1 = SdSegment(p, new Vector2(-0.28f, -0.52f), new Vector2(-0.28f, 0.52f)) - 0.13f;
                    float d2 = SdSegment(p, new Vector2(0.28f, -0.52f), new Vector2(0.28f, 0.52f)) - 0.13f;
                    float d = Mathf.Min(d1, d2);
                    float alpha = Mathf.Clamp01(0.04f - d) / 0.04f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            return MakeSprite(tex);
        }

        static Sprite CreateIconLightning(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2((x / (float)size) * 2f - 1f, (y / (float)size) * 2f - 1f);
                    float d1 = SdSegment(p, new Vector2(0.18f, 0.72f), new Vector2(-0.26f, 0.04f)) - 0.11f;
                    float d2 = SdSegment(p, new Vector2(-0.26f, 0.04f), new Vector2(0.22f, 0.04f)) - 0.09f;
                    float d3 = SdSegment(p, new Vector2(0.22f, 0.04f), new Vector2(-0.18f, -0.72f)) - 0.11f;
                    float d = Mathf.Min(d1, Mathf.Min(d2, d3));
                    float alpha = Mathf.Clamp01(0.04f - d) / 0.04f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            return MakeSprite(tex);
        }

        static Sprite CreateIconChrono(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2((x / (float)size) * 2f - 1f, (y / (float)size) * 2f - 1f);
                    float r = p.magnitude;
                    float dOuter = Mathf.Abs(r - 0.62f) - 0.075f;
                    float dHand1 = SdSegment(p, Vector2.zero, new Vector2(0f, 0.42f)) - 0.065f;
                    float dHand2 = SdSegment(p, Vector2.zero, new Vector2(0.32f, -0.18f)) - 0.065f;
                    float dDot = r - 0.11f;
                    float d = Mathf.Min(Mathf.Min(dOuter, dDot), Mathf.Min(dHand1, dHand2));
                    float alpha = Mathf.Clamp01(0.04f - d) / 0.04f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            return MakeSprite(tex);
        }

        static Sprite CreateIconCrown(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2((x / (float)size) * 2f - 1f, (y / (float)size) * 2f - 1f);
                    float dBase = SdSegment(p, new Vector2(-0.56f, -0.40f), new Vector2(0.56f, -0.40f)) - 0.10f;
                    float dL = SdSegment(p, new Vector2(-0.54f, -0.35f), new Vector2(-0.62f, 0.35f)) - 0.08f;
                    float dL2 = SdSegment(p, new Vector2(-0.62f, 0.35f), new Vector2(-0.22f, -0.05f)) - 0.08f;
                    float dM1 = SdSegment(p, new Vector2(-0.22f, -0.05f), new Vector2(0f, 0.56f)) - 0.085f;
                    float dM2 = SdSegment(p, new Vector2(0f, 0.56f), new Vector2(0.22f, -0.05f)) - 0.085f;
                    float dR1 = SdSegment(p, new Vector2(0.22f, -0.05f), new Vector2(0.62f, 0.35f)) - 0.08f;
                    float dR2 = SdSegment(p, new Vector2(0.62f, 0.35f), new Vector2(0.54f, -0.35f)) - 0.08f;
                    float d = Mathf.Min(dBase, Mathf.Min(Mathf.Min(dL, dL2), Mathf.Min(Mathf.Min(dM1, dM2), Mathf.Min(dR1, dR2))));
                    float alpha = Mathf.Clamp01(0.04f - d) / 0.04f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            return MakeSprite(tex);
        }
    }
}
