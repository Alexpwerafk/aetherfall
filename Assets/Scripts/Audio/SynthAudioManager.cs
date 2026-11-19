using UnityEngine;

namespace Aetherfall.Audio
{
    /// <summary>
    /// Procedural PCM AudioClip synthesizer generating intentional SFX and dark astral ambient bed.
    /// Exposes MusicVol and SFXVol gain settings for the Pause/Settings shell sliders.
    /// </summary>
    public class SynthAudioManager : MonoBehaviour
    {
        public static SynthAudioManager Instance { get; private set; }

        [Range(0f, 1f)] public float MusicVolume = 0.55f;
        [Range(0f, 1f)] public float SFXVolume = 0.85f;
        public bool IsDucked { get; private set; }

        AudioSource _musicSource;
        AudioSource _sfxSource;

        AudioClip _slashClip;
        AudioClip _resonanceClip;
        AudioClip _parryBellClip;
        AudioClip _riposteClip;
        AudioClip _dodgeClip;
        AudioClip _fireClip;
        AudioClip _lightningClip;
        AudioClip _frostClip;
        AudioClip _qteRiserClip;
        AudioClip _chestClip;
        AudioClip _levelUpClip;
        AudioClip _uiClickClip;
        AudioClip _hitTakenClip;
        AudioClip _ambientBedClip;

        const int SampleRate = 44100;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.spatialBlend = 0f;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.spatialBlend = 0f;

            BuildAllClips();
            _musicSource.clip = _ambientBedClip;
            UpdateMusicGain();
            _musicSource.Play();
        }

        public void SetMusicVolume(float v)
        {
            MusicVolume = Mathf.Clamp01(v);
            UpdateMusicGain();
        }

        public void SetSFXVolume(float v)
        {
            SFXVolume = Mathf.Clamp01(v);
        }

        public void SetDucked(bool ducked)
        {
            IsDucked = ducked;
            UpdateMusicGain();
        }

        void UpdateMusicGain()
        {
            if (_musicSource != null)
            {
                float mult = IsDucked ? 0.45f : 1.0f;
                _musicSource.volume = MusicVolume * 0.35f * mult;
            }
        }

        void PlayClip(AudioClip clip, float pitchMin = 0.94f, float pitchMax = 1.06f, float gain = 1f)
        {
            if (clip == null || _sfxSource == null) return;
            _sfxSource.pitch = Random.Range(pitchMin, pitchMax);
            _sfxSource.PlayOneShot(clip, SFXVolume * gain);
        }

        public void PlaySlash() => PlayClip(_slashClip, 0.92f, 1.08f, 0.85f);
        public void PlayPerfectResonance() => PlayClip(_resonanceClip, 0.98f, 1.03f, 0.95f);
        public void PlayParryBell() => PlayClip(_parryBellClip, 0.97f, 1.04f, 1.0f);
        public void PlayRiposteThunder() => PlayClip(_riposteClip, 0.93f, 1.03f, 1.0f);
        public void PlayDodgeWhoosh() => PlayClip(_dodgeClip, 0.95f, 1.1f, 0.75f);
        public void PlayQTERiser() => PlayClip(_qteRiserClip, 1f, 1f, 0.55f);
        public void PlayChestFanfare() => PlayClip(_chestClip, 1f, 1f, 0.9f);
        public void PlayLevelUpSting() => PlayClip(_levelUpClip, 1f, 1f, 0.95f);
        public void PlayUIClick() => PlayClip(_uiClickClip, 0.95f, 1.05f, 0.55f);
        public void PlayHitTaken() => PlayClip(_hitTakenClip, 0.90f, 1.02f, 0.9f);

        public void PlayElementalReaction(string kind)
        {
            switch (kind)
            {
                case "Fire":
                case "Oil":
                    PlayClip(_fireClip, 0.9f, 1.08f, 0.95f);
                    break;
                case "Lightning":
                    PlayClip(_lightningClip, 0.95f, 1.1f, 0.9f);
                    break;
                case "Frost":
                    PlayClip(_frostClip, 0.95f, 1.08f, 0.85f);
                    break;
                default:
                    PlayClip(_slashClip, 0.85f, 0.95f, 0.85f);
                    break;
            }
        }

