using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace RPGDefete.Character
{
    /// <summary>
    /// 手の左右を指定する列挙型
    /// </summary>
    public enum HandType { Left, Right }

    /// <summary>
    /// 足の左右を指定する列挙型
    /// </summary>
    public enum FootType { Left, Right }

    /// <summary>
    /// VRMキャラクターのIKを制御するコンポーネント
    /// VRMRigSetupで作成されたRig構造を使用してIKを制御する
    /// </summary>
    public class VRMIKController : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private VRMRigSetup rigSetup;

        [Header("設定")]
        [SerializeField] private string characterName = "Character";
        [SerializeField] private float defaultTransitionDuration = 0.3f;

        [Header("デバッグ")]
        [SerializeField] private bool debugMode = false;

        private Coroutine _lookAtCoroutine;
        private Coroutine _leftHandCoroutine;
        private Coroutine _rightHandCoroutine;
        private Coroutine _leftFootCoroutine;
        private Coroutine _rightFootCoroutine;
        private Coroutine _hipCoroutine;

        /// <summary>キャラクター名</summary>
        public string CharacterName => characterName;

        /// <summary>IKが有効か</summary>
        public bool IsValid => rigSetup != null && rigSetup.IsSetup;

        /// <summary>現在のLookAt Weight</summary>
        public float LookAtWeight => IsValid && rigSetup.HeadAimConstraint != null
            ? rigSetup.HeadAimConstraint.weight : 0f;

        private void Awake()
        {
            if (rigSetup == null)
            {
                rigSetup = GetComponent<VRMRigSetup>();
            }
        }

        private void Start()
        {
            if (!IsValid)
            {
                Debug.LogWarning($"[VRMIKController] RigSetup not configured for {characterName}. Run 'Setup Rig Structure' from context menu.");
            }
            else if (debugMode)
            {
                Debug.Log($"[VRMIKController] Initialized for {characterName}");
            }
        }

        #region LookAt

        /// <summary>
        /// LookAtターゲットを設定
        /// </summary>
        public void SetLookAtTarget(Transform target)
        {
            if (!IsValid || rigSetup.LookAtTarget == null) return;

            if (target != null)
            {
                rigSetup.LookAtTarget.position = target.position;
            }

            if (debugMode)
            {
                Debug.Log($"[VRMIKController] {characterName}: SetLookAtTarget = {(target != null ? target.name : "null")}");
            }
        }

        /// <summary>
        /// LookAt Weightを設定（遷移付き）
        /// </summary>
        public IEnumerator SetLookAtWeight(float weight, float duration = -1f)
        {
            if (!IsValid) yield break;

            if (_lookAtCoroutine != null)
            {
                StopCoroutine(_lookAtCoroutine);
            }

            float dur = duration < 0 ? defaultTransitionDuration : duration;
            yield return TransitionRigWeight(rigSetup.HeadAimConstraint, weight, dur);

            if (debugMode)
            {
                Debug.Log($"[VRMIKController] {characterName}: LookAt weight = {weight}");
            }
        }

        /// <summary>
        /// LookAtターゲットを追従させる（毎フレーム更新）
        /// </summary>
        public void UpdateLookAtTarget(Transform target)
        {
            if (!IsValid || rigSetup.LookAtTarget == null || target == null) return;
            rigSetup.LookAtTarget.position = target.position;
        }

        /// <summary>
        /// LookAtをクリア
        /// </summary>
        public IEnumerator ClearLookAt(float duration = -1f)
        {
            yield return SetLookAtWeight(0f, duration);
        }

        #endregion

        #region Hand IK

        /// <summary>
        /// 手のIKターゲットを設定
        /// </summary>
        public void SetHandIKTarget(HandType hand, Transform target)
        {
            if (!IsValid) return;

            var ikTarget = hand == HandType.Left ? rigSetup.LeftHandTarget : rigSetup.RightHandTarget;
            if (ikTarget == null || target == null) return;

            ikTarget.position = target.position;
            ikTarget.rotation = target.rotation;

            if (debugMode)
            {
                Debug.Log($"[VRMIKController] {characterName}: Set{hand}HandIKTarget = {target.name}");
            }
        }

        /// <summary>
        /// 手のIK Weightを設定（遷移付き）
        /// </summary>
        public IEnumerator SetHandIKWeight(HandType hand, float weight, float duration = -1f)
        {
            if (!IsValid) yield break;

            var constraint = hand == HandType.Left
                ? rigSetup.LeftHandIKConstraint
                : rigSetup.RightHandIKConstraint;

            if (constraint == null) yield break;

            ref Coroutine coroutine = ref (hand == HandType.Left ? ref _leftHandCoroutine : ref _rightHandCoroutine);
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }

            float dur = duration < 0 ? defaultTransitionDuration : duration;
            yield return TransitionConstraintWeight(constraint, weight, dur);

            if (debugMode)
            {
                Debug.Log($"[VRMIKController] {characterName}: {hand}HandIK weight = {weight}");
            }
        }

        /// <summary>
        /// 手のIKをクリア
        /// </summary>
        public IEnumerator ClearHandIK(HandType hand, float duration = -1f)
        {
            yield return SetHandIKWeight(hand, 0f, duration);
        }

        #endregion

        #region Foot IK

        /// <summary>
        /// 足のIKターゲットを設定
        /// </summary>
        public void SetFootIKTarget(FootType foot, Transform target)
        {
            if (!IsValid) return;

            var ikTarget = foot == FootType.Left ? rigSetup.LeftFootTarget : rigSetup.RightFootTarget;
            if (ikTarget == null || target == null) return;

            ikTarget.position = target.position;
            ikTarget.rotation = target.rotation;

            if (debugMode)
            {
                Debug.Log($"[VRMIKController] {characterName}: Set{foot}FootIKTarget = {target.name}");
            }
        }

        /// <summary>
        /// 足のIK Weightを設定（遷移付き）
        /// </summary>
        public IEnumerator SetFootIKWeight(FootType foot, float weight, float duration = -1f)
        {
            if (!IsValid) yield break;

            var constraint = foot == FootType.Left
                ? rigSetup.LeftFootIKConstraint
                : rigSetup.RightFootIKConstraint;

            if (constraint == null) yield break;

            ref Coroutine coroutine = ref (foot == FootType.Left ? ref _leftFootCoroutine : ref _rightFootCoroutine);
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }

            float dur = duration < 0 ? defaultTransitionDuration : duration;
            yield return TransitionConstraintWeight(constraint, weight, dur);

            if (debugMode)
            {
                Debug.Log($"[VRMIKController] {characterName}: {foot}FootIK weight = {weight}");
            }
        }

        /// <summary>
        /// 足のIKをクリア
        /// </summary>
        public IEnumerator ClearFootIK(FootType foot, float duration = -1f)
        {
            yield return SetFootIKWeight(foot, 0f, duration);
        }

        #endregion

        #region Hip IK

        /// <summary>
        /// 腰のIKターゲットを設定
        /// </summary>
        public void SetHipIKTarget(Transform target)
        {
            if (!IsValid || rigSetup.HipTarget == null || target == null) return;

            rigSetup.HipTarget.position = target.position;

            if (debugMode)
            {
                Debug.Log($"[VRMIKController] {characterName}: SetHipIKTarget = {target.name}");
            }
        }

        /// <summary>
        /// 腰のIK Weightを設定（遷移付き）
        /// </summary>
        public IEnumerator SetHipIKWeight(float weight, float duration = -1f)
        {
            if (!IsValid || rigSetup.HipConstraint == null) yield break;

            if (_hipCoroutine != null)
            {
                StopCoroutine(_hipCoroutine);
            }

            float dur = duration < 0 ? defaultTransitionDuration : duration;
            yield return TransitionConstraintWeight(rigSetup.HipConstraint, weight, dur);

            if (debugMode)
            {
                Debug.Log($"[VRMIKController] {characterName}: HipIK weight = {weight}");
            }
        }

        /// <summary>
        /// 腰のIKをクリア
        /// </summary>
        public IEnumerator ClearHipIK(float duration = -1f)
        {
            yield return SetHipIKWeight(0f, duration);
        }

        #endregion

        #region Bulk Control

        /// <summary>
        /// 全てのIKを無効化（歩行時など）
        /// </summary>
        public IEnumerator DisableAllIK(float duration = 0.2f)
        {
            if (!IsValid) yield break;

            if (debugMode)
            {
                Debug.Log($"[VRMIKController] {characterName}: Disabling all IK");
            }

            // 並列で全てのIKを無効化
            StartCoroutine(SetLookAtWeight(0f, duration));
            StartCoroutine(SetHandIKWeight(HandType.Left, 0f, duration));
            StartCoroutine(SetHandIKWeight(HandType.Right, 0f, duration));
            StartCoroutine(SetFootIKWeight(FootType.Left, 0f, duration));
            StartCoroutine(SetFootIKWeight(FootType.Right, 0f, duration));
            yield return SetHipIKWeight(0f, duration);
        }

        /// <summary>
        /// 全てのIKを有効化
        /// </summary>
        public IEnumerator EnableAllIK(float duration = 0.2f)
        {
            if (!IsValid) yield break;

            if (debugMode)
            {
                Debug.Log($"[VRMIKController] {characterName}: Enabling all IK");
            }

            // 並列で全てのIKを有効化
            StartCoroutine(SetLookAtWeight(1f, duration));
            StartCoroutine(SetHandIKWeight(HandType.Left, 1f, duration));
            StartCoroutine(SetHandIKWeight(HandType.Right, 1f, duration));
            StartCoroutine(SetFootIKWeight(FootType.Left, 1f, duration));
            StartCoroutine(SetFootIKWeight(FootType.Right, 1f, duration));
            yield return SetHipIKWeight(1f, duration);
        }

        #endregion

        #region Transition Helpers

        private IEnumerator TransitionRigWeight(IRigConstraint constraint, float targetWeight, float duration)
        {
            if (constraint == null) yield break;

            // MultiAimConstraintはweightプロパティを直接持つ
            if (constraint is MultiAimConstraint aimConstraint)
            {
                float startWeight = aimConstraint.weight;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    aimConstraint.weight = Mathf.Lerp(startWeight, targetWeight, t);
                    yield return null;
                }

                aimConstraint.weight = targetWeight;
            }
        }

        private IEnumerator TransitionConstraintWeight(TwoBoneIKConstraint constraint, float targetWeight, float duration)
        {
            if (constraint == null) yield break;

            float startWeight = constraint.weight;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                constraint.weight = Mathf.Lerp(startWeight, targetWeight, t);
                yield return null;
            }

            constraint.weight = targetWeight;
        }

        private IEnumerator TransitionConstraintWeight(MultiPositionConstraint constraint, float targetWeight, float duration)
        {
            if (constraint == null) yield break;

            float startWeight = constraint.weight;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                constraint.weight = Mathf.Lerp(startWeight, targetWeight, t);
                yield return null;
            }

            constraint.weight = targetWeight;
        }

        #endregion

        /// <summary>
        /// ターゲットのTransformを直接取得（手動制御用）
        /// </summary>
        public Transform GetIKTarget(string targetType)
        {
            if (!IsValid) return null;

            return targetType.ToLower() switch
            {
                "lookat" => rigSetup.LookAtTarget,
                "lefthand" => rigSetup.LeftHandTarget,
                "righthand" => rigSetup.RightHandTarget,
                "leftfoot" => rigSetup.LeftFootTarget,
                "rightfoot" => rigSetup.RightFootTarget,
                "hip" => rigSetup.HipTarget,
                _ => null
            };
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
}
