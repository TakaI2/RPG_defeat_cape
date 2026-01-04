# 仕様書: カメラシステム

## 概要

プレイヤーを中心としたオービットカメラシステム。
回転、ズーム、フォーカス切替、障害物回避機能を実装する。

---

## 1. OrbitCamera クラス

```csharp
public class OrbitCamera : MonoBehaviour
{
    [Header("ターゲット")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset = new Vector3(0, 1.5f, 0);

    [Header("距離")]
    [SerializeField] private float defaultDistance = 5f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 15f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float zoomSmoothTime = 0.1f;

    [Header("回転")]
    [SerializeField] private float rotationSpeed = 3f;
    [SerializeField] private float minVerticalAngle = -20f;
    [SerializeField] private float maxVerticalAngle = 80f;
    [SerializeField] private float rotationSmoothTime = 0.1f;

    [Header("障害物回避")]
    [SerializeField] private bool avoidObstacles = true;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float obstacleOffset = 0.2f;

    [Header("フォーカス")]
    [SerializeField] private float focusTransitionTime = 0.5f;

    // 現在の値
    private float _currentDistance;
    private float _targetDistance;
    private float _horizontalAngle;
    private float _verticalAngle = 30f;
    private Vector3 _currentRotation;
    private Vector3 _rotationVelocity;
    private float _distanceVelocity;

    // フォーカス用
    private Transform _focusTarget;
    private bool _isFocusing;
    private float _focusStartTime;
    private Vector3 _focusStartPosition;
    private Quaternion _focusStartRotation;

    private void Start()
    {
        _currentDistance = defaultDistance;
        _targetDistance = defaultDistance;

        if (target != null)
        {
            // 初期角度をターゲットに向ける
            Vector3 direction = transform.position - GetTargetPosition();
            _horizontalAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

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
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            _targetDistance = Mathf.Clamp(
                _targetDistance - scroll * zoomSpeed,
                minDistance,
                maxDistance
            );
        }

        // 右ドラッグで回転
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            _horizontalAngle += mouseX * rotationSpeed;
            _verticalAngle = Mathf.Clamp(
                _verticalAngle - mouseY * rotationSpeed,
                minVerticalAngle,
                maxVerticalAngle
            );
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

        if (Physics.Raycast(targetPos, direction.normalized, out RaycastHit hit,
            distance, obstacleLayer))
        {
            // 障害物の手前に配置
            return hit.point - direction.normalized * obstacleOffset;
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

        if (duration > 0)
        {
            StartCoroutine(EndFocusAfter(duration));
        }
    }

    /// <summary>
    /// フォーカス遷移更新
    /// </summary>
    private void UpdateFocusTransition()
    {
        float t = (Time.time - _focusStartTime) / focusTransitionTime;
        t = Mathf.Clamp01(t);

        // イージング
        t = t * t * (3f - 2f * t); // smoothstep

        // フォーカスターゲットを見る位置を計算
        Vector3 focusPos = _focusTarget.position;
        Vector3 targetPos = GetTargetPosition();

        // 中間点を見るような位置
        Vector3 lookPoint = Vector3.Lerp(targetPos, focusPos, 0.5f);
        Vector3 desiredPos = lookPoint - (focusPos - targetPos).normalized * _currentDistance;

        // 補間
        transform.position = Vector3.Lerp(_focusStartPosition, desiredPos, t);

        Quaternion desiredRot = Quaternion.LookRotation(lookPoint - transform.position);
        transform.rotation = Quaternion.Slerp(_focusStartRotation, desiredRot, t);
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
}
```

---

## 2. Cinemachine連携（推奨）

より高度なカメラ制御にはCinemachineを使用することを推奨。

### 2.1 CinemachineManager

