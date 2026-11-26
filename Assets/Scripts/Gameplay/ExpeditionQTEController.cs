using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Aetherfall.Audio;
using Aetherfall.CameraRig;
using Aetherfall.Core;
using Aetherfall.Feel;

namespace Aetherfall.Gameplay
{
    public enum DefenseOutcome
    {
        Hit,
        Dodged,
        Parried
    }

    /// <summary>
    /// Clair Obscur: Expedition 33 reactive turn mechanics:
    /// - Attack Resonance Ring QTE (timing hit for +50% Crit & +1 AP refund)
    /// - Enemy Turn Dodge [Space] & Parry+Counter Riposte [E / Right Click]
    /// </summary>
    public class ExpeditionQTEController : MonoBehaviour
    {
        public static ExpeditionQTEController Instance { get; private set; }

        public bool IsAttackQTEActive { get; private set; }
        public bool IsDefenseQTEActive { get; private set; }

        // Starts at 1.95f and shrinks down to 0.55f; sweet spot around 1.0f ([0.80f, 1.24f])
        public float ResonanceRingScale { get; private set; } = 1.8f;

        // 0..1 progress of enemy windup; Parry sweet spot [0.68, 0.98], Dodge window [0.48, 0.98]
        public float DefenseWindupProgress { get; private set; }
        public string ActiveBannerText { get; private set; } = string.Empty;

        bool _queuedPerfectResonance;
        bool _queuedParry;
        bool _queuedDodge;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void ForceTriggerResonance() => _queuedPerfectResonance = true;
        public void ForceTriggerParry() => _queuedParry = true;
        public void ForceTriggerDodge() => _queuedDodge = true;

        bool WasAttackQTEPressed()
        {
            if (_queuedPerfectResonance)
            {
                _queuedPerfectResonance = false;
                return true;
            }
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            return (kb != null && kb.spaceKey.wasPressedThisFrame) ||
                   (mouse != null && mouse.leftButton.wasPressedThisFrame);
        }

        bool WasParryPressed()
        {
            if (_queuedParry)
            {
                _queuedParry = false;
                return true;
            }
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            return (kb != null && kb.eKey.wasPressedThisFrame) ||
                   (mouse != null && mouse.rightButton.wasPressedThisFrame);
        }

        bool WasDodgePressed()
        {
            if (_queuedDodge)
            {
                _queuedDodge = false;
                return true;
            }
            var kb = Keyboard.current;
            return kb != null && kb.spaceKey.wasPressedThisFrame;
        }

        public IEnumerator RunAttackResonanceQTE(HeroController hero, Transform targetTransform, Action<bool> onComplete)
        {
            _queuedPerfectResonance = false;
            IsAttackQTEActive = true;
            ActiveBannerText = "¡RESONANCIA! PULSA [ESPACIO / CLIC IZQ.] EN EL ANILLO DORADO";

            if (TacticalCameraDirector.Instance != null && targetTransform != null)
                TacticalCameraDirector.Instance.FocusDuel(hero.transform, targetTransform, enemyAttacking: false);

            SynthAudioManager.Instance?.PlayQTERiser();

            // Wait 1 frame so the button press that launched the attack doesn't consume the QTE
            yield return null;

            const float duration = 1.25f;
            float elapsed = 0f;
            bool decided = false;
            bool isPerfect = false;

            while (elapsed < duration && !decided)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                ResonanceRingScale = Mathf.Lerp(1.95f, 0.55f, u);

                if (WasAttackQTEPressed())
                {
                    decided = true;
                    isPerfect = ResonanceRingScale >= 0.78f && ResonanceRingScale <= 1.28f;
                }
                yield return null;
            }

            IsAttackQTEActive = false;
            ActiveBannerText = string.Empty;

            if (isPerfect)
            {
                if (GameLoop.Instance != null)
                {
                    GameLoop.Instance.PerfectResonances++;
                    GameLoop.Instance.NotifyStatsChanged();
                }
                SynthAudioManager.Instance?.PlayPerfectResonance();
                FeelManager.Instance?.TriggerHitStop(0.075f);
                FeelManager.Instance?.TriggerShake(0.65f);
                VfxPoolManager.Instance?.SpawnResonanceBurst(targetTransform != null ? targetTransform.position : hero.transform.position);
                FeelManager.Instance?.SpawnFloatingText(
                    hero.transform.position,
                    "¡RESONANCIA PERFECTA! +1 PA",
                    Palette.FiligreeGold,
                    1.25f);
            }
            else
            {
                SynthAudioManager.Instance?.PlaySlash();
                FeelManager.Instance?.TriggerShake(0.28f);
            }

