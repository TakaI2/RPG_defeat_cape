using System.Collections;
using UnityEngine;
using RootMotion.FinalIK;

namespace RPGDefete.Character
{
    // Note: HandTypeとFootTypeはVRMIKController.csにも定義されていますが、
    // VRMFinalIKControllerは独立して動作するため、ここでも定義します
    // 将来的には共通のファイルに移動することを検討してください

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

        // Custom Head Effector
        private Vector3 _customHeadEffectorTarget;
        private float _customHeadEffectorWeight = 0f;
        private Transform _headBone;

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

            // 頭のボーンを取得
            var animator = GetComponent<Animator>();
            if (animator != null)
            {
                _headBone = animator.GetBoneTransform(HumanBodyBones.Head);
            }

            // カスタムHeadEffectorのイベントを登録
            if (fbbik != null)
            {
                fbbik.solver.OnPostUpdate += ApplyCustomHeadEffector;
            }
        }

        private void OnDestroy()
        {
            // イベント解除
            if (fbbik != null)
            {
                fbbik.solver.OnPostUpdate -= ApplyCustomHeadEffector;
            }

            // すべてのコルーチンを停止
            StopAllCoroutines();
        }

        /// <summary>
        /// カスタムHeadEffectorを適用（FinalIKのOnPostUpdate後に実行）
        /// </summary>
        private void ApplyCustomHeadEffector()
        {
            if (_headBone == null || _customHeadEffectorWeight <= 0f) return;

            // 現在の頭の位置から目標位置へ、重みに応じて補間
            Vector3 currentPos = _headBone.position;
            Vector3 targetPos = _customHeadEffectorTarget;
            _headBone.position = Vector3.Lerp(currentPos, targetPos, _customHeadEffectorWeight);

            if (debugMode && Time.frameCount % 30 == 0)
            {
                float distance = Vector3.Distance(currentPos, targetPos);
                Debug.Log($"[CustomHeadEffector] Weight: {_customHeadEffectorWeight:F2}, Distance to target: {distance:F3}m");
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
            // targetを直接設定（毎フレーム自動追従）
            effector.target = target;
            // 位置・回転も設定（即座に反映）
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

            // weight=0になったらtargetをクリア
            if (weight <= 0f)
            {
                effector.target = null;
            }
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

        #region Kiss Action Support

        private Vector3 _headOffset;
        private float _spineBendAngle;
        private float _heelHeight;
        private Transform _spineTransform;
        private GameObject _headIKTarget;
        private Coroutine _headApproachCoroutine;

        /// <summary>
        /// 頭のオフセットを設定（顔を近づける）
        /// 注意：この実装は簡易版です。本格的にはFABRIKやLookAtIKと組み合わせて使用します
        /// </summary>
        public void SetHeadOffset(Vector3 offset)
        {
            _headOffset = offset;

            if (debugMode)
                Debug.Log($"[VRMFinalIKController] SetHeadOffset: {offset}");

            // TODO: FinalIKのLookAtやFABRIKで適用
            // 現在は記録のみ（実際の適用はUpdateで行う）
        }

        public Vector3 GetHeadOffset() => _headOffset;

        /// <summary>
        /// 頭を特定の位置に近づける（カスタムHeadEffector使用）
        /// </summary>
        public IEnumerator ApproachHeadToPosition(Vector3 targetPosition, float duration = 0.5f, float stopDistance = 0.05f)
        {
            if (_headApproachCoroutine != null)
                StopCoroutine(_headApproachCoroutine);

            if (_headBone == null)
            {
                if (debugMode)
                    Debug.LogWarning("[VRMFinalIKController] ApproachHeadToPosition: Head bone not found");
                yield break;
            }

            // 目標位置を設定（stopDistance分手前）
            Vector3 direction = (targetPosition - _headBone.position).normalized;
            float distance = Vector3.Distance(_headBone.position, targetPosition);
            Vector3 finalTarget = targetPosition - direction * stopDistance;

            if (debugMode)
            {
                Debug.Log($"[VRMFinalIKController] ApproachHeadToPosition (CustomHeadEffector): target={targetPosition}, distance={distance:F3}m, stopDistance={stopDistance:F3}m");
            }

            // IKターゲット作成（LookAt用）
            if (_headIKTarget == null)
            {
                _headIKTarget = new GameObject("HeadIKTarget");
            }
            _headIKTarget.transform.position = targetPosition;

            // LookAtターゲットを設定（頭の向きを制御）
            if (lookAtIK != null)
            {
                SetLookAtTarget(_headIKTarget.transform);
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / duration));

                // LookAtで頭の向きを制御
                if (lookAtIK != null)
                {
                    lookAtIK.solver.IKPositionWeight = Mathf.Lerp(0f, 0.5f, t); // 向きのみ、位置は控えめ
                    lookAtIK.solver.bodyWeight = 0.3f; // 体はあまり動かさない
                    lookAtIK.solver.headWeight = 0f;   // 頭の回転は無効化（Custom HeadEffectorで位置のみ制御）
                }

                // カスタムHeadEffectorで頭の位置を直接制御
                _customHeadEffectorTarget = finalTarget;
                _customHeadEffectorWeight = t; // 徐々に重みを上げる

                // デバッグ: 現在の頭の位置と距離を表示
                if (debugMode && Time.frameCount % 10 == 0)
                {
                    float currentDistance = Vector3.Distance(_headBone.position, targetPosition);
                    Debug.Log($"[VRMFinalIKController] Progress: {t:F2}, HeadEffector weight: {_customHeadEffectorWeight:F2}, Distance: {currentDistance:F3}m");
                }

                yield return null;
            }

            // 最終位置を設定
            if (lookAtIK != null)
            {
                lookAtIK.solver.IKPositionWeight = 0.5f;
                lookAtIK.solver.bodyWeight = 0.3f;
                lookAtIK.solver.headWeight = 0f; // 頭の回転は無効化
            }
            _customHeadEffectorWeight = 1f;

            // 1フレーム待ってからHeadBoneの最終位置を確認
            yield return null;

            // FBBIKを再有効化（Kiss終了時にRetractHeadで無効化される）
            // ここでは再有効化しない

            if (debugMode)
            {
                float finalDistance = Vector3.Distance(_headBone.position, targetPosition);
                Debug.Log($"[VRMFinalIKController] ApproachHeadToPosition: Completed.");
                Debug.Log($"  Target position: {targetPosition}");
                Debug.Log($"  Final HeadBone position: {_headBone.position}");
                Debug.Log($"  Final distance: {finalDistance:F3}m");
            }
        }

        /// <summary>
        /// 頭を元の位置に戻す（カスタムHeadEffectorをリセット）
        /// </summary>
        public IEnumerator RetractHead(float duration = 0.4f)
        {
            if (debugMode)
                Debug.Log("[VRMFinalIKController] RetractHead: Starting");

            float elapsed = 0f;
            float startHeadEffectorWeight = _customHeadEffectorWeight;
            float startLookAtWeight = lookAtIK != null ? lookAtIK.solver.IKPositionWeight : 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // カスタムHeadEffectorの重みを0に戻す
                _customHeadEffectorWeight = Mathf.Lerp(startHeadEffectorWeight, 0f, t);

                // LookAtの重みも0に戻す
                if (lookAtIK != null)
                {
                    lookAtIK.solver.IKPositionWeight = Mathf.Lerp(startLookAtWeight, 0f, t);
                }

                yield return null;
            }

            // 完全にリセット
            _customHeadEffectorWeight = 0f;
            if (lookAtIK != null)
            {
                lookAtIK.solver.IKPositionWeight = 0f;
                lookAtIK.solver.target = null;
                lookAtIK.solver.bodyWeight = 0f;
                lookAtIK.solver.headWeight = 0f;
            }

            // ターゲットオブジェクトを削除
            if (_headIKTarget != null)
            {
                Destroy(_headIKTarget);
                _headIKTarget = null;
            }

            if (debugMode)
                Debug.Log("[VRMFinalIKController] RetractHead: Completed");
        }

        /// <summary>
        /// 頭のIKターゲットを更新（毎フレーム呼び出し可能）
        /// </summary>
        public void UpdateHeadIKTarget(Vector3 targetPosition)
        {
            if (_headIKTarget != null)
            {
                _headIKTarget.transform.position = targetPosition;
            }
        }

        /// <summary>
        /// 脊椎の曲げ角度を設定（前かがみ）
        /// </summary>
        public void SetSpineBend(float angle)
        {
            _spineBendAngle = Mathf.Clamp(angle, 0f, 90f);

            if (debugMode)
                Debug.Log($"[VRMFinalIKController] SetSpineBend: {angle} degrees");

            ApplySpineBend();
        }

        /// <summary>
        /// 脊椎IKを適用
        /// </summary>
        private void ApplySpineBend()
        {
            if (_spineTransform == null && fbbik != null)
            {
                var animator = GetComponent<Animator>();
                if (animator != null)
                    _spineTransform = animator.GetBoneTransform(HumanBodyBones.Spine);
            }

            if (_spineTransform != null)
            {
                // 前方に傾ける
                Quaternion bendRotation = Quaternion.Euler(_spineBendAngle, 0, 0);
                _spineTransform.localRotation = bendRotation * Quaternion.identity;
            }
        }

        /// <summary>
        /// 踵の高さを設定（つま先立ち）
        /// </summary>
        public void SetHeelHeight(float height)
        {
            _heelHeight = Mathf.Clamp(height, 0f, 0.15f);

            if (debugMode)
                Debug.Log($"[VRMFinalIKController] SetHeelHeight: {height}m");

            ApplyHeelHeight();
        }

        /// <summary>
        /// つま先立ちIKを適用
        /// </summary>
        private void ApplyHeelHeight()
        {
            if (fbbik == null) return;

            // 両足のIKターゲットを上げる
            // 実際の実装ではfoot effectorのpositionを調整
            var leftFoot = fbbik.solver.leftFootEffector;
            var rightFoot = fbbik.solver.rightFootEffector;

            if (leftFoot != null)
            {
                Vector3 pos = leftFoot.position;
                pos.y += _heelHeight;
                leftFoot.position = pos;
            }

            if (rightFoot != null)
            {
                Vector3 pos = rightFoot.position;
                pos.y += _heelHeight;
                rightFoot.position = pos;
            }
        }

        /// <summary>
        /// スムーズに脊椎を曲げる
        /// </summary>
        public IEnumerator BendSpineSmooth(float targetAngle, float duration = 0.4f)
        {
            float startAngle = _spineBendAngle;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float currentAngle = Mathf.Lerp(startAngle, targetAngle, Mathf.SmoothStep(0, 1, t));
                SetSpineBend(currentAngle);
                yield return null;
            }

            SetSpineBend(targetAngle);
        }

        /// <summary>
        /// スムーズにつま先立ち
        /// </summary>
        public IEnumerator TipToeSmooth(float targetHeight, float duration = 0.3f)
        {
            float startHeight = _heelHeight;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float currentHeight = Mathf.Lerp(startHeight, targetHeight, t);
                SetHeelHeight(currentHeight);
                yield return null;
            }

            SetHeelHeight(targetHeight);
        }

        /// <summary>
        /// 姿勢をリセット（KissAction後に使用）
        /// </summary>
        public IEnumerator ResetKissPosture(float duration = 0.4f)
        {
            StartCoroutine(BendSpineSmooth(0f, duration));
            yield return TipToeSmooth(0f, duration);
            SetHeadOffset(Vector3.zero);
        }

        #endregion

    }
}