```csharp
using Cinemachine;

public class CinemachineManager : MonoBehaviour
{
    [Header("Virtual Cameras")]
    [SerializeField] private CinemachineVirtualCamera mainCamera;
    [SerializeField] private CinemachineVirtualCamera focusCamera;
    [SerializeField] private CinemachineVirtualCamera combatCamera;

    [Header("Blend Settings")]
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private float blendTime = 0.5f;

    private CinemachineVirtualCamera _currentCamera;

    private void Start()
    {
        _currentCamera = mainCamera;
        SetCameraPriority(mainCamera);
    }

    /// <summary>
    /// メインカメラに切り替え
    /// </summary>
    public void SwitchToMain()
    {
        SetCameraPriority(mainCamera);
    }

    /// <summary>
    /// フォーカスカメラに切り替え
    /// </summary>
    public void SwitchToFocus(Transform target)
    {
        focusCamera.LookAt = target;
        SetCameraPriority(focusCamera);
    }

    /// <summary>
    /// 戦闘カメラに切り替え
    /// </summary>
    public void SwitchToCombat()
    {
        SetCameraPriority(combatCamera);
    }

    /// <summary>
    /// カメラ優先度設定
    /// </summary>
    private void SetCameraPriority(CinemachineVirtualCamera cam)
    {
        mainCamera.Priority = 10;
        focusCamera.Priority = 10;
        combatCamera.Priority = 10;

        cam.Priority = 20;
        _currentCamera = cam;
    }

    /// <summary>
    /// シェイク
    /// </summary>
    public void TriggerShake(float amplitude, float frequency, float duration)
    {
        var noise = _currentCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        if (noise != null)
        {
            StartCoroutine(ShakeCoroutine(noise, amplitude, frequency, duration));
        }
    }

    private IEnumerator ShakeCoroutine(CinemachineBasicMultiChannelPerlin noise,
        float amplitude, float frequency, float duration)
    {
        noise.m_AmplitudeGain = amplitude;
        noise.m_FrequencyGain = frequency;

        yield return new WaitForSeconds(duration);

        noise.m_AmplitudeGain = 0;
        noise.m_FrequencyGain = 0;
    }
}
```

---

## 3. カメラモード

| モード | 用途 | 挙動 |
|--------|------|------|
| Follow | 通常プレイ | プレイヤー追従 |
| Focus | 会話/イベント | 対象にフォーカス |
| Combat | 戦闘中 | やや引いた視点 |
| Cinematic | カットシーン | 固定/レール |

---

## 4. 操作方法

| 入力 | 動作 |
|------|------|
| 右ドラッグ | カメラ回転 |
| スクロール | ズームイン/アウト |
| 中ボタン | カメラリセット |

---

## 5. 障害物回避フロー

```
┌─────────────────────────────────────────────────────────┐
│                  障害物回避フロー                        │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ターゲット位置                                         │
│       │                                                 │
│       ▼                                                 │
│  ┌──────────────────┐                                   │
│  │カメラ位置を計算   │                                   │
│  │(角度 + 距離)      │                                   │
│  └────────┬─────────┘                                   │
│           │                                             │
│           ▼                                             │
│  ┌──────────────────┐                                   │
│  │Raycast           │                                   │
│  │(Target → Camera) │                                   │
│  └────────┬─────────┘                                   │
│           │                                             │
│      ┌────┴────┐                                        │
│      │         │                                        │
│   Hit?        No Hit                                    │
│      │         │                                        │
│      ▼         ▼                                        │
│  障害物手前に  そのままの位置に                         │
│  カメラ配置    カメラ配置                               │
│      │         │                                        │
│      └────┬────┘                                        │
│           │                                             │
│           ▼                                             │
│  ┌──────────────────┐                                   │
│  │LookAt(Target)    │                                   │
│  └──────────────────┘                                   │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## 6. テストケース

| # | テスト | 期待結果 |
|---|-------|---------|
| 1 | 右ドラッグ | カメラが水平/垂直回転 |
| 2 | スクロール | ズームイン/アウト |
| 3 | 壁の近く | カメラが壁を貫通しない |
| 4 | FocusOn呼び出し | 指定オブジェクトにスムーズ遷移 |
| 5 | EndFocus呼び出し | 通常視点に復帰 |
| 6 | Shake呼び出し | カメラが揺れる |
| 7 | 垂直角度制限 | 真上/真下を向けない |

