using UnityEngine;
using RPGDefete.Character;
using RPG.Action;
using RPG.Interaction;

namespace RPG.Input
{
    /// <summary>
    /// MouseInputControllerとActionExecutorを接続するブリッジコンポーネント
    /// マウス入力イベントをアクションコンテキストに変換して実行する
    /// </summary>
    public class PlayerActionBridge : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private MouseInputController mouseInput;
        [SerializeField] private ActionExecutor actionExecutor;
        [SerializeField] private GameCharacter character;

        [Header("デバッグ")]
        [SerializeField] private bool showDebugLog = true;

        private void Awake()
        {
            // 自動取得
            if (mouseInput == null)
                mouseInput = GetComponent<MouseInputController>();
            if (actionExecutor == null)
                actionExecutor = GetComponent<ActionExecutor>();
            if (character == null)
                character = GetComponent<GameCharacter>();
        }

        private void OnEnable()
        {
            if (mouseInput != null)
            {
                mouseInput.OnActionCommand += HandleActionCommand;
            }
        }

        private void OnDisable()
        {
            if (mouseInput != null)
            {
                mouseInput.OnActionCommand -= HandleActionCommand;
            }
        }

        /// <summary>
        /// マウス入力からのアクションコマンドを処理
        /// </summary>
        private void HandleActionCommand(GameObject target, ActionType actionType, Vector3 hitPoint)
        {
            if (actionExecutor == null || character == null)
            {
                if (showDebugLog)
                {
                    Debug.LogWarning("[PlayerActionBridge] Missing ActionExecutor or GameCharacter reference");
                }
                return;
            }

            // ActionTypeをアクション名に変換
            string actionName = ConvertActionTypeToName(actionType);
            if (string.IsNullOrEmpty(actionName))
            {
                if (showDebugLog)
                {
                    Debug.Log($"[PlayerActionBridge] No action mapped for {actionType}");
                }
                return;
            }

            // コンテキスト作成
            ActionContext context = CreateActionContext(target, hitPoint, actionType);

            // アクション実行
            bool success = actionExecutor.TryExecuteAction(actionName, context);

            if (showDebugLog)
            {
                if (success)
                {
                    Debug.Log($"[PlayerActionBridge] Executed {actionName} on {target?.name ?? "ground"}");
                }
                else
                {
                    Debug.Log($"[PlayerActionBridge] Failed to execute {actionName}");
                }
            }
        }

        /// <summary>
        /// ActionTypeをアクション名に変換
        /// </summary>
        private string ConvertActionTypeToName(ActionType actionType)
        {
            return actionType switch
            {
                ActionType.Attack => "Attack",
                ActionType.Magic => "Magic",
                ActionType.Grab => "Grab",
                ActionType.Touch => "Touch",
                ActionType.Sit => "Sit",
                ActionType.Eat => "Eat",
                ActionType.Stomp => "Stomp",
                ActionType.Talk => "Talk",
                ActionType.Interact => "Interact",
                ActionType.None => null,
                _ => null
            };
        }

        /// <summary>
        /// アクションコンテキストを作成
        /// </summary>
        private ActionContext CreateActionContext(GameObject target, Vector3 hitPoint, ActionType actionType)
        {
            var context = new ActionContext
            {
                Actor = character,
                Target = target,
                TargetPosition = hitPoint
            };

            // ターゲット情報を追加
            if (target != null)
            {
                // 距離計算
                context.Distance = Vector3.Distance(transform.position, target.transform.position);

                // InteractableObject取得
                var interactable = target.GetComponent<InteractableObject>();
                if (interactable != null)
                {
                    context.TargetInteractable = interactable;
                    context.TargetSize = interactable.Size;

                    // インタラクションポイント取得（アクションタイプに応じて）
                    var pointType = GetInteractionPointType(actionType);
                    context.TargetPoint = interactable.GetPoint(pointType);
                }

                // GameCharacterターゲットの場合
                var targetCharacter = target.GetComponent<GameCharacter>();
                if (targetCharacter != null)
                {
                    // NPCのインタラクションポイントを取得
                    var eyePoint = targetCharacter.GetInteractionPoint(InteractionPointType.Eye);
                    if (eyePoint != null)
                    {
                        context.TargetPoint = eyePoint;
                    }
                }
            }
            else
            {
                // 地面クリックの場合
                context.Distance = Vector3.Distance(transform.position, hitPoint);
            }

            return context;
        }

        /// <summary>
        /// ActionTypeからInteractionPointTypeを取得
        /// </summary>
        private InteractionPointType GetInteractionPointType(ActionType actionType)
        {
            return actionType switch
            {
                ActionType.Touch => InteractionPointType.Touch,
                ActionType.Sit => InteractionPointType.Sit,
                ActionType.Grab => InteractionPointType.Grab,
                ActionType.Talk => InteractionPointType.Eye,
                _ => InteractionPointType.Look
            };
        }

        /// <summary>
        /// 手動でアクションを実行（UIボタン等から呼び出し用）
        /// </summary>
        public bool ExecuteAction(string actionName, GameObject target = null, Vector3? position = null)
        {
            if (actionExecutor == null || character == null) return false;

            var context = new ActionContext
            {
                Actor = character,
                Target = target,
                TargetPosition = position ?? (target?.transform.position ?? transform.position)
            };

            if (target != null)
            {
                context.Distance = Vector3.Distance(transform.position, target.transform.position);
                context.TargetInteractable = target.GetComponent<InteractableObject>();
            }

            return actionExecutor.TryExecuteAction(actionName, context);
        }
    }
}
