using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Aetherfall.Audio;
using Aetherfall.Core;
using Aetherfall.Feel;

namespace Aetherfall.Gameplay
{
    public enum SurfaceType
    {
        Oil,
        Water,
        Fire,
        ElectrifiedWater,
        Ice
    }

    public class ElementalSurface : MonoBehaviour
    {
        public SurfaceType Type;
        public float Radius = 2.6f;
        public int TurnsRemaining = 3;

        Renderer _renderer;
        Material _mat;
        float _baseScale;

        public void Init(SurfaceType type, float radius, int turns)
        {
            Type = type;
            Radius = radius;
            TurnsRemaining = turns;
            _baseScale = radius * 2f;
            transform.localScale = new Vector3(_baseScale, 0.04f, _baseScale);

            _renderer = GetComponent<Renderer>();
            if (Application.isPlaying)
                ApplyVisuals();
        }

        void Start()
        {
            _baseScale = Radius * 2f;
            ApplyVisuals();
        }

        public void SetSurfaceType(SurfaceType newType, int turns = 3)
        {
            Type = newType;
            TurnsRemaining = Mathf.Max(TurnsRemaining, turns);
            ApplyVisuals();
        }

        void ApplyVisuals()
        {
            if (_renderer == null) _renderer = GetComponent<Renderer>();
            if (_renderer == null) return;

            Color baseCol;
            Color emission;
            float pulse = 0f;

            switch (Type)
            {
                case SurfaceType.Oil:
                    baseCol = Palette.OilDark;
                    emission = Palette.OilDark * 0.1f;
                    break;
                case SurfaceType.Water:
                    baseCol = Palette.WaterAzure;
                    emission = Palette.WaterAzure * 0.45f;
                    pulse = 1.8f;
                    break;
                case SurfaceType.Fire:
                    baseCol = Color.Lerp(Palette.HeroCrimson, Palette.FireAmber, 0.55f);
                    emission = Palette.FireAmber * 0.72f;
                    pulse = 6.5f;
                    break;
                case SurfaceType.ElectrifiedWater:
                    baseCol = Palette.WaterAzure;
                    emission = Palette.LightningCyan * 0.88f;
                    pulse = 9.0f;
                    break;
                default: // Ice
                    baseCol = Palette.FrostIce;
                    emission = Palette.FrostIce * 0.52f;
                    pulse = 2.2f;
                    break;
            }

            if (_mat == null)
            {
                _mat = Palette.CreateToonMaterial($"Surface_{Type}", baseCol, emission, pulse, 0.4f, emission);
                _renderer.sharedMaterial = _mat;
            }
            else
            {
                _mat.SetColor("_BaseColor", baseCol);
                if (_mat.HasProperty("_EmissionColor")) _mat.SetColor("_EmissionColor", emission);
                if (_mat.HasProperty("_PulseSpeed")) _mat.SetFloat("_PulseSpeed", pulse);
            }
        }

        void Update()
        {
            if (_baseScale <= 0f) return;
            float breath = 1f + Mathf.Sin(Time.time * 2.5f + transform.position.x) * 0.03f;
            transform.localScale = new Vector3(_baseScale * breath, 0.045f, _baseScale * breath);
        }
    }

    public class ExplosiveBarrel : MonoBehaviour
    {
        public bool IsDetonated { get; private set; }
        public float ExplosionRadius = 4.4f;
        public int ExplosionDamage = 44;

