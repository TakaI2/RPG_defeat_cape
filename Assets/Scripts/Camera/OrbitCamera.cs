using System.Collections;
using UnityEngine;

namespace RPG.Camera
{
    /// <summary>
    /// カメラモード
    /// </summary>
    public enum CameraMode
    {
        Follow,     // 通常追従
        Focus,      // 対象フォーカス
        Combat,     // 戦闘モード
        Cinematic   // カットシーン
    }

    /// <summary>
    /// プレイヤーを中心としたオービットカメラシステム
    /// 回転、ズーム、フォーカス切替、障害物回避機能を実装
    /// </summary>
    public class OrbitCamera : MonoBehaviour
    {
        [Header("ターゲット")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0, 1.5f, 0);

        [Header("距離")]
        [SerializeField] private float defaultDistance = 5f;
        [SerializeField] private float minDistance = 2f;
        [SerializeField] private float maxDistance = 15f;
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float zoomSmoothTime = 0.1f;

        [Header("回転")]
        [SerializeField] private float rotationSpeed = 3f;
        [SerializeField] private float minVerticalAngle = -20f;
        [SerializeField] private float maxVerticalAngle = 80f;
        [SerializeField] private float rotationSmoothTime = 0.1f;

        [Header("障害物回避")]
        [SerializeField] private bool avoidObstacles = true;
        [SerializeField] private LayerMask obstacleLayer = -1;
        [SerializeField] private float obstacleOffset = 0.3f;
        [SerializeField] private float obstacleCheckRadius = 0.2f;

        [Header("フォーカス")]
        [SerializeField] private float focusTransitionTime = 0.5f;

        [Header("デバッグ")]
        [SerializeField] private bool showDebugLog = false;

        // 現在の値
        private float _currentDistance;
        private float _targetDistance;
        private float _horizontalAngle;
        private float _verticalAngle = 30f;
        private float _distanceVelocity;

        // フォーカス用
        private Transform _focusTarget;
        private bool _isFocusing;
        private float _focusStartTime;
        private Vector3 _focusStartPosition;
        private Quaternion _focusStartRotation;
        private Coroutine _focusEndCoroutine;

        // モード
        public CameraMode CurrentMode { get; private set; } = CameraMode.Follow;
        public bool IsEnabled { get; set; } = true;

        // プロパティ
        public Transform Target => target;
        public float CurrentDistance => _currentDistance;
        public float HorizontalAngle => _horizontalAngle;
        public float VerticalAngle => _verticalAngle;

        private void Start()
        {
            _currentDistance = defaultDistance;
            _targetDistance = defaultDistance;

            if (target != null)
            {
                // 初期角度をターゲットに基づいて設定
                Vector3 direction = transform.position - GetTargetPosition();
                if (direction != Vector3.zero)
                {
                    _horizontalAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                    _verticalAngle = Mathf.Asin(direction.normalized.y) * Mathf.Rad2Deg;
                    _verticalAngle = Mathf.Clamp(_verticalAngle, minVerticalAngle, maxVerticalAngle);
                }
            }

            if (showDebugLog)
            {
                Debug.Log($"[OrbitCamera] Initialized. Distance={_currentDistance}, Angles=({_horizontalAngle}, {_verticalAngle})");
            }
        }

        private void LateUpdate()
        {
            if (!IsEnabled || target == null) return;

            if (_isFocusing)
            {
                UpdateFocusTransition();
            }
            else
            {
                HandleInput();
                UpdateCameraPosition();
            }
        }

        /// <summary>
        /// 入力処理
        /// </summary>
        private void HandleInput()
        {
            // マウスホイールでズーム
            float scroll = UnityEngine.Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _targetDistance = Mathf.Clamp(
                    _targetDistance - scroll * zoomSpeed,
                    minDistance,
                    maxDistance
                );
            }

            // 右ドラッグで回転
            if (UnityEngine.Input.GetMouseButton(1))
            {
                float mouseX = UnityEngine.Input.GetAxis("Mouse X");
                float mouseY = UnityEngine.Input.GetAxis("Mouse Y");

                _horizontalAngle += mouseX * rotationSpeed;
                _verticalAngle = Mathf.Clamp(
                    _verticalAngle - mouseY * rotationSpeed,
                    minVerticalAngle,
                    maxVerticalAngle
                );
            }

            // 中ボタンでリセット
            if (UnityEngine.Input.GetMouseButtonDown(2))
            {
                ResetCamera();
            }
        }

        /// <summary>
        /// カメラ位置更新
        /// </summary>
        private void UpdateCameraPosition()
        {
            // 距離のスムージング
            _currentDistance = Mathf.SmoothDamp(
                _currentDistance,
                _targetDistance,
                ref _distanceVelocity,
                zoomSmoothTime
            );

            // ターゲット位置
            Vector3 targetPos = GetTargetPosition();

            // 回転からオフセット計算
            Quaternion rotation = Quaternion.Euler(_verticalAngle, _horizontalAngle, 0);
            Vector3 offset = rotation * new Vector3(0, 0, -_currentDistance);

            // 目標位置
            Vector3 desiredPosition = targetPos + offset;

            // 障害物回避
            if (avoidObstacles)
            {
                desiredPosition = AvoidObstacles(targetPos, desiredPosition);
            }

            // 位置と回転を更新
            transform.position = desiredPosition;
            transform.LookAt(targetPos);
        }

