using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RPGDefete.Character;

namespace RPG.Action
{
    /// <summary>
    /// アクション実行管理
    /// </summary>
    public class ActionExecutor : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private GameCharacter character;

        [Header("デバッグ")]
        [SerializeField] private bool showDebugLog = true;

        // 登録されたアクション
        private Dictionary<string, ActionBase> _actions = new Dictionary<string, ActionBase>();

        // 現在実行中のアクション
        private ActionBase _currentAction;
        private Coroutine _currentCoroutine;

        // プロパティ
        public ActionBase CurrentAction => _currentAction;
        public bool IsExecuting => _currentAction != null && _currentAction.State == ActionState.Executing;

        // イベント
        public event Action<ActionBase> OnActionStarted;
        public event Action<ActionBase> OnActionCompleted;
        public event Action<ActionBase> OnActionCancelled;

        private void Awake()
        {
            if (character == null)
            {
                character = GetComponent<GameCharacter>();
            }

            RegisterDefaultActions();
        }

        /// <summary>
        /// デフォルトアクションを登録
        /// </summary>
        private void RegisterDefaultActions()
        {
            RegisterAction(new AttackAction());
            RegisterAction(new MagicAction());
            RegisterAction(new GrabAction());
            RegisterAction(new ReleaseAction());
            RegisterAction(new TouchAction());
            RegisterAction(new SitAction());
            RegisterAction(new StandAction());
            RegisterAction(new TalkAction());
        }

        /// <summary>
        /// アクションを登録
        /// </summary>
        public void RegisterAction(ActionBase action)
        {
            _actions[action.ActionName] = action;

            if (showDebugLog)
            {
                Debug.Log($"[ActionExecutor] Registered action: {action.ActionName}");
            }
        }

        /// <summary>
        /// アクションを取得
        /// </summary>
        public ActionBase GetAction(string actionName)
        {
            return _actions.TryGetValue(actionName, out var action) ? action : null;
        }

        /// <summary>
        /// アクションを実行（名前指定）
        /// </summary>
        public bool TryExecuteAction(string actionName, ActionContext context)
        {
            if (!_actions.TryGetValue(actionName, out var action))
            {
                if (showDebugLog)
                {
                    Debug.LogWarning($"[ActionExecutor] Action not found: {actionName}");
                }
                return false;
            }

            return TryExecuteAction(action, context);
        }

        /// <summary>
        /// アクションを実行
        /// </summary>
        public bool TryExecuteAction(ActionBase action, ActionContext context)
        {
            // 実行中のアクションをチェック
            if (IsExecuting)
            {
                // 優先度比較
                if (action.Priority <= _currentAction.Priority)
                {
                    if (showDebugLog)
                    {
                        Debug.Log($"[ActionExecutor] Cannot execute {action.ActionName}: {_currentAction.ActionName} is executing with higher priority");
                    }
                    return false;
                }

                // 現在のアクションをキャンセル可能か
                if (!_currentAction.CanBeCancelled)
                {
                    if (showDebugLog)
                    {
                        Debug.Log($"[ActionExecutor] Cannot execute {action.ActionName}: {_currentAction.ActionName} cannot be cancelled");
                    }
                    return false;
                }

                // キャンセル
                CancelCurrentAction();
            }

            // 実行可能かチェック
            if (!action.CanExecute(context))
            {
                if (showDebugLog)
                {
                    Debug.Log($"[ActionExecutor] {action.ActionName} cannot execute in current context");
                }
                return false;
            }

            // 実行
            _currentCoroutine = StartCoroutine(ExecuteActionCoroutine(action, context));
            return true;
        }

        private IEnumerator ExecuteActionCoroutine(ActionBase action, ActionContext context)
        {
            _currentAction = action;
            action.OnStart(context);
            OnActionStarted?.Invoke(action);

            // キャラクター状態を更新
            if (character != null)
            {
                character.State = CharacterState.Interacting;
            }

            yield return action.Execute(context);

            action.OnEnd(context);
            OnActionCompleted?.Invoke(action);

            // キャラクター状態を戻す
            if (character != null && character.State == CharacterState.Interacting)
            {
                character.State = CharacterState.Idle;
            }

            _currentAction = null;
            _currentCoroutine = null;
            action.Reset();
        }

        /// <summary>
        /// 現在のアクションをキャンセル
        /// </summary>
        public void CancelCurrentAction()
        {
            if (_currentAction == null) return;

            if (_currentCoroutine != null)
            {
                StopCoroutine(_currentCoroutine);
                _currentCoroutine = null;
            }

            _currentAction.Cancel(new ActionContext { Actor = character });
            OnActionCancelled?.Invoke(_currentAction);
            _currentAction.Reset();
            _currentAction = null;

            // キャラクター状態を戻す
            if (character != null)
            {
                character.State = CharacterState.Idle;
            }
        }

        /// <summary>
        /// 全アクション名を取得
        /// </summary>
        public IEnumerable<string> GetAllActionNames()
        {
            return _actions.Keys;
        }
    }
}
