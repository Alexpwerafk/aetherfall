using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Aetherfall.Audio;
using Aetherfall.CameraRig;
using Aetherfall.Core;
using Aetherfall.Feel;

namespace Aetherfall.Gameplay
{
    public enum TacticalActionMode
    {
        MoveOrSelect,
        Strike,
        Flask,
        FreeAimShot,
        Shove
    }

    /// <summary>
    /// Controls both Real-Time Dungeon Exploration and BG3/Expedition33 Tactical Turn Combat.
    /// </summary>
    public class HeroController : MonoBehaviour
    {
        public static HeroController Instance { get; private set; }

        public ProceduralCharacterAnimator Animator { get; private set; }

        [Header("Stats")]
        public int MaxHP = 120;
        public int CurrentHP = 120;
        public int MaxAP = 6;
        public int CurrentAP = 3;
        public int APPerTurn = 3;
        public float MoveSpeed = 6.2f;
        public float MaxTacticalMoveDist = 9.0f;
        public float RemainingMoveDist = 9.0f;

        [Header("Equipment & Skills")]
        public WeaponType EquippedWeapon = WeaponType.SolarGreatsword;
        public ElementType CurrentElement { get; private set; } = ElementType.Fire;
        public TacticalActionMode ActiveAction { get; private set; } = TacticalActionMode.MoveOrSelect;
        public bool FlaskThrowsOil = true;

        [Header("Weapon Visuals")]
        public Transform WeaponMount;
        public GameObject SolarGreatswordModel;
        public GameObject StormHalberdModel;
        public GameObject FrostRunebladeModel;

        [Header("Tactical Visuals")]
        public LineRenderer MoveRingVisual;
        public LineRenderer AimIndicatorLine;
        public GameObject TargetReticle;

        Vector3 _moveTarget;
        bool _hasClickMoveTarget;
        bool _isExecutingAction;
        float _footstepTimer;
        Vector3 _lastPos;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Animator = GetComponent<ProceduralCharacterAnimator>();
            if (Animator == null)
                Animator = gameObject.AddComponent<ProceduralCharacterAnimator>();
            _lastPos = transform.position;
        }

        void Start()
        {
            EquipWeapon(EquippedWeapon, silent: true);
            BuildTacticalIndicators();
            if (TacticalCameraDirector.Instance != null)
                TacticalCameraDirector.Instance.HeroTarget = transform;
            GameLoop.Instance?.NotifyStatsChanged();
        }

        void BuildTacticalIndicators()
        {
            if (MoveRingVisual == null)
            {
                GameObject ringGo = new GameObject("TacticalMoveRing");
                ringGo.transform.SetParent(transform, false);
                MoveRingVisual = ringGo.AddComponent<LineRenderer>();
                MoveRingVisual.useWorldSpace = false;
                MoveRingVisual.loop = true;
                MoveRingVisual.startWidth = 0.08f;
                MoveRingVisual.endWidth = 0.08f;
                MoveRingVisual.positionCount = 48;
                Material ringMat = new Material(Shader.Find("Sprites/Default")) { color = Palette.LightningCyan };
                MoveRingVisual.sharedMaterial = ringMat;
                MoveRingVisual.startColor = Palette.LightningCyan;
                MoveRingVisual.endColor = Palette.LightningCyan;
            }

            if (AimIndicatorLine == null)
            {
                GameObject aimGo = new GameObject("TacticalAimLine");
                aimGo.transform.SetParent(transform, false);
                AimIndicatorLine = aimGo.AddComponent<LineRenderer>();
                AimIndicatorLine.useWorldSpace = true;
                AimIndicatorLine.startWidth = 0.09f;
                AimIndicatorLine.endWidth = 0.05f;
                AimIndicatorLine.positionCount = 2;
                Material aimMat = new Material(Shader.Find("Sprites/Default")) { color = Palette.FiligreeGold };
                AimIndicatorLine.sharedMaterial = aimMat;
                AimIndicatorLine.startColor = Palette.FiligreeGold;
                AimIndicatorLine.endColor = Palette.FireAmber;
                AimIndicatorLine.enabled = false;
            }

            if (TargetReticle == null)
            {
                TargetReticle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                TargetReticle.name = "TacticalTargetReticle";
                Destroy(TargetReticle.GetComponent<Collider>());
                TargetReticle.transform.localScale = new Vector3(1.6f, 0.03f, 1.6f);
                Renderer r = TargetReticle.GetComponent<Renderer>();
                r.sharedMaterial = Palette.CreateToonMaterial("ReticleMat", Palette.FiligreeGold, Palette.FiligreeGold * 1.4f, 4f);
                TargetReticle.SetActive(false);
            }
        }

