using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Aetherfall.Audio;
using Aetherfall.Core;
using Aetherfall.Feel;
using Aetherfall.Gameplay;

namespace Aetherfall.UI
{
    /// <summary>
    /// AAA Tactical RPG Game Shell & HUD (uGUI + TextMeshPro + Procedural SDF Vector Art).
    /// 100% Spanish localized with chamfered ornamental frames, custom vector icons,
    /// delayed HP damage trail, faceted AP crystal diamonds, hover-lift skill/boon cards,
    /// and Expedition 33 active QTE resonance rings.
    /// </summary>
    public class GameShellUI : MonoBehaviour
    {
        public static GameShellUI Instance { get; private set; }

        static readonly Color PanelDeepNavy = new Color(0.08f, 0.11f, 0.21f, 0.94f);
        static readonly Color PanelCardNavy = new Color(0.13f, 0.17f, 0.30f, 0.96f);
        static readonly Color PanelCardHover = new Color(0.20f, 0.25f, 0.42f, 0.98f);
        static readonly Color PanelCardSelected = new Color(0.28f, 0.24f, 0.15f, 0.98f);
        static readonly Color BarTrackDark = new Color(0.05f, 0.07f, 0.14f, 0.96f);
        static readonly Color ScrimDark = new Color(0.04f, 0.06f, 0.13f, 0.80f);
        static readonly Color MutedIvory = new Color(0.86f, 0.88f, 0.94f, 0.88f);

        Canvas _canvas;

        // Title Screen
        GameObject _titlePanel;
        UnityEngine.UI.Image _titleGlowImg;
        TextMeshProUGUI _titleSubPromptText;
        UnityEngine.UI.Image _titleCtaGlow;

        // Main HUD
        GameObject _hudPanel;
        TextMeshProUGUI _chamberHeaderText;
        TextMeshProUGUI _turnBannerText;
        UnityEngine.UI.Image _turnAccentLine;
        TextMeshProUGUI _controlsStripText;

        // Hero Vitals
        UnityEngine.UI.Image _hpFillImg;
        UnityEngine.UI.Image _hpTrailFillImg;
        TextMeshProUGUI _hpValueText;
        UnityEngine.UI.Image _xpFillImg;
        TextMeshProUGUI _levelBadgeText;
        TextMeshProUGUI _weaponText;
        UnityEngine.UI.Image _weaponDotImg;

        // AP Crystal Rack
        TextMeshProUGUI _apValueText;
        readonly List<UnityEngine.UI.Image> _apPips = new List<UnityEngine.UI.Image>();
        readonly List<UnityEngine.UI.Image> _apGlows = new List<UnityEngine.UI.Image>();

        // Tactical Action Bar
        GameObject _tacticalBar;
        GameObject _explorationCard;
        UIJuiceButton _juiceStrike;
        UIJuiceButton _juiceFlask;
        UIJuiceButton _juiceShot;
        UIJuiceButton _juiceShove;
        TextMeshProUGUI _btnFlaskLabel;
        UnityEngine.UI.Image _btnFlaskIcon;

        // QTE Overlay
        GameObject _qteOverlay;
        RectTransform _qteDynamicRing;
        UnityEngine.UI.Image _qteDynamicRingImg;
        UnityEngine.UI.Image _qteTargetRingImg;
        UnityEngine.UI.Image _qteCenterGlowImg;
        TextMeshProUGUI _qtePromptText;

        // Boon Draft Modal
        GameObject _draftPanel;
        readonly List<TextMeshProUGUI> _draftTags = new List<TextMeshProUGUI>();
        readonly List<TextMeshProUGUI> _draftTitles = new List<TextMeshProUGUI>();
        readonly List<TextMeshProUGUI> _draftDescs = new List<TextMeshProUGUI>();
        readonly List<UnityEngine.UI.Image> _draftIcons = new List<UnityEngine.UI.Image>();
        readonly List<UnityEngine.UI.Image> _draftIconGlows = new List<UnityEngine.UI.Image>();

        // Pause Modal
        GameObject _pausePanel;
        UnityEngine.UI.Slider _musicSlider;
        UnityEngine.UI.Slider _sfxSlider;
        TextMeshProUGUI _musicValText;
        TextMeshProUGUI _sfxValText;

        // End Screen
        GameObject _endPanel;
        UnityEngine.UI.Image _endIconImg;
        TextMeshProUGUI _endTitleText;
        TextMeshProUGUI _endSubtitleText;
        TextMeshProUGUI _endStatsLeftText;
        TextMeshProUGUI _endStatsRightText;

        float _displayedHpFill = 1f;
        float _trailHpFill = 1f;
        float _displayedXpFill = 0f;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            UIArtFactory.EnsureInitialized();
            BuildCanvasHierarchy();
        }

        void OnEnable()
        {
            if (GameLoop.Instance != null)
            {
                GameLoop.Instance.OnStateChanged += HandleStateChanged;
                GameLoop.Instance.OnStatsChanged += RefreshStats;
            }
        }

        void Start()
        {
            if (GameLoop.Instance != null)
            {
                GameLoop.Instance.OnStateChanged -= HandleStateChanged;
                GameLoop.Instance.OnStatsChanged -= RefreshStats;
                GameLoop.Instance.OnStateChanged += HandleStateChanged;
                GameLoop.Instance.OnStatsChanged += RefreshStats;
                HandleStateChanged(GameLoop.Instance.State);
                RefreshStats();
            }
        }

        void OnDestroy()
        {
            if (GameLoop.Instance != null)
            {
                GameLoop.Instance.OnStateChanged -= HandleStateChanged;
                GameLoop.Instance.OnStatsChanged -= RefreshStats;
            }
        }

        void BuildCanvasHierarchy()
        {
            GameObject canvasGo = new GameObject("Aetherfall_ShellCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = Camera.main;
            _canvas.planeDistance = 1.1f;
            _canvas.sortingOrder = 20;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            BuildTitleScreen(canvasGo.transform);
            BuildHud(canvasGo.transform);
            BuildQteOverlay(canvasGo.transform);
            BuildDraftModal(canvasGo.transform);
            BuildPauseModal(canvasGo.transform);
            BuildEndScreen(canvasGo.transform);
        }

        // ==================== UI BUILDER HELPERS ====================

        static RectTransform MakeRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        static UnityEngine.UI.Image MakeImage(string name, Transform parent, Sprite sprite, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size,
            UnityEngine.UI.Image.Type imgType = UnityEngine.UI.Image.Type.Simple, bool raycast = false)
        {
            RectTransform rt = MakeRect(name, parent, anchorMin, anchorMax, pivot, pos, size);
            UnityEngine.UI.Image img = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = imgType;
            img.raycastTarget = raycast;
            return img;
        }

        static GameObject MakeChamferPanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 pos, Vector2 size, Color bgColor, bool ornate = true, bool raycast = false)
        {
            Sprite spr = ornate ? UIArtFactory.ChamferPanel : UIArtFactory.ChamferPanelSoft;
            UnityEngine.UI.Image img = MakeImage(name, parent, spr, bgColor, anchorMin, anchorMax, pivot, pos, size, UnityEngine.UI.Image.Type.Sliced, raycast);
            return img.gameObject;
        }

        static UnityEngine.UI.Image MakeDivider(Transform parent, Vector2 pos, float width, Color color)
        {
            return MakeImage("GoldDivider", parent, UIArtFactory.GoldDivider, color,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                pos, new Vector2(width, 16f));
        }

