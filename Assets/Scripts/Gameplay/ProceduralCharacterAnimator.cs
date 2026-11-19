using System.Collections;
using UnityEngine;
using Aetherfall.Feel;

namespace Aetherfall.Gameplay
{
    /// <summary>
    /// Velocity-driven procedural character motion (gait bob, limb swing, acceleration lean,
    /// cape sway, idle breathing, weapon cleave overshoot, parry guard, and boss halo rotation).
    /// </summary>
    public class ProceduralCharacterAnimator : MonoBehaviour
    {
        public float NormalizedSpeed;
        public bool IsParrying;
        public bool IsProne;

        Transform _body;
        Transform _head;
        Transform _cape;
        Transform _leftArm;
        Transform _rightArm;
        Transform _leftLeg;
        Transform _rightLeg;
        Transform _haloRing;

        Vector3 _bodyBaseLocalPos;
        Quaternion _capeBaseRot;
        Quaternion _bodyBaseRot;
        Quaternion _headBaseRot;

        float _gaitTimer;
        float _stepFreq = 2.5f;
        float _bobAmp = 0.065f;
        float _swingAmp = 34f;
        float _currentSwingBlend;
        float _attackAnimAngle;
        bool _isAttacking;

        void Awake()
        {
            CacheBones();
        }

        public void CacheBones()
        {
            _body = FindDeepChild(transform, "Body");
            _head = FindDeepChild(transform, "Head");
            _cape = FindDeepChild(transform, "Cape");
            _leftArm = FindDeepChild(transform, "LeftArm");
            _rightArm = FindDeepChild(transform, "RightArm");
            _leftLeg = FindDeepChild(transform, "LeftLeg");
            _rightLeg = FindDeepChild(transform, "RightLeg");
            _haloRing = FindDeepChild(transform, "HaloRing");

            if (_body != null)
            {
                _bodyBaseLocalPos = _body.localPosition;
                _bodyBaseRot = _body.localRotation;
            }
            if (_cape != null)
                _capeBaseRot = _cape.localRotation;
            if (_head != null)
                _headBaseRot = _head.localRotation;
        }

        static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform res = FindDeepChild(parent.GetChild(i), name);
                if (res != null) return res;
            }
            return null;
        }

        public void TriggerAttackSwing()
        {
            StopCoroutine(nameof(AttackSwingRoutine));
            StartCoroutine(AttackSwingRoutine());
        }

        public void TriggerParryGuard()
        {
            StopCoroutine(nameof(ParryRoutine));
            StartCoroutine(ParryRoutine());
        }

        public void TriggerHitRecoil()
        {
            StopCoroutine(nameof(RecoilRoutine));
            StartCoroutine(RecoilRoutine());
        }

        IEnumerator AttackSwingRoutine()
        {
            _isAttacking = true;
            // Windup back
            yield return Ease.Tween(0.14f, t => _attackAnimAngle = Mathf.Lerp(0f, -72f, t), Ease.OutCubic, true);
            // Slash forward with OutBack overshoot
            yield return Ease.Tween(0.18f, t => _attackAnimAngle = Mathf.LerpUnclamped(-72f, 92f, t), Ease.OutBack, true);
            // Recover
            yield return Ease.Tween(0.22f, t => _attackAnimAngle = Mathf.Lerp(92f, 0f, t), Ease.OutCubic, true);
            _attackAnimAngle = 0f;
            _isAttacking = false;
        }

        IEnumerator ParryRoutine()
        {
            IsParrying = true;
            yield return new WaitForSecondsRealtime(0.32f);
            IsParrying = false;
        }

        IEnumerator RecoilRoutine()
        {
            if (_body == null) yield break;
            yield return Ease.Tween(0.12f, t =>
            {
                if (_body != null)
                    _body.localRotation = _bodyBaseRot * Quaternion.Euler(-18f * t, 0f, 0f);
            }, Ease.OutCubic, true);
            yield return Ease.Tween(0.2f, t =>
            {
                if (_body != null)
                    _body.localRotation = _bodyBaseRot * Quaternion.Euler(-18f * (1f - t), 0f, 0f);
            }, Ease.OutBack, true);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) dt = Time.unscaledDeltaTime * 0.05f;

            // Smooth blend so stops settle smoothly to idle breathe rather than freezing
            float targetBlend = Mathf.Clamp01(NormalizedSpeed);
            _currentSwingBlend = Mathf.MoveTowards(_currentSwingBlend, targetBlend, dt * 3.8f);
            _gaitTimer += dt * _stepFreq * Mathf.PI * 2f * (0.4f + 0.6f * _currentSwingBlend);

            float idleBreathe = Mathf.Sin(Time.unscaledTime * 2.2f) * 0.022f;
            float gaitBob = Mathf.Abs(Mathf.Sin(_gaitTimer)) * _bobAmp * _currentSwingBlend;

            if (IsProne)
            {
                if (_body != null)
                    _body.localRotation = Quaternion.Slerp(_body.localRotation, _bodyBaseRot * Quaternion.Euler(78f, 0f, 12f), dt * 8f);
                return;
            }

            if (_body != null)
            {
                _body.localPosition = _bodyBaseLocalPos + new Vector3(0f, gaitBob + idleBreathe, 0f);
                float leanForward = 8f * _currentSwingBlend;
                float roll = Mathf.Sin(_gaitTimer * 0.5f) * 2.5f * _currentSwingBlend;
                float attackYaw = _isAttacking ? (_attackAnimAngle * 0.28f) : 0f;
                _body.localRotation = _bodyBaseRot * Quaternion.Euler(leanForward, attackYaw, roll);
            }

            if (_head != null)
            {
                float headNod = Mathf.Sin(Time.unscaledTime * 1.9f) * 2.5f;
                _head.localRotation = _headBaseRot * Quaternion.Euler(headNod, 0f, 0f);
            }

            if (_cape != null)
            {
                float capeFlutter = Mathf.Sin(Time.unscaledTime * 3.2f) * 4f + _currentSwingBlend * 22f;
                _cape.localRotation = _capeBaseRot * Quaternion.Euler(capeFlutter, 0f, 0f);
            }

            float legSwing = Mathf.Sin(_gaitTimer) * _swingAmp * _currentSwingBlend;
            if (_leftLeg != null) _leftLeg.localRotation = Quaternion.Euler(legSwing, 0f, 0f);
            if (_rightLeg != null) _rightLeg.localRotation = Quaternion.Euler(-legSwing, 0f, 0f);

            if (_leftArm != null)
            {
                float armRot = IsParrying ? -68f : -legSwing * 0.7f;
                float armYaw = IsParrying ? 40f : 0f;
                _leftArm.localRotation = Quaternion.Euler(armRot, armYaw, 0f);
            }

            if (_rightArm != null)
            {
                float armRot = _isAttacking ? _attackAnimAngle : (IsParrying ? -55f : legSwing * 0.65f);
                float armRoll = _isAttacking ? -22f : (IsParrying ? -35f : 0f);
                _rightArm.localRotation = Quaternion.Euler(armRot, 0f, armRoll);
            }

            if (_haloRing != null)
            {
                _haloRing.Rotate(Vector3.forward, 48f * Time.deltaTime, Space.Self);
            }
        }
    }
}
