using System;
using UnityEngine;
using Aetherfall.Audio;
using Aetherfall.Core;
using Aetherfall.Feel;

namespace Aetherfall.Gameplay
{
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
}
