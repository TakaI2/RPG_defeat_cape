using System.Collections;
using UnityEngine;
using UniVRM10;

namespace RPGDefete.Character
{
    /// <summary>
    /// VRM10の視線制御（目玉のみ）を管理するコンポーネント
    /// Vrm10Instance.Runtime.LookAtを使用して目玉をターゲットに向ける
    /// FinalIK LookAtIK（頭の回転）と組み合わせて自然な視線を実現
    /// </summary>
    public class VRMEyeGazeController : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private Vrm10Instance vrm10Instance;
        [SerializeField] private VRMFinalIKController finalIKController;

        [Header("視線設定")]
        [SerializeField] private Transform gazeTarget;
        [SerializeField] private float defaultTransitionDuration = 0.3f;

        [Header("視線制限")]
        [Tooltip("最大Yaw角（度）")]
        [SerializeField] private float maxYaw = 45f;
        [Tooltip("最大Pitch角（度）")]
        [SerializeField] private float maxPitch = 30f;

        [Header("頭IK連携")]
        [Tooltip("頭IKと目玉視線を連携させるか")]
        [SerializeField] private bool syncWithHeadIK = true;
        [Tooltip("目玉のみで追従する角度範囲（度）。これを超えると頭も動く")]
        [SerializeField] private float eyeOnlyRange = 15f;
        [Tooltip("頭IKの最大weight（目玉と合わせて100%を超えないように）")]
        [SerializeField] private float maxHeadIKWeight = 0.7f;

        [Header("デバッグ")]
        [SerializeField] private bool debugMode = false;

        private Vrm10RuntimeLookAt _runtimeLookAt;
        private Coroutine _transitionCoroutine;
        private float _currentWeight = 0f;
        private bool _isEnabled = false;

        /// <summary>視線制御が有効か</summary>
        public bool IsValid => _runtimeLookAt != null;

        /// <summary>現在の視線weight（0-1）</summary>
        public float CurrentWeight => _currentWeight;

        /// <summary>視線が有効か</summary>
        public bool IsEnabled => _isEnabled;

        /// <summary>現在のYaw角</summary>
        public float CurrentYaw => _runtimeLookAt?.Yaw ?? 0f;

        /// <summary>現在のPitch角</summary>
        public float CurrentPitch => _runtimeLookAt?.Pitch ?? 0f;

        private void Awake()
        {
            if (vrm10Instance == null)
                vrm10Instance = GetComponent<Vrm10Instance>();
            if (finalIKController == null)
                finalIKController = GetComponent<VRMFinalIKController>();
        }

        private void Start()
        {
            if (vrm10Instance != null && vrm10Instance.Runtime != null)
            {
                _runtimeLookAt = vrm10Instance.Runtime.LookAt;

                // VRM10のLookAt設定をYawPitchValueモードに変更
                vrm10Instance.LookAtTargetType = VRM10ObjectLookAt.LookAtTargetTypes.YawPitchValue;

                Debug.Log($"[VRMEyeGazeController] Initialized: {gameObject.name}, LookAtType: {vrm10Instance.Vrm.LookAt.LookAtType}");
            }
            else
            {
                Debug.LogWarning($"[VRMEyeGazeController] Vrm10Instance not found on {gameObject.name}");
            }
        }

        private void LateUpdate()
        {
            if (!IsValid || !_isEnabled || gazeTarget == null) return;

            UpdateGaze();
        }

        /// <summary>
        /// 視線を更新
        /// </summary>
        private void UpdateGaze()
        {
            // ターゲットへの視線角度を計算
            var (yaw, pitch) = _runtimeLookAt.CalculateYawPitchFromLookAtPosition(gazeTarget.position);

            // 角度を制限
            float clampedYaw = Mathf.Clamp(yaw, -maxYaw, maxYaw);
            float clampedPitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

            // weightを適用
            float weightedYaw = clampedYaw * _currentWeight;
            float weightedPitch = clampedPitch * _currentWeight;

            // 視線を設定（SetYawPitchManuallyを使用）
            _runtimeLookAt.SetYawPitchManually(weightedYaw, weightedPitch);

            if (debugMode && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[VRMEyeGazeController] Yaw:{weightedYaw:F1} Pitch:{weightedPitch:F1} Weight:{_currentWeight:F2}");
            }

            // 頭IKとの連携
            if (syncWithHeadIK && finalIKController != null && finalIKController.HasLookAt)
            {
                UpdateHeadIKSync(yaw, pitch);
            }
        }

