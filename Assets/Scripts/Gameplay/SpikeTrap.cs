using System.Collections;
using UnityEngine;
using Aetherfall.Audio;
using Aetherfall.Core;
using Aetherfall.Feel;

namespace Aetherfall.Gameplay
{
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
}
