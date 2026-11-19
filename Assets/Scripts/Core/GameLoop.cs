using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Aetherfall.Audio;

namespace Aetherfall.Core
{
    public enum GameState
    {
        Title,
        Exploring,
        TacticalPlayerTurn,
        ActionExecutionQTE,
        EnemyTurnDefense,
        ChamberClearDraft,
        Paused,
        Won,
        Lost
    }

    public enum ElementType
    {
        Physical,
        Fire,
        Lightning,
        Frost,
        Oil,
        Water
    }

    public enum WeaponType
    {
        SolarGreatsword,
        StormHalberd,
        FrostRuneblade
    }

    [Serializable]
    public class ExpeditionSaveData
    {
        public int highestChamber;
        public int totalParries;
        public int totalPerfectResonances;
        public int totalElementalCombos;
        public int totalVictories;
    }

    /// <summary>
    /// Single authoritative run-state owner for Aetherfall.
    /// Coordinates progression, roguelite boons, combat metrics, and cloud save integration.
    /// </summary>
    public class GameLoop : MonoBehaviour
    {
        public static GameLoop Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Title;
        public GameState StateBeforePause { get; private set; } = GameState.Exploring;

        [Header("Progression & Metrics")]
        public int CurrentChamber = 1;
        public int MaxChambers = 3;
        public int Level = 1;
        public int CurrentXP = 0;
        public int XPToNextLevel = 100;

        public int ParriesLanded = 0;
        public int PerfectResonances = 0;
        public int ElementalCombos = 0;
        public int EnemiesDefeated = 0;

        [Header("Roguelite Boons")]
        public bool BoonTempestRiposte = false;
        public bool BoonAlchemistStride = false;
        public bool BoonChronoResonance = false;
        public bool BoonColossalForce = false;

        public event Action<GameState> OnStateChanged;
        public event Action OnStatsChanged;
        public event Action<int> OnLevelUp;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            SetState(GameState.Title);
            CheckLiveOps();
        }

        async void CheckLiveOps()
        {
            try
            {
                await LiveOps.IsEnabledAsync();
            }
            catch
            {
                // Live services remain safely optional offline.
            }
        }

        public void SetState(GameState newState)
        {
            if (State == newState) return;
            State = newState;
            if (State != GameState.Paused)
                Time.timeScale = 1f;
            OnStateChanged?.Invoke(State);
        }

        public void StartExpedition()
        {
            if (State != GameState.Title) return;
            SynthAudioManager.Instance?.PlayUIClick();
            SetState(GameState.Exploring);
        }

        public void TogglePause()
        {
            if (State == GameState.Title || State == GameState.Won || State == GameState.Lost)
                return;

            SynthAudioManager.Instance?.PlayUIClick();
            if (State == GameState.Paused)
            {
                Time.timeScale = 1f;
                SynthAudioManager.Instance?.SetDucked(false);
                SetState(StateBeforePause);
            }
            else
            {
                StateBeforePause = State;
                Time.timeScale = 0f;
                SynthAudioManager.Instance?.SetDucked(true);
                SetState(GameState.Paused);
            }
        }

        public void GrantXP(int amount)
        {
            CurrentXP += amount;
            while (CurrentXP >= XPToNextLevel)
            {
                CurrentXP -= XPToNextLevel;
                Level++;
                XPToNextLevel = Mathf.RoundToInt(XPToNextLevel * 1.35f);
                SynthAudioManager.Instance?.PlayLevelUpSting();
                OnLevelUp?.Invoke(Level);
            }
            NotifyStatsChanged();
        }

        public void NotifyStatsChanged()
        {
            OnStatsChanged?.Invoke();
        }

        public void WinExpedition()
        {
            if (State == GameState.Won) return;
            SynthAudioManager.Instance?.PlayLevelUpSting();
            SetState(GameState.Won);
            SaveRunAsync(true);
        }

        public void LoseExpedition()
        {
            if (State == GameState.Lost) return;
            SynthAudioManager.Instance?.PlayHitTaken();
            SetState(GameState.Lost);
            SaveRunAsync(false);
        }

        async void SaveRunAsync(bool victory)
        {
            try
            {
                var data = new ExpeditionSaveData
                {
                    highestChamber = CurrentChamber,
                    totalParries = ParriesLanded,
                    totalPerfectResonances = PerfectResonances,
                    totalElementalCombos = ElementalCombos,
                    totalVictories = victory ? 1 : 0
                };
                await PlayerSave.SaveAsync(data);
            }
            catch
            {
                // Offline fallback requires no crash.
            }
        }

        public void RestartExpedition()
        {
            Time.timeScale = 1f;
            SynthAudioManager.Instance?.SetDucked(false);
            SynthAudioManager.Instance?.PlayUIClick();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