        public void EquipWeapon(WeaponType weapon, bool silent = false)
        {
            EquippedWeapon = weapon;
            switch (weapon)
            {
                case WeaponType.SolarGreatsword:
                    CurrentElement = ElementType.Fire;
                    break;
                case WeaponType.StormHalberd:
                    CurrentElement = ElementType.Lightning;
                    break;
                case WeaponType.FrostRuneblade:
                    CurrentElement = ElementType.Frost;
                    break;
            }

            if (SolarGreatswordModel != null) SolarGreatswordModel.SetActive(weapon == WeaponType.SolarGreatsword);
            if (StormHalberdModel != null) StormHalberdModel.SetActive(weapon == WeaponType.StormHalberd);
            if (FrostRunebladeModel != null) FrostRunebladeModel.SetActive(weapon == WeaponType.FrostRuneblade);

            if (!silent)
            {
                FeelManager.Instance?.PopTransform(transform, 1.15f, 0.22f);
                GameLoop.Instance?.NotifyStatsChanged();
            }
        }

        public string GetWeaponDisplayName()
        {
            switch (EquippedWeapon)
            {
                case WeaponType.SolarGreatsword: return "Solar Greatsword (Fire)";
                case WeaponType.StormHalberd: return "Storm Halberd (Lightning)";
                default: return "Frost Runeblade (Frost)";
            }
        }

        public void StartTurn()
        {
            CurrentAP = Mathf.Min(MaxAP, CurrentAP + APPerTurn);
            RemainingMoveDist = MaxTacticalMoveDist;
            ActiveAction = TacticalActionMode.MoveOrSelect;
            _hasClickMoveTarget = false;
            GameLoop.Instance?.NotifyStatsChanged();
        }

        public void RefundAP(int amount)
        {
            CurrentAP = Mathf.Min(MaxAP, CurrentAP + amount);
            GameLoop.Instance?.NotifyStatsChanged();
        }

        public void SelectAction(TacticalActionMode mode)
        {
            if (GameLoop.Instance == null || GameLoop.Instance.State != GameState.TacticalPlayerTurn)
                return;

            if (mode == TacticalActionMode.Flask && ActiveAction == TacticalActionMode.Flask)
                FlaskThrowsOil = !FlaskThrowsOil;

            ActiveAction = mode;
            _hasClickMoveTarget = false;
            SynthAudioManager.Instance?.PlayUIClick();
            GameLoop.Instance?.NotifyStatsChanged();
        }

        public void ToggleFlaskType()
        {
            FlaskThrowsOil = !FlaskThrowsOil;
            SynthAudioManager.Instance?.PlayUIClick();
            GameLoop.Instance?.NotifyStatsChanged();
        }

        void Update()
        {
            if (GameLoop.Instance == null) return;
            GameLoop loop = GameLoop.Instance;
            var kb = Keyboard.current;
            var mouse = Mouse.current;

            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                loop.TogglePause();
                return;
            }

            if (loop.State == GameState.Title)
            {
                if ((kb != null && kb.anyKey.wasPressedThisFrame) ||
                    (mouse != null && mouse.leftButton.wasPressedThisFrame))
                {
                    loop.StartExpedition();
                }
                UpdateVisualMoveRing(false);
                return;
            }

            if (loop.State == GameState.Paused || loop.State == GameState.Won ||
                loop.State == GameState.Lost || loop.State == GameState.ChamberClearDraft)
            {
                UpdateVisualMoveRing(false);
                return;
            }

