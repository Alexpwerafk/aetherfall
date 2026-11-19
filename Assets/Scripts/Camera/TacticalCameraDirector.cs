using Unity.Cinemachine;
using UnityEngine;
using Aetherfall.Core;

namespace Aetherfall.CameraRig
{
    /// <summary>
    /// Cinemachine 3.x Dual Camera Rig:
    /// - IsoTacticalCam for dungeon exploration and BG3-style tactical positioning
    /// - DuelActionCam for Clair-Obscur Expedition 33 over-the-shoulder QTE & parry duels
    /// - TitleOrbitCam for live animated Sanctum title backdrop
    /// </summary>
    public class TacticalCameraDirector : MonoBehaviour
    {
        public static TacticalCameraDirector Instance { get; private set; }

        public CinemachineCamera TitleOrbitCam;
        public CinemachineCamera IsoTacticalCam;
        public CinemachineCamera DuelActionCam;

        public Transform OrbitCenter;
        public Transform HeroTarget;

        Transform _isoFollowProxy;
        Transform _duelFollowPivot;
        Transform _duelLookPivot;

        float _orbitAngle = 25f;
        Transform _activeDuelEnemy;
        bool _duelEnemyAttacking;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _isoFollowProxy = new GameObject("Cam_IsoFollowProxy").transform;
            _duelFollowPivot = new GameObject("Cam_DuelFollowPivot").transform;
            _duelLookPivot = new GameObject("Cam_DuelLookPivot").transform;
            _isoFollowProxy.SetParent(transform, false);
            _duelFollowPivot.SetParent(transform, false);
            _duelLookPivot.SetParent(transform, false);
        }

        void Start()
        {
            if (IsoTacticalCam != null)
            {
                IsoTacticalCam.Target.TrackingTarget = _isoFollowProxy;
                IsoTacticalCam.Target.LookAtTarget = _isoFollowProxy;
            }
            if (TitleOrbitCam != null && OrbitCenter != null)
            {
                TitleOrbitCam.Target.TrackingTarget = OrbitCenter;
                TitleOrbitCam.Target.LookAtTarget = OrbitCenter;
            }
        }

        public void FocusDuel(Transform hero, Transform enemy, bool enemyAttacking)
        {
            HeroTarget = hero;
            _activeDuelEnemy = enemy;
            _duelEnemyAttacking = enemyAttacking;
            UpdateDuelPivots(true);
        }

        void LateUpdate()
        {
            if (GameLoop.Instance == null) return;
            GameState state = GameLoop.Instance.State;

            if (HeroTarget != null)
            {
                _isoFollowProxy.position = Vector3.Lerp(
                    _isoFollowProxy.position,
                    HeroTarget.position + Vector3.up * 0.8f,
                    Time.unscaledDeltaTime * 7f);
            }

            bool isTitle = state == GameState.Title;
            bool isDuel = (state == GameState.ActionExecutionQTE || state == GameState.EnemyTurnDefense) && _activeDuelEnemy != null;

            if (TitleOrbitCam != null)
                TitleOrbitCam.Priority = isTitle ? 40 : 5;
            if (DuelActionCam != null)
                DuelActionCam.Priority = isDuel ? 35 : 10;
            if (IsoTacticalCam != null)
                IsoTacticalCam.Priority = (!isTitle && !isDuel) ? 25 : 15;

            if (isTitle && TitleOrbitCam != null && OrbitCenter != null)
            {
                _orbitAngle += Time.unscaledDeltaTime * 5.2f;
                float rad = _orbitAngle * Mathf.Deg2Rad;
                Vector3 desired = OrbitCenter.position + new Vector3(Mathf.Sin(rad) * 13.5f, 9.2f, Mathf.Cos(rad) * 13.5f);
                TitleOrbitCam.transform.position = desired;
                TitleOrbitCam.transform.LookAt(OrbitCenter.position + Vector3.up * 1.1f);
            }
            else if (isDuel)
            {
                UpdateDuelPivots(false);
            }
        }

        void UpdateDuelPivots(bool snap)
        {
            if (HeroTarget == null || _activeDuelEnemy == null) return;

            Vector3 heroPos = HeroTarget.position;
            Vector3 enemyPos = _activeDuelEnemy.position;
            Vector3 toEnemy = (enemyPos - heroPos);
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude < 0.01f) toEnemy = Vector3.forward;
            Vector3 dir = toEnemy.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;

            Vector3 camTargetPos;
            Vector3 lookTargetPos;

            if (_duelEnemyAttacking)
            {
                // Dramatic Expedition 33 defense angle behind & beside Hero facing incoming strike
                camTargetPos = heroPos - dir * 4.2f + right * 1.85f + Vector3.up * 2.5f;
                lookTargetPos = Vector3.Lerp(heroPos, enemyPos, 0.55f) + Vector3.up * 1.15f;
            }
            else
            {
                // Over-the-shoulder attacker camera during player Resonance QTE
                camTargetPos = heroPos - dir * 3.9f + right * 1.75f + Vector3.up * 2.4f;
                lookTargetPos = Vector3.Lerp(heroPos, enemyPos, 0.65f) + Vector3.up * 1.1f;
            }

            if (snap)
            {
                _duelFollowPivot.position = camTargetPos;
                _duelLookPivot.position = lookTargetPos;
                if (DuelActionCam != null)
                {
                    DuelActionCam.transform.position = camTargetPos;
                    DuelActionCam.transform.LookAt(lookTargetPos);
                }
            }
            else
            {
                _duelFollowPivot.position = Vector3.Lerp(_duelFollowPivot.position, camTargetPos, Time.unscaledDeltaTime * 10f);
                _duelLookPivot.position = Vector3.Lerp(_duelLookPivot.position, lookTargetPos, Time.unscaledDeltaTime * 12f);
                if (DuelActionCam != null)
                {
                    DuelActionCam.transform.position = _duelFollowPivot.position;
                    DuelActionCam.transform.LookAt(_duelLookPivot.position);
                }
            }
        }
    }
}
