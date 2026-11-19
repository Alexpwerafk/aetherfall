using System.Collections;
using UnityEngine;
using TMPro;
using Aetherfall.Audio;
using Aetherfall.Core;
using Aetherfall.Feel;

namespace Aetherfall.Gameplay
{
    public enum EnemyArchetype
    {
        CorruptedVanguard,
        ArcaneAlchemist,
        ChronoInquisitorBoss
    }

    /// <summary>
    /// Tactical enemy AI supporting Corrupted Vanguards, Arcane Alchemists, and Chrono-Inquisitor Malakor.
    /// Drives real-time Dodge/Parry duels on enemy turns and Posture Break mechanics.
    /// </summary>
    public class EnemyAI : MonoBehaviour
    {
        public EnemyArchetype Archetype = EnemyArchetype.CorruptedVanguard;
        public string DisplayName = "Corrupted Vanguard";
        public int ChamberIndex = 1;

        public int MaxHP = 65;
        public int CurrentHP = 65;
        public int MaxPosture = 3;
        public int CurrentPosture = 3;
        public int AttackDamage = 18;
        public int XPReward = 45;

        public bool IsStunned { get; private set; }
        public bool IsProne { get; private set; }
        public bool IsDead { get; private set; }

        public ProceduralCharacterAnimator Animator { get; private set; }

        TextMeshPro _overheadLabel;

        void Awake()
        {
            Animator = GetComponent<ProceduralCharacterAnimator>();
            if (Animator == null)
                Animator = gameObject.AddComponent<ProceduralCharacterAnimator>();
        }

        void Start()
        {
            BuildOverheadStatus();
            RefreshOverheadStatus();
        }

        void BuildOverheadStatus()
        {
            GameObject go = new GameObject("EnemyOverheadStatus");
            go.transform.SetParent(transform, false);
            float h = Archetype == EnemyArchetype.ChronoInquisitorBoss ? 3.55f : 2.35f;
            go.transform.localPosition = new Vector3(0f, h, 0f);

            _overheadLabel = go.AddComponent<TextMeshPro>();
            _overheadLabel.alignment = TextAlignmentOptions.Center;
            _overheadLabel.fontSize = 2.25f;
            _overheadLabel.fontStyle = FontStyles.Bold;
            _overheadLabel.outlineWidth = 0.22f;
            _overheadLabel.outlineColor = Palette.AbyssShadow;
            _overheadLabel.color = Palette.IvoryCloth;
        }

        void LateUpdate()
        {
            if (_overheadLabel != null && Camera.main != null)
                _overheadLabel.transform.rotation = Camera.main.transform.rotation;
        }

        void RefreshOverheadStatus()
        {
            if (_overheadLabel == null) return;
            string statusTag = "";
            if (IsStunned) statusTag = " [STUNNED]";
            else if (IsProne) statusTag = " [PRONE]";
            string postureStr = new string('*', Mathf.Max(0, CurrentPosture));
            _overheadLabel.text = $"{DisplayName}\nHP {CurrentHP}/{MaxHP} | BRK [{postureStr}]{statusTag}";
            _overheadLabel.color = IsStunned ? Palette.LightningCyan : (IsProne ? Palette.FrostIce : Palette.IvoryCloth);
        }

        public void ApplyStatus(bool stunned, bool prone)
        {
            if (stunned) IsStunned = true;
            if (prone)
            {
                IsProne = true;
                if (Animator != null) Animator.IsProne = true;
            }
            RefreshOverheadStatus();
        }

        public void DamagePosture(int amount)
        {
            CurrentPosture = Mathf.Max(0, CurrentPosture - amount);
            if (CurrentPosture == 0)
            {
                IsStunned = true;
                FeelManager.Instance?.SpawnFloatingText(transform.position, "POSTURE BROKEN! STUNNED!", Palette.FiligreeGold, 1.25f);
            }
            RefreshOverheadStatus();
        }

        public void TakeDamage(int dmg, ElementType element, bool isCrit)
        {
            if (IsDead) return;

            bool bonusCrit = isCrit || IsProne || CurrentPosture == 0;
            if ((IsProne || CurrentPosture == 0) && !isCrit)
                dmg = Mathf.RoundToInt(dmg * 1.45f);

            CurrentHP = Mathf.Max(0, CurrentHP - dmg);
            Animator?.TriggerHitRecoil();

            Color flashCol = element == ElementType.Fire ? Palette.FireAmber :
                (element == ElementType.Lightning ? Palette.LightningCyan :
                (element == ElementType.Frost ? Palette.FrostIce : Palette.FiligreeGold));

            FeelManager.Instance?.FlashObject(gameObject, flashCol, 0.16f);
            FeelManager.Instance?.PopTransform(transform, 1.12f, 0.16f);

            string tag = bonusCrit ? $"CRIT -{dmg}!" : $"-{dmg}";
            FeelManager.Instance?.SpawnFloatingText(transform.position, tag, flashCol, bonusCrit ? 1.25f : 1.0f);

            if (element == ElementType.Fire) VfxPoolManager.Instance?.SpawnFireExplosion(transform.position);
            else if (element == ElementType.Lightning) VfxPoolManager.Instance?.SpawnLightningArc(transform.position);
            else if (element == ElementType.Frost) VfxPoolManager.Instance?.SpawnFrostShatter(transform.position);

            RefreshOverheadStatus();

            if (CurrentHP <= 0)
                Die();
        }