            if (_isExecutingAction)
            {
                UpdateVisualMoveRing(false);
                UpdateVelocityAnimation(0f);
                return;
            }

            if (loop.State == GameState.Exploring)
            {
                UpdateVisualMoveRing(false);
                HandleExplorationUpdate(kb, mouse);
            }
            else if (loop.State == GameState.TacticalPlayerTurn)
            {
                UpdateVisualMoveRing(true);
                HandleTacticalTurnUpdate(kb, mouse);
            }
            else
            {
                UpdateVisualMoveRing(false);
                UpdateVelocityAnimation(0f);
            }
        }

        void UpdateVisualMoveRing(bool show)
        {
            if (MoveRingVisual != null)
            {
                MoveRingVisual.enabled = show && RemainingMoveDist > 0.25f;
                if (MoveRingVisual.enabled)
                {
                    float r = Mathf.Max(0.4f, RemainingMoveDist);
                    for (int i = 0; i < MoveRingVisual.positionCount; i++)
                    {
                        float ang = i / (float)MoveRingVisual.positionCount * Mathf.PI * 2f;
                        MoveRingVisual.SetPosition(i, new Vector3(Mathf.Cos(ang) * r, 0.06f, Mathf.Sin(ang) * r));
                    }
                }
            }
            if (!show)
            {
                if (AimIndicatorLine != null) AimIndicatorLine.enabled = false;
                if (TargetReticle != null) TargetReticle.SetActive(false);
            }
        }

        Vector3 ReadCameraAlignedMoveInput(Keyboard kb)
        {
            if (kb == null) return Vector3.zero;
            float h = 0f;
            float v = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;

            if (Mathf.Abs(h) < 0.01f && Mathf.Abs(v) < 0.01f)
                return Vector3.zero;

            Camera cam = Camera.main;
            Vector3 fwd = cam != null ? cam.transform.forward : new Vector3(1f, 0f, 1f);
            Vector3 right = cam != null ? cam.transform.right : new Vector3(1f, 0f, -1f);
            fwd.y = 0f;
            right.y = 0f;
            fwd.Normalize();
            right.Normalize();
            return (fwd * v + right * h).normalized;
        }

        void MoveWithinArena(Vector3 delta)
        {
            Vector3 next = transform.position + delta;
            next = ClampToPlayableWalkway(next);
            transform.position = next;
        }

        Vector3 ClampToPlayableWalkway(Vector3 pos)
        {
            float maxZ = DungeonDirector.Instance != null ? DungeonDirector.Instance.GetMaxUnlockedZ() : 8f;
            pos.z = Mathf.Clamp(pos.z, -8.5f, maxZ);

            // Narrow bridges between chambers (Z: 9..19 and 37..47)
            bool onBridge1 = pos.z > 9.2f && pos.z < 18.8f;
            bool onBridge2 = pos.z > 37.2f && pos.z < 46.8f;
            float maxX = (onBridge1 || onBridge2) ? 2.1f : 9.2f;
            pos.x = Mathf.Clamp(pos.x, -maxX, maxX);
            pos.y = 0f;
            return pos;
        }

        void UpdateVelocityAnimation(float speedNorm)
        {
            if (Animator != null)
                Animator.NormalizedSpeed = speedNorm;

            if (speedNorm > 0.25f)
            {
                _footstepTimer += Time.deltaTime;
                if (_footstepTimer >= 0.34f)
                {
                    _footstepTimer = 0f;
                    VfxPoolManager.Instance?.SpawnFootstep(transform.position);
                }
            }
            _lastPos = transform.position;
        }

        void HandleExplorationUpdate(Keyboard kb, Mouse mouse)
        {
            Vector3 moveDir = ReadCameraAlignedMoveInput(kb);
            if (moveDir.sqrMagnitude > 0.01f)
            {
                _hasClickMoveTarget = false;
                MoveWithinArena(moveDir * MoveSpeed * Time.deltaTime);
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 12f);
                UpdateVelocityAnimation(1f);
            }
            else if (_hasClickMoveTarget)
            {
                Vector3 toTarget = _moveTarget - transform.position;
                toTarget.y = 0f;
                if (toTarget.magnitude > 0.25f)
                {
                    Vector3 step = toTarget.normalized * MoveSpeed * Time.deltaTime;
                    if (step.magnitude > toTarget.magnitude) step = toTarget;
                    MoveWithinArena(step);
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toTarget.normalized), Time.deltaTime * 12f);
                    UpdateVelocityAnimation(0.9f);
                }
                else
                {
                    _hasClickMoveTarget = false;
                    UpdateVelocityAnimation(0f);
                }
            }
            else
            {
                UpdateVelocityAnimation(0f);
            }

            // Auto-open Golden Chests on proximity
            if (ElementalSurfaceManager.Instance != null)
            {
                foreach (GoldenChest chest in ElementalSurfaceManager.Instance.Chests)
                {
                    if (chest != null && !chest.IsOpened && Vector3.Distance(transform.position, chest.transform.position) <= 2.1f)
                        chest.Open(this);
                }

                // Activate crystal switches on strike/F key or click
                if (kb != null && (kb.fKey.wasPressedThisFrame || kb.digit1Key.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                {
                    Animator?.TriggerAttackSwing();
                    SynthAudioManager.Instance?.PlaySlash();
                    foreach (RuneCrystalSwitch sw in ElementalSurfaceManager.Instance.Switches)
                    {
                        if (sw != null && !sw.IsActivated && Vector3.Distance(transform.position, sw.transform.position) <= 3.0f)
                            sw.Activate();
                    }
                }
            }

            // Click to move or shoot crystal switches during exploration
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !IsPointerOverUI())
            {
                if (TryRaycastCursor(out Vector3 hitPoint, out EnemyAI _, out ExplosiveBarrel barrel, out RuneCrystalSwitch crystal, out GoldenChest chest))
                {
                    if (crystal != null && !crystal.IsActivated)
                    {
                        StartCoroutine(ExplorationCrystalBolt(crystal));
                    }
                    else if (barrel != null && !barrel.IsDetonated)
                    {
                        barrel.Detonate();
                    }
                    else
                    {
                        _moveTarget = chest != null ? chest.transform.position : hitPoint;
                        _hasClickMoveTarget = true;
                    }
                }
            }

            DungeonDirector.Instance?.CheckChamberEntry(transform.position);
        }

        IEnumerator ExplorationCrystalBolt(RuneCrystalSwitch crystal)
        {
            _isExecutingAction = true;
            Vector3 dir = crystal.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);
            Animator?.TriggerAttackSwing();
            SynthAudioManager.Instance?.PlayElementalReaction("Lightning");
            VfxPoolManager.Instance?.SpawnLightningArc(crystal.transform.position);
            crystal.Activate();
            yield return new WaitForSeconds(0.25f);
            _isExecutingAction = false;
        }

        void HandleTacticalTurnUpdate(Keyboard kb, Mouse mouse)
        {
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) SelectAction(TacticalActionMode.Strike);
                else if (kb.digit2Key.wasPressedThisFrame) SelectAction(TacticalActionMode.Flask);
                else if (kb.digit3Key.wasPressedThisFrame) SelectAction(TacticalActionMode.FreeAimShot);
                else if (kb.digit4Key.wasPressedThisFrame) SelectAction(TacticalActionMode.Shove);
                else if (kb.tabKey.wasPressedThisFrame) { EndPlayerTurn(); return; }
            }

            // WASD tactical movement
            Vector3 moveDir = ReadCameraAlignedMoveInput(kb);
            if (moveDir.sqrMagnitude > 0.01f && RemainingMoveDist > 0.05f)
            {
                _hasClickMoveTarget = false;
                Vector3 delta = moveDir * MoveSpeed * Time.deltaTime;
                if (delta.magnitude > RemainingMoveDist)
                    delta = delta.normalized * RemainingMoveDist;
                Vector3 prev = transform.position;
                MoveWithinArena(delta);
                RemainingMoveDist = Mathf.Max(0f, RemainingMoveDist - Vector3.Distance(prev, transform.position));
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 12f);
                UpdateVelocityAnimation(0.95f);
            }
            else if (_hasClickMoveTarget && RemainingMoveDist > 0.05f)
            {
                Vector3 toTarget = _moveTarget - transform.position;
                toTarget.y = 0f;
                if (toTarget.magnitude > 0.25f)
                {
                    Vector3 step = toTarget.normalized * MoveSpeed * Time.deltaTime;
                    if (step.magnitude > toTarget.magnitude) step = toTarget;
                    if (step.magnitude > RemainingMoveDist) step = step.normalized * RemainingMoveDist;
                    Vector3 prev = transform.position;
                    MoveWithinArena(step);
                    RemainingMoveDist = Mathf.Max(0f, RemainingMoveDist - Vector3.Distance(prev, transform.position));
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toTarget.normalized), Time.deltaTime * 12f);
                    UpdateVelocityAnimation(0.9f);
                }
                else
                {
                    _hasClickMoveTarget = false;
                    UpdateVelocityAnimation(0f);
                }
            }
            else
            {
                UpdateVelocityAnimation(0f);
            }

            // Update cursor targeting visual
            if (TryRaycastCursor(out Vector3 hitPoint, out EnemyAI hitEnemy, out ExplosiveBarrel hitBarrel, out RuneCrystalSwitch hitCrystal, out GoldenChest _))
            {
                if (TargetReticle != null)
                {
                    TargetReticle.SetActive(ActiveAction != TacticalActionMode.MoveOrSelect || hitEnemy != null || hitBarrel != null);
                    TargetReticle.transform.position = (hitEnemy != null ? hitEnemy.transform.position : hitPoint) + Vector3.up * 0.04f;
                }

                if (AimIndicatorLine != null && ActiveAction != TacticalActionMode.MoveOrSelect)
                {
                    AimIndicatorLine.enabled = true;
                    AimIndicatorLine.SetPosition(0, transform.position + Vector3.up * 1.0f);
                    Vector3 endPt = hitEnemy != null ? hitEnemy.transform.position + Vector3.up * 1.0f : hitPoint + Vector3.up * 0.5f;
                    AimIndicatorLine.SetPosition(1, endPt);
                }
                else if (AimIndicatorLine != null)
                {
                    AimIndicatorLine.enabled = false;
                }

                if (mouse != null && mouse.leftButton.wasPressedThisFrame && !IsPointerOverUI())
                {
                    HandleTacticalClick(hitPoint, hitEnemy, hitBarrel, hitCrystal);
                }
            }
        }

        void HandleTacticalClick(Vector3 hitPoint, EnemyAI hitEnemy, ExplosiveBarrel hitBarrel, RuneCrystalSwitch hitCrystal)
        {
            if (ActiveAction == TacticalActionMode.MoveOrSelect)
            {
                if (hitEnemy != null && !hitEnemy.IsDead && Vector3.Distance(transform.position, hitEnemy.transform.position) <= 3.8f)
                {
                    ExecuteStrikeOn(hitEnemy.transform);
                }
                else if (hitBarrel != null && !hitBarrel.IsDetonated && Vector3.Distance(transform.position, hitBarrel.transform.position) <= 3.8f)
                {
                    ExecuteStrikeOn(hitBarrel.transform);
                }
                else
                {
                    _moveTarget = hitPoint;
                    _hasClickMoveTarget = true;
                }
                return;
            }

            if (CurrentAP <= 0)
            {
                FeelManager.Instance?.SpawnFloatingText(transform.position, "NO AP LEFT! END TURN", Palette.HeroCrimson);
                return;
            }

            switch (ActiveAction)
            {
                case TacticalActionMode.Strike:
                    Transform target = hitEnemy != null ? hitEnemy.transform : (hitBarrel != null ? hitBarrel.transform : (hitCrystal != null ? hitCrystal.transform : null));
                    if (target != null && Vector3.Distance(transform.position, target.position) <= 4.2f)
                        ExecuteStrikeOn(target);
                    else
                        FeelManager.Instance?.SpawnFloatingText(transform.position, "MOVE CLOSER OR USE [3] SHOT!", Palette.FiligreeGold);
                    break;

                case TacticalActionMode.Flask:
                    ExecuteFlaskAt(hitEnemy != null ? hitEnemy.transform.position : hitPoint);
                    break;

                case TacticalActionMode.FreeAimShot:
                    Vector3 shotTarget = hitEnemy != null ? hitEnemy.transform.position :
                        (hitBarrel != null ? hitBarrel.transform.position :
                        (hitCrystal != null ? hitCrystal.transform.position : hitPoint));
                    ExecuteFreeAimShotAt(shotTarget, hitEnemy, hitBarrel, hitCrystal);
                    break;

                case TacticalActionMode.Shove:
                    if (hitEnemy != null && !hitEnemy.IsDead && Vector3.Distance(transform.position, hitEnemy.transform.position) <= 4.0f)
                        ExecuteShoveOn(hitEnemy);
                    else
                        FeelManager.Instance?.SpawnFloatingText(transform.position, "CLICK AN ADJACENT ENEMY!", Palette.FiligreeGold);
                    break;
            }
        }

        public void ExecuteStrikeOn(Transform target)
        {
            if (_isExecutingAction || CurrentAP < 1 || target == null) return;
            StartCoroutine(StrikeCoroutine(target));
        }

        IEnumerator StrikeCoroutine(Transform target)
        {
            _isExecutingAction = true;
            CurrentAP--;
            GameLoop.Instance?.SetState(GameState.ActionExecutionQTE);
            GameLoop.Instance?.NotifyStatsChanged();

            Vector3 dir = target.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);

            bool perfect = false;
            yield return ExpeditionQTEController.Instance.RunAttackResonanceQTE(this, target, res => perfect = res);

            Animator?.TriggerAttackSwing();
            yield return new WaitForSecondsRealtime(0.14f);

            int baseDmg = (GameLoop.Instance != null && GameLoop.Instance.BoonColossalForce) ? 38 : 28;
            int finalDmg = perfect ? Mathf.RoundToInt(baseDmg * 1.55f) : baseDmg;

            EnemyAI enemy = target.GetComponentInParent<EnemyAI>();
            ExplosiveBarrel barrel = target.GetComponentInParent<ExplosiveBarrel>();
            RuneCrystalSwitch crystal = target.GetComponentInParent<RuneCrystalSwitch>();

            if (enemy != null && !enemy.IsDead)
            {
                enemy.TakeDamage(finalDmg, CurrentElement, perfect);
                if (perfect && GameLoop.Instance != null && GameLoop.Instance.BoonChronoResonance && !enemy.IsDead)
                {
                    yield return new WaitForSecondsRealtime(0.16f);
                    FeelManager.Instance?.SpawnFloatingText(enemy.transform.position, "CHRONO ECHO!", Palette.LightningCyan, 1.15f);
                    enemy.TakeDamage(16, CurrentElement, true);
                }
            }
            else if (barrel != null)
            {
                barrel.Detonate();
            }
            else if (crystal != null)
            {
                crystal.Activate();
            }

            ElementalSurfaceManager.Instance?.ApplyElementAt(target.position, 2.4f, CurrentElement);

            if (perfect)
                RefundAP(1);

            yield return new WaitForSecondsRealtime(0.25f);
            ActiveAction = TacticalActionMode.MoveOrSelect;
            _isExecutingAction = false;

            if (GameLoop.Instance != null && GameLoop.Instance.State == GameState.ActionExecutionQTE)
                GameLoop.Instance.SetState(GameState.TacticalPlayerTurn);

            DungeonDirector.Instance?.CheckChamberCleared();
        }

        public void ExecuteFlaskAt(Vector3 point)
        {
            if (_isExecutingAction || CurrentAP < 1) return;
            StartCoroutine(FlaskCoroutine(point));
        }

        IEnumerator FlaskCoroutine(Vector3 point)
        {
            _isExecutingAction = true;
            CurrentAP--;
            GameLoop.Instance?.NotifyStatsChanged();

            Vector3 dir = point - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);
            Animator?.TriggerAttackSwing();
            SynthAudioManager.Instance?.PlaySlash();

            yield return new WaitForSeconds(0.18f);

            ElementType flaskElem = FlaskThrowsOil ? ElementType.Oil : ElementType.Water;
            string label = FlaskThrowsOil ? "OIL FLASK!" : "WATER FLASK!";
            Color col = FlaskThrowsOil ? Palette.FiligreeGold : Palette.WaterAzure;
            FeelManager.Instance?.SpawnFloatingText(point, label, col, 1.05f);
            ElementalSurfaceManager.Instance?.ApplyElementAt(point, 2.7f, flaskElem);

            ActiveAction = TacticalActionMode.MoveOrSelect;
            _isExecutingAction = false;
        }

        public void ExecuteFreeAimShotAt(Vector3 point, EnemyAI enemy = null, ExplosiveBarrel barrel = null, RuneCrystalSwitch crystal = null)
        {
            if (_isExecutingAction || CurrentAP < 1) return;
            StartCoroutine(FreeAimCoroutine(point, enemy, barrel, crystal));
        }

        IEnumerator FreeAimCoroutine(Vector3 point, EnemyAI enemy, ExplosiveBarrel barrel, RuneCrystalSwitch crystal)
        {
            _isExecutingAction = true;
            CurrentAP--;
            GameLoop.Instance?.SetState(GameState.ActionExecutionQTE);
            GameLoop.Instance?.NotifyStatsChanged();

            Vector3 dir = point - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);

            Transform focusTransform = enemy != null ? enemy.transform : (barrel != null ? barrel.transform : (crystal != null ? crystal.transform : null));
            bool perfect = false;
            yield return ExpeditionQTEController.Instance.RunAttackResonanceQTE(this, focusTransform, res => perfect = res);

            Animator?.TriggerAttackSwing();

            // Visual elemental bolt
            GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(bolt.GetComponent<Collider>());
            bolt.transform.localScale = Vector3.one * 0.34f;
            Color elemColor = CurrentElement == ElementType.Fire ? Palette.FireAmber :
                (CurrentElement == ElementType.Lightning ? Palette.LightningCyan : Palette.FrostIce);
            bolt.GetComponent<Renderer>().sharedMaterial = Palette.CreateToonMaterial("ArcaneBolt", elemColor, elemColor * 2.2f);
            Vector3 startPos = transform.position + Vector3.up * 1.25f;
            Vector3 endPos = point + Vector3.up * 0.9f;
            yield return Ease.Tween(0.16f, t => bolt.transform.position = Vector3.Lerp(startPos, endPos, t), Ease.OutCubic, true);
            Destroy(bolt);

            int dmg = perfect ? 34 : 22;
            if (enemy != null && !enemy.IsDead)
                enemy.TakeDamage(dmg, CurrentElement, perfect);
            if (barrel != null)
                barrel.Detonate();
            if (crystal != null)
                crystal.Activate();

            ElementalSurfaceManager.Instance?.ApplyElementAt(point, 2.5f, CurrentElement);
            if (perfect) RefundAP(1);

            yield return new WaitForSecondsRealtime(0.2f);
            ActiveAction = TacticalActionMode.MoveOrSelect;
            _isExecutingAction = false;

            if (GameLoop.Instance != null && GameLoop.Instance.State == GameState.ActionExecutionQTE)
                GameLoop.Instance.SetState(GameState.TacticalPlayerTurn);

            DungeonDirector.Instance?.CheckChamberCleared();
        }

        public void ExecuteShoveOn(EnemyAI enemy)
        {
            if (_isExecutingAction || CurrentAP < 1 || enemy == null || enemy.IsDead) return;
            StartCoroutine(ShoveCoroutine(enemy));
        }

        IEnumerator ShoveCoroutine(EnemyAI enemy)
        {
            _isExecutingAction = true;
            CurrentAP--;
            GameLoop.Instance?.NotifyStatsChanged();

            Vector3 dir = enemy.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            dir.Normalize();
            transform.rotation = Quaternion.LookRotation(dir);

            Animator?.TriggerAttackSwing();
            SynthAudioManager.Instance?.PlayRiposteThunder();
            FeelManager.Instance?.TriggerShake(0.55f);
            FeelManager.Instance?.SpawnFloatingText(enemy.transform.position, "TACTICAL SHOVE!", Palette.FiligreeGold, 1.2f);

            float pushDist = (GameLoop.Instance != null && GameLoop.Instance.BoonColossalForce) ? 7.0f : 4.8f;
            Vector3 start = enemy.transform.position;
            Vector3 dest = start + dir * pushDist;

            yield return Ease.Tween(0.24f, t =>
            {
                if (enemy == null || enemy.IsDead) return;
                enemy.transform.position = Vector3.Lerp(start, dest, t);

                // Check barrel collision mid-shove
                if (ElementalSurfaceManager.Instance != null)
                {
                    foreach (ExplosiveBarrel b in ElementalSurfaceManager.Instance.Barrels)
                    {
                        if (b != null && !b.IsDetonated && Vector3.Distance(enemy.transform.position, b.transform.position) <= 1.5f)
                            b.Detonate();
                    }
                }
            }, Ease.OutCubic, false);

            if (enemy != null && !enemy.IsDead)
            {
                // Check Abyss chasm ledge! (|x| > 9.35f)
                if (Mathf.Abs(enemy.transform.position.x) > 9.35f)
                {
                    enemy.KillByAbyssFall();
                }
                else
                {
                    enemy.TakeDamage(10, ElementType.Physical, false);
                    ElementalSurfaceManager.Instance?.EvaluateUnitSurfaceEntry(enemy);
                }
            }

            ActiveAction = TacticalActionMode.MoveOrSelect;
            _isExecutingAction = false;
            DungeonDirector.Instance?.CheckChamberCleared();
        }

        public void EndPlayerTurn()
        {
            if (_isExecutingAction) return;
            if (GameLoop.Instance == null || GameLoop.Instance.State != GameState.TacticalPlayerTurn) return;
            SynthAudioManager.Instance?.PlayUIClick();
            DungeonDirector.Instance?.BeginEnemyTurn();
        }

        public void TakeDamage(int dmg)
        {
            CurrentHP = Mathf.Max(0, CurrentHP - dmg);
            Animator?.TriggerHitRecoil();
            FeelManager.Instance?.FlashObject(gameObject, Palette.HeroCrimson, 0.16f);
            FeelManager.Instance?.TriggerShake(0.55f);
            FeelManager.Instance?.SpawnFloatingText(transform.position, $"-{dmg} HP", Palette.HeroCrimson, 1.15f);
            SynthAudioManager.Instance?.PlayHitTaken();
            GameLoop.Instance?.NotifyStatsChanged();

            if (CurrentHP <= 0)
                GameLoop.Instance?.LoseExpedition();
        }

        public void Heal(int amount)
        {
            CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
            FeelManager.Instance?.SpawnFloatingText(transform.position, $"+{amount} HP", Palette.HealEmerald, 1.15f);
            GameLoop.Instance?.NotifyStatsChanged();
        }

        static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        bool TryRaycastCursor(out Vector3 point, out EnemyAI enemy, out ExplosiveBarrel barrel, out RuneCrystalSwitch crystal, out GoldenChest chest)
        {
            point = transform.position;
            enemy = null;
            barrel = null;
            crystal = null;
            chest = null;

            Camera cam = Camera.main;
            var mouse = Mouse.current;
            if (cam == null || mouse == null) return false;

            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 120f))
            {
                point = hit.point;
                point.y = 0f;
                enemy = hit.collider.GetComponentInParent<EnemyAI>();
                barrel = hit.collider.GetComponentInParent<ExplosiveBarrel>();
                crystal = hit.collider.GetComponentInParent<RuneCrystalSwitch>();
                chest = hit.collider.GetComponentInParent<GoldenChest>();
                return true;
            }

            Plane ground = new Plane(Vector3.up, Vector3.zero);
            if (ground.Raycast(ray, out float enter))
            {
                point = ray.GetPoint(enter);
                point.y = 0f;
                return true;
            }
            return false;
        }
    }
}