        void BuildAllClips()
        {
            _slashClip = MakeNoiseSweep("SFX_Slash", 0.16f, 620f, 160f, 0.55f);
            _dodgeClip = MakeNoiseSweep("SFX_Dodge", 0.14f, 440f, 920f, 0.45f);
            _hitTakenClip = MakeImpact("SFX_HitTaken", 0.22f, 150f, 55f);
            _uiClickClip = MakeTone("SFX_UIClick", 0.035f, 960f, 640f, 0.35f);
            _resonanceClip = MakeTriad("SFX_PerfectResonance", new[] { 587.33f, 880f, 1174.66f }, 0.11f, 0.04f, 0.5f);
            _parryBellClip = MakeMetallicBell("SFX_ParryBell", 0.48f, 1046.5f);
            _riposteClip = MakeImpact("SFX_Riposte", 0.35f, 220f, 48f);
            _fireClip = MakeNoiseSweep("SFX_FireBurst", 0.42f, 340f, 90f, 0.75f);
            _lightningClip = MakeZap("SFX_Lightning", 0.28f);
            _frostClip = MakeMetallicBell("SFX_FrostShatter", 0.32f, 1567.98f);
            _qteRiserClip = MakeTone("SFX_QTERiser", 0.45f, 320f, 680f, 0.25f);
            _chestClip = MakeTriad("SFX_ChestFanfare", new[] { 440f, 554.37f, 659.25f, 880f }, 0.11f, 0.03f, 0.45f);
            _levelUpClip = MakeTriad("SFX_LevelUp", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.13f, 0.04f, 0.5f);
            _ambientBedClip = MakeAstralAmbientBed("Amb_SanctumBed", 6.0f);
        }

        static void ApplyFadeEnvelope(float[] samples, float fadeSec = 0.005f)
        {
            int fadeSamples = Mathf.Min(samples.Length / 2, Mathf.RoundToInt(fadeSec * SampleRate));
            for (int i = 0; i < fadeSamples; i++)
            {
                float gain = i / (float)Mathf.Max(1, fadeSamples);
                samples[i] *= gain;
                samples[samples.Length - 1 - i] *= gain;
            }
        }

        static AudioClip ToClip(string name, float[] data)
        {
            ApplyFadeEnvelope(data);
            AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip MakeTone(string name, float duration, float fStart, float fEnd, float amp)
        {
            int n = Mathf.RoundToInt(duration * SampleRate);
            float[] data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float freq = Mathf.Lerp(fStart, fEnd, t);
                phase += 2f * Mathf.PI * freq / SampleRate;
                float env = Mathf.Pow(1f - t, 1.5f);
                data[i] = Mathf.Sin(phase) * env * amp;
            }
            return ToClip(name, data);
        }

