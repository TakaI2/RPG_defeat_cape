using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RPGDefete.Character;
using RPG.Interaction;

namespace RPG.Action
{
    /// <summary>
    /// アクション状態
    /// </summary>
    public enum ActionState
    {
        Ready,
        Executing,
        Completed,
        Cancelled
    }

    /// <summary>
    /// アクション優先度
    /// </summary>
    public enum ActionPriority
    {
        Low = 0,      // 日常行動
        Normal = 1,   // 通常行動
        High = 2,     // 戦闘行動
        Critical = 3  // 緊急行動（ダメージ反応等）
    }

    /// <summary>
    /// アクションコンテキスト
    /// アクション実行に必要な情報を保持
    /// </summary>
    public class ActionContext
    {
        // 実行者
        public GameCharacter Actor { get; set; }

        // ターゲット情報
        public GameObject Target { get; set; }
        public Vector3 TargetPosition { get; set; }
        public InteractionPoint TargetPoint { get; set; }
        public InteractableObject TargetInteractable { get; set; }

        // 追加パラメータ
        public float Distance { get; set; }
        public SizeCategory TargetSize { get; set; }

        // コントローラー参照（便利アクセサ）
        public VRMFinalIKController IKController => Actor?.IKController;
        public VRMExpressionController ExpressionController => Actor?.ExpressionController;
        public VRMAnimationController AnimationController => Actor?.AnimationController;
        public VRMEyeGazeController EyeGazeController => Actor?.EyeGazeController;
        public CharacterNavigator Navigator => Actor?.Navigator;
    }

    /// <summary>
    /// アクション基底クラス
    /// </summary>
    public abstract class ActionBase
    {
        public abstract string ActionName { get; }
        public virtual ActionPriority Priority => ActionPriority.Normal;
        public virtual bool CanBeCancelled => true;

        public ActionState State { get; protected set; } = ActionState.Ready;

        /// <summary>
        /// このアクションが実行可能かどうか
        /// </summary>
        public abstract bool CanExecute(ActionContext context);

        /// <summary>
        /// アクション実行（コルーチン）
        /// </summary>
        public abstract IEnumerator Execute(ActionContext context);

        /// <summary>
        /// アクション開始時のコールバック
        /// </summary>
        public virtual void OnStart(ActionContext context)
        {
            State = ActionState.Executing;
            Debug.Log($"[Action] {ActionName} started by {context.Actor?.CharacterName}");
        }

        /// <summary>
        /// アクション終了時のコールバック
        /// </summary>
        public virtual void OnEnd(ActionContext context)
        {
            State = ActionState.Completed;
            Debug.Log($"[Action] {ActionName} completed");
        }

        /// <summary>
        /// アクションキャンセル
        /// </summary>
        public virtual void Cancel(ActionContext context)
        {
            State = ActionState.Cancelled;
            Debug.Log($"[Action] {ActionName} cancelled");
        }

        /// <summary>
        /// 状態をリセット
        /// </summary>
        public void Reset()
        {
            State = ActionState.Ready;
        }
    }

}
