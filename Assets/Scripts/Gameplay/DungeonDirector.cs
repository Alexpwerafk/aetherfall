using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Aetherfall.Audio;
using Aetherfall.Core;
using Aetherfall.Feel;

namespace Aetherfall.Gameplay
{
    public struct BoonOption
    {
        public string Id;
        public string Title;
        public string Description;
    }

    /// <summary>
    /// Directs the 3 connected Sanctum Chambers, puzzle barriers, turn orchestration, and Roguelite Boon Drafts.
    /// </summary>
    public class DungeonDirector : MonoBehaviour
    {
        public static DungeonDirector Instance { get; private set; }

        public GameObject Bridge1Barrier;
        public GameObject Bridge2Barrier;

        readonly bool[] _chamberStarted = new bool[4];
        readonly bool[] _chamberCleared = new bool[4];
        bool _enemyTurnRunning;

        readonly List<BoonOption> _currentDraftChoices = new List<BoonOption>();

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
            ElementalSurfaceManager.Instance?.RefreshSceneProps();
            if (ElementalSurfaceManager.Instance != null)
            {
                foreach (RuneCrystalSwitch sw in ElementalSurfaceManager.Instance.Switches)
                {
                    if (sw != null)
                        sw.OnActivated += OnCrystalActivated;
                }
            }
        }

        void OnCrystalActivated(RuneCrystalSwitch sw)
        {
            if (AreBothSwitchesActivated() && _chamberCleared[2])
                UnlockBarrier(2);
            else if (AreBothSwitchesActivated())
                FeelManager.Instance?.SpawnFloatingText(sw.transform.position, "¡SELLO DEL SANTUARIO DEBILITADO!", Palette.FiligreeGold, 1.15f);
        }

        bool AreBothSwitchesActivated()
        {
            if (ElementalSurfaceManager.Instance == null) return true;
            foreach (RuneCrystalSwitch sw in ElementalSurfaceManager.Instance.Switches)
            {
                if (sw != null && !sw.IsActivated) return false;
            }
            return true;
        }

        public float GetMaxUnlockedZ()
        {
            if (_chamberCleared[2] && AreBothSwitchesActivated()) return 66f;
            if (_chamberCleared[1]) return 36.5f;
            return 8.5f;
        }

        public void CheckChamberEntry(Vector3 heroPos)
        {
            if (GameLoop.Instance == null || GameLoop.Instance.State != GameState.Exploring)
                return;

            if (!_chamberStarted[1] && heroPos.z >= -4f && heroPos.z <= 9f)
            {
                StartChamberEncounter(1);
            }
            else if (_chamberCleared[1] && !_chamberStarted[2] && heroPos.z >= 19.5f && heroPos.z <= 37f)
            {
                StartChamberEncounter(2);
            }
            else if (_chamberCleared[2] && !_chamberStarted[3] && heroPos.z >= 47.5f)
            {
                StartChamberEncounter(3);
            }
        }

        public void StartChamberEncounter(int chamberIndex)
        {
            _chamberStarted[chamberIndex] = true;
            GameLoop.Instance.CurrentChamber = chamberIndex;
            GameLoop.Instance.SetState(GameState.TacticalPlayerTurn);
            HeroController.Instance?.StartTurn();

            string title = chamberIndex == 1 ? "CÁMARA 1: EL PATIO QUEBRADO" :
                (chamberIndex == 2 ? "CÁMARA 2: SALA INUNDADA DEL ALQUIMISTA" : "¡CÁMARA 3: TRONO DEL ECLIPSE!");
            FeelManager.Instance?.SpawnFloatingText(HeroController.Instance.transform.position, title, Palette.FiligreeGold, 1.3f);
            SynthAudioManager.Instance?.PlayChestFanfare();
        }

        public List<EnemyAI> GetActiveChamberEnemies()
        {
            List<EnemyAI> list = new List<EnemyAI>();
            int activeChamber = GameLoop.Instance != null ? GameLoop.Instance.CurrentChamber : 1;
            EnemyAI[] all = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
            foreach (EnemyAI e in all)
            {
                if (e != null && !e.IsDead && e.ChamberIndex == activeChamber)
                    list.Add(e);
            }
            return list;
        }

        public void BeginEnemyTurn()
        {
            if (_enemyTurnRunning) return;
            StartCoroutine(EnemyTurnRoutine());
        }