        /// <summary>
        /// 障害物回避
        /// </summary>
        private Vector3 AvoidObstacles(Vector3 targetPos, Vector3 desiredPosition)
        {
            Vector3 direction = desiredPosition - targetPos;
            float distance = direction.magnitude;

            // SphereCastで太めの線で障害物チェック
            if (Physics.SphereCast(targetPos, obstacleCheckRadius, direction.normalized,
                out RaycastHit hit, distance, obstacleLayer))
            {
                // 障害物の手前に配置
                float newDistance = Mathf.Max(minDistance, hit.distance - obstacleOffset);
                return targetPos + direction.normalized * newDistance;
            }

            return desiredPosition;
        }

        /// <summary>
        /// ターゲット位置取得（オフセット込み）
        /// </summary>
        private Vector3 GetTargetPosition()
        {
            return target.position + targetOffset;
        }

        /// <summary>
        /// ターゲット設定
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;

            if (showDebugLog)
            {
                Debug.Log($"[OrbitCamera] Target set to: {newTarget?.name ?? "null"}");
            }
        }

        /// <summary>
        /// 特定オブジェクトにフォーカス
        /// </summary>
        public void FocusOn(Transform focusTarget, float duration = -1)
        {
            if (focusTarget == null) return;

            _focusTarget = focusTarget;
            _isFocusing = true;
            _focusStartTime = Time.time;
            _focusStartPosition = transform.position;
            _focusStartRotation = transform.rotation;
            CurrentMode = CameraMode.Focus;

            // 既存のコルーチンをキャンセル
            if (_focusEndCoroutine != null)
            {
                StopCoroutine(_focusEndCoroutine);
            }

            if (duration > 0)
            {
                _focusEndCoroutine = StartCoroutine(EndFocusAfter(duration));
            }

            if (showDebugLog)
            {
                Debug.Log($"[OrbitCamera] Focusing on: {focusTarget.name}");
            }
        }

        /// <summary>
        /// フォーカス遷移更新
        /// </summary>
        private void UpdateFocusTransition()
        {
            if (_focusTarget == null)
            {
                EndFocus();
                return;
            }

            float t = (Time.time - _focusStartTime) / focusTransitionTime;
            t = Mathf.Clamp01(t);

            // イージング（smoothstep）
            t = t * t * (3f - 2f * t);

            // フォーカスターゲットを見る位置を計算
            Vector3 focusPos = _focusTarget.position;
            Vector3 targetPos = GetTargetPosition();

            // 中間点を見るような位置
            Vector3 lookPoint = Vector3.Lerp(targetPos, focusPos, 0.5f);
            Vector3 dirToFocus = (focusPos - targetPos).normalized;
            Vector3 desiredPos = lookPoint - dirToFocus * _currentDistance;

            // 補間
            transform.position = Vector3.Lerp(_focusStartPosition, desiredPos, t);

            Vector3 lookDir = lookPoint - transform.position;
            if (lookDir != Vector3.zero)
            {
                Quaternion desiredRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(_focusStartRotation, desiredRot, t);
            }
        }

        /// <summary>
        /// フォーカス終了（指定時間後）
        /// </summary>
        private IEnumerator EndFocusAfter(float duration)
        {
            yield return new WaitForSeconds(duration);
            EndFocus();
        }

        /// <summary>
        /// フォーカス終了
        /// </summary>
        public void EndFocus()
        {
            _isFocusing = false;
            _focusTarget = null;
            CurrentMode = CameraMode.Follow;

            if (showDebugLog)
            {
                Debug.Log("[OrbitCamera] Focus ended");
            }
        }

        /// <summary>
        /// カメラリセット
        /// </summary>
        public void ResetCamera()
        {
            _targetDistance = defaultDistance;

            // ターゲットの後方に配置
            if (target != null)
            {
                _horizontalAngle = target.eulerAngles.y + 180f;
            }
            _verticalAngle = 30f;

            if (showDebugLog)
            {
                Debug.Log("[OrbitCamera] Camera reset");
            }
        }

        /// <summary>
        /// 距離設定
        /// </summary>
        public void SetDistance(float distance)
        {
            _targetDistance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        /// <summary>
        /// 角度設定
        /// </summary>
        public void SetAngles(float horizontal, float vertical)
        {
            _horizontalAngle = horizontal;
            _verticalAngle = Mathf.Clamp(vertical, minVerticalAngle, maxVerticalAngle);
        }

        /// <summary>
        /// モード設定
        /// </summary>
        public void SetMode(CameraMode mode)
        {
            CurrentMode = mode;

            switch (mode)
            {
                case CameraMode.Combat:
                    // 戦闘モード：やや引いた視点
                    _targetDistance = Mathf.Max(_targetDistance, defaultDistance * 1.2f);
                    break;
                case CameraMode.Follow:
                    _targetDistance = defaultDistance;
                    break;
            }

            if (showDebugLog)
            {
                Debug.Log($"[OrbitCamera] Mode changed to: {mode}");
            }
        }

        /// <summary>
        /// カメラシェイク
        /// </summary>
        public void Shake(float intensity, float duration)
        {
            StartCoroutine(ShakeCoroutine(intensity, duration));
        }

        private IEnumerator ShakeCoroutine(float intensity, float duration)
        {
            float elapsed = 0f;
            Vector3 originalOffset = targetOffset;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * intensity;
                float y = Random.Range(-1f, 1f) * intensity;

                targetOffset = originalOffset + new Vector3(x, y, 0);

                elapsed += Time.deltaTime;
                intensity *= 0.95f; // 減衰

                yield return null;
            }

            targetOffset = originalOffset;
        }

        private void OnDrawGizmosSelected()
        {
            if (target == null) return;

            Vector3 targetPos = Application.isPlaying ? GetTargetPosition() : target.position + targetOffset;

            // ターゲット位置
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPos, 0.2f);

            // 距離範囲
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetPos, minDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPos, maxDistance);

            // カメラへの線
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(targetPos, transform.position);
        }
    }
}
