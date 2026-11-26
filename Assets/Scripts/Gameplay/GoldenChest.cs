using System.Collections;
using UnityEngine;
using Aetherfall.Audio;
using Aetherfall.Core;
using Aetherfall.Feel;

namespace Aetherfall.Gameplay
{
    public class GoldenChest : MonoBehaviour
    {
        public WeaponType RewardWeapon = WeaponType.StormHalberd;
        public string WeaponTitle = "Alabarda de Tormenta [Rayo]";
        public Transform LidTransform;
        public bool IsOpened { get; private set; }

        string GetLocalizedWeaponTitle()
        {
            switch (RewardWeapon)
            {
                case WeaponType.SolarGreatsword: return "Espadón Solar [Fuego]";
                case WeaponType.StormHalberd: return "Alabarda de Tormenta [Rayo]";
                default: return "Hoja Rúnica Glacial [Hielo]";
            }
        }

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
            FeelManager.Instance?.SpawnFloatingText(transform.position, $"¡BOTÍN: {GetLocalizedWeaponTitle()}!", Palette.FiligreeGold, 1.2f);

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
}