        public void Detonate()
        {
            if (IsDetonated) return;
            IsDetonated = true;

            Vector3 pos = transform.position;
            VfxPoolManager.Instance?.SpawnFireExplosion(pos);
            SynthAudioManager.Instance?.PlayElementalReaction("Fire");
            FeelManager.Instance?.TriggerShake(0.85f);
            FeelManager.Instance?.TriggerHitStop(0.07f);
            FeelManager.Instance?.SpawnFloatingText(pos, "BARREL DETONATED!", Palette.FireAmber, 1.25f);

            if (GameLoop.Instance != null)
            {
                GameLoop.Instance.ElementalCombos++;
                GameLoop.Instance.NotifyStatsChanged();
            }

            ElementalSurfaceManager.Instance?.SpawnSurface(pos, SurfaceType.Fire, 3.1f, 3);

            EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
            foreach (EnemyAI enemy in enemies)
            {
                if (enemy == null || enemy.IsDead) continue;
                if (Vector3.Distance(pos, enemy.transform.position) <= ExplosionRadius)
                    enemy.TakeDamage(ExplosionDamage, ElementType.Fire, isCrit: true);
            }

            HeroController hero = FindFirstObjectByType<HeroController>();
            if (hero != null && Vector3.Distance(pos, hero.transform.position) <= ExplosionRadius * 0.65f)
                hero.TakeDamage(12);

            gameObject.SetActive(false);
        }
    }

    public class RuneCrystalSwitch : MonoBehaviour
    {
        public bool IsActivated { get; private set; }
        public int SwitchIndex;
        public Transform CrystalVisual;
        public event Action<RuneCrystalSwitch> OnActivated;

        void Start()
        {
            if (CrystalVisual == null && transform.childCount > 0)
            {
                Transform spin = transform.Find("CrystalSpin");
                CrystalVisual = spin != null ? spin : transform.GetChild(0);
            }
        }

        void Update()
        {
            if (CrystalVisual != null)
            {
                float speed = IsActivated ? 140f : 42f;
                CrystalVisual.Rotate(Vector3.up, speed * Time.deltaTime, Space.World);
            }
        }

        public void Activate()
        {
            if (IsActivated) return;
            IsActivated = true;

            Material activeMat = Palette.CreateToonMaterial(
                "CrystalActivatedMat",
                Palette.FiligreeGold,
                Palette.FiligreeGold * 2.1f,
                4f,
                0.5f,
                Palette.FiligreeGold);

            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                if (r.name.Contains("Crystal"))
                    r.sharedMaterial = activeMat;
            }

