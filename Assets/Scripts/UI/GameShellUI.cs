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
    /// Complete Game Shell & Tactical HUD (ScreenSpaceCamera uGUI + TextMeshPro).
    /// Styled strictly from Palette.cs with animated bars, AP pips, Expedition 33 QTE rings,
    /// Roguelite Boon Draft cards, Pause Settings (MusicVol/SFXVol), and End Screen.
    /// </summary>
    public class GameShellUI : MonoBehaviour
    {
        public static GameShellUI Instance { get; private set; }

        Canvas _canvas;
        Sprite _solidSprite;
        Sprite _ringSprite;

        GameObject _titlePanel;
        TextMeshProUGUI _titlePromptText;

        GameObject _hudPanel;
        TextMeshProUGUI _chamberHeaderText;
        TextMeshProUGUI _turnBannerText;
        TextMeshProUGUI _objectiveHintText;

        Image _hpFillImg;
        TextMeshProUGUI _hpValueText;
        Image _xpFillImg;
        TextMeshProUGUI _levelText;
        TextMeshProUGUI _weaponText;
        TextMeshProUGUI _apValueText;
        readonly List<Image> _apPips = new List<Image>();

        GameObject _tacticalBar;
        Button _btnStrike;
        Button _btnFlask;
        TextMeshProUGUI _btnFlaskLabel;
        Button _btnShot;
        Button _btnShove;
        Button _btnEndTurn;

        GameObject _qteOverlay;
        RectTransform _qteDynamicRing;
        Image _qteDynamicRingImg;
        Image _qteTargetRingImg;
        TextMeshProUGUI _qtePromptText;

        GameObject _draftPanel;
        readonly List<Button> _draftButtons = new List<Button>();
        readonly List<TextMeshProUGUI> _draftTitles = new List<TextMeshProUGUI>();
        readonly List<TextMeshProUGUI> _draftDescs = new List<TextMeshProUGUI>();

        GameObject _pausePanel;
        Slider _musicSlider;
        Slider _sfxSlider;

        GameObject _endPanel;
        TextMeshProUGUI _endTitleText;
        TextMeshProUGUI _endStatsText;

        float _displayedHpFill = 1f;
        float _displayedXpFill = 0f;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            CreateProceduralSprites();
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

        void CreateProceduralSprites()
        {
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color[] cols = new Color[16];
            for (int i = 0; i < 16; i++) cols[i] = Color.white;
            tex.SetPixels(cols);
            tex.Apply();
            _solidSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));

            const int size = 256;
            Texture2D ringTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = (dist >= 0.80f && dist <= 0.96f) ? 1f : 0f;
                    if (dist >= 0.76f && dist < 0.80f) alpha = Mathf.InverseLerp(0.76f, 0.80f, dist);
                    else if (dist > 0.96f && dist <= 1.0f) alpha = Mathf.InverseLerp(1.0f, 0.96f, dist);
                    ringTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            ringTex.Apply();
            _ringSprite = Sprite.Create(ringTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
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

        GameObject MakePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, Color bgColor, bool addBorder = true)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            Image img = go.AddComponent<Image>();
            img.sprite = _solidSprite;
            img.color = bgColor;
            img.raycastTarget = false;

            if (addBorder)
            {
                Outline outline = go.AddComponent<Outline>();
                outline.effectColor = Palette.FiligreeGold;
                outline.effectDistance = new Vector2(2f, -2f);
            }
            return go;
        }

        TextMeshProUGUI MakeText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center, FontStyles style = FontStyles.Bold)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(10f, 4f);
            rt.offsetMax = new Vector2(-10f, -4f);

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.fontStyle = style;
            tmp.characterSpacing = 3f;
            tmp.outlineWidth = 0.2f;
            tmp.outlineColor = Palette.AbyssShadow;
            tmp.raycastTarget = false;
            return tmp;
        }

        Button MakeButton(string name, Transform parent, string label, Vector2 anchoredPos, Vector2 size, Color bgColor, Color textColor, out TextMeshProUGUI labelOut)
        {
            GameObject go = MakePanel(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPos, size, bgColor, true);
            Image img = go.GetComponent<Image>();
            img.raycastTarget = true;

            Button btn = go.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.18f, 1.12f, 1.0f, 1f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            btn.colors = cb;

            labelOut = MakeText("Label", go.transform, label, 20f, textColor);
            return btn;
        }

        void BuildTitleScreen(Transform root)
        {
            _titlePanel = MakePanel("TitlePanel", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.04f, 0.05f, 0.09f, 0.35f), false);

            GameObject card = MakePanel("TitleHeader", _titlePanel.transform, new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 220f), Palette.UiPanelBg, true);
            TextMeshProUGUI mainTitle = MakeText("Title", card.transform, "AETHERFALL", 76f, Palette.FiligreeGold);
            mainTitle.characterSpacing = 12f;
            RectTransform mRt = mainTitle.rectTransform;
            mRt.offsetMin = new Vector2(10f, 68f);

            TextMeshProUGUI sub = MakeText("Subtitle", card.transform, "TACTICAL EXPEDITION  ·  ELEMENTAL ALCHEMY  ·  SANCTUM ROGUELITE", 22f, Palette.IvoryCloth);
            sub.rectTransform.offsetMax = new Vector2(-10f, -125f);

            GameObject promptBox = MakePanel("TitlePromptBox", _titlePanel.transform, new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 70f), Palette.UiPanelBg, true);
            _titlePromptText = MakeText("Prompt", promptBox.transform, "PRESS ANY KEY OR CLICK TO ENTER THE SANCTUM", 24f, Palette.LightningCyan);

            Button enterBtn = promptBox.AddComponent<Button>();
            promptBox.GetComponent<Image>().raycastTarget = true;
            enterBtn.onClick.AddListener(() => GameLoop.Instance?.StartExpedition());
        }

        void BuildHud(Transform root)
        {
            _hudPanel = new GameObject("HudPanel");
            _hudPanel.transform.SetParent(root, false);
            RectTransform hudRt = _hudPanel.AddComponent<RectTransform>();
            hudRt.anchorMin = Vector2.zero;
            hudRt.anchorMax = Vector2.one;
            hudRt.offsetMin = Vector2.zero;
            hudRt.offsetMax = Vector2.zero;

            // Top Banner
            GameObject topBar = MakePanel("TopBanner", _hudPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(1040f, 108f), Palette.UiPanelBg, true);
            _chamberHeaderText = MakeText("ChamberText", topBar.transform, "CHAMBER 1 / 3  ·  THE SHATTERED COURTYARD", 20f, Palette.FiligreeGold, TextAlignmentOptions.Top);
            _chamberHeaderText.rectTransform.offsetMin = new Vector2(16f, 66f);

            _turnBannerText = MakeText("TurnText", topBar.transform, "YOUR TURN — MOVE & SELECT ACTION", 27f, Palette.IvoryCloth, TextAlignmentOptions.Center);
            _turnBannerText.rectTransform.offsetMin = new Vector2(16f, 28f);
            _turnBannerText.rectTransform.offsetMax = new Vector2(-16f, -34f);

            _objectiveHintText = MakeText("HintText", topBar.transform, "WASD / Click: Move  |  [1] Strike  [2] Oil/Water Flask  [3] Shot  [4] Shove  |  [ESC] Pause", 16f, Palette.LightningCyan, TextAlignmentOptions.Bottom);
            _objectiveHintText.rectTransform.offsetMax = new Vector2(-16f, -68f);

            // Pause button top-right
            Button pauseBtn = MakeButton("PauseBtn", _hudPanel.transform, "PAUSE [ESC]", Vector2.zero, new Vector2(170f, 46f), Palette.UiPanelBg, Palette.IvoryCloth, out _);
            RectTransform pRt = pauseBtn.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(1f, 1f);
            pRt.anchorMax = new Vector2(1f, 1f);
            pRt.pivot = new Vector2(1f, 1f);
            pRt.anchoredPosition = new Vector2(-24f, -24f);
            pauseBtn.onClick.AddListener(() => GameLoop.Instance?.TogglePause());

            // Bottom Tactical Bar
            GameObject bottomPanel = MakePanel("BottomBar", _hudPanel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(1380f, 152f), Palette.UiPanelBg, true);

            // Left Stats Column (HP, XP, Level, Weapon)
            GameObject statsBox = MakePanel("StatsBox", bottomPanel.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(360f, 124f), Palette.UiPanelInner, true);

            _levelText = MakeText("LevelText", statsBox.transform, "LVL 1 EXPEDITIONER", 18f, Palette.FiligreeGold, TextAlignmentOptions.TopLeft);
            _levelText.rectTransform.offsetMin = new Vector2(12f, 88f);

            // HP Bar
            GameObject hpBg = MakePanel("HpBg", statsBox.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(-24f, 26f), Palette.AbyssShadow, true);
            GameObject hpFill = new GameObject("HpFill");
            hpFill.transform.SetParent(hpBg.transform, false);
            RectTransform hpFillRt = hpFill.AddComponent<RectTransform>();
            hpFillRt.anchorMin = Vector2.zero;
            hpFillRt.anchorMax = Vector2.one;
            hpFillRt.offsetMin = new Vector2(2f, 2f);
            hpFillRt.offsetMax = new Vector2(-2f, -2f);
            _hpFillImg = hpFill.AddComponent<Image>();
            _hpFillImg.sprite = _solidSprite;
            _hpFillImg.type = Image.Type.Filled;
            _hpFillImg.fillMethod = Image.FillMethod.Horizontal;
            _hpFillImg.color = Palette.HeroCrimson;
            _hpFillImg.raycastTarget = false;
            _hpValueText = MakeText("HpVal", hpBg.transform, "120 / 120 HP", 15f, Palette.IvoryCloth);

            // XP Bar
            GameObject xpBg = MakePanel("XpBg", statsBox.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(-24f, 16f), Palette.AbyssShadow, true);
            GameObject xpFill = new GameObject("XpFill");
            xpFill.transform.SetParent(xpBg.transform, false);
            RectTransform xpFillRt = xpFill.AddComponent<RectTransform>();
            xpFillRt.anchorMin = Vector2.zero;
            xpFillRt.anchorMax = Vector2.one;
            xpFillRt.offsetMin = new Vector2(2f, 2f);
            xpFillRt.offsetMax = new Vector2(-2f, -2f);
            _xpFillImg = xpFill.AddComponent<Image>();
            _xpFillImg.sprite = _solidSprite;
            _xpFillImg.type = Image.Type.Filled;
            _xpFillImg.fillMethod = Image.FillMethod.Horizontal;
            _xpFillImg.color = Palette.FiligreeGold;
            _xpFillImg.raycastTarget = false;

            _weaponText = MakeText("WeaponText", statsBox.transform, "Solar Greatsword (Fire)", 16f, Palette.FireAmber, TextAlignmentOptions.BottomLeft);
            _weaponText.rectTransform.offsetMax = new Vector2(-12f, -94f);

            // AP Crystals Display
            GameObject apBox = MakePanel("ApBox", bottomPanel.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(394f, 0f), new Vector2(180f, 124f), Palette.UiPanelInner, true);
            _apValueText = MakeText("ApHeader", apBox.transform, "AP: 3 / 6", 20f, Palette.LightningCyan, TextAlignmentOptions.Top);
            _apValueText.rectTransform.offsetMin = new Vector2(6f, 80f);

            for (int i = 0; i < 6; i++)
            {
                float x = -56f + (i % 3) * 56f;
                float y = (i < 3) ? -2f : -38f;
                GameObject pip = MakePanel($"ApPip_{i}", apBox.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(26f, 26f), Palette.LightningCyan, true);
                _apPips.Add(pip.GetComponent<Image>());
            }

            // Tactical Skill Buttons Bar
            _tacticalBar = new GameObject("TacticalButtons");
            _tacticalBar.transform.SetParent(bottomPanel.transform, false);
            RectTransform tbRt = _tacticalBar.AddComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0f, 0.5f);
            tbRt.anchorMax = new Vector2(1f, 0.5f);
            tbRt.offsetMin = new Vector2(590f, -58f);
            tbRt.offsetMax = new Vector2(-18f, 58f);

            _btnStrike = MakeButton("BtnStrike", _tacticalBar.transform, "[1] STRIKE\n1 AP", new Vector2(-300f, 0f), new Vector2(148f, 108f), Palette.UiPanelInner, Palette.FiligreeGold, out _);
            _btnFlask = MakeButton("BtnFlask", _tacticalBar.transform, "[2] OIL FLASK\n1 AP", new Vector2(-145f, 0f), new Vector2(148f, 108f), Palette.UiPanelInner, Palette.FireAmber, out _btnFlaskLabel);
            _btnShot = MakeButton("BtnShot", _tacticalBar.transform, "[3] AIM SHOT\n1 AP", new Vector2(10f, 0f), new Vector2(148f, 108f), Palette.UiPanelInner, Palette.LightningCyan, out _);
            _btnShove = MakeButton("BtnShove", _tacticalBar.transform, "[4] SHOVE\n1 AP", new Vector2(165f, 0f), new Vector2(148f, 108f), Palette.UiPanelInner, Palette.IvoryCloth, out _);
            _btnEndTurn = MakeButton("BtnEndTurn", _tacticalBar.transform, "END TURN\n[TAB]", new Vector2(315f, 0f), new Vector2(138f, 108f), Palette.HeroCrimson, Palette.IvoryCloth, out _);

            _btnStrike.onClick.AddListener(() => HeroController.Instance?.SelectAction(TacticalActionMode.Strike));
            _btnFlask.onClick.AddListener(() => HeroController.Instance?.SelectAction(TacticalActionMode.Flask));
            _btnShot.onClick.AddListener(() => HeroController.Instance?.SelectAction(TacticalActionMode.FreeAimShot));
            _btnShove.onClick.AddListener(() => HeroController.Instance?.SelectAction(TacticalActionMode.Shove));
            _btnEndTurn.onClick.AddListener(() => HeroController.Instance?.EndPlayerTurn());
        }

        void BuildQteOverlay(Transform root)
        {
            _qteOverlay = new GameObject("QteOverlay");
            _qteOverlay.transform.SetParent(root, false);
            RectTransform rt = _qteOverlay.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            GameObject targetRing = new GameObject("TargetRing");
            targetRing.transform.SetParent(_qteOverlay.transform, false);
            RectTransform tRt = targetRing.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.5f, 0.5f);
            tRt.anchorMax = new Vector2(0.5f, 0.5f);
            tRt.sizeDelta = new Vector2(220f, 220f);
            _qteTargetRingImg = targetRing.AddComponent<Image>();
            _qteTargetRingImg.sprite = _ringSprite;
            _qteTargetRingImg.color = Palette.FiligreeGold;
            _qteTargetRingImg.raycastTarget = false;

            GameObject dynRing = new GameObject("DynamicRing");
            dynRing.transform.SetParent(_qteOverlay.transform, false);
            _qteDynamicRing = dynRing.AddComponent<RectTransform>();
            _qteDynamicRing.anchorMin = new Vector2(0.5f, 0.5f);
            _qteDynamicRing.anchorMax = new Vector2(0.5f, 0.5f);
            _qteDynamicRing.sizeDelta = new Vector2(220f, 220f);
            _qteDynamicRingImg = dynRing.AddComponent<Image>();
            _qteDynamicRingImg.sprite = _ringSprite;
            _qteDynamicRingImg.color = Palette.LightningCyan;
            _qteDynamicRingImg.raycastTarget = false;

            GameObject banner = MakePanel("QteBanner", _qteOverlay.transform, new Vector2(0.5f, 0.34f), new Vector2(0.5f, 0.34f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 64f), Palette.UiPanelBg, true);
            _qtePromptText = MakeText("QtePrompt", banner.transform, "PRESS [SPACE / LMB] ON GOLDEN RING!", 24f, Palette.FiligreeGold);

            _qteOverlay.SetActive(false);
        }

        void BuildDraftModal(Transform root)
        {
            _draftPanel = MakePanel("DraftModal", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Palette.UiScrim, false);
            _draftPanel.GetComponent<Image>().raycastTarget = true;

            GameObject header = MakePanel("DraftHeader", _draftPanel.transform, new Vector2(0.5f, 0.80f), new Vector2(0.5f, 0.80f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880f, 90f), Palette.UiPanelBg, true);
            MakeText("HeaderLabel", header.transform, "CHAMBER CLEARED — CHOOSE A ROGUELITE BOON", 32f, Palette.FiligreeGold);

            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 340f;
                GameObject card = MakePanel($"BoonCard_{i}", _draftPanel.transform, new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(300f, 360f), Palette.UiPanelBg, true);
                card.GetComponent<Image>().raycastTarget = true;
                Button btn = card.AddComponent<Button>();

                TextMeshProUGUI titleTxt = MakeText("BoonTitle", card.transform, "BOON", 24f, Palette.FiligreeGold, TextAlignmentOptions.Top);
                titleTxt.rectTransform.offsetMin = new Vector2(18f, 240f);

                TextMeshProUGUI descTxt = MakeText("BoonDesc", card.transform, "Effect description.", 20f, Palette.IvoryCloth, TextAlignmentOptions.Center, FontStyles.Normal);
                descTxt.rectTransform.offsetMin = new Vector2(18f, 70f);
                descTxt.rectTransform.offsetMax = new Vector2(-18f, -100f);

                MakeText("SelectTag", card.transform, "[ CLICK TO ATTUNE ]", 16f, Palette.LightningCyan, TextAlignmentOptions.Bottom);

                int idx = i;
                btn.onClick.AddListener(() =>
                {
                    var choices = DungeonDirector.Instance?.GetDraftChoices();
                    if (choices != null && idx < choices.Count)
                        DungeonDirector.Instance.PickBoon(choices[idx].Id);
                });

                _draftButtons.Add(btn);
                _draftTitles.Add(titleTxt);
                _draftDescs.Add(descTxt);
            }
            _draftPanel.SetActive(false);
        }

        void BuildPauseModal(Transform root)
        {
            _pausePanel = MakePanel("PausePanel", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Palette.UiScrim, false);
            _pausePanel.GetComponent<Image>().raycastTarget = true;

            GameObject box = MakePanel("PauseBox", _pausePanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540f, 460f), Palette.UiPanelBg, true);
            TextMeshProUGUI hdr = MakeText("PauseHdr", box.transform, "EXPEDITION PAUSED", 34f, Palette.FiligreeGold, TextAlignmentOptions.Top);
            hdr.rectTransform.offsetMin = new Vector2(10f, 380f);

            TextMeshProUGUI mLabel = MakeText("MusicLabel", box.transform, "MUSIC VOLUME", 18f, Palette.IvoryCloth, TextAlignmentOptions.Left);
            mLabel.rectTransform.anchoredPosition = new Vector2(0f, 80f);
            mLabel.rectTransform.sizeDelta = new Vector2(-60f, -380f);

            _musicSlider = MakeSlider("MusicSlider", box.transform, new Vector2(0f, 45f), 0.55f);
            _musicSlider.onValueChanged.AddListener(v => SynthAudioManager.Instance?.SetMusicVolume(v));

            TextMeshProUGUI sLabel = MakeText("SfxLabel", box.transform, "SFX VOLUME", 18f, Palette.IvoryCloth, TextAlignmentOptions.Left);
            sLabel.rectTransform.anchoredPosition = new Vector2(0f, -15f);
            sLabel.rectTransform.sizeDelta = new Vector2(-60f, -380f);

            _sfxSlider = MakeSlider("SfxSlider", box.transform, new Vector2(0f, -50f), 0.85f);
            _sfxSlider.onValueChanged.AddListener(v => SynthAudioManager.Instance?.SetSFXVolume(v));

            Button resumeBtn = MakeButton("ResumeBtn", box.transform, "RESUME EXPEDITION", new Vector2(0f, -125f), new Vector2(320f, 54f), Palette.UiPanelInner, Palette.LightningCyan, out _);
            resumeBtn.onClick.AddListener(() => GameLoop.Instance?.TogglePause());

            Button restartBtn = MakeButton("RestartBtn", box.transform, "RESTART EXPEDITION", new Vector2(0f, -190f), new Vector2(320f, 54f), Palette.HeroCrimson, Palette.IvoryCloth, out _);
            restartBtn.onClick.AddListener(() => GameLoop.Instance?.RestartExpedition());

            _pausePanel.SetActive(false);
        }

        Slider MakeSlider(string name, Transform parent, Vector2 anchoredPos, float initial)
        {
            GameObject go = MakePanel(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPos, new Vector2(380f, 26f), Palette.AbyssShadow, true);
            Slider slider = go.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = initial;

            GameObject fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(go.transform, false);
            RectTransform faRt = fillArea.AddComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero;
            faRt.anchorMax = Vector2.one;
            faRt.offsetMin = new Vector2(4f, 4f);
            faRt.offsetMax = new Vector2(-4f, -4f);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fRt = fill.AddComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = Vector2.one;
            fRt.offsetMin = Vector2.zero;
            fRt.offsetMax = Vector2.zero;
            Image fImg = fill.AddComponent<Image>();
            fImg.sprite = _solidSprite;
            fImg.color = Palette.FiligreeGold;

            GameObject handle = MakePanel("Handle", go.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 34f), Palette.LightningCyan, true);
            handle.GetComponent<Image>().raycastTarget = true;

            slider.fillRect = fRt;
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handle.GetComponent<Image>();
            return slider;
        }

        void BuildEndScreen(Transform root)
        {
            _endPanel = MakePanel("EndPanel", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Palette.UiScrim, false);
            _endPanel.GetComponent<Image>().raycastTarget = true;

            GameObject box = MakePanel("EndBox", _endPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 440f), Palette.UiPanelBg, true);
            _endTitleText = MakeText("EndTitle", box.transform, "EXPEDITION VICTORIOUS!", 40f, Palette.FiligreeGold, TextAlignmentOptions.Top);
            _endTitleText.rectTransform.offsetMin = new Vector2(18f, 340f);

            _endStatsText = MakeText("EndStats", box.transform, "Stats...", 22f, Palette.IvoryCloth, TextAlignmentOptions.Center, FontStyles.Normal);
            _endStatsText.rectTransform.offsetMin = new Vector2(24f, 110f);
            _endStatsText.rectTransform.offsetMax = new Vector2(-24f, -110f);

            Button playAgain = MakeButton("PlayAgainBtn", box.transform, "NEW EXPEDITION", new Vector2(0f, -155f), new Vector2(300f, 58f), Palette.FiligreeGold, Palette.AbyssShadow, out _);
            playAgain.onClick.AddListener(() => GameLoop.Instance?.RestartExpedition());

            _endPanel.SetActive(false);
        }

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
                _endTitleText.text = won ? "SANCTUM CONQUERED!" : "EXPEDITION FALLEN";
                _endTitleText.color = won ? Palette.FiligreeGold : Palette.HeroCrimson;
                GameLoop loop = GameLoop.Instance;
                _endStatsText.text = $"Chamber Reached: {loop.CurrentChamber} / {loop.MaxChambers}\n" +
                                     $"Hero Level: {loop.Level}\n" +
                                     $"Perfect Parries: {loop.ParriesLanded}\n" +
                                     $"Perfect Resonances: {loop.PerfectResonances}\n" +
                                     $"Elemental Combos: {loop.ElementalCombos}\n" +
                                     $"Enemies Defeated: {loop.EnemiesDefeated}";
            }

            RefreshStats();
        }

        void RefreshStats()
        {
            if (GameLoop.Instance == null || HeroController.Instance == null) return;
            GameLoop loop = GameLoop.Instance;
            HeroController hero = HeroController.Instance;

            string chName = loop.CurrentChamber == 1 ? "THE SHATTERED COURTYARD" :
                (loop.CurrentChamber == 2 ? "ALCHEMIST'S FLOODED HALL" : "THRONE OF THE ECLIPSE");
            if (_chamberHeaderText != null)
                _chamberHeaderText.text = $"CHAMBER {loop.CurrentChamber} / {loop.MaxChambers}  ·  {chName}";

            if (_turnBannerText != null)
            {
                switch (loop.State)
                {
                    case GameState.Exploring:
                        _turnBannerText.text = "EXPLORATION — OPEN CHESTS & ADVANCE";
                        _turnBannerText.color = Palette.LightningCyan;
                        break;
                    case GameState.TacticalPlayerTurn:
                        _turnBannerText.text = $"YOUR TURN — {hero.ActiveAction}  (MOVE LEFT: {hero.RemainingMoveDist:F1}m)";
                        _turnBannerText.color = Palette.FiligreeGold;
                        break;
                    case GameState.ActionExecutionQTE:
                        _turnBannerText.text = "ACTIVE RESONANCE EXECUTION!";
                        _turnBannerText.color = Palette.FireAmber;
                        break;
                    case GameState.EnemyTurnDefense:
                        _turnBannerText.text = "ENEMY TURN — DEFEND IN REAL TIME!";
                        _turnBannerText.color = Palette.HeroCrimson;
                        break;
                }
            }

            if (_tacticalBar != null)
            {
                bool showBar = loop.State == GameState.TacticalPlayerTurn ||
                               loop.State == GameState.ActionExecutionQTE ||
                               loop.State == GameState.EnemyTurnDefense;
                _tacticalBar.SetActive(showBar);
            }

            if (_hpValueText != null)
                _hpValueText.text = $"{hero.CurrentHP} / {hero.MaxHP} HP";
            if (_levelText != null)
                _levelText.text = $"LVL {loop.Level} EXPEDITIONER";
            if (_weaponText != null)
                _weaponText.text = hero.GetWeaponDisplayName();
            if (_apValueText != null)
                _apValueText.text = $"AP: {hero.CurrentAP} / {hero.MaxAP}";

            for (int i = 0; i < _apPips.Count; i++)
            {
                _apPips[i].color = i < hero.CurrentAP ? Palette.LightningCyan : Palette.AbyssShadow;
            }

            if (_btnFlaskLabel != null)
            {
                string flaskName = hero.FlaskThrowsOil ? "OIL FLASK" : "WATER FLASK";
                _btnFlaskLabel.text = $"[2] {flaskName}\n1 AP";
                _btnFlaskLabel.color = hero.FlaskThrowsOil ? Palette.FireAmber : Palette.WaterAzure;
            }

            HighlightButton(_btnStrike, hero.ActiveAction == TacticalActionMode.Strike);
            HighlightButton(_btnFlask, hero.ActiveAction == TacticalActionMode.Flask);
            HighlightButton(_btnShot, hero.ActiveAction == TacticalActionMode.FreeAimShot);
            HighlightButton(_btnShove, hero.ActiveAction == TacticalActionMode.Shove);
        }

        static void HighlightButton(Button btn, bool selected)
        {
            if (btn == null) return;
            Image img = btn.GetComponent<Image>();
            if (img != null)
                img.color = selected ? Palette.MarbleTrim : Palette.UiPanelInner;
        }

        void Update()
        {
            if (_titlePromptText != null && GameLoop.Instance != null && GameLoop.Instance.State == GameState.Title)
            {
                float alpha = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 3.8f);
                Color c = Palette.LightningCyan;
                c.a = alpha;
                _titlePromptText.color = c;
            }

            // Smooth animated HP and XP bars (never snap)
            if (HeroController.Instance != null && GameLoop.Instance != null)
            {
                float targetHp = Mathf.Clamp01(HeroController.Instance.CurrentHP / (float)Mathf.Max(1, HeroController.Instance.MaxHP));
                float targetXp = Mathf.Clamp01(GameLoop.Instance.CurrentXP / (float)Mathf.Max(1, GameLoop.Instance.XPToNextLevel));
                _displayedHpFill = Mathf.MoveTowards(_displayedHpFill, targetHp, Time.unscaledDeltaTime * 2.4f);
                _displayedXpFill = Mathf.MoveTowards(_displayedXpFill, targetXp, Time.unscaledDeltaTime * 2.0f);
                if (_hpFillImg != null) _hpFillImg.fillAmount = _displayedHpFill;
                if (_xpFillImg != null) _xpFillImg.fillAmount = _displayedXpFill;
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
                        _qteTargetRingImg.color = Palette.FiligreeGold;
                        _qteDynamicRingImg.color = (qte.ResonanceRingScale >= 0.78f && qte.ResonanceRingScale <= 1.28f)
                            ? Palette.FireAmber : Palette.LightningCyan;
                        _qteDynamicRing.localScale = Vector3.one * qte.ResonanceRingScale;
                    }
                    else
                    {
                        _qteTargetRingImg.color = Palette.HeroCrimson;
                        bool inParry = qte.DefenseWindupProgress >= 0.62f;
                        _qteDynamicRingImg.color = inParry ? Palette.LightningCyan : Palette.IvoryCloth;
                        float s = Mathf.Lerp(2.1f, 0.85f, qte.DefenseWindupProgress);
                        _qteDynamicRing.localScale = Vector3.one * s;
                    }
                }
            }
        }
    }
}