        /// <summary>
        /// 頭IKとの連携を更新
        /// 目玉だけで追従できる範囲を超えたら頭も動かす
        /// </summary>
        private void UpdateHeadIKSync(float yaw, float pitch)
        {
            float absYaw = Mathf.Abs(yaw);
            float absPitch = Mathf.Abs(pitch);
            float maxAngle = Mathf.Max(absYaw, absPitch);

            // eyeOnlyRangeを超えたら頭IKを有効化
            if (maxAngle > eyeOnlyRange)
            {
                // 角度に応じて頭IKのweightを計算
                float excess = maxAngle - eyeOnlyRange;
                float headWeight = Mathf.Clamp01(excess / (maxYaw - eyeOnlyRange)) * maxHeadIKWeight * _currentWeight;

                finalIKController.SetLookAtWeightImmediate(headWeight);
            }
            else
            {
                finalIKController.SetLookAtWeightImmediate(0f);
            }
        }

        #region Public API

        /// <summary>
        /// 視線ターゲットを設定して有効化
        /// </summary>
        public void SetGazeTarget(Transform target)
        {
            gazeTarget = target;

            // 頭IKにも同じターゲットを設定
            if (syncWithHeadIK && finalIKController != null)
            {
                finalIKController.SetLookAtTarget(target);
            }

            if (debugMode)
                Debug.Log($"[VRMEyeGazeController] SetGazeTarget: {(target != null ? target.name : "null")}");
        }

        /// <summary>
        /// 視線を有効化（遷移付き）
        /// </summary>
        public IEnumerator EnableGaze(float duration = -1f)
        {
            if (!IsValid) yield break;

            float dur = duration < 0 ? defaultTransitionDuration : duration;
            yield return SetWeight(1f, dur);
            _isEnabled = true;

            if (debugMode)
                Debug.Log("[VRMEyeGazeController] Gaze enabled");
        }

        /// <summary>
        /// 視線を無効化（遷移付き）
        /// </summary>
        public IEnumerator DisableGaze(float duration = -1f)
        {
            if (!IsValid) yield break;

            float dur = duration < 0 ? defaultTransitionDuration : duration;
            yield return SetWeight(0f, dur);
            _isEnabled = false;

            // 頭IKも無効化
            if (syncWithHeadIK && finalIKController != null)
            {
                StartCoroutine(finalIKController.SetLookAtWeight(0f, dur));
            }

            if (debugMode)
                Debug.Log("[VRMEyeGazeController] Gaze disabled");
        }

        /// <summary>
        /// 視線weightを設定（遷移付き）
        /// </summary>
        public IEnumerator SetWeight(float weight, float duration = -1f)
        {
            if (!IsValid) yield break;

            if (_transitionCoroutine != null)
                StopCoroutine(_transitionCoroutine);

            float dur = duration < 0 ? defaultTransitionDuration : duration;
            float startWeight = _currentWeight;
            float elapsed = 0f;

            if (debugMode)
                Debug.Log($"[VRMEyeGazeController] Weight transition: {startWeight} -> {weight}");

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                _currentWeight = Mathf.Lerp(startWeight, weight, t);
                yield return null;
            }

            _currentWeight = weight;
            _isEnabled = weight > 0;
        }

        /// <summary>
        /// 視線weightを即座に設定
        /// </summary>
        public void SetWeightImmediate(float weight)
        {
            _currentWeight = Mathf.Clamp01(weight);
            _isEnabled = _currentWeight > 0;
        }

        /// <summary>
        /// ワールド座標を見る
        /// </summary>
        public void LookAtPosition(Vector3 worldPosition)
        {
            if (!IsValid) return;

            _runtimeLookAt.LookAtInput = new LookAtInput { WorldPosition = worldPosition };
            _isEnabled = true;
        }

        /// <summary>
        /// Yaw/Pitchを直接設定
        /// </summary>
        public void SetYawPitch(float yaw, float pitch)
        {
            if (!IsValid) return;

            _runtimeLookAt.SetYawPitchManually(yaw * _currentWeight, pitch * _currentWeight);
            _isEnabled = true;
        }

        /// <summary>
        /// 視線をリセット（正面を向く）
        /// </summary>
        public IEnumerator ResetGaze(float duration = -1f)
        {
            yield return DisableGaze(duration);

            if (IsValid)
            {
                _runtimeLookAt.SetYawPitchManually(0f, 0f);
            }
        }

        /// <summary>
        /// ターゲットを見る（ワンショット、遷移付き）
        /// </summary>
        public IEnumerator LookAt(Transform target, float weight = 1f, float duration = -1f)
        {
            SetGazeTarget(target);
            yield return SetWeight(weight, duration);
        }

        /// <summary>
        /// ターゲットから視線を外す
        /// </summary>
        public IEnumerator LookAway(float duration = -1f)
        {
            yield return DisableGaze(duration);
            gazeTarget = null;
        }

        #endregion

        private void OnDestroy()
        {
            if (_transitionCoroutine != null)
                StopCoroutine(_transitionCoroutine);
        }

        private void OnDrawGizmosSelected()
        {
            if (gazeTarget == null) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, gazeTarget.position);
            Gizmos.DrawWireSphere(gazeTarget.position, 0.1f);
        }
    }
}
