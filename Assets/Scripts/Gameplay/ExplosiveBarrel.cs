using UnityEngine;
using Aetherfall.Audio;
using Aetherfall.Core;
using Aetherfall.Feel;

namespace Aetherfall.Gameplay
{
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
            FeelManager.Instance?.SpawnFloatingText(pos, "¡BARRIL DETONADO!", Palette.FireAmber, 1.25f);

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
}
