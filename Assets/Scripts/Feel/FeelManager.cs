using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using TMPro;
using Aetherfall.Core;

namespace Aetherfall.Feel
{
    /// <summary>
    /// Coordinates Hit-Stop, Cinemachine Impulse screenshake, renderer flash, and floating combat text.
    /// </summary>
    public class FeelManager : MonoBehaviour
    {
        public static FeelManager Instance { get; private set; }

        CinemachineImpulseSource _impulseSource;
        Coroutine _hitStopRoutine;
        readonly Queue<TextMeshPro> _textPool = new Queue<TextMeshPro>();
        MaterialPropertyBlock _flashBlock;
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _flashBlock = new MaterialPropertyBlock();

            _impulseSource = GetComponent<CinemachineImpulseSource>();
            if (_impulseSource == null)
                _impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
        }

        public void TriggerHitStop(float unscaledSeconds = 0.065f)
        {
            if (_hitStopRoutine != null)
                StopCoroutine(_hitStopRoutine);
            _hitStopRoutine = StartCoroutine(HitStopCoroutine(unscaledSeconds));
        }

        IEnumerator HitStopCoroutine(float unscaledSeconds)
        {
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(unscaledSeconds);
            if (GameLoop.Instance == null || GameLoop.Instance.State != GameState.Paused)
                Time.timeScale = 1f;
            _hitStopRoutine = null;
        }

        public void TriggerShake(float force = 0.45f)
        {
            if (_impulseSource == null) return;
            Vector3 dir = new Vector3(Random.Range(-0.5f, 0.5f), 0.8f, Random.Range(-0.5f, 0.5f)).normalized;
            _impulseSource.GenerateImpulseWithVelocity(dir * force);
        }

        public void FlashObject(GameObject target, Color flashColor, float duration = 0.14f)
        {
            if (target == null) return;
            StartCoroutine(FlashCoroutine(target, flashColor, duration));
        }

        IEnumerator FlashCoroutine(GameObject target, Color flashColor, float duration)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_flashBlock);
                _flashBlock.SetColor(EmissionId, flashColor * 2.2f);
                r.SetPropertyBlock(_flashBlock);
            }

            yield return new WaitForSecondsRealtime(duration);

            if (target == null) yield break;
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                r.SetPropertyBlock(null);
            }
        }

        public void PopTransform(Transform target, float peakScale = 1.24f, float duration = 0.18f)
        {
            if (target == null) return;
            StartCoroutine(PopCoroutine(target, peakScale, duration));
        }

        IEnumerator PopCoroutine(Transform target, float peakScale, float duration)
        {
            Vector3 baseline = target.localScale;
            yield return Ease.Tween(duration * 0.45f, t =>
            {
                if (target != null)
                    target.localScale = Vector3.LerpUnclamped(baseline, baseline * peakScale, t);
            }, Ease.OutBack, unscaled: true);

            yield return Ease.Tween(duration * 0.55f, t =>
            {
                if (target != null)
                    target.localScale = Vector3.LerpUnclamped(baseline * peakScale, baseline, t);
            }, Ease.OutCubic, unscaled: true);

            if (target != null)
                target.localScale = baseline;
        }

        public void SpawnFloatingText(Vector3 worldPos, string text, Color color, float scaleMult = 1f)
        {
            StartCoroutine(FloatingTextCoroutine(worldPos + Vector3.up * 2.1f, text, color, scaleMult));
        }

        TextMeshPro GetTextObj()
        {
            if (_textPool.Count > 0)
            {
                TextMeshPro pooled = _textPool.Dequeue();
                if (pooled != null)
                {
                    pooled.gameObject.SetActive(true);
                    return pooled;
                }
            }

            GameObject go = new GameObject("FloatingCombatText");
            go.transform.SetParent(transform, false);
            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 4.6f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.outlineWidth = 0.28f;
            tmp.outlineColor = Palette.ObsidianArmor;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }

        IEnumerator FloatingTextCoroutine(Vector3 startPos, string text, Color color, float scaleMult)
        {
            TextMeshPro tmp = GetTextObj();
            tmp.text = text;
            tmp.color = color;
            tmp.transform.position = startPos;
            tmp.transform.localScale = Vector3.one * scaleMult;

            Camera cam = Camera.main;
            Vector3 endPos = startPos + Vector3.up * 1.15f;

            yield return Ease.Tween(0.85f, t =>
            {
                if (tmp == null) return;
                tmp.transform.position = Vector3.Lerp(startPos, endPos, t);
                if (cam != null)
                    tmp.transform.rotation = cam.transform.rotation;
                Color c = color;
                c.a = 1f - Mathf.Pow(t, 2.2f);
                tmp.color = c;
            }, Ease.OutCubic, unscaled: true);

            if (tmp != null)
            {
                tmp.gameObject.SetActive(false);
                _textPool.Enqueue(tmp);
            }
        }
    }
}