            onComplete?.Invoke(isPerfect);
        }

        public IEnumerator RunEnemyDefenseQTE(EnemyAI attacker, HeroController hero, Action<DefenseOutcome> onComplete)
        {
            _queuedParry = false;
            _queuedDodge = false;
            IsDefenseQTEActive = true;
            ActiveBannerText = "¡ATAQUE ENTRANTE! [E / CLIC DER.] PARRY   ·   [ESPACIO] ESQUIVAR";

            if (TacticalCameraDirector.Instance != null && attacker != null)
                TacticalCameraDirector.Instance.FocusDuel(hero.transform, attacker.transform, enemyAttacking: true);

            SynthAudioManager.Instance?.PlayQTERiser();

            const float windupDuration = 1.15f;
            float elapsed = 0f;
            DefenseOutcome outcome = DefenseOutcome.Hit;
            bool resolved = false;

            while (elapsed < windupDuration && !resolved)
            {
                elapsed += Time.deltaTime;
                DefenseWindupProgress = Mathf.Clamp01(elapsed / windupDuration);

                if (WasParryPressed())
                {
                    if (DefenseWindupProgress >= 0.62f)
                    {
                        outcome = DefenseOutcome.Parried;
                        resolved = true;
                    }
                    else
                    {
                        // Early parry misses timing!
                        FeelManager.Instance?.SpawnFloatingText(hero.transform.position, "¡PARRY MUY PRONTO!", Palette.HeroCrimson, 0.95f);
                        resolved = true;
                    }
                }
                else if (WasDodgePressed())
                {
                    if (DefenseWindupProgress >= 0.42f)
                    {
                        outcome = DefenseOutcome.Dodged;
                        resolved = true;
                    }
                }

                yield return null;
            }

            IsDefenseQTEActive = false;
            ActiveBannerText = string.Empty;
            DefenseWindupProgress = 0f;

            if (outcome == DefenseOutcome.Parried)
            {
                if (GameLoop.Instance != null)
                {
                    GameLoop.Instance.ParriesLanded++;
                    GameLoop.Instance.NotifyStatsChanged();
                }

                hero.Animator?.TriggerParryGuard();
                SynthAudioManager.Instance?.PlayParryBell();
                FeelManager.Instance?.TriggerHitStop(0.085f);
                FeelManager.Instance?.TriggerShake(0.75f);
                VfxPoolManager.Instance?.SpawnParryShockwave(hero.transform.position);
                FeelManager.Instance?.SpawnFloatingText(hero.transform.position, "¡PARRY PERFECTO! ¡CONTRAATAQUE!", Palette.LightningCyan, 1.3f);

                yield return new WaitForSecondsRealtime(0.18f);

                // Immediate Riposte Counter-Attack during Enemy's turn!
                if (attacker != null && !attacker.IsDead)
                {
                    hero.Animator?.TriggerAttackSwing();
                    SynthAudioManager.Instance?.PlayRiposteThunder();
                    FeelManager.Instance?.TriggerShake(0.6f);
                    int counterDmg = GameLoop.Instance != null && GameLoop.Instance.BoonTempestRiposte ? 54 : 34;
                    attacker.TakeDamage(counterDmg, hero.CurrentElement, isCrit: true);
                    attacker.DamagePosture(2);

                    if (GameLoop.Instance != null && GameLoop.Instance.BoonTempestRiposte)
                        ElementalSurfaceManager.Instance?.ApplyElementAt(attacker.transform.position, 2.4f, ElementType.Lightning);
                }
            }
            else if (outcome == DefenseOutcome.Dodged)
            {
                SynthAudioManager.Instance?.PlayDodgeWhoosh();
                VfxPoolManager.Instance?.SpawnFootstep(hero.transform.position);
                FeelManager.Instance?.SpawnFloatingText(hero.transform.position, "¡ESQUIVA LIMPIA!", Palette.FrostIce, 1.1f);
                Vector3 dodgeOffset = hero.transform.right * 1.1f;
                hero.transform.position += dodgeOffset;

                if (GameLoop.Instance != null && GameLoop.Instance.BoonAlchemistStride)
                {
                    ElementalSurfaceManager.Instance?.SpawnSurface(hero.transform.position, SurfaceType.Oil, 2.2f, 3);
                    hero.RefundAP(1);
                }
            }

            onComplete?.Invoke(outcome);
        }
    }
}
