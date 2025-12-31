using System.Collections;
using UnityEngine;
using RootMotion.FinalIK;

namespace RPGDefete.Character
{
    /// <summary>
    /// FinalIKを使用したVRMキャラクターのIK制御
    /// FullBodyBipedIK + LookAtIKを使用して視線・手足・腰を制御する
    /// </summary>
    public class VRMFinalIKController : MonoBehaviour
    {
        [Header("FinalIK Components")]
        [SerializeField] private FullBodyBipedIK fbbik;
        [SerializeField] private LookAtIK lookAtIK;

        [Header("Settings")]
        [SerializeField] private float defaultTransitionDuration = 0.3f;

        [Header("Debug")]
        [SerializeField] private bool debugMode = true;

        // Properties
        public bool IsValid => fbbik != null;
        public bool HasLookAt => lookAtIK != null;
        public float LookAtWeight => lookAtIK != null ? lookAtIK.solver.IKPositionWeight : 0f;

        private Coroutine _lookAtCoroutine;
        private Coroutine _leftHandCoroutine;
        private Coroutine _rightHandCoroutine;
        private Coroutine _leftFootCoroutine;
        private Coroutine _rightFootCoroutine;
        private Coroutine _bodyCoroutine;

        private void Awake()
        {
            // 自動検出
            if (fbbik == null)
                fbbik = GetComponent<FullBodyBipedIK>();
            if (lookAtIK == null)
                lookAtIK = GetComponent<LookAtIK>();
        }

        private void Start()
        {
            if (debugMode)
            {
                Debug.Log($"[VRMFinalIKController] FBBIK: {(fbbik != null ? "Found" : "Not Found")}, LookAtIK: {(lookAtIK != null ? "Found" : "Not Found")}");
            }
        }

        #region LookAt

        /// <summary>
        /// LookAtターゲットを設定
        /// </summary>
        public void SetLookAtTarget(Transform target)
        {
            if (lookAtIK == null) return;
            lookAtIK.solver.target = target;

            if (debugMode)
                Debug.Log($"[VRMFinalIKController] SetLookAtTarget: {(target != null ? target.name : "null")}");
        }

        /// <summary>
        /// LookAt weightを設定（遷移付き）
        /// </summary>
        public IEnumerator SetLookAtWeight(float weight, float duration = -1f)
        {
            if (lookAtIK == null) yield break;

            if (_lookAtCoroutine != null)
                StopCoroutine(_lookAtCoroutine);

            float dur = duration < 0 ? defaultTransitionDuration : duration;
            float startWeight = lookAtIK.solver.IKPositionWeight;
            float elapsed = 0f;

            if (debugMode)
                Debug.Log($"[VRMFinalIKController] LookAt transition: {startWeight} -> {weight}");

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                lookAtIK.solver.IKPositionWeight = Mathf.Lerp(startWeight, weight, t);
                yield return null;
            }

            lookAtIK.solver.IKPositionWeight = weight;

            if (debugMode)
                Debug.Log($"[VRMFinalIKController] LookAt weight set to: {weight}");
        }

        /// <summary>
        /// LookAtをすぐに設定（遷移なし）
        /// </summary>
        public void SetLookAtWeightImmediate(float weight)
        {
            if (lookAtIK == null) return;
            lookAtIK.solver.IKPositionWeight = weight;
        }

        /// <summary>
        /// LookAtターゲット位置を更新（毎フレーム呼び出し用）
        /// </summary>
        public void UpdateLookAtTarget(Transform target)
        {
            if (lookAtIK == null || lookAtIK.solver.target == null || target == null) return;
            lookAtIK.solver.target.position = target.position;
        }

        #endregion

        #region Hand IK

        /// <summary>
        /// 手のIKターゲット位置を設定
        /// </summary>
        public void SetHandIKTarget(HandType hand, Transform target)
        {
            if (fbbik == null || target == null) return;

            var effector = hand == HandType.Left ? fbbik.solver.leftHandEffector : fbbik.solver.rightHandEffector;
            effector.position = target.position;
            effector.rotation = target.rotation;

            if (debugMode)
                Debug.Log($"[VRMFinalIKController] Set{hand}HandIKTarget: {target.name}");
        }

        /// <summary>
        /// 手のIK weightを設定（遷移付き）
        /// </summary>
        public IEnumerator SetHandIKWeight(HandType hand, float weight, float duration = -1f)
        {
            if (fbbik == null) yield break;

            var effector = hand == HandType.Left ? fbbik.solver.leftHandEffector : fbbik.solver.rightHandEffector;

            // 進行中のコルーチンを停止
            if (hand == HandType.Left)
            {
                if (_leftHandCoroutine != null) StopCoroutine(_leftHandCoroutine);
            }
            else
            {
                if (_rightHandCoroutine != null) StopCoroutine(_rightHandCoroutine);
            }

            float dur = duration < 0 ? defaultTransitionDuration : duration;
            float startPosWeight = effector.positionWeight;
            float startRotWeight = effector.rotationWeight;
            float elapsed = 0f;

            if (debugMode)
                Debug.Log($"[VRMFinalIKController] {hand}Hand transition: {startPosWeight} -> {weight}");

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                effector.positionWeight = Mathf.Lerp(startPosWeight, weight, t);
                effector.rotationWeight = Mathf.Lerp(startRotWeight, weight, t);
                yield return null;
            }