            VfxPoolManager.Instance?.SpawnResonanceBurst(transform.position);
            SynthAudioManager.Instance?.PlayParryBell();
            FeelManager.Instance?.SpawnFloatingText(transform.position, "RUNE ACTIVATED!", Palette.FiligreeGold, 1.1f);
            OnActivated?.Invoke(this);
        }
    }

    public class SpikeTrap : MonoBehaviour
    {
        public Transform SpikesTransform;
        public int Damage = 24;
        bool _coolingDown;

        void Start()
        {
            if (SpikesTransform == null)
                SpikesTransform = transform.Find("Spikes");
            if (SpikesTransform != null)
                SpikesTransform.localPosition = new Vector3(0f, -0.36f, 0f);
        }

        public void CheckTrigger(EnemyAI enemy)
        {
            if (_coolingDown || enemy == null || enemy.IsDead) return;
            if (Vector3.Distance(transform.position, enemy.transform.position) <= 1.45f)
                StartCoroutine(SpringCoroutine(enemy));
        }

        IEnumerator SpringCoroutine(EnemyAI victim)
        {
            _coolingDown = true;
            SynthAudioManager.Instance?.PlaySlash();
            FeelManager.Instance?.TriggerShake(0.45f);
            FeelManager.Instance?.SpawnFloatingText(transform.position, "IMPALED BY TRAP!", Palette.HeroCrimson, 1.15f);

            if (SpikesTransform != null)
            {
                yield return Ease.Tween(0.12f, t =>
                {
                    SpikesTransform.localPosition = Vector3.Lerp(new Vector3(0f, -0.36f, 0f), new Vector3(0f, 0.25f, 0f), t);
                }, Ease.OutBack, false);
            }

            if (victim != null && !victim.IsDead)
            {
                victim.TakeDamage(Damage, ElementType.Physical, isCrit: true);
                victim.ApplyStatus(stunned: true, prone: false);
            }

            yield return new WaitForSeconds(0.45f);

            if (SpikesTransform != null)
            {
                yield return Ease.Tween(0.25f, t =>
                {
                    SpikesTransform.localPosition = Vector3.Lerp(new Vector3(0f, 0.25f, 0f), new Vector3(0f, -0.36f, 0f), t);
                }, Ease.OutCubic, false);
            }
            _coolingDown = false;
        }
    }

    public class GoldenChest : MonoBehaviour
    {
        public WeaponType RewardWeapon = WeaponType.StormHalberd;
        public string WeaponTitle = "Stormcaller Halberd [Lightning]";
        public Transform LidTransform;
        public bool IsOpened { get; private set; }

        void Start()
        {
            if (LidTransform == null)
                LidTransform = transform.Find("ChestLid");
        }

        public void Open(HeroController hero)
        {
            if (IsOpened) return;
            IsOpened = true;
            StartCoroutine(OpenRoutine(hero));
        }

        IEnumerator OpenRoutine(HeroController hero)
        {
            SynthAudioManager.Instance?.PlayChestFanfare();
            VfxPoolManager.Instance?.SpawnLevelUpConfetti(transform.position);
            FeelManager.Instance?.SpawnFloatingText(transform.position, $"LOOT: {WeaponTitle}!", Palette.FiligreeGold, 1.2f);

            if (hero != null)
                hero.EquipWeapon(RewardWeapon);
            GameLoop.Instance?.GrantXP(55);

            if (LidTransform != null)
            {
                Quaternion startRot = LidTransform.localRotation;
                Quaternion endRot = startRot * Quaternion.Euler(-105f, 0f, 0f);
                yield return Ease.Tween(0.45f, t =>
                {
                    LidTransform.localRotation = Quaternion.SlerpUnclamped(startRot, endRot, t);
                }, Ease.OutBack, true);
            }
        }
    }

    /// <summary>
    /// Manages BG3-style elemental surfaces and chemical interactions across the dungeon floor.
    /// </summary>
    public class ElementalSurfaceManager : MonoBehaviour
    {
        public static ElementalSurfaceManager Instance { get; private set; }

        readonly List<ElementalSurface> _surfaces = new List<ElementalSurface>();
        readonly List<ExplosiveBarrel> _barrels = new List<ExplosiveBarrel>();
        readonly List<RuneCrystalSwitch> _switches = new List<RuneCrystalSwitch>();
        readonly List<SpikeTrap> _traps = new List<SpikeTrap>();
        readonly List<GoldenChest> _chests = new List<GoldenChest>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void RefreshSceneProps()
        {
            _surfaces.Clear();
            _barrels.Clear();
            _switches.Clear();
            _traps.Clear();
            _chests.Clear();

            _surfaces.AddRange(FindObjectsByType<ElementalSurface>(FindObjectsSortMode.None));
            _barrels.AddRange(FindObjectsByType<ExplosiveBarrel>(FindObjectsSortMode.None));
            _switches.AddRange(FindObjectsByType<RuneCrystalSwitch>(FindObjectsSortMode.None));
            _traps.AddRange(FindObjectsByType<SpikeTrap>(FindObjectsSortMode.None));
            _chests.AddRange(FindObjectsByType<GoldenChest>(FindObjectsSortMode.None));
        }

        public IReadOnlyList<ExplosiveBarrel> Barrels => _barrels;
        public IReadOnlyList<RuneCrystalSwitch> Switches => _switches;
        public IReadOnlyList<GoldenChest> Chests => _chests;
        public IReadOnlyList<SpikeTrap> Traps => _traps;

        public ElementalSurface SpawnSurface(Vector3 pos, SurfaceType type, float radius = 2.6f, int turns = 3)
        {
            pos.y = 0.03f;

            for (int i = 0; i < _surfaces.Count; i++)
            {
                ElementalSurface existing = _surfaces[i];
                if (existing == null || !existing.gameObject.activeInHierarchy) continue;
                if (Vector3.Distance(pos, existing.transform.position) <= existing.Radius * 0.85f)
                {
                    if (existing.Type == SurfaceType.Oil && type == SurfaceType.Fire)
                    {
                        TriggerOilExplosion(existing);
                        return existing;
                    }
                    existing.SetSurfaceType(type, turns);
                    return existing;
                }
            }

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = $"ElementalSurface_{type}";
            go.transform.SetParent(transform, true);
            go.transform.position = pos;
            Collider col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            ElementalSurface surf = go.AddComponent<ElementalSurface>();
            surf.Init(type, radius, turns);
            _surfaces.Add(surf);
            return surf;
        }

        public void ApplyElementAt(Vector3 pos, float radius, ElementType element)
        {
            // 1. Check barrels
            for (int i = 0; i < _barrels.Count; i++)
            {
                ExplosiveBarrel barrel = _barrels[i];
                if (barrel != null && !barrel.IsDetonated &&
                    Vector3.Distance(pos, barrel.transform.position) <= radius + 0.9f)
                {
                    if (element == ElementType.Fire || element == ElementType.Lightning || element == ElementType.Physical)
                        barrel.Detonate();
                }
            }

            // 2. Check rune switches
            for (int i = 0; i < _switches.Count; i++)
            {
                RuneCrystalSwitch sw = _switches[i];
                if (sw != null && !sw.IsActivated &&
                    Vector3.Distance(pos, sw.transform.position) <= radius + 1.0f)
                {
                    sw.Activate();
                }
            }

            // 3. Check elemental surface chemical reactions
            bool reacted = false;
            for (int i = 0; i < _surfaces.Count; i++)
            {
                ElementalSurface surf = _surfaces[i];
                if (surf == null || !surf.gameObject.activeInHierarchy) continue;
                if (Vector3.Distance(pos, surf.transform.position) > surf.Radius + radius * 0.5f) continue;

                if (surf.Type == SurfaceType.Oil && element == ElementType.Fire)
                {
                    TriggerOilExplosion(surf);
                    reacted = true;
                }
                else if (surf.Type == SurfaceType.Water && element == ElementType.Lightning)
                {
                    TriggerElectrifiedWater(surf);
                    reacted = true;
                }
                else if (surf.Type == SurfaceType.Water && element == ElementType.Frost)
                {
                    TriggerFrozenIce(surf);
                    reacted = true;
                }
                else if (surf.Type == SurfaceType.Ice && element == ElementType.Fire)
                {
                    surf.SetSurfaceType(SurfaceType.Water, 3);
                    VfxPoolManager.Instance?.SpawnWaterSplash(surf.transform.position);
                    reacted = true;
                }
            }

            if (!reacted)
            {
                if (element == ElementType.Oil)
                {
                    SpawnSurface(pos, SurfaceType.Oil, 2.7f, 4);
                    VfxPoolManager.Instance?.SpawnOilSplash(pos);
                }
                else if (element == ElementType.Water)
                {
                    SpawnSurface(pos, SurfaceType.Water, 2.7f, 4);
                    VfxPoolManager.Instance?.SpawnWaterSplash(pos);
                }
            }
        }

        void TriggerOilExplosion(ElementalSurface surf)
        {
            Vector3 center = surf.transform.position;
            surf.SetSurfaceType(SurfaceType.Fire, 3);
            VfxPoolManager.Instance?.SpawnFireExplosion(center);
            SynthAudioManager.Instance?.PlayElementalReaction("Fire");
            FeelManager.Instance?.TriggerShake(0.75f);
            FeelManager.Instance?.SpawnFloatingText(center, "OIL + FIRE! IGNITED!", Palette.FireAmber, 1.25f);

            if (GameLoop.Instance != null)
            {
                GameLoop.Instance.ElementalCombos++;
                GameLoop.Instance.NotifyStatsChanged();
            }

            EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
            foreach (EnemyAI e in enemies)
            {
                if (e != null && !e.IsDead && Vector3.Distance(center, e.transform.position) <= surf.Radius + 1.4f)
                    e.TakeDamage(32, ElementType.Fire, isCrit: true);
            }
        }

        void TriggerElectrifiedWater(ElementalSurface surf)
        {
            Vector3 center = surf.transform.position;
            surf.SetSurfaceType(SurfaceType.ElectrifiedWater, 2);
            VfxPoolManager.Instance?.SpawnLightningArc(center);
            SynthAudioManager.Instance?.PlayElementalReaction("Lightning");
            FeelManager.Instance?.TriggerShake(0.55f);
            FeelManager.Instance?.SpawnFloatingText(center, "WATER + LIGHTNING! STUNNED!", Palette.LightningCyan, 1.2f);

            if (GameLoop.Instance != null)
            {
                GameLoop.Instance.ElementalCombos++;
                GameLoop.Instance.NotifyStatsChanged();
            }

            EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
            foreach (EnemyAI e in enemies)
            {
                if (e != null && !e.IsDead && Vector3.Distance(center, e.transform.position) <= surf.Radius + 1.2f)
                {
                    e.TakeDamage(26, ElementType.Lightning, isCrit: true);
                    e.ApplyStatus(stunned: true, prone: false);
                }
            }
        }

        void TriggerFrozenIce(ElementalSurface surf)
        {
            Vector3 center = surf.transform.position;
            surf.SetSurfaceType(SurfaceType.Ice, 3);
            VfxPoolManager.Instance?.SpawnFrostShatter(center);
            SynthAudioManager.Instance?.PlayElementalReaction("Frost");
            FeelManager.Instance?.SpawnFloatingText(center, "WATER + FROST! PRONE!", Palette.FrostIce, 1.2f);

            if (GameLoop.Instance != null)
            {
                GameLoop.Instance.ElementalCombos++;
                GameLoop.Instance.NotifyStatsChanged();
            }

            EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
            foreach (EnemyAI e in enemies)
            {
                if (e != null && !e.IsDead && Vector3.Distance(center, e.transform.position) <= surf.Radius + 1.2f)
                {
                    e.TakeDamage(18, ElementType.Frost, isCrit: false);
                    e.ApplyStatus(stunned: false, prone: true);
                }
            }
        }

        public void EvaluateUnitSurfaceEntry(EnemyAI enemy)
        {
            if (enemy == null || enemy.IsDead) return;
            Vector3 pos = enemy.transform.position;

            for (int i = 0; i < _traps.Count; i++)
                _traps[i]?.CheckTrigger(enemy);

            for (int i = 0; i < _surfaces.Count; i++)
            {
                ElementalSurface surf = _surfaces[i];
                if (surf == null || !surf.gameObject.activeInHierarchy) continue;
                if (Vector3.Distance(pos, surf.transform.position) > surf.Radius + 0.4f) continue;

                switch (surf.Type)
                {
                    case SurfaceType.Fire:
                        enemy.TakeDamage(18, ElementType.Fire, false);
                        FeelManager.Instance?.SpawnFloatingText(pos, "BURNING!", Palette.FireAmber);
                        break;
                    case SurfaceType.ElectrifiedWater:
                        enemy.TakeDamage(16, ElementType.Lightning, false);
                        enemy.ApplyStatus(stunned: true, prone: false);
                        FeelManager.Instance?.SpawnFloatingText(pos, "SHOCKED!", Palette.LightningCyan);
                        break;
                    case SurfaceType.Ice:
                        enemy.ApplyStatus(stunned: false, prone: true);
                        FeelManager.Instance?.SpawnFloatingText(pos, "SLIPPED! PRONE", Palette.FrostIce);
                        break;
                }
            }
        }

        public void TickTurnSurfaces()
        {
            for (int i = _surfaces.Count - 1; i >= 0; i--)
            {
                ElementalSurface surf = _surfaces[i];
                if (surf == null)
                {
                    _surfaces.RemoveAt(i);
                    continue;
                }
                surf.TurnsRemaining--;
                if (surf.TurnsRemaining <= 0)
                {
                    Destroy(surf.gameObject);
                    _surfaces.RemoveAt(i);
                }
            }
        }
    }
}
