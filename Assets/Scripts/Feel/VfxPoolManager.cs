using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Aetherfall.Core;

namespace Aetherfall.Feel
{
    /// <summary>
    /// Object-pooled VFX manager using UnityEngine.Pool.ObjectPool<ParticleSystem>.
    /// Uses Sprites/Default tinted from Palette so alpha gradients render cleanly in URP.
    /// </summary>
    public class VfxPoolManager : MonoBehaviour
    {
        public static VfxPoolManager Instance { get; private set; }

        ObjectPool<ParticleSystem> _burstPool;
        readonly List<ParticleSystem> _activeTracked = new List<ParticleSystem>();
        Material _particleMat;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader == null)
                spriteShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            _particleMat = new Material(spriteShader) { name = "VFX_PaletteParticleMat" };
            _particleMat.mainTexture = CreateRadialGlowTexture(64);

            _burstPool = new ObjectPool<ParticleSystem>(
                createFunc: CreatePooledParticle,
                actionOnGet: ps => ps.gameObject.SetActive(true),
                actionOnRelease: ps =>
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.gameObject.SetActive(false);
                },
                actionOnDestroy: ps => { if (ps != null) Destroy(ps.gameObject); },
                collectionCheck: false,
                defaultCapacity: 16,
                maxSize: 40);
        }

        void Update()
        {
            for (int i = _activeTracked.Count - 1; i >= 0; i--)
            {
                ParticleSystem ps = _activeTracked[i];
                if (ps == null)
                {
                    _activeTracked.RemoveAt(i);
                    continue;
                }
                if (!ps.IsAlive(true))
                {
                    _activeTracked.RemoveAt(i);
                    _burstPool.Release(ps);
                }
            }
        }

        ParticleSystem CreatePooledParticle()
        {
            GameObject go = new GameObject("PooledVFX");
            go.transform.SetParent(transform, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 0.55f;
            main.startSpeed = 4.5f;
            main.startSize = 0.24f;
            main.gravityModifier = 0.6f;

            var emission = ps.emission;
            emission.enabled = false;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(1f, 0f));
            sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _particleMat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        static Texture2D CreateRadialGlowTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half + 0.5f) / half;
                    float dy = (y - half + 0.5f) / half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - r);
                    a = a * a * (3f - 2f * a);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            return tex;
        }

        void Start()
        {
            CreateAmbientMotes(new Vector3(0f, 3.2f, 28f), new Vector3(18f, 4.5f, 70f));
        }

        public void SpawnBurst(
            Vector3 pos,
            Color colorA,
            Color colorB,
            int count = 16,
            float speed = 5.5f,
            float size = 0.26f,
            float lifetime = 0.5f,
            float gravity = 0.5f,
            float radius = 0.3f)
        {
            ParticleSystem ps = _burstPool.Get();
            ps.transform.position = pos;

            var main = ps.main;
            main.startLifetime = lifetime;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.55f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size * 1.2f);
            main.gravityModifier = gravity;
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;

            ps.Clear(true);
            ps.Emit(count);
            ps.Play();
            if (!_activeTracked.Contains(ps))
                _activeTracked.Add(ps);
        }

        public void SpawnResonanceBurst(Vector3 pos)
        {
            SpawnBurst(pos + Vector3.up * 1.1f, Palette.FiligreeGold, Palette.FireAmber, 28, 7.2f, 0.34f, 0.55f, 0.3f, 0.35f);
        }

        public void SpawnParryShockwave(Vector3 pos)
        {
            SpawnBurst(pos + Vector3.up * 1.1f, Palette.LightningCyan, Palette.FiligreeGold, 32, 8.5f, 0.36f, 0.50f, 0.05f, 0.45f);
        }

        public void SpawnFireExplosion(Vector3 pos)
        {
            SpawnBurst(pos + Vector3.up * 0.6f, Palette.FireAmber, Palette.HeroCrimson, 36, 8.0f, 0.48f, 0.65f, -0.25f, 0.7f);
        }

        public void SpawnLightningArc(Vector3 pos)
        {
            SpawnBurst(pos + Vector3.up * 1.0f, Palette.LightningCyan, Palette.IvoryCloth, 24, 9.0f, 0.28f, 0.40f, 0f, 0.5f);
        }

        public void SpawnFrostShatter(Vector3 pos)
        {
            SpawnBurst(pos + Vector3.up * 0.8f, Palette.FrostIce, Palette.WaterAzure, 26, 6.2f, 0.32f, 0.60f, 0.9f, 0.45f);
        }

        public void SpawnOilSplash(Vector3 pos)
        {
            SpawnBurst(pos + Vector3.up * 0.4f, Palette.OilDark, Palette.MarbleTrim, 18, 4.5f, 0.30f, 0.45f, 1.2f, 0.4f);
        }

        public void SpawnWaterSplash(Vector3 pos)
        {
            SpawnBurst(pos + Vector3.up * 0.4f, Palette.WaterAzure, Palette.FrostIce, 22, 5.2f, 0.28f, 0.45f, 1.1f, 0.45f);
        }

        public void SpawnFootstep(Vector3 pos)
        {
            SpawnBurst(pos + Vector3.up * 0.08f, Palette.MarbleTrim, Palette.ObsidianFloor, 4, 1.5f, 0.16f, 0.30f, -0.1f, 0.15f);
        }

        public void SpawnLevelUpConfetti(Vector3 pos)
        {
            SpawnBurst(pos + Vector3.up * 1.4f, Palette.FiligreeGold, Palette.LightningCyan, 42, 7.8f, 0.28f, 1.1f, 0.7f, 0.5f);
        }

        public void CreateAmbientMotes(Vector3 center, Vector3 boxSize)
        {
            GameObject go = new GameObject("AmbientAstralMotes");
            go.transform.SetParent(transform, false);
            go.transform.position = center;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.maxParticles = 30;
            main.startLifetime = 4.5f;
            main.startSpeed = 0.25f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(Palette.FiligreeGold.r, Palette.FiligreeGold.g, Palette.FiligreeGold.b, 0.45f),
                new Color(Palette.LightningCyan.r, Palette.LightningCyan.g, Palette.LightningCyan.b, 0.45f));

            var emission = ps.emission;
            emission.rateOverTime = 7f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = boxSize;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _particleMat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
        }
    }
}
