using System.Collections.Generic;
using UnityEngine;
using Aetherfall.Audio;
using Aetherfall.Core;
using Aetherfall.Feel;

namespace Aetherfall.Gameplay
{
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

            for (int i = 0; i < _switches.Count; i++)
            {
                RuneCrystalSwitch sw = _switches[i];
                if (sw != null && !sw.IsActivated &&
                    Vector3.Distance(pos, sw.transform.position) <= radius + 1.0f)
                {
                    sw.Activate();
                }
            }

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
