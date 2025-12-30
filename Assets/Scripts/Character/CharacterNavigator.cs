using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace RPGDefete.Character
{
    /// <summary>
    /// キャラクターのNavMesh移動を制御するコンポーネント
    /// NavMeshAgentのラッパーとして目的地への移動と完了検知を提供
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class CharacterNavigator : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private VRMAnimationController animationController;

        [Header("設定")]
        [SerializeField] private string characterName = "Character";
        [SerializeField] private float defaultSpeed = 3.5f;
        [SerializeField] private float stoppingDistance = 0.1f;
        [SerializeField] private float rotationSpeed = 120f;

        [Header("アニメーション連携")]
        [SerializeField] private bool useSpeedParameter = true;
        [SerializeField] private string speedParam = "Speed";

        [Header("移動ポイント")]
        [SerializeField] private List<MovePointEntry> movePoints = new List<MovePointEntry>();

        [Header("デバッグ")]
        [SerializeField] private bool debugMode = false;

        /// <summary>移動ポイントの登録エントリ</summary>
        [Serializable]
        public class MovePointEntry
        {
            public string name;
            public Transform point;
        }

        private Dictionary<string, Transform> _movePointsDict;
        private Coroutine _moveCoroutine;
        private Action _onArrivalCallback;

        /// <summary>キャラクター名</summary>
        public string CharacterName => characterName;

        /// <summary>NavMeshAgentが有効か</summary>
        public bool IsValid => agent != null && agent.isOnNavMesh;

        /// <summary>移動中か</summary>
        public bool IsMoving { get; private set; }

        /// <summary>到着イベント</summary>
        public event Action OnArrived;

        private void Awake()
        {
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }

            if (animationController == null)
            {
                animationController = GetComponent<VRMAnimationController>();
            }

            InitializeMovePoints();
        }

        private void Start()
        {
            if (agent != null)
            {
                agent.speed = defaultSpeed;
                agent.stoppingDistance = stoppingDistance;
                agent.angularSpeed = rotationSpeed;
            }

            if (debugMode)
            {
                Debug.Log($"[CharacterNavigator] Initialized for {characterName}");
            }
        }

        /// <summary>
        /// 移動ポイント辞書を初期化
        /// </summary>
        private void InitializeMovePoints()
        {
            _movePointsDict = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in movePoints)
            {
                if (!string.IsNullOrEmpty(entry.name) && entry.point != null)
                {
                    _movePointsDict[entry.name] = entry.point;
                }
            }
        }

        /// <summary>
        /// 移動ポイントを登録（外部から）
        /// </summary>
        public void RegisterMovePoint(string name, Transform point)
        {
            if (string.IsNullOrEmpty(name) || point == null) return;
            _movePointsDict[name] = point;

            if (debugMode)
            {
                Debug.Log($"[CharacterNavigator] {characterName}: Registered move point '{name}'");
            }
        }

        /// <summary>
        /// 移動ポイントを解除
        /// </summary>
        public void UnregisterMovePoint(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            _movePointsDict.Remove(name);
        }

        /// <summary>
        /// 座標へ移動
        /// </summary>
        public void MoveTo(Vector3 destination, float speed = -1f)
        {
            if (!IsValid)
            {
                Debug.LogWarning($"[CharacterNavigator] Agent not available for {characterName}");
                return;
            }

            StopMovement();

            agent.speed = speed > 0 ? speed : defaultSpeed;
            agent.SetDestination(destination);
            IsMoving = true;

            // アニメーション連携
            if (animationController != null)
            {
                if (useSpeedParameter)
                {
                    // CombatAnimator互換: Speedパラメータで歩行/走行切替
                    float normalizedSpeed = agent.speed / defaultSpeed; // 0-1に正規化
                    animationController.SetFloat(speedParam, normalizedSpeed * 0.5f); // 0.3程度でWalk
                }
                else
                {
                    animationController.SetIsMoving(true);
                    animationController.SetMoveSpeed(agent.speed);
                }
            }

            _moveCoroutine = StartCoroutine(MonitorMovement());

            if (debugMode)
            {
                Debug.Log($"[CharacterNavigator] {characterName}: MoveTo position={destination}, speed={agent.speed}");
            }
        }

        /// <summary>
        /// 名前付きポイントへ移動
        /// </summary>
        public void MoveToPoint(string pointName, float speed = -1f)
        {
            if (string.IsNullOrEmpty(pointName))
            {
                Debug.LogWarning($"[CharacterNavigator] Point name is null or empty");
                return;
            }

            if (!_movePointsDict.TryGetValue(pointName, out var point))
            {
                Debug.LogWarning($"[CharacterNavigator] Move point not found: {pointName}");
                return;
            }

            MoveTo(point.position, speed);

            if (debugMode)
            {
                Debug.Log($"[CharacterNavigator] {characterName}: MoveToPoint name={pointName}");
            }
        }

        /// <summary>
        /// 移動停止
        /// </summary>
        public void Stop()
        {
            StopMovement();

            if (debugMode)
            {
                Debug.Log($"[CharacterNavigator] {characterName}: Stop");
            }
        }

        private void StopMovement()
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            if (agent != null && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }

            IsMoving = false;

            // アニメーション連携
            if (animationController != null)
            {
                if (useSpeedParameter)
                {
                    animationController.SetFloat(speedParam, 0f);
                }
                else
                {
                    animationController.SetIsMoving(false);
                    animationController.SetMoveSpeed(0f);
                }
            }
        }

        /// <summary>
        /// 移動完了を監視するコルーチン
        /// </summary>
        private IEnumerator MonitorMovement()
        {
            // パス計算待ち
            while (agent.pathPending)
            {
                yield return null;
            }

            // 到着待ち
            while (agent.remainingDistance > agent.stoppingDistance)
            {
                // アニメーション速度同期
                if (animationController != null)
                {
                    if (useSpeedParameter)
                    {
                        float normalizedSpeed = agent.velocity.magnitude / defaultSpeed;
                        animationController.SetFloat(speedParam, normalizedSpeed * 0.5f);
                    }
                    else
                    {
                        animationController.SetMoveSpeed(agent.velocity.magnitude);
                    }
                }
                yield return null;
            }

            // 到着処理
            IsMoving = false;
            _moveCoroutine = null;

            // アニメーション連携
            if (animationController != null)
            {
                if (useSpeedParameter)
                {
                    animationController.SetFloat(speedParam, 0f);
                }
                else
                {
                    animationController.SetIsMoving(false);
                    animationController.SetMoveSpeed(0f);
                }
            }

            if (debugMode)
            {
                Debug.Log($"[CharacterNavigator] {characterName}: Arrived at destination");
            }

            OnArrived?.Invoke();
            _onArrivalCallback?.Invoke();
            _onArrivalCallback = null;
        }

        /// <summary>
        /// 到着待機コルーチン
        /// </summary>
        public IEnumerator WaitForArrival()
        {
            while (IsMoving)
            {
                yield return null;
            }
        }

        /// <summary>
        /// 到着時コールバック設定
        /// </summary>
        public void SetOnArrivalCallback(Action callback)
        {
            _onArrivalCallback = callback;
        }

        /// <summary>
        /// 現在位置を取得
        /// </summary>
        public Vector3 GetPosition()
        {
            return transform.position;
        }

        /// <summary>
        /// 指定方向に回転
        /// </summary>
        public void LookAt(Vector3 target)
        {
            Vector3 direction = (target - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        /// <summary>
        /// ゆっくり回転（コルーチン）
        /// </summary>
        public IEnumerator LookAtSmooth(Vector3 target, float duration)
        {
            Vector3 direction = (target - transform.position).normalized;
            direction.y = 0;
            if (direction == Vector3.zero) yield break;

            Quaternion startRotation = transform.rotation;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                yield return null;
            }

            transform.rotation = targetRotation;
        }

        /// <summary>
        /// 移動ポイントが存在するか確認
        /// </summary>
        public bool HasMovePoint(string pointName)
        {
            return !string.IsNullOrEmpty(pointName) && _movePointsDict.ContainsKey(pointName);
        }

        /// <summary>
        /// 登録済み移動ポイント名一覧を取得
        /// </summary>
        public IReadOnlyCollection<string> GetMovePointNames()
        {
            return _movePointsDict.Keys;
        }

        private void OnDestroy()
        {
            StopMovement();
        }

        private void OnDrawGizmosSelected()
        {
            if (_movePointsDict == null) return;

            Gizmos.color = Color.cyan;
            foreach (var kvp in _movePointsDict)
            {
                if (kvp.Value != null)
                {
                    Gizmos.DrawWireSphere(kvp.Value.position, 0.3f);
                }
            }
        }
    }
}