            effector.positionWeight = weight;
            effector.rotationWeight = weight;
        }

        /// <summary>
        /// 手のIKターゲットを毎フレーム更新
        /// </summary>
        public void UpdateHandIKTarget(HandType hand, Transform target)
        {
            if (fbbik == null || target == null) return;

            var effector = hand == HandType.Left ? fbbik.solver.leftHandEffector : fbbik.solver.rightHandEffector;
            effector.position = target.position;
            effector.rotation = target.rotation;
        }

        #endregion

        #region Foot IK

        /// <summary>
        /// 足のIKターゲット位置を設定
        /// </summary>
        public void SetFootIKTarget(FootType foot, Transform target)
        {
            if (fbbik == null || target == null) return;

            var effector = foot == FootType.Left ? fbbik.solver.leftFootEffector : fbbik.solver.rightFootEffector;
            effector.position = target.position;
            effector.rotation = target.rotation;

            if (debugMode)
                Debug.Log($"[VRMFinalIKController] Set{foot}FootIKTarget: {target.name}");
        }

        /// <summary>
        /// 足のIK weightを設定（遷移付き）
        /// </summary>
        public IEnumerator SetFootIKWeight(FootType foot, float weight, float duration = -1f)
        {
            if (fbbik == null) yield break;

            var effector = foot == FootType.Left ? fbbik.solver.leftFootEffector : fbbik.solver.rightFootEffector;

            // 進行中のコルーチンを停止
            if (foot == FootType.Left)
            {
                if (_leftFootCoroutine != null) StopCoroutine(_leftFootCoroutine);
            }
            else
            {
                if (_rightFootCoroutine != null) StopCoroutine(_rightFootCoroutine);
            }

            float dur = duration < 0 ? defaultTransitionDuration : duration;
            float startPosWeight = effector.positionWeight;
            float startRotWeight = effector.rotationWeight;
            float elapsed = 0f;

            if (debugMode)
                Debug.Log($"[VRMFinalIKController] {foot}Foot transition: {startPosWeight} -> {weight}");

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                effector.positionWeight = Mathf.Lerp(startPosWeight, weight, t);
                effector.rotationWeight = Mathf.Lerp(startRotWeight, weight, t);
                yield return null;
            }

            effector.positionWeight = weight;
            effector.rotationWeight = weight;
        }

        #endregion

        #region Body/Hip IK

        /// <summary>
        /// 腰/体のIKターゲット位置を設定
        /// </summary>
        public void SetBodyIKTarget(Transform target)
        {
            if (fbbik == null || target == null) return;

            fbbik.solver.bodyEffector.position = target.position;

            if (debugMode)
                Debug.Log($"[VRMFinalIKController] SetBodyIKTarget: {target.name}");
        }

        /// <summary>
        /// 腰/体のIK weightを設定（遷移付き）
        /// </summary>
        public IEnumerator SetBodyIKWeight(float weight, float duration = -1f)
        {
            if (fbbik == null) yield break;

            if (_bodyCoroutine != null)
                StopCoroutine(_bodyCoroutine);

            float dur = duration < 0 ? defaultTransitionDuration : duration;
            float startWeight = fbbik.solver.bodyEffector.positionWeight;
            float elapsed = 0f;

            if (debugMode)
                Debug.Log($"[VRMFinalIKController] Body transition: {startWeight} -> {weight}");

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                fbbik.solver.bodyEffector.positionWeight = Mathf.Lerp(startWeight, weight, t);
                yield return null;
            }

            fbbik.solver.bodyEffector.positionWeight = weight;

            if (debugMode)
                Debug.Log($"[VRMFinalIKController] Body weight set to: {weight}");
        }

        /// <summary>
        /// 腰ターゲットを毎フレーム更新
        /// </summary>
        public void UpdateBodyIKTarget(Transform target)
        {
            if (fbbik == null || target == null) return;
            fbbik.solver.bodyEffector.position = target.position;
        }

        // Hip IK のエイリアス（Animation Rigging版との互換性のため）
        public void SetHipIKTarget(Transform target) => SetBodyIKTarget(target);
        public IEnumerator SetHipIKWeight(float weight, float duration = -1f) => SetBodyIKWeight(weight, duration);

        #endregion

        #region Bulk Control

        /// <summary>
        /// 全IKを無効化
        /// </summary>
        public IEnumerator DisableAllIK(float duration = 0.2f)
        {
            if (debugMode)
                Debug.Log("[VRMFinalIKController] Disabling all IK");

            if (lookAtIK != null)
                StartCoroutine(SetLookAtWeight(0f, duration));

            if (fbbik != null)
            {
                StartCoroutine(SetHandIKWeight(HandType.Left, 0f, duration));
                StartCoroutine(SetHandIKWeight(HandType.Right, 0f, duration));
                StartCoroutine(SetFootIKWeight(FootType.Left, 0f, duration));
                StartCoroutine(SetFootIKWeight(FootType.Right, 0f, duration));
                yield return SetBodyIKWeight(0f, duration);
            }
            else
            {
                yield return new WaitForSeconds(duration);
            }
        }

        /// <summary>
        /// 全IKを有効化
        /// </summary>
        public IEnumerator EnableAllIK(float duration = 0.2f)
        {
            if (debugMode)
                Debug.Log("[VRMFinalIKController] Enabling all IK");

            if (lookAtIK != null)
                StartCoroutine(SetLookAtWeight(1f, duration));

            if (fbbik != null)
            {
                StartCoroutine(SetHandIKWeight(HandType.Left, 1f, duration));
                StartCoroutine(SetHandIKWeight(HandType.Right, 1f, duration));
                StartCoroutine(SetFootIKWeight(FootType.Left, 1f, duration));
                StartCoroutine(SetFootIKWeight(FootType.Right, 1f, duration));
                yield return SetBodyIKWeight(1f, duration);
            }
            else
            {
                yield return new WaitForSeconds(duration);
            }
        }

        #endregion

        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
}
