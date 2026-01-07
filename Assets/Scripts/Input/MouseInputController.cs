using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using RPGDefete.Character;
using RPG.Interaction;

namespace RPG.Input
{
    /// <summary>
    /// アクションタイプ
    /// </summary>
    public enum ActionType
    {
        None,
        Attack,         // 近距離攻撃
        Magic,          // 魔法攻撃
        Grab,           // 掴む
        Touch,          // 触る
        Sit,            // 座る
        Eat,            // 食べる
        Stomp,          // 踏む
        Talk,           // 話す
        Interact        // 汎用インタラクト
    }

    /// <summary>
    /// ホバー情報
    /// </summary>
    public class HoverInfo
    {
        public GameObject Target;
        public Vector3 HitPoint;
        public Vector3 HitNormal;
        public ActionType SuggestedAction;
        public string DisplayName;
        public bool IsEnemy;
        public bool IsNPC;
        public bool IsInteractable;
    }

    /// <summary>
    /// マウス入力コントローラー
    /// 右クリック: 移動
    /// 左クリック: アクション（ターゲットに応じて）
    /// ホバー: ターゲット情報表示
    /// </summary>
    public class MouseInputController : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private CharacterNavigator navigator;
        [SerializeField] private UnityEngine.Camera mainCamera;

        [Header("レイヤー設定")]
        [SerializeField] private LayerMask groundLayer = 1;
        [SerializeField] private LayerMask interactableLayer = -1;
        [SerializeField] private LayerMask enemyLayer;

        [Header("距離設定")]
        [SerializeField] private float maxRayDistance = 100f;
        [SerializeField] private float meleeRange = 2f;

        [Header("タグ設定")]
        [SerializeField] private string enemyTag = "Enemy";
        [SerializeField] private string npcTag = "NPC";
        [SerializeField] private string playerTag = "Player";

        [Header("デバッグ")]
        [SerializeField] private bool showDebugLog = true;
        [SerializeField] private bool showClickMarker = true;
        [SerializeField] private GameObject clickMarkerPrefab;

        // 現在のホバー情報
        private HoverInfo _currentHover = new HoverInfo();
        private GameObject _lastHoverTarget;

        // イベント
        public event Action<Vector3> OnMoveCommand;
        public event Action<GameObject, ActionType, Vector3> OnActionCommand;
        public event Action<HoverInfo> OnHoverChanged;

        // プロパティ
        public HoverInfo CurrentHover => _currentHover;
        public bool IsEnabled { get; set; } = true;