        static AudioClip MakeNoiseSweep(string name, float duration, float fStart, float fEnd, float amp)
        {
            int n = Mathf.RoundToInt(duration * SampleRate);
            float[] data = new float[n];
            float lp = 0f;
            float phase = 0f;
            System.Random rng = new System.Random(name.GetHashCode());
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                float cutoff = Mathf.Lerp(fStart, fEnd, t);
                float rc = 1f / (2f * Mathf.PI * cutoff);
                float dt = 1f / SampleRate;
                float alpha = dt / (rc + dt);
                lp += alpha * (white - lp);
                phase += 2f * Mathf.PI * cutoff * 0.5f / SampleRate;
                float env = Mathf.Pow(1f - t, 1.8f);
                data[i] = (lp * 0.75f + Mathf.Sin(phase) * 0.25f) * env * amp;
            }
            return ToClip(name, data);
        }

        static AudioClip MakeImpact(string name, float duration, float fStart, float fEnd)
        {
            int n = Mathf.RoundToInt(duration * SampleRate);
            float[] data = new float[n];
            float phase = 0f;
            System.Random rng = new System.Random(name.GetHashCode());
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float freq = Mathf.Lerp(fStart, fEnd, Mathf.Pow(t, 0.5f));
                phase += 2f * Mathf.PI * freq / SampleRate;
                float punch = Mathf.Sin(phase) * Mathf.Pow(1f - t, 1.4f);
                float crunch = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Pow(1f - t, 4.5f) * 0.45f;
                data[i] = Mathf.Clamp((punch * 0.8f + crunch) * 0.75f, -0.95f, 0.95f);
            }
            return ToClip(name, data);
        }

        static AudioClip MakeMetallicBell(string name, float duration, float baseFreq)
        {
            int n = Mathf.RoundToInt(duration * SampleRate);
            float[] data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float norm = i / (float)n;
                float env = Mathf.Exp(-norm * 4.5f);
                float s = Mathf.Sin(2f * Mathf.PI * baseFreq * t) * 0.5f
                        + Mathf.Sin(2f * Mathf.PI * baseFreq * 1.5f * t) * 0.3f
                        + Mathf.Sin(2f * Mathf.PI * baseFreq * 2.41f * t) * 0.2f;
                data[i] = s * env * 0.65f;
            }
            return ToClip(name, data);
        }

        static AudioClip MakeTriad(string name, float[] freqs, float noteLen, float overlap, float amp)
        {
            float step = noteLen - overlap;
            float totalDur = noteLen + (freqs.Length - 1) * step;
            int n = Mathf.RoundToInt(totalDur * SampleRate);
            float[] data = new float[n];

            for (int idx = 0; idx < freqs.Length; idx++)
            {
                int start = Mathf.RoundToInt(idx * step * SampleRate);
                int len = Mathf.RoundToInt(noteLen * 1.6f * SampleRate);
                float f = freqs[idx];
                for (int j = 0; j < len && start + j < n; j++)
                {
                    float t = j / (float)SampleRate;
                    float localNorm = j / (float)len;
                    float env = Mathf.Sin(Mathf.Min(localNorm * 6f, 1f) * Mathf.PI * 0.5f) * Mathf.Pow(1f - localNorm, 1.5f);
                    float harmonic = Mathf.Sin(2f * Mathf.PI * f * t) * 0.75f + Mathf.Sin(4f * Mathf.PI * f * t) * 0.25f;
                    data[start + j] += harmonic * env * amp * 0.5f;
                }
            }
            return ToClip(name, data);
        }

        static AudioClip MakeZap(string name, float duration)
        {
            int n = Mathf.RoundToInt(duration * SampleRate);
            float[] data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float freq = 1400f - 1050f * t + Mathf.Sin(t * 95f) * 320f;
                phase += 2f * Mathf.PI * freq / SampleRate;
                float sq = Mathf.Sign(Mathf.Sin(phase)) * 0.35f + Mathf.Sin(phase) * 0.45f;
                data[i] = sq * Mathf.Pow(1f - t, 1.4f) * 0.5f;
            }
            return ToClip(name, data);
        }

        static AudioClip MakeAstralAmbientBed(string name, float duration)
        {
            int n = Mathf.RoundToInt(duration * SampleRate);
            float[] data = new float[n];
            float brown = 0f;
            System.Random rng = new System.Random(1337);
            // Seamless integer cycles over 6s: D2 (73.5Hz ~ 441 cycles), A2 (110Hz ~ 660 cycles), F3 (174.5Hz ~ 1047 cycles)
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float lfo = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * (1f / duration) * t);
                float pad = Mathf.Sin(2f * Mathf.PI * 73.5f * t) * 0.32f
                          + Mathf.Sin(2f * Mathf.PI * 110.0f * t) * 0.22f
                          + Mathf.Sin(2f * Mathf.PI * 174.5f * t) * (0.08f + 0.08f * lfo);
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                brown = Mathf.Clamp(brown * 0.992f + white * 0.015f, -0.5f, 0.5f);
                data[i] = (pad * 0.35f + brown * 0.22f) * 0.55f;
            }
            AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
