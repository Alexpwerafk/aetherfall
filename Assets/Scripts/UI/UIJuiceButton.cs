using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Aetherfall.Audio;

namespace Aetherfall.UI
{
    /// <summary>
    /// Tactile UI/UX micro-interaction driver for buttons and cards.
    /// Provides smooth unscaled hover lift, scale punch, active selection state,
    /// and glow frame illumination (works even when Time.timeScale == 0).
    /// </summary>
    public class UIJuiceButton : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        public float HoverScale = 1.055f;
        public float PressScale = 0.95f;
        public float HoverLiftY = 4f;
        public float SelectedLiftY = 10f;

        public UnityEngine.UI.Image BackgroundImage;
        public UnityEngine.UI.Image GlowImage;
        public UnityEngine.UI.Image SelectionAccentBar;

        public Color NormalBg = new Color(0.14f, 0.18f, 0.32f, 0.96f);
        public Color HoverBg = new Color(0.22f, 0.28f, 0.46f, 0.98f);
        public Color SelectedBg = new Color(0.28f, 0.25f, 0.18f, 0.98f);
        public Color GlowColor = new Color(1f, 0.82f, 0.25f, 0.55f);

        RectTransform _rt;
        Vector2 _basePos;
        bool _baseCaptured;
        bool _hovered;
        bool _pressed;
        bool _selected;

        float _currentScale = 1f;
        float _currentLift = 0f;
        float _currentGlow = 0f;

        void Awake()
        {
            _rt = GetComponent<RectTransform>();
        }

        void OnEnable()
        {
            _hovered = false;
            _pressed = false;
            if (_rt != null && !_baseCaptured)
            {
                _basePos = _rt.anchoredPosition;
                _baseCaptured = true;
            }
        }

        public void SetBasePosition(Vector2 pos)
        {
            _basePos = pos;
            _baseCaptured = true;
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            if (SelectionAccentBar != null)
                SelectionAccentBar.enabled = selected;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true;
            SynthAudioManager.Instance?.PlayUIClick();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pressed = false;
        }

        void Update()
        {
            if (_rt == null) return;
            if (!_baseCaptured)
            {
                _basePos = _rt.anchoredPosition;
                _baseCaptured = true;
            }

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float speed = 16f;

            float targetScale = _pressed ? PressScale : (_hovered ? HoverScale : (_selected ? 1.03f : 1f));
            float targetLift = _selected ? SelectedLiftY : (_hovered ? HoverLiftY : 0f);
            float targetGlow = _selected ? 0.85f : (_hovered ? 0.55f : 0f);

            _currentScale = Mathf.Lerp(_currentScale, targetScale, dt * speed);
            _currentLift = Mathf.Lerp(_currentLift, targetLift, dt * speed);
            _currentGlow = Mathf.Lerp(_currentGlow, targetGlow, dt * speed);

            _rt.localScale = Vector3.one * _currentScale;
            _rt.anchoredPosition = _basePos + new Vector2(0f, _currentLift);

            if (BackgroundImage != null)
            {
                Color targetBg = _selected ? SelectedBg : (_hovered ? HoverBg : NormalBg);
                BackgroundImage.color = Color.Lerp(BackgroundImage.color, targetBg, dt * speed);
            }

            if (GlowImage != null)
            {
                Color g = GlowColor;
                g.a = GlowColor.a * _currentGlow;
                GlowImage.color = g;
            }
        }
    }
}
