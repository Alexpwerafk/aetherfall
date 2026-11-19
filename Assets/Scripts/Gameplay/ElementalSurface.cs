using UnityEngine;
using Aetherfall.Core;

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
                default:
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
}