        static TextMeshProUGUI MakeText(string name, Transform parent, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center, FontStyles style = FontStyles.Bold, float spacing = 2f)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8f, 4f);
            rt.offsetMax = new Vector2(-8f, -4f);

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.fontStyle = style;
            tmp.characterSpacing = spacing;
            tmp.outlineWidth = 0.18f;
            tmp.outlineColor = new Color(0.03f, 0.05f, 0.10f, 0.95f);
            tmp.raycastTarget = false;
            return tmp;
        }

        static UnityEngine.UI.Button MakeActionButton(string name, Transform parent, string label, Vector2 pos, Vector2 size,
            Color bgNormal, Color bgHover, Color textColor, Color glowColor, out UIJuiceButton juiceOut)
        {
            GameObject go = MakeChamferPanel(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                pos, size, bgNormal, true, true);

            // Soft hover glow behind
            UnityEngine.UI.Image glow = MakeImage("HoverGlow", go.transform, UIArtFactory.RadialGlow, new Color(glowColor.r, glowColor.g, glowColor.b, 0f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36f, 36f));
            glow.transform.SetAsFirstSibling();

            UnityEngine.UI.Button btn = go.AddComponent<UnityEngine.UI.Button>();
            btn.transition = Selectable.Transition.None;

            juiceOut = go.AddComponent<UIJuiceButton>();
            juiceOut.BackgroundImage = go.GetComponent<UnityEngine.UI.Image>();
            juiceOut.GlowImage = glow;
            juiceOut.NormalBg = bgNormal;
            juiceOut.HoverBg = bgHover;
            juiceOut.SelectedBg = bgHover;
            juiceOut.GlowColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0.45f);
            juiceOut.HoverLiftY = 3f;
            juiceOut.SetBasePosition(pos);

            MakeText("Label", go.transform, label, 20f, textColor, TextAlignmentOptions.Center, FontStyles.Bold, 3f);
            return btn;
        }

        // ==================== 1. PANTALLA DE TÍTULO ====================

        void BuildTitleScreen(Transform root)
        {
            _titlePanel = new GameObject("TitlePanel");
            _titlePanel.transform.SetParent(root, false);
            RectTransform tRt = _titlePanel.AddComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;

            // Cinematic top & bottom framing gradients (keeps 3D sky-sanctum crystal clear in center!)
            MakeImage("TopVignette", _titlePanel.transform, UIArtFactory.VignetteTop, new Color(0.04f, 0.06f, 0.14f, 0.72f),
                new Vector2(0f, 0.62f), Vector2.one, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            MakeImage("BottomVignette", _titlePanel.transform, UIArtFactory.VignetteBottom, new Color(0.04f, 0.06f, 0.14f, 0.85f),
                Vector2.zero, new Vector2(1f, 0.36f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);

            // Title Crest Container
            RectTransform crest = MakeRect("TitleCrest", _titlePanel.transform,
                new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1020f, 300f));

            _titleGlowImg = MakeImage("TitleAura", crest, UIArtFactory.RadialGlow, new Color(Palette.FiligreeGold.r, Palette.FiligreeGold.g, Palette.FiligreeGold.b, 0.24f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 15f), new Vector2(880f, 360f));

            // Floating Crown Emblem above title
            MakeImage("CrownEmblem", crest, UIArtFactory.IconCrown, Palette.FiligreeGold,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -24f), new Vector2(54f, 54f));

            // Main Title Banner Panel (translucent royal obsidian with filigree trim)
            GameObject titleCard = MakeChamferPanel("TitleCard", crest,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -12f), new Vector2(960f, 200f), new Color(0.07f, 0.10f, 0.20f, 0.82f), true);

            TextMeshProUGUI mainTitle = MakeText("MainTitle", titleCard.transform, "AETHERFALL", 84f, Palette.FiligreeGold, TextAlignmentOptions.Center, FontStyles.Bold, 16f);
            mainTitle.rectTransform.offsetMin = new Vector2(16f, 68f);
            mainTitle.rectTransform.offsetMax = new Vector2(-16f, -12f);

            MakeDivider(titleCard.transform, new Vector2(0f, -14f), 620f, Palette.FiligreeGold);

            TextMeshProUGUI sub = MakeText("Subtitle", titleCard.transform,
                "EXPEDICIÓN TÁCTICA   ·   ALQUIMIA ELEMENTAL   ·   SANTUARIO ROGUELITE",
                20f, Palette.IvoryCloth, TextAlignmentOptions.Center, FontStyles.Bold, 4f);
            sub.rectTransform.offsetMin = new Vector2(20f, 16f);
            sub.rectTransform.offsetMax = new Vector2(-20f, -128f);

            // Interactive Enter Sanctum CTA Button
            RectTransform ctaAnchor = MakeRect("CtaAnchor", _titlePanel.transform,
                new Vector2(0.5f, 0.21f), new Vector2(0.5f, 0.21f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(540f, 120f));

            _titleCtaGlow = MakeImage("CtaAura", ctaAnchor, UIArtFactory.RadialGlow,
                new Color(Palette.LightningCyan.r, Palette.LightningCyan.g, Palette.LightningCyan.b, 0.30f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 14f), new Vector2(520f, 120f));

            UnityEngine.UI.Button enterBtn = MakeActionButton("EnterSanctumBtn", ctaAnchor, "ENTRAR AL SANTUARIO",
                new Vector2(0f, 14f), new Vector2(420f, 68f),
                new Color(0.14f, 0.20f, 0.36f, 0.96f), new Color(0.22f, 0.32f, 0.52f, 0.98f),
                Palette.FiligreeGold, Palette.LightningCyan, out _);
            enterBtn.onClick.AddListener(() => GameLoop.Instance?.StartExpedition());

            RectTransform hintRt = MakeRect("SubPromptRt", ctaAnchor,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -6f), new Vector2(600f, 34f));
            _titleSubPromptText = MakeText("SubPrompt", hintRt, "PULSA CUALQUIER TECLA O HAZ CLIC PARA COMENZAR",
                15f, Palette.LightningCyan, TextAlignmentOptions.Center, FontStyles.Normal, 3f);
        }

        // ==================== 2. HUD TÁCTICO ====================

        void BuildHud(Transform root)
        {
            _hudPanel = new GameObject("HudPanel");
            _hudPanel.transform.SetParent(root, false);
            RectTransform hudRt = _hudPanel.AddComponent<RectTransform>();
            hudRt.anchorMin = Vector2.zero;
            hudRt.anchorMax = Vector2.one;
            hudRt.offsetMin = Vector2.zero;
            hudRt.offsetMax = Vector2.zero;

            // --- TOP-LEFT: Compact Chamber Pill ---
            GameObject chamberPill = MakeChamferPanel("ChamberPill", _hudPanel.transform,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -20f), new Vector2(410f, 52f), PanelDeepNavy, false);

            MakeImage("ChamberGem", chamberPill.transform, UIArtFactory.DiamondGem, Palette.FiligreeGold,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(24f, 0f), new Vector2(18f, 18f));

            _chamberHeaderText = MakeText("ChamberText", chamberPill.transform, "CÁMARA 1/3 · EL PATIO QUEBRADO",
                16f, Palette.FiligreeGold, TextAlignmentOptions.Left, FontStyles.Bold, 2f);
            _chamberHeaderText.rectTransform.offsetMin = new Vector2(44f, 4f);
            _chamberHeaderText.rectTransform.offsetMax = new Vector2(-14f, -4f);

            // --- TOP-CENTER: Sleek Turn Phase Ribbon ---
            GameObject turnRibbon = MakeChamferPanel("TurnRibbon", _hudPanel.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -18f), new Vector2(680f, 62f), PanelDeepNavy, true);

            _turnBannerText = MakeText("TurnText", turnRibbon.transform, "TU TURNO — ELIGE UNA ACCIÓN",
                22f, Palette.FiligreeGold, TextAlignmentOptions.Center, FontStyles.Bold, 3f);
            _turnBannerText.rectTransform.offsetMin = new Vector2(18f, 10f);
            _turnBannerText.rectTransform.offsetMax = new Vector2(-18f, -6f);

            _turnAccentLine = MakeImage("TurnAccent", turnRibbon.transform, UIArtFactory.GoldDivider, Palette.FiligreeGold,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 4f), new Vector2(420f, 10f));

            // --- TOP-RIGHT: Sleek Pause Pill ---
            UnityEngine.UI.Button pauseBtn = MakeActionButton("PauseBtn", _hudPanel.transform, "PAUSA [ESC]",
                Vector2.zero, new Vector2(216f, 50f), PanelDeepNavy, PanelCardHover, Palette.IvoryCloth, Palette.FiligreeGold, out UIJuiceButton pauseJuice);
            RectTransform pRt = pauseBtn.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(1f, 1f);
            pRt.anchorMax = new Vector2(1f, 1f);
            pRt.pivot = new Vector2(1f, 1f);
            pRt.anchoredPosition = new Vector2(-24f, -20f);
            pauseJuice.SetBasePosition(pRt.anchoredPosition);
            pauseBtn.onClick.AddListener(() => GameLoop.Instance?.TogglePause());

            TextMeshProUGUI pauseLabel = pauseBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (pauseLabel != null)
            {
                pauseLabel.fontSize = 16f;
                pauseLabel.alignment = TextAlignmentOptions.MidlineLeft;
                pauseLabel.rectTransform.offsetMin = new Vector2(46f, 4f);
                pauseLabel.rectTransform.offsetMax = new Vector2(-12f, -4f);
            }

            MakeImage("PauseIcon", pauseBtn.transform, UIArtFactory.IconPause, Palette.FiligreeGold,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(25f, 0f), new Vector2(18f, 18f));

            // --- BOTTOM-CENTER: Tactical Command Deck ---
            GameObject deck = MakeChamferPanel("TacticalDeck", _hudPanel.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 36f), new Vector2(1380f, 152f), PanelDeepNavy, true);

            // Subtle top gold filigree rule along the deck
            MakeImage("DeckFiligree", deck.transform, UIArtFactory.GoldDivider, new Color(Palette.FiligreeGold.r, Palette.FiligreeGold.g, Palette.FiligreeGold.b, 0.75f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -2f), new Vector2(820f, 12f));

            // === LEFT MODULE: Hero Portrait Crest & Vitals ===
            GameObject vitals = MakeChamferPanel("HeroVitals", deck.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(18f, 0f), new Vector2(380f, 126f), PanelCardNavy, false);

            // Level Diamond Crest
            UnityEngine.UI.Image lvlCrest = MakeImage("LevelCrest", vitals.transform, UIArtFactory.DiamondGem, Palette.FiligreeGold,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(30f, -26f), new Vector2(38f, 38f));
            _levelBadgeText = MakeText("LvlNum", lvlCrest.transform, "1", 17f, new Color(0.06f, 0.08f, 0.16f, 1f), TextAlignmentOptions.Center, FontStyles.Bold, 0f);
            _levelBadgeText.outlineWidth = 0f;

            TextMeshProUGUI heroTitle = MakeText("HeroTitle", vitals.transform, "EXPEDICIONARIO",
                17f, Palette.FiligreeGold, TextAlignmentOptions.TopLeft, FontStyles.Bold, 3f);
            heroTitle.rectTransform.offsetMin = new Vector2(56f, 88f);
            heroTitle.rectTransform.offsetMax = new Vector2(-12f, -12f);

            // HP Bar with Delayed Damage Trail
            UnityEngine.UI.Image hpTrack = MakeImage("HpTrack", vitals.transform, UIArtFactory.RoundedPill, BarTrackDark,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -48f), new Vector2(-28f, 26f), UnityEngine.UI.Image.Type.Sliced);

            // Delayed white/amber catch-up trail
            _hpTrailFillImg = MakeImage("HpTrail", hpTrack.transform, UIArtFactory.GradientBar, Palette.FiligreeGold,
                Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(-4f, -4f), UnityEngine.UI.Image.Type.Filled);
            _hpTrailFillImg.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;

            // Main Crimson HP Fill
            _hpFillImg = MakeImage("HpFill", hpTrack.transform, UIArtFactory.GradientBar, Palette.HeroCrimson,
                Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(-4f, -4f), UnityEngine.UI.Image.Type.Filled);
            _hpFillImg.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;

            // Crisp pill outline over HP bar
            MakeImage("HpBorder", hpTrack.transform, UIArtFactory.RoundedPillOutline, new Color(Palette.FiligreeGold.r, Palette.FiligreeGold.g, Palette.FiligreeGold.b, 0.55f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, UnityEngine.UI.Image.Type.Sliced);

            _hpValueText = MakeText("HpVal", hpTrack.transform, "120 / 120 PV", 15f, Palette.IvoryCloth, TextAlignmentOptions.Center, FontStyles.Bold, 1.5f);

            // XP Bar
            UnityEngine.UI.Image xpTrack = MakeImage("XpTrack", vitals.transform, UIArtFactory.RoundedPill, BarTrackDark,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -80f), new Vector2(-28f, 11f), UnityEngine.UI.Image.Type.Sliced);

            _xpFillImg = MakeImage("XpFill", xpTrack.transform, UIArtFactory.GradientBar, Palette.FiligreeGold,
                Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(-2f, -2f), UnityEngine.UI.Image.Type.Filled);
            _xpFillImg.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;

            // Weapon Chip
            _weaponDotImg = MakeImage("WeaponDot", vitals.transform, UIArtFactory.DiamondGem, Palette.FireAmber,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(22f, 18f), new Vector2(14f, 14f));

            _weaponText = MakeText("WeaponText", vitals.transform, "Espadón Solar · Fuego",
                15f, Palette.FireAmber, TextAlignmentOptions.BottomLeft, FontStyles.Bold, 1f);
            _weaponText.rectTransform.offsetMin = new Vector2(36f, 8f);
            _weaponText.rectTransform.offsetMax = new Vector2(-12f, -92f);

            // === CENTER-LEFT MODULE: Arcane AP Crystal Rack ===
            GameObject apRack = MakeChamferPanel("ApRack", deck.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(410f, 0f), new Vector2(166f, 126f), PanelCardNavy, false);

            _apValueText = MakeText("ApHeader", apRack.transform, "PA  3 / 6",
                17f, Palette.LightningCyan, TextAlignmentOptions.Top, FontStyles.Bold, 2.5f);
            _apValueText.rectTransform.offsetMin = new Vector2(8f, 92f);
            _apValueText.rectTransform.offsetMax = new Vector2(-8f, -6f);

            for (int i = 0; i < 6; i++)
            {
                float x = -46f + (i % 3) * 46f;
                float y = (i < 3) ? 6f : -34f;

                UnityEngine.UI.Image glow = MakeImage($"ApGlow_{i}", apRack.transform, UIArtFactory.RadialGlow,
                    new Color(Palette.LightningCyan.r, Palette.LightningCyan.g, Palette.LightningCyan.b, 0.45f),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(x, y), new Vector2(46f, 46f));

                UnityEngine.UI.Image gem = MakeImage($"ApGem_{i}", apRack.transform, UIArtFactory.DiamondGem, Palette.LightningCyan,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(x, y), new Vector2(28f, 28f));

                MakeImage($"ApRim_{i}", gem.transform, UIArtFactory.DiamondOutline, new Color(1f, 1f, 1f, 0.45f),
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

                _apGlows.Add(glow);
                _apPips.Add(gem);
            }

            // === RIGHT MODULE: Tactical Skill Cards ===
            _tacticalBar = new GameObject("TacticalSkillsBar");
            _tacticalBar.transform.SetParent(deck.transform, false);
            RectTransform tbRt = _tacticalBar.AddComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0f, 0.5f);
            tbRt.anchorMax = new Vector2(1f, 0.5f);
            tbRt.offsetMin = new Vector2(588f, -64f);
            tbRt.offsetMax = new Vector2(-18f, 64f);

            CreateSkillCard("BtnStrike", _tacticalBar.transform, "1", "GOLPE", "1 PA",
                UIArtFactory.IconSword, Palette.FiligreeGold, new Vector2(-304f, 0f), new Vector2(146f, 118f),
                PanelCardNavy, PanelCardHover, PanelCardSelected,
                () => HeroController.Instance?.SelectAction(TacticalActionMode.Strike),
                out _juiceStrike, out _, out _);

            CreateSkillCard("BtnFlask", _tacticalBar.transform, "2", "ACEITE", "1 PA",
                UIArtFactory.IconFlask, Palette.FireAmber, new Vector2(-150f, 0f), new Vector2(146f, 118f),
                PanelCardNavy, PanelCardHover, PanelCardSelected,
                () => HeroController.Instance?.SelectAction(TacticalActionMode.Flask),
                out _juiceFlask, out _btnFlaskLabel, out _btnFlaskIcon);

            CreateSkillCard("BtnShot", _tacticalBar.transform, "3", "DISPARO", "1 PA",
                UIArtFactory.IconCrosshair, Palette.LightningCyan, new Vector2(4f, 0f), new Vector2(146f, 118f),
                PanelCardNavy, PanelCardHover, PanelCardSelected,
                () => HeroController.Instance?.SelectAction(TacticalActionMode.FreeAimShot),
                out _juiceShot, out _, out _);

            CreateSkillCard("BtnShove", _tacticalBar.transform, "4", "EMPUJÓN", "1 PA",
                UIArtFactory.IconShove, Palette.IvoryCloth, new Vector2(158f, 0f), new Vector2(146f, 118f),
                PanelCardNavy, PanelCardHover, PanelCardSelected,
                () => HeroController.Instance?.SelectAction(TacticalActionMode.Shove),
                out _juiceShove, out _, out _);

            // End Turn Distinct Crimson-Gold Card
            CreateSkillCard("BtnEndTurn", _tacticalBar.transform, "TAB", "FIN TURNO", "PASAR",
                UIArtFactory.IconHourglass, Palette.IvoryCloth, new Vector2(314f, 0f), new Vector2(142f, 118f),
                new Color(0.52f, 0.12f, 0.18f, 0.96f), new Color(0.72f, 0.18f, 0.24f, 0.98f), new Color(0.72f, 0.18f, 0.24f, 0.98f),
                () => HeroController.Instance?.EndPlayerTurn(),
                out _, out _, out _);

            // Exploration Mode Info Card (replaces skill cards during free exploration so the deck never looks empty)
            _explorationCard = MakeChamferPanel("ExplorationCard", deck.transform,
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, PanelCardNavy, false);
            RectTransform expRt = _explorationCard.GetComponent<RectTransform>();
            expRt.offsetMin = new Vector2(588f, -63f);
            expRt.offsetMax = new Vector2(-18f, 63f);

            MakeImage("ExpIcon", _explorationCard.transform, UIArtFactory.IconCrown, Palette.LightningCyan,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(46f, 0f), new Vector2(44f, 44f));

            TextMeshProUGUI expTitle = MakeText("ExpTitle", _explorationCard.transform, "MODO EXPLORACIÓN — EXPLORA EL SANTUARIO",
                18f, Palette.LightningCyan, TextAlignmentOptions.TopLeft, FontStyles.Bold, 2.5f);
            expTitle.rectTransform.offsetMin = new Vector2(88f, 70f);
            expTitle.rectTransform.offsetMax = new Vector2(-20f, -14f);

            TextMeshProUGUI expDesc = MakeText("ExpDesc", _explorationCard.transform,
                "Muévete libremente con WASD o clic izquierdo. Acércate a los cofres dorados para equipar nuevas armas elementales y dispara a los cristales rúnicos para abrir barreras.",
                15f, MutedIvory, TextAlignmentOptions.BottomLeft, FontStyles.Normal, 0.5f);
            expDesc.rectTransform.offsetMin = new Vector2(88f, 14f);
            expDesc.rectTransform.offsetMax = new Vector2(-20f, -48f);
            _explorationCard.SetActive(false);

            // Compact translucent Controls Strip pill tucked right below the deck
            UnityEngine.UI.Image stripPill = MakeImage("ControlsStrip", _hudPanel.transform, UIArtFactory.RoundedPill,
                new Color(0.05f, 0.08f, 0.16f, 0.82f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 5f), new Vector2(1180f, 26f), UnityEngine.UI.Image.Type.Sliced);
            _controlsStripText = MakeText("ControlsText", stripPill.transform,
                "WASD / Clic: Mover   ·   [1-4] Habilidades   ·   [2] Doble clic: Alternar Aceite/Agua   ·   [TAB] Fin de Turno   ·   [ESC] Pausa",
                13.5f, MutedIvory, TextAlignmentOptions.Center, FontStyles.Normal, 1.2f);
        }

        void CreateSkillCard(string name, Transform parent, string keycap, string title, string costTag,
            Sprite iconSprite, Color accentColor, Vector2 pos, Vector2 size,
            Color bgNormal, Color bgHover, Color bgSelected, UnityEngine.Events.UnityAction onClick,
            out UIJuiceButton juiceOut, out TextMeshProUGUI titleLabelOut, out UnityEngine.UI.Image iconImgOut)
        {
            GameObject card = MakeChamferPanel(name, parent,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                pos, size, bgNormal, true, true);

            // Hover / Selected radial glow behind card
            UnityEngine.UI.Image glow = MakeImage("Glow", card.transform, UIArtFactory.RadialGlow,
                new Color(accentColor.r, accentColor.g, accentColor.b, 0f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36f, 36f));
            glow.transform.SetAsFirstSibling();

            // Top Selection Accent Bar (visible only when active)
            UnityEngine.UI.Image selBar = MakeImage("SelectedAccent", card.transform, UIArtFactory.RoundedPill, Palette.FiligreeGold,
                new Vector2(0.16f, 1f), new Vector2(0.84f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -2f), new Vector2(0f, 5f), UnityEngine.UI.Image.Type.Sliced);
            selBar.enabled = false;

            // Keycap Pill top-left
            float keyW = keycap.Length > 1 ? 46f : 26f;
            UnityEngine.UI.Image keyPill = MakeImage("Keycap", card.transform, UIArtFactory.RoundedPill, BarTrackDark,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(9f, -9f), new Vector2(keyW, 22f), UnityEngine.UI.Image.Type.Sliced);
            MakeText("KeyTxt", keyPill.transform, keycap, 12f, Palette.FiligreeGold, TextAlignmentOptions.Center, FontStyles.Bold, 0.5f);

            // Center Vector Icon
            iconImgOut = MakeImage("Icon", card.transform, iconSprite, accentColor,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 16f), new Vector2(46f, 46f));

            // Title Label
            RectTransform titleRt = MakeRect("TitleRt", card.transform,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 25f), new Vector2(-10f, 26f));
            titleLabelOut = MakeText("Title", titleRt, title, 15f, accentColor, TextAlignmentOptions.Center, FontStyles.Bold, 1.5f);

            // Cost Pill bottom
            RectTransform costRt = MakeRect("CostRt", card.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 6f), new Vector2(72f, 18f));
            MakeText("Cost", costRt, costTag, 12f, MutedIvory, TextAlignmentOptions.Center, FontStyles.Normal, 1.5f);

            UnityEngine.UI.Button btn = card.AddComponent<UnityEngine.UI.Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(onClick);

            juiceOut = card.AddComponent<UIJuiceButton>();
            juiceOut.BackgroundImage = card.GetComponent<UnityEngine.UI.Image>();
            juiceOut.GlowImage = glow;
            juiceOut.SelectionAccentBar = selBar;
            juiceOut.NormalBg = bgNormal;
            juiceOut.HoverBg = bgHover;
            juiceOut.SelectedBg = bgSelected;
            juiceOut.GlowColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.55f);
            juiceOut.HoverLiftY = 6f;
            juiceOut.SelectedLiftY = 12f;
            juiceOut.SetBasePosition(pos);
        }

        // ==================== 3. OVERLAY QTE (EXPEDITION 33) ====================

        void BuildQteOverlay(Transform root)
        {
            _qteOverlay = new GameObject("QteOverlay");
            _qteOverlay.transform.SetParent(root, false);
            RectTransform rt = _qteOverlay.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Soft radial center aura
            _qteCenterGlowImg = MakeImage("QteCenterGlow", _qteOverlay.transform, UIArtFactory.RadialGlow,
                new Color(Palette.FiligreeGold.r, Palette.FiligreeGold.g, Palette.FiligreeGold.b, 0.25f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(340f, 340f));

            // Target Golden Ring with 4 cardinal diamonds
            _qteTargetRingImg = MakeImage("TargetRing", _qteOverlay.transform, UIArtFactory.QteTargetRing, Palette.FiligreeGold,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(236f, 236f));

            // Shrinking Dynamic Ring
            _qteDynamicRingImg = MakeImage("DynamicRing", _qteOverlay.transform, UIArtFactory.QteRing, Palette.LightningCyan,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(236f, 236f));
            _qteDynamicRing = _qteDynamicRingImg.rectTransform;

            // Sleek Prompt Pill
            GameObject banner = MakeChamferPanel("QteBanner", _qteOverlay.transform,
                new Vector2(0.5f, 0.30f), new Vector2(0.5f, 0.30f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(820f, 64f), PanelDeepNavy, true);
            _qtePromptText = MakeText("QtePrompt", banner.transform, "¡RESONANCIA! PULSA [ESPACIO / CLIC IZQ.] EN EL ANILLO DORADO",
                21f, Palette.FiligreeGold, TextAlignmentOptions.Center, FontStyles.Bold, 2f);

            _qteOverlay.SetActive(false);
        }

        // ==================== 4. MODAL DE BENDICIONES (BOON DRAFT) ====================

        void BuildDraftModal(Transform root)
        {
            _draftPanel = MakeImage("DraftModal", root, UIArtFactory.Solid, ScrimDark,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                UnityEngine.UI.Image.Type.Simple, true).gameObject;

            // Header Crest
            GameObject header = MakeChamferPanel("DraftHeader", _draftPanel.transform,
                new Vector2(0.5f, 0.83f), new Vector2(0.5f, 0.83f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(860f, 96f), PanelDeepNavy, true);

            TextMeshProUGUI hTitle = MakeText("HeaderLabel", header.transform, "CÁMARA DESPEJADA — ELIGE UNA BENDICIÓN",
                28f, Palette.FiligreeGold, TextAlignmentOptions.Center, FontStyles.Bold, 4f);
            hTitle.rectTransform.offsetMin = new Vector2(16f, 26f);
            MakeDivider(header.transform, new Vector2(0f, -24f), 520f, Palette.FiligreeGold);

            Sprite[] cardIcons = { UIArtFactory.IconLightning, UIArtFactory.IconFlask, UIArtFactory.IconChrono };
            Color[] cardColors = { Palette.LightningCyan, Palette.FireAmber, Palette.FiligreeGold };
            string[] cardTags = { "CONTRAATAQUE", "ALQUIMIA", "CRONOMANCIA" };

            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 365f;
                Vector2 cardPos = new Vector2(x, -26f);
                Color accent = cardColors[i];

                GameObject card = MakeChamferPanel($"BoonCard_{i}", _draftPanel.transform,
                    new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.5f),
                    cardPos, new Vector2(328f, 430f), PanelCardNavy, true, true);

                // Hover glow behind card
                UnityEngine.UI.Image glow = MakeImage("CardGlow", card.transform, UIArtFactory.RadialGlow,
                    new Color(accent.r, accent.g, accent.b, 0f),
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f));
                glow.transform.SetAsFirstSibling();

                // Category Pill at top
                UnityEngine.UI.Image tagPill = MakeImage("CategoryPill", card.transform, UIArtFactory.RoundedPill, BarTrackDark,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -20f), new Vector2(180f, 26f), UnityEngine.UI.Image.Type.Sliced);
                TextMeshProUGUI tagTxt = MakeText("TagTxt", tagPill.transform, cardTags[i], 12f, accent, TextAlignmentOptions.Center, FontStyles.Bold, 3f);

                // Emblem Medallion + Glow
                UnityEngine.UI.Image iconGlow = MakeImage("IconGlow", card.transform, UIArtFactory.RadialGlow,
                    new Color(accent.r, accent.g, accent.b, 0.38f),
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, -104f), new Vector2(130f, 130f));

                UnityEngine.UI.Image iconImg = MakeImage("BoonIcon", card.transform, cardIcons[i], accent,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, -104f), new Vector2(70f, 70f));

                // Title
                RectTransform titleRt = MakeRect("TitleRt", card.transform,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -158f), new Vector2(-24f, 44f));
                TextMeshProUGUI titleTxt = MakeText("BoonTitle", titleRt, "BENDICIÓN", 22f, Palette.FiligreeGold, TextAlignmentOptions.Center, FontStyles.Bold, 2f);

                MakeDivider(card.transform, new Vector2(0f, 5f), 220f, new Color(accent.r, accent.g, accent.b, 0.85f));

                // Description
                RectTransform descRt = MakeRect("DescRt", card.transform,
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 76f), new Vector2(-32f, 128f));
                TextMeshProUGUI descTxt = MakeText("BoonDesc", descRt, "Descripción del efecto.", 18f, Palette.IvoryCloth, TextAlignmentOptions.Center, FontStyles.Normal, 0.5f);

                // Bottom CTA Pill
                UnityEngine.UI.Image ctaPill = MakeImage("SelectPill", card.transform, UIArtFactory.RoundedPill, BarTrackDark,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 22f), new Vector2(226f, 38f), UnityEngine.UI.Image.Type.Sliced);
                MakeImage("SelectPillRim", ctaPill.transform, UIArtFactory.RoundedPillOutline, accent,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, UnityEngine.UI.Image.Type.Sliced);
                MakeText("SelectTag", ctaPill.transform, "SINTONIZAR", 15f, accent, TextAlignmentOptions.Center, FontStyles.Bold, 3f);

                UnityEngine.UI.Button btn = card.AddComponent<UnityEngine.UI.Button>();
                btn.transition = Selectable.Transition.None;

                UIJuiceButton juice = card.AddComponent<UIJuiceButton>();
                juice.BackgroundImage = card.GetComponent<UnityEngine.UI.Image>();
                juice.GlowImage = glow;
                juice.NormalBg = PanelCardNavy;
                juice.HoverBg = PanelCardHover;
                juice.SelectedBg = PanelCardHover;
                juice.GlowColor = new Color(accent.r, accent.g, accent.b, 0.52f);
                juice.HoverScale = 1.045f;
                juice.HoverLiftY = 14f;
                juice.SetBasePosition(cardPos);

                int idx = i;
                btn.onClick.AddListener(() =>
                {
                    var choices = DungeonDirector.Instance?.GetDraftChoices();
                    if (choices != null && idx < choices.Count)
                        DungeonDirector.Instance.PickBoon(choices[idx].Id);
                });

                _draftTags.Add(tagTxt);
                _draftTitles.Add(titleTxt);
                _draftDescs.Add(descTxt);
                _draftIcons.Add(iconImg);
                _draftIconGlows.Add(iconGlow);
            }
            _draftPanel.SetActive(false);
        }

        // ==================== 5. MODAL DE PAUSA ====================

        void BuildPauseModal(Transform root)
        {
            _pausePanel = MakeImage("PausePanel", root, UIArtFactory.Solid, ScrimDark,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                UnityEngine.UI.Image.Type.Simple, true).gameObject;

            GameObject box = MakeChamferPanel("PauseBox", _pausePanel.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(560f, 480f), PanelDeepNavy, true);

            RectTransform hdrRt = MakeRect("PauseHdrRt", box.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -24f), new Vector2(-32f, 46f));
            MakeText("PauseHdr", hdrRt, "EXPEDICIÓN EN PAUSA", 30f, Palette.FiligreeGold, TextAlignmentOptions.Center, FontStyles.Bold, 4f);

            MakeDivider(box.transform, new Vector2(0f, 158f), 380f, Palette.FiligreeGold);

            // Music Slider Row
            RectTransform mRow = MakeRect("MusicLabelRow", box.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 102f), new Vector2(420f, 28f));
            MakeText("MusicLabel", mRow, "VOLUMEN DE MÚSICA", 16f, Palette.IvoryCloth, TextAlignmentOptions.Left, FontStyles.Bold, 2f);
            _musicValText = MakeText("MusicPct", mRow, "55%", 16f, Palette.FiligreeGold, TextAlignmentOptions.Right, FontStyles.Bold, 1f);

            _musicSlider = MakeSlider("MusicSlider", box.transform, new Vector2(0f, 66f), 0.55f);
            _musicSlider.onValueChanged.AddListener(v =>
            {
                SynthAudioManager.Instance?.SetMusicVolume(v);
                if (_musicValText != null) _musicValText.text = $"{Mathf.RoundToInt(v * 100f)}%";
            });

            // SFX Slider Row
            RectTransform sRow = MakeRect("SfxLabelRow", box.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 10f), new Vector2(420f, 28f));
            MakeText("SfxLabel", sRow, "VOLUMEN DE EFECTOS", 16f, Palette.IvoryCloth, TextAlignmentOptions.Left, FontStyles.Bold, 2f);
            _sfxValText = MakeText("SfxPct", sRow, "85%", 16f, Palette.LightningCyan, TextAlignmentOptions.Right, FontStyles.Bold, 1f);

            _sfxSlider = MakeSlider("SfxSlider", box.transform, new Vector2(0f, -26f), 0.85f);
            _sfxSlider.onValueChanged.AddListener(v =>
            {
                SynthAudioManager.Instance?.SetSFXVolume(v);
                if (_sfxValText != null) _sfxValText.text = $"{Mathf.RoundToInt(v * 100f)}%";
            });

            UnityEngine.UI.Button resumeBtn = MakeActionButton("ResumeBtn", box.transform, "REANUDAR EXPEDICIÓN",
                new Vector2(0f, -110f), new Vector2(360f, 56f), PanelCardNavy, PanelCardHover, Palette.LightningCyan, Palette.LightningCyan, out _);
            resumeBtn.onClick.AddListener(() => GameLoop.Instance?.TogglePause());

            UnityEngine.UI.Button restartBtn = MakeActionButton("RestartBtn", box.transform, "REINICIAR EXPEDICIÓN",
                new Vector2(0f, -180f), new Vector2(360f, 56f),
                new Color(0.50f, 0.12f, 0.18f, 0.96f), new Color(0.70f, 0.18f, 0.24f, 0.98f),
                Palette.IvoryCloth, Palette.HeroCrimson, out _);
            restartBtn.onClick.AddListener(() => GameLoop.Instance?.RestartExpedition());

            _pausePanel.SetActive(false);
        }

        UnityEngine.UI.Slider MakeSlider(string name, Transform parent, Vector2 anchoredPos, float initial)
        {
            UnityEngine.UI.Image track = MakeImage(name, parent, UIArtFactory.RoundedPill, BarTrackDark,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                anchoredPos, new Vector2(420f, 22f), UnityEngine.UI.Image.Type.Sliced, true);

            MakeImage("TrackBorder", track.transform, UIArtFactory.RoundedPillOutline,
                new Color(Palette.FiligreeGold.r, Palette.FiligreeGold.g, Palette.FiligreeGold.b, 0.45f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, UnityEngine.UI.Image.Type.Sliced);

            UnityEngine.UI.Slider slider = track.gameObject.AddComponent<UnityEngine.UI.Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = initial;

            RectTransform fillArea = MakeRect("FillArea", track.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-8f, -8f));
            UnityEngine.UI.Image fill = MakeImage("Fill", fillArea, UIArtFactory.GradientBar, Palette.FiligreeGold,
                Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);

            RectTransform handleArea = MakeRect("HandleArea", track.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-16f, 0f));
            UnityEngine.UI.Image handle = MakeImage("Handle", handleArea, UIArtFactory.DiamondGem, Palette.LightningCyan,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(30f, 30f), UnityEngine.UI.Image.Type.Simple, true);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            return slider;
        }

        // ==================== 6. PANTALLA DE VICTORIA / DERROTA ====================

        void BuildEndScreen(Transform root)
        {
            _endPanel = MakeImage("EndPanel", root, UIArtFactory.Solid, ScrimDark,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                UnityEngine.UI.Image.Type.Simple, true).gameObject;

            GameObject box = MakeChamferPanel("EndBox", _endPanel.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(680f, 500f), PanelDeepNavy, true);

            _endIconImg = MakeImage("EndEmblem", box.transform, UIArtFactory.IconCrown, Palette.FiligreeGold,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -46f), new Vector2(56f, 56f));

            RectTransform titleRt = MakeRect("EndTitleRt", box.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -76f), new Vector2(-36f, 52f));
            _endTitleText = MakeText("EndTitle", titleRt, "¡SANTUARIO CONQUISTADO!", 36f, Palette.FiligreeGold, TextAlignmentOptions.Center, FontStyles.Bold, 4f);

            RectTransform subRt = MakeRect("EndSubRt", box.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -124f), new Vector2(-48f, 28f));
            _endSubtitleText = MakeText("EndSub", subRt, "RESUMEN DE LA EXPEDICIÓN", 15f, MutedIvory, TextAlignmentOptions.Center, FontStyles.Bold, 3f);

            MakeDivider(box.transform, new Vector2(0f, 92f), 460f, Palette.FiligreeGold);

            // Structured 2-column stats panel
            GameObject statsCard = MakeChamferPanel("EndStatsCard", box.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -18f), new Vector2(540f, 200f), PanelCardNavy, false);

            RectTransform leftCol = MakeRect("StatsLeft", statsCard.transform,
                new Vector2(0f, 0f), new Vector2(0.68f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, Vector2.zero);
            _endStatsLeftText = MakeText("LeftTxt", leftCol, "", 19f, MutedIvory, TextAlignmentOptions.Left, FontStyles.Normal, 1f);
            _endStatsLeftText.rectTransform.offsetMin = new Vector2(26f, 16f);
            _endStatsLeftText.lineSpacing = 12f;

            RectTransform rightCol = MakeRect("StatsRight", statsCard.transform,
                new Vector2(0.68f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                Vector2.zero, Vector2.zero);
            _endStatsRightText = MakeText("RightTxt", rightCol, "", 19f, Palette.FiligreeGold, TextAlignmentOptions.Right, FontStyles.Bold, 1f);
            _endStatsRightText.rectTransform.offsetMax = new Vector2(-26f, -16f);
            _endStatsRightText.lineSpacing = 12f;

            UnityEngine.UI.Button playAgain = MakeActionButton("PlayAgainBtn", box.transform, "NUEVA EXPEDICIÓN",
                new Vector2(0f, -176f), new Vector2(340f, 60f),
                new Color(0.75f, 0.56f, 0.14f, 0.98f), new Color(0.95f, 0.74f, 0.22f, 1f),
                new Color(0.06f, 0.08f, 0.16f, 1f), Palette.FiligreeGold, out _);
            playAgain.onClick.AddListener(() => GameLoop.Instance?.RestartExpedition());

            _endPanel.SetActive(false);
        }

        // ==================== STATE & DATA BINDING ====================

        void HandleStateChanged(GameState state)
        {
            _titlePanel?.SetActive(state == GameState.Title);
            bool inGame = state == GameState.Exploring || state == GameState.TacticalPlayerTurn ||
                          state == GameState.ActionExecutionQTE || state == GameState.EnemyTurnDefense;
            _hudPanel?.SetActive(inGame);
            _draftPanel?.SetActive(state == GameState.ChamberClearDraft);
            _pausePanel?.SetActive(state == GameState.Paused);
            _endPanel?.SetActive(state == GameState.Won || state == GameState.Lost);

            if (state == GameState.ChamberClearDraft && DungeonDirector.Instance != null)
            {
                var choices = DungeonDirector.Instance.GetDraftChoices();
                for (int i = 0; i < 3 && i < choices.Count; i++)
                {
                    _draftTitles[i].text = choices[i].Title;
                    _draftDescs[i].text = choices[i].Description;
                }
            }

            if (state == GameState.Won || state == GameState.Lost)
            {
                bool won = state == GameState.Won;
                _endTitleText.text = won ? "¡SANTUARIO CONQUISTADO!" : "EXPEDICIÓN CAÍDA";
                _endTitleText.color = won ? Palette.FiligreeGold : Palette.HeroCrimson;
                _endIconImg.color = won ? Palette.FiligreeGold : Palette.HeroCrimson;
                _endSubtitleText.text = won ? "HAS DOMINADO LA ALQUIMIA DEL SANTUARIO" : "EL ECLIPSE HA RECLAMADO A TU EXPEDICIONARIO";

                GameLoop loop = GameLoop.Instance;
                _endStatsLeftText.text =
                    "Cámara alcanzada\n" +
                    "Nivel del héroe\n" +
                    "Parries perfectos\n" +
                    "Resonancias perfectas\n" +
                    "Combos elementales\n" +
                    "Enemigos derrotados";

                _endStatsRightText.text =
                    $"{loop.CurrentChamber} / {loop.MaxChambers}\n" +
                    $"NV {loop.Level}\n" +
                    $"{loop.ParriesLanded}\n" +
                    $"{loop.PerfectResonances}\n" +
                    $"{loop.ElementalCombos}\n" +
                    $"{loop.EnemiesDefeated}";
            }

            RefreshStats();
        }

        static string GetActionSpanishName(TacticalActionMode mode, bool flaskOil)
        {
            switch (mode)
            {
                case TacticalActionMode.Strike: return "GOLPE";
                case TacticalActionMode.Flask: return flaskOil ? "FRASCO DE ACEITE" : "FRASCO DE AGUA";
                case TacticalActionMode.FreeAimShot: return "DISPARO PRECISO";
                case TacticalActionMode.Shove: return "EMPUJÓN";
                default: return "MOVER O ELEGIR ACCIÓN";
            }
        }

        void RefreshStats()
        {
            if (GameLoop.Instance == null || HeroController.Instance == null) return;
            GameLoop loop = GameLoop.Instance;
            HeroController hero = HeroController.Instance;

            string chName = loop.CurrentChamber == 1 ? "EL PATIO QUEBRADO" :
                (loop.CurrentChamber == 2 ? "SALA INUNDADA DEL ALQUIMISTA" : "TRONO DEL ECLIPSE");
            if (_chamberHeaderText != null)
                _chamberHeaderText.text = $"CÁMARA {loop.CurrentChamber}/{loop.MaxChambers} · {chName}";

            if (_turnBannerText != null)
            {
                Color accent = Palette.FiligreeGold;
                switch (loop.State)
                {
                    case GameState.Exploring:
                        _turnBannerText.text = "EXPLORACIÓN — ABRE COFRES Y AVANZA";
                        accent = Palette.LightningCyan;
                        break;
                    case GameState.TacticalPlayerTurn:
                        string act = GetActionSpanishName(hero.ActiveAction, hero.FlaskThrowsOil);
                        _turnBannerText.text = $"TU TURNO — {act}  ({hero.RemainingMoveDist:F1}m)";
                        accent = Palette.FiligreeGold;
                        break;
                    case GameState.ActionExecutionQTE:
                        _turnBannerText.text = "¡EJECUCIÓN DE RESONANCIA ACTIVA!";
                        accent = Palette.FireAmber;
                        break;
                    case GameState.EnemyTurnDefense:
                        _turnBannerText.text = "¡TURNO ENEMIGO — DEFIÉNDETE EN TIEMPO REAL!";
                        accent = Palette.HeroCrimson;
                        break;
                }
                _turnBannerText.color = accent;
                if (_turnAccentLine != null) _turnAccentLine.color = accent;
            }

            if (_tacticalBar != null)
            {
                bool showBar = loop.State == GameState.TacticalPlayerTurn ||
                               loop.State == GameState.ActionExecutionQTE ||
                               loop.State == GameState.EnemyTurnDefense;
                _tacticalBar.SetActive(showBar);
                if (_explorationCard != null)
                    _explorationCard.SetActive(!showBar);
            }

            if (_hpValueText != null)
                _hpValueText.text = $"{hero.CurrentHP} / {hero.MaxHP} PV";
            if (_levelBadgeText != null)
                _levelBadgeText.text = loop.Level.ToString();

            Color elemColor = hero.CurrentElement == ElementType.Fire ? Palette.FireAmber :
                (hero.CurrentElement == ElementType.Lightning ? Palette.LightningCyan : Palette.FrostIce);
            if (_weaponText != null)
            {
                _weaponText.text = hero.GetWeaponDisplayName();
                _weaponText.color = elemColor;
            }
            if (_weaponDotImg != null)
                _weaponDotImg.color = elemColor;

            if (_apValueText != null)
                _apValueText.text = $"PA  {hero.CurrentAP} / {hero.MaxAP}";

            for (int i = 0; i < _apPips.Count; i++)
            {
                bool active = i < hero.CurrentAP;
                _apPips[i].color = active ? Palette.LightningCyan : BarTrackDark;
                if (i < _apGlows.Count)
                    _apGlows[i].enabled = active;
            }

            if (_btnFlaskLabel != null)
            {
                Color flaskCol = hero.FlaskThrowsOil ? Palette.FireAmber : Palette.WaterAzure;
                _btnFlaskLabel.text = hero.FlaskThrowsOil ? "ACEITE" : "AGUA";
                _btnFlaskLabel.color = flaskCol;
                if (_btnFlaskIcon != null) _btnFlaskIcon.color = flaskCol;
            }

            _juiceStrike?.SetSelected(hero.ActiveAction == TacticalActionMode.Strike);
            _juiceFlask?.SetSelected(hero.ActiveAction == TacticalActionMode.Flask);
            _juiceShot?.SetSelected(hero.ActiveAction == TacticalActionMode.FreeAimShot);
            _juiceShove?.SetSelected(hero.ActiveAction == TacticalActionMode.Shove);
        }

        void Update()
        {
            float t = Time.unscaledTime;

            // Title screen subtle breathing aura
            if (GameLoop.Instance != null && GameLoop.Instance.State == GameState.Title)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(t * 2.8f);
                if (_titleSubPromptText != null)
                {
                    Color c = Palette.LightningCyan;
                    c.a = Mathf.Lerp(0.45f, 1f, pulse);
                    _titleSubPromptText.color = c;
                }
                if (_titleCtaGlow != null)
                {
                    Color g = Palette.LightningCyan;
                    g.a = Mathf.Lerp(0.18f, 0.42f, pulse);
                    _titleCtaGlow.color = g;
                }
                if (_titleGlowImg != null)
                {
                    Color tg = Palette.FiligreeGold;
                    tg.a = Mathf.Lerp(0.18f, 0.30f, 0.5f + 0.5f * Mathf.Sin(t * 1.6f));
                    _titleGlowImg.color = tg;
                }
            }

            // Smooth animated HP bar + delayed catch-up damage trail + XP bar
            if (HeroController.Instance != null && GameLoop.Instance != null)
            {
                float targetHp = Mathf.Clamp01(HeroController.Instance.CurrentHP / (float)Mathf.Max(1, HeroController.Instance.MaxHP));
                float targetXp = Mathf.Clamp01(GameLoop.Instance.CurrentXP / (float)Mathf.Max(1, GameLoop.Instance.XPToNextLevel));

                _displayedHpFill = Mathf.MoveTowards(_displayedHpFill, targetHp, Time.unscaledDeltaTime * 3.2f);
                if (_trailHpFill < _displayedHpFill)
                    _trailHpFill = _displayedHpFill;
                else
                    _trailHpFill = Mathf.MoveTowards(_trailHpFill, _displayedHpFill, Time.unscaledDeltaTime * 0.75f);

                _displayedXpFill = Mathf.MoveTowards(_displayedXpFill, targetXp, Time.unscaledDeltaTime * 2.0f);

                if (_hpFillImg != null) _hpFillImg.fillAmount = _displayedHpFill;
                if (_hpTrailFillImg != null) _hpTrailFillImg.fillAmount = _trailHpFill;
                if (_xpFillImg != null) _xpFillImg.fillAmount = _displayedXpFill;

                // Subtle pulse on active AP crystal glows
                float apPulse = 0.32f + 0.16f * Mathf.Sin(t * 3.5f);
                for (int i = 0; i < _apGlows.Count; i++)
                {
                    if (_apGlows[i].enabled)
                    {
                        Color gc = Palette.LightningCyan;
                        gc.a = apPulse;
                        _apGlows[i].color = gc;
                    }
                }
            }

            // Update Expedition 33 QTE Rings
            if (_qteOverlay != null && ExpeditionQTEController.Instance != null)
            {
                var qte = ExpeditionQTEController.Instance;
                bool active = qte.IsAttackQTEActive || qte.IsDefenseQTEActive;
                _qteOverlay.SetActive(active);

                if (active)
                {
                    _qtePromptText.text = qte.ActiveBannerText;
                    if (qte.IsAttackQTEActive)
                    {
                        bool inSweetSpot = qte.ResonanceRingScale >= 0.78f && qte.ResonanceRingScale <= 1.28f;
                        _qteTargetRingImg.color = inSweetSpot ? Color.white : Palette.FiligreeGold;
                        _qteDynamicRingImg.color = inSweetSpot ? Palette.FireAmber : Palette.LightningCyan;
                        _qteDynamicRing.localScale = Vector3.one * qte.ResonanceRingScale;
                        if (_qteCenterGlowImg != null)
                            _qteCenterGlowImg.color = inSweetSpot
                                ? new Color(Palette.FireAmber.r, Palette.FireAmber.g, Palette.FireAmber.b, 0.45f)
                                : new Color(Palette.FiligreeGold.r, Palette.FiligreeGold.g, Palette.FiligreeGold.b, 0.22f);
                    }
                    else
                    {
                        bool inParry = qte.DefenseWindupProgress >= 0.62f;
                        _qteTargetRingImg.color = inParry ? Palette.LightningCyan : Palette.HeroCrimson;
                        _qteDynamicRingImg.color = inParry ? Palette.LightningCyan : Palette.IvoryCloth;
                        float s = Mathf.Lerp(2.1f, 0.85f, qte.DefenseWindupProgress);
                        _qteDynamicRing.localScale = Vector3.one * s;
                        if (_qteCenterGlowImg != null)
                            _qteCenterGlowImg.color = inParry
                                ? new Color(Palette.LightningCyan.r, Palette.LightningCyan.g, Palette.LightningCyan.b, 0.45f)
                                : new Color(Palette.HeroCrimson.r, Palette.HeroCrimson.g, Palette.HeroCrimson.b, 0.26f);
                    }
                }
            }
        }
    }
}