        public void KillByAbyssFall()
        {
            if (IsDead) return;
            IsDead = true;
            StartCoroutine(AbyssFallCoroutine());
        }

        IEnumerator AbyssFallCoroutine()
        {
            FeelManager.Instance?.SpawnFloatingText(transform.position, "SHOVED INTO ABYSS!", Palette.FiligreeGold, 1.35f);
            SynthAudioManager.Instance?.PlayLevelUpSting();
            Vector3 start = transform.position;
            Vector3 end = start + Vector3.down * 11f + Vector3.right * Mathf.Sign(start.x) * 2f;
            yield return Ease.Tween(0.55f, t =>
            {
                transform.position = Vector3.Lerp(start, end, t);
                transform.Rotate(Vector3.forward * 240f * Time.deltaTime);
            }, Ease.OutCubic, false);

            AwardDeathRewards();
            gameObject.SetActive(false);
            DungeonDirector.Instance?.CheckChamberCleared();
        }

        void Die()
        {
            IsDead = true;
            AwardDeathRewards();
            StartCoroutine(DeathSinkCoroutine());
        }

        void AwardDeathRewards()
        {
            VfxPoolManager.Instance?.SpawnBurst(transform.position + Vector3.up * 0.8f, Palette.FiligreeGold, Palette.EnemyAlchemist, 24, 6f, 0.28f);
            if (GameLoop.Instance != null)
            {
                GameLoop.Instance.EnemiesDefeated++;
                GameLoop.Instance.GrantXP(XPReward);
            }
        }

        IEnumerator DeathSinkCoroutine()
        {
            Vector3 start = transform.position;
            Vector3 end = start + Vector3.down * 1.6f;
            yield return Ease.Tween(0.35f, t =>
            {
                transform.position = Vector3.Lerp(start, end, t);
                transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.05f, t);
            }, Ease.OutCubic, false);
            gameObject.SetActive(false);
            DungeonDirector.Instance?.CheckChamberCleared();
        }

        public IEnumerator ExecuteTurn(HeroController hero)
        {
            if (IsDead || hero == null) yield break;

            ElementalSurfaceManager.Instance?.EvaluateUnitSurfaceEntry(this);
            if (IsDead) yield break;

            if (IsStunned || IsProne)
            {
                string msg = IsStunned ? "STUNNED — TURN SKIPPED!" : "RECOVERING FROM PRONE!";
                FeelManager.Instance?.SpawnFloatingText(transform.position, msg, Palette.LightningCyan, 1.15f);
                IsStunned = false;
                IsProne = false;
                CurrentPosture = MaxPosture;
                if (Animator != null) Animator.IsProne = false;
                RefreshOverheadStatus();
                yield return new WaitForSeconds(0.55f);
                yield break;
            }

            Vector3 toHero = hero.transform.position - transform.position;
            toHero.y = 0f;
            float desiredDist = Archetype == EnemyArchetype.ArcaneAlchemist ? 5.8f : 2.3f;

            // Step toward ideal range
            if (toHero.magnitude > desiredDist)
            {
                Vector3 targetPos = hero.transform.position - toHero.normalized * desiredDist;
                targetPos.x = Mathf.Clamp(targetPos.x, -8.5f, 8.5f);
                Vector3 start = transform.position;
                Vector3 moveDir = (targetPos - start).normalized;
                if (moveDir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(moveDir);

                if (Animator != null) Animator.NormalizedSpeed = 1f;
                yield return Ease.Tween(0.55f, t =>
                {
                    transform.position = Vector3.Lerp(start, targetPos, t);
                }, Ease.OutCubic, false);
                if (Animator != null) Animator.NormalizedSpeed = 0f;
                ElementalSurfaceManager.Instance?.EvaluateUnitSurfaceEntry(this);
                if (IsDead) yield break;
            }

            // Face Hero
            Vector3 lookDir = hero.transform.position - transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(lookDir);

            // Boss executes a 2-hit Expedition 33 Combo!
            int hits = Archetype == EnemyArchetype.ChronoInquisitorBoss ? 2 : 1;
            for (int h = 0; h < hits; h++)
            {
                if (IsDead || hero.CurrentHP <= 0) break;

                GameLoop.Instance?.SetState(GameState.EnemyTurnDefense);
                DefenseOutcome outcome = DefenseOutcome.Hit;
                yield return ExpeditionQTEController.Instance.RunEnemyDefenseQTE(this, hero, res => outcome = res);

                if (IsDead) break;

                Animator?.TriggerAttackSwing();

                if (Archetype == EnemyArchetype.ArcaneAlchemist)
                {
                    VfxPoolManager.Instance?.SpawnLightningArc(hero.transform.position);
                    if (outcome == DefenseOutcome.Hit)
                        ElementalSurfaceManager.Instance?.ApplyElementAt(hero.transform.position, 2.2f, ElementType.Oil);
                }

                if (outcome == DefenseOutcome.Hit)
                {
                    hero.TakeDamage(AttackDamage);
                }

                yield return new WaitForSeconds(0.32f);
            }
        }
    }
}