        private void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = UnityEngine.Camera.main;
            }

            if (navigator == null)
            {
                navigator = GetComponent<CharacterNavigator>();
            }
        }

        private void Update()
        {
            if (!IsEnabled) return;

            // UI上にマウスがある場合は処理しない
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            UpdateHover();
            HandleInput();
        }

        /// <summary>
        /// ホバー情報を更新
        /// </summary>
        private void UpdateHover()
        {
            Ray ray = mainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, interactableLayer))
            {
                GameObject target = hit.collider.gameObject;

                // ホバー情報更新
                _currentHover.Target = target;
                _currentHover.HitPoint = hit.point;
                _currentHover.HitNormal = hit.normal;
                _currentHover.IsEnemy = target.CompareTag(enemyTag);
                _currentHover.IsNPC = target.CompareTag(npcTag);
                _currentHover.IsInteractable = HasInteractableAttribute(target);
                _currentHover.SuggestedAction = DetermineAction(target, hit.point);
                _currentHover.DisplayName = GetDisplayName(target);

                // ホバーターゲット変更通知
                if (target != _lastHoverTarget)
                {
                    _lastHoverTarget = target;
                    OnHoverChanged?.Invoke(_currentHover);

                    if (showDebugLog)
                    {
                        Debug.Log($"[MouseInput] Hover: {target.name}, Action: {_currentHover.SuggestedAction}");
                    }
                }
            }
            else
            {
                // ホバー解除
                if (_lastHoverTarget != null)
                {
                    _currentHover.Target = null;
                    _currentHover.IsEnemy = false;
                    _currentHover.IsNPC = false;
                    _currentHover.IsInteractable = false;
                    _currentHover.SuggestedAction = ActionType.None;
                    _currentHover.DisplayName = null;
                    _lastHoverTarget = null;
                    OnHoverChanged?.Invoke(_currentHover);
                }
            }
        }

        /// <summary>
        /// 入力処理
        /// </summary>
        private void HandleInput()
        {
            // 右クリック: 移動
            if (UnityEngine.Input.GetMouseButtonDown(1))
            {
                HandleRightClick();
            }

            // 左クリック: アクション
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                HandleLeftClick();
            }
        }

        /// <summary>
        /// 右クリック処理（移動）
        /// </summary>
        private void HandleRightClick()
        {
            Ray ray = mainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, groundLayer))
            {
                Vector3 destination = hit.point;

                // NavMesh上の最寄り点を取得
                if (NavMesh.SamplePosition(destination, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
                {
                    destination = navHit.position;
                }

                // 移動実行
                if (navigator != null)
                {
                    navigator.MoveTo(destination);
                }

                OnMoveCommand?.Invoke(destination);

                // クリックマーカー表示
                if (showClickMarker && clickMarkerPrefab != null)
                {
                    var marker = Instantiate(clickMarkerPrefab, destination, Quaternion.identity);
                    Destroy(marker, 1f);
                }

                if (showDebugLog)
                {
                    Debug.Log($"[MouseInput] Move to: {destination}");
                }
            }
        }

        /// <summary>
        /// 左クリック処理（アクション）
        /// </summary>
        private void HandleLeftClick()
        {
            Ray ray = mainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, interactableLayer))
            {
                GameObject target = hit.collider.gameObject;
                ActionType action = DetermineAction(target, hit.point);

                OnActionCommand?.Invoke(target, action, hit.point);

                if (showDebugLog)
                {
                    Debug.Log($"[MouseInput] Action: {action} on {target.name}");
                }
            }
            else if (Physics.Raycast(ray, out RaycastHit groundHit, maxRayDistance, groundLayer))
            {
                // 地面クリック → その位置へ魔法
                OnActionCommand?.Invoke(null, ActionType.Magic, groundHit.point);

                if (showDebugLog)
                {
                    Debug.Log($"[MouseInput] Magic to ground: {groundHit.point}");
                }
            }
        }

        /// <summary>
        /// ターゲットに応じたアクションを決定
        /// </summary>
        private ActionType DetermineAction(GameObject target, Vector3 hitPoint)
        {
            if (target == null) return ActionType.None;

            // Enemy判定
            if (target.CompareTag(enemyTag))
            {
                float distance = Vector3.Distance(transform.position, target.transform.position);
                return distance < meleeRange ? ActionType.Attack : ActionType.Magic;
            }

            // NPC/Player判定
            if (target.CompareTag(npcTag) || target.CompareTag(playerTag))
            {
                return ActionType.Talk;
            }

            // InteractableObject属性判定（優先）
            var interactableObject = target.GetComponent<InteractableObject>();
            if (interactableObject != null && interactableObject.IsEnabled)
            {
                return GetActionFromInteractableAttribute(interactableObject.Attributes);
            }

            // 旧Interactable属性判定（後方互換）
            var interactable = target.GetComponent<Interactable>();
            if (interactable != null)
            {
                return interactable.GetActionType();
            }

            // レイヤーベースの判定
            if (((1 << target.layer) & enemyLayer) != 0)
            {
                float distance = Vector3.Distance(transform.position, target.transform.position);
                return distance < meleeRange ? ActionType.Attack : ActionType.Magic;
            }

            return ActionType.Interact;
        }

        /// <summary>
        /// InteractableAttributeからActionTypeを決定
        /// 複数属性がある場合は優先度順で最初のものを返す
        /// </summary>
        private ActionType GetActionFromInteractableAttribute(InteractableAttribute attr)
        {
            // 優先度順でチェック
            if ((attr & InteractableAttribute.Talkable) != 0) return ActionType.Talk;
            if ((attr & InteractableAttribute.Grabbable) != 0) return ActionType.Grab;
            if ((attr & InteractableAttribute.Sittable) != 0) return ActionType.Sit;
            if ((attr & InteractableAttribute.Touchable) != 0) return ActionType.Touch;
            if ((attr & InteractableAttribute.Eatable) != 0) return ActionType.Eat;
            if ((attr & InteractableAttribute.Stompable) != 0) return ActionType.Stomp;

            return ActionType.Interact;
        }

        /// <summary>
        /// インタラクタブル属性を持つか
        /// </summary>
        private bool HasInteractableAttribute(GameObject target)
        {
            // InteractableObjectを優先チェック
            var interactableObject = target.GetComponent<InteractableObject>();
            if (interactableObject != null) return true;

            // 旧Interactableもチェック（後方互換）
            return target.GetComponent<Interactable>() != null;
        }

        /// <summary>
        /// 表示名を取得
        /// </summary>
        private string GetDisplayName(GameObject target)
        {
            // InteractableObjectから名前取得（優先）
            var interactableObject = target.GetComponent<InteractableObject>();
            if (interactableObject != null && !string.IsNullOrEmpty(interactableObject.DisplayName))
            {
                return interactableObject.DisplayName;
            }

            // 旧Interactableから名前取得
            var interactable = target.GetComponent<Interactable>();
            if (interactable != null && !string.IsNullOrEmpty(interactable.DisplayName))
            {
                return interactable.DisplayName;
            }

            // EnemyControllerから名前取得
            var enemy = target.GetComponent<EnemyController>();
            if (enemy != null)
            {
                return enemy.EnemyName;
            }

            return target.name;
        }

        /// <summary>
        /// 近接距離を設定
        /// </summary>
        public void SetMeleeRange(float range)
        {
            meleeRange = range;
        }

        /// <summary>
        /// Navigatorを設定
        /// </summary>
        public void SetNavigator(CharacterNavigator nav)
        {
            navigator = nav;
        }

        private void OnDrawGizmosSelected()
        {
            // 近接範囲を表示
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, meleeRange);
        }
    }

    /// <summary>
    /// インタラクタブルオブジェクトの基底クラス
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        [Header("インタラクション設定")]
        [SerializeField] private string displayName;
        [SerializeField] private ActionType actionType = ActionType.Interact;
        [SerializeField] private bool isEnabled = true;

        public string DisplayName => displayName;
        public bool IsEnabled => isEnabled;

        public virtual ActionType GetActionType()
        {
            return actionType;
        }

        public virtual bool CanInteract(GameObject actor)
        {
            return isEnabled;
        }

        public virtual void OnInteract(GameObject actor)
        {
            Debug.Log($"[Interactable] {actor.name} interacted with {gameObject.name}");
        }

        public virtual void OnHoverEnter()
        {
            // ハイライト等
        }

        public virtual void OnHoverExit()
        {
            // ハイライト解除等
        }
    }
}
