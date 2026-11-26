using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Aetherfall.Audio;
using Aetherfall.Core;
using Aetherfall.Feel;
using Aetherfall.UI;

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

        Transform _overheadRoot;
        TextMeshProUGUI _overheadNameText;
        TextMeshProUGUI _overheadHpText;
        UnityEngine.UI.Image _overheadHpFill;
        readonly List<UnityEngine.UI.Image> _postureGems = new List<UnityEngine.UI.Image>();

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

        public string GetLocalizedDisplayName()
        {
            switch (DisplayName)
            {
                case "Corrupted Vanguard": return "Vanguardia Corrupta";
                case "Ledge Sentinel": return "Centinela del Abismo";
                case "Sanctum Warden": return "Guardián del Santuario";
                case "Arcane Alchemist": return "Alquimista Arcano";
                case "Storm Alchemist": return "Alquimista de Tormenta";
                case "Chrono-Inquisitor Malakor": return "Crono-Inquisidor Malakor";
                case "Eclipse Guard": return "Guardia del Eclipse";
                default: return DisplayName;
            }
        }

        void BuildOverheadStatus()
        {
            UIArtFactory.EnsureInitialized();
            bool isBoss = Archetype == EnemyArchetype.ChronoInquisitorBoss;

            GameObject rootGo = new GameObject("EnemyOverheadCanvas");
            rootGo.transform.SetParent(transform, false);
            Canvas c = rootGo.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.sortingOrder = 10;

            RectTransform cRt = rootGo.GetComponent<RectTransform>();
            float h = isBoss ? 3.65f : 2.45f;
            cRt.localPosition = new Vector3(0f, h, 0f);
            cRt.localScale = Vector3.one * (isBoss ? 0.0115f : 0.0095f);
            cRt.sizeDelta = new Vector2(260f, 76f);
            _overheadRoot = cRt;

            // Backing pill
            GameObject bgGo = new GameObject("NameplateBg");
            bgGo.transform.SetParent(rootGo.transform, false);
            RectTransform bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            UnityEngine.UI.Image bgImg = bgGo.AddComponent<UnityEngine.UI.Image>();
            bgImg.sprite = UIArtFactory.ChamferPanelSoft;
            bgImg.type = UnityEngine.UI.Image.Type.Sliced;
            bgImg.color = new Color(0.07f, 0.10f, 0.20f, 0.88f);
            bgImg.raycastTarget = false;

            // Name label
            GameObject nameGo = new GameObject("NameTxt");
            nameGo.transform.SetParent(bgGo.transform, false);
            RectTransform nRt = nameGo.AddComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0f, 0.58f);
            nRt.anchorMax = new Vector2(1f, 1f);
            nRt.offsetMin = new Vector2(8f, 0f);
            nRt.offsetMax = new Vector2(-8f, -4f);
            _overheadNameText = nameGo.AddComponent<TextMeshProUGUI>();
            _overheadNameText.alignment = TextAlignmentOptions.Center;
            _overheadNameText.fontSize = isBoss ? 19f : 17f;
            _overheadNameText.fontStyle = FontStyles.Bold;
            _overheadNameText.outlineWidth = 0.2f;
            _overheadNameText.outlineColor = new Color(0.03f, 0.05f, 0.10f, 0.95f);
            _overheadNameText.color = isBoss ? Palette.FiligreeGold : Palette.IvoryCloth;
            _overheadNameText.raycastTarget = false;

            // HP Bar Track
            GameObject trackGo = new GameObject("HpTrack");
            trackGo.transform.SetParent(bgGo.transform, false);
            RectTransform tRt = trackGo.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.06f, 0.28f);
            tRt.anchorMax = new Vector2(0.94f, 0.56f);
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;
            UnityEngine.UI.Image trackImg = trackGo.AddComponent<UnityEngine.UI.Image>();
            trackImg.sprite = UIArtFactory.RoundedPill;
            trackImg.type = UnityEngine.UI.Image.Type.Sliced;
            trackImg.color = new Color(0.04f, 0.06f, 0.12f, 0.95f);
            trackImg.raycastTarget = false;

            GameObject fillGo = new GameObject("HpFill");
            fillGo.transform.SetParent(trackGo.transform, false);
            RectTransform fRt = fillGo.AddComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = Vector2.one;
            fRt.offsetMin = new Vector2(2f, 2f);
            fRt.offsetMax = new Vector2(-2f, -2f);
            _overheadHpFill = fillGo.AddComponent<UnityEngine.UI.Image>();
            _overheadHpFill.sprite = UIArtFactory.GradientBar;
            _overheadHpFill.type = UnityEngine.UI.Image.Type.Filled;
            _overheadHpFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            _overheadHpFill.color = isBoss ? Palette.BossEclipse : Palette.HeroCrimson;
            _overheadHpFill.raycastTarget = false;

            GameObject hpTxtGo = new GameObject("HpVal");
            hpTxtGo.transform.SetParent(trackGo.transform, false);
            RectTransform htRt = hpTxtGo.AddComponent<RectTransform>();
            htRt.anchorMin = Vector2.zero;
            htRt.anchorMax = Vector2.one;
            htRt.offsetMin = Vector2.zero;
            htRt.offsetMax = Vector2.zero;
            _overheadHpText = hpTxtGo.AddComponent<TextMeshProUGUI>();
            _overheadHpText.alignment = TextAlignmentOptions.Center;
            _overheadHpText.fontSize = 14f;
            _overheadHpText.fontStyle = FontStyles.Bold;
            _overheadHpText.color = Palette.IvoryCloth;
            _overheadHpText.outlineWidth = 0.2f;
            _overheadHpText.outlineColor = new Color(0.03f, 0.05f, 0.10f, 0.95f);
            _overheadHpText.raycastTarget = false;

            // Posture Diamonds row
            _postureGems.Clear();
            int pips = Mathf.Max(1, MaxPosture);
            float spacing = 20f;
            float startX = -((pips - 1) * spacing) * 0.5f;
            for (int i = 0; i < pips; i++)
            {
                GameObject gemGo = new GameObject($"PostureGem_{i}");
                gemGo.transform.SetParent(bgGo.transform, false);
                RectTransform gRt = gemGo.AddComponent<RectTransform>();
                gRt.anchorMin = new Vector2(0.5f, 0f);
                gRt.anchorMax = new Vector2(0.5f, 0f);
                gRt.pivot = new Vector2(0.5f, 0.5f);
                gRt.anchoredPosition = new Vector2(startX + i * spacing, 12f);
                gRt.sizeDelta = new Vector2(14f, 14f);
                UnityEngine.UI.Image gImg = gemGo.AddComponent<UnityEngine.UI.Image>();
                gImg.sprite = UIArtFactory.DiamondGem;
                gImg.color = Palette.FiligreeGold;
                gImg.raycastTarget = false;
                _postureGems.Add(gImg);
            }
        }

        void LateUpdate()
        {
            if (_overheadRoot != null && Camera.main != null)
                _overheadRoot.rotation = Camera.main.transform.rotation;
        }

        void RefreshOverheadStatus()
        {
            if (_overheadNameText == null) return;
            string statusTag = "";
            if (IsStunned) statusTag = " [ATURDIDO]";
            else if (IsProne) statusTag = " [DERRIBADO]";

            _overheadNameText.text = $"{GetLocalizedDisplayName()}{statusTag}";
            _overheadNameText.color = IsStunned ? Palette.LightningCyan : (IsProne ? Palette.FrostIce :
                (Archetype == EnemyArchetype.ChronoInquisitorBoss ? Palette.FiligreeGold : Palette.IvoryCloth));

            if (_overheadHpFill != null)
                _overheadHpFill.fillAmount = Mathf.Clamp01(CurrentHP / (float)Mathf.Max(1, MaxHP));
            if (_overheadHpText != null)
                _overheadHpText.text = $"{CurrentHP} / {MaxHP} PV";

            for (int i = 0; i < _postureGems.Count; i++)
                _postureGems[i].color = i < CurrentPosture ? Palette.FiligreeGold : new Color(0.15f, 0.18f, 0.28f, 0.7f);
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
                FeelManager.Instance?.SpawnFloatingText(transform.position, "¡POSTURA ROTA! ¡ATURDIDO!", Palette.FiligreeGold, 1.25f);
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

            string tag = bonusCrit ? $"¡CRÍTICO -{dmg}!" : $"-{dmg}";
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
            FeelManager.Instance?.SpawnFloatingText(transform.position, "¡EMPUJADO AL ABISMO!", Palette.FiligreeGold, 1.35f);
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
                string msg = IsStunned ? "¡ATURDIDO — TURNO PERDIDO!" : "¡RECUPERÁNDOSE!";
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