        IEnumerator EnemyTurnRoutine()
        {
            _enemyTurnRunning = true;
            GameLoop.Instance?.SetState(GameState.EnemyTurnDefense);

            List<EnemyAI> enemies = GetActiveChamberEnemies();
            foreach (EnemyAI enemy in enemies)
            {
                if (enemy == null || enemy.IsDead) continue;
                if (HeroController.Instance == null || HeroController.Instance.CurrentHP <= 0) break;

                yield return enemy.ExecuteTurn(HeroController.Instance);
                yield return new WaitForSeconds(0.25f);
            }

            ElementalSurfaceManager.Instance?.TickTurnSurfaces();
            _enemyTurnRunning = false;

            if (HeroController.Instance != null && HeroController.Instance.CurrentHP > 0 &&
                GameLoop.Instance != null && GameLoop.Instance.State != GameState.Won && GameLoop.Instance.State != GameState.Lost)
            {
                if (GetActiveChamberEnemies().Count == 0)
                {
                    CheckChamberCleared();
                }
                else
                {
                    GameLoop.Instance.SetState(GameState.TacticalPlayerTurn);
                    HeroController.Instance.StartTurn();
                }
            }
        }

        public void CheckChamberCleared()
        {
            if (GameLoop.Instance == null) return;
            int ch = GameLoop.Instance.CurrentChamber;
            if (_chamberCleared[ch]) return;
            if (!_chamberStarted[ch]) return;

            if (GetActiveChamberEnemies().Count > 0) return;

            _chamberCleared[ch] = true;
            if (ch >= GameLoop.Instance.MaxChambers)
            {
                GameLoop.Instance.WinExpedition();
                return;
            }

            UnlockBarrier(ch);
            HeroController.Instance?.Heal(35);
            OpenBoonDraft();
        }

        void UnlockBarrier(int chamberIndex)
        {
            if (chamberIndex == 1 && Bridge1Barrier != null)
            {
                VfxPoolManager.Instance?.SpawnParryShockwave(Bridge1Barrier.transform.position);
                Bridge1Barrier.SetActive(false);
            }
            else if (chamberIndex == 2 && Bridge2Barrier != null && AreBothSwitchesActivated())
            {
                VfxPoolManager.Instance?.SpawnParryShockwave(Bridge2Barrier.transform.position);
                Bridge2Barrier.SetActive(false);
            }
        }

        void OpenBoonDraft()
        {
            _currentDraftChoices.Clear();
            _currentDraftChoices.Add(new BoonOption
            {
                Id = "TempestRiposte",
                Title = "RÉPLICA TEMPESTUOSA",
                Description = "Los Parries perfectos infligen +60% de daño de contraataque y electrifican el suelo."
            });
            _currentDraftChoices.Add(new BoonOption
            {
                Id = "AlchemistStride",
                Title = "PASO DEL ALQUIMISTA",
                Description = "Esquivar deja un charco de Aceite detrás de ti y recupera +1 PA."
            });
            _currentDraftChoices.Add(new BoonOption
            {
                Id = "ChronoResonance",
                Title = "CRONO RESONANCIA",
                Description = "Los golpes de Resonancia Perfecta desatan un Eco Crono adicional al instante."
            });

            SynthAudioManager.Instance?.PlayLevelUpSting();
            if (HeroController.Instance != null)
                VfxPoolManager.Instance?.SpawnLevelUpConfetti(HeroController.Instance.transform.position);
            GameLoop.Instance?.SetState(GameState.ChamberClearDraft);
        }

        public IReadOnlyList<BoonOption> GetDraftChoices() => _currentDraftChoices;

        public void PickBoon(string boonId)
        {
            if (GameLoop.Instance == null) return;
            switch (boonId)
            {
                case "TempestRiposte": GameLoop.Instance.BoonTempestRiposte = true; break;
                case "AlchemistStride": GameLoop.Instance.BoonAlchemistStride = true; break;
                case "ChronoResonance": GameLoop.Instance.BoonChronoResonance = true; break;
                default: GameLoop.Instance.BoonColossalForce = true; break;
            }
            SynthAudioManager.Instance?.PlayChestFanfare();
            GameLoop.Instance.NotifyStatsChanged();
            GameLoop.Instance.SetState(GameState.Exploring);
            if (HeroController.Instance != null)
                FeelManager.Instance?.SpawnFloatingText(HeroController.Instance.transform.position, "¡BENDICIÓN SINTONIZADA! AVANZA", Palette.FiligreeGold, 1.2f);
        }
    }
}
