using System;
using System.Collections;
using UnityEngine;

namespace RPGDefete.Character
{
    /// <summary>
    /// VRMキャラクターのアニメーションを制御するコンポーネント
    /// AnimatorコンポーネントのラッパーとしてTrigger名ベースのアニメーション再生を提供
    /// </summary>
    public class VRMAnimationController : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private Animator animator;

        [Header("設定")]
        [SerializeField] private string characterName = "Character";
        [SerializeField] private float defaultFadeTime = 0.25f;

        [Header("移動連携")]
        [SerializeField] private string isMovingParam = "IsMoving";
        [SerializeField] private string moveSpeedParam = "MoveSpeed";

        [Header("デバッグ")]
        [SerializeField] private bool debugMode = false;

        /// <summary>キャラクター名</summary>
        public string CharacterName => characterName;

        /// <summary>Animatorが有効か</summary>
        public bool IsValid => animator != null && animator.runtimeAnimatorController != null;

        /// <summary>現在遷移中か</summary>
        public bool IsTransitioning => IsValid && animator.IsInTransition(0);

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        private void Start()
        {
            if (animator == null)
            {
                Debug.LogWarning($"[VRMAnimationController] Animator not found on {gameObject.name}");
            }
            else if (debugMode)
            {
                Debug.Log($"[VRMAnimationController] Initialized for {characterName}");
            }
        }

        /// <summary>
        /// Trigger名でアニメーションを再生
        /// </summary>
        /// <param name="triggerName">Trigger名</param>
        public void PlayAnimation(string triggerName)
        {
            if (!IsValid)
            {
                Debug.LogWarning($"[VRMAnimationController] Animator not available for {characterName}");
                return;
            }

            if (string.IsNullOrEmpty(triggerName))
            {
                Debug.LogWarning($"[VRMAnimationController] Trigger name is null or empty");
                return;
            }

            animator.SetTrigger(triggerName);

            if (debugMode)
            {
                Debug.Log($"[VRMAnimationController] {characterName}: PlayAnimation trigger={triggerName}");
            }
        }

        /// <summary>
        /// ステート名でクロスフェード再生
        /// </summary>
        /// <param name="stateName">ステート名</param>
        /// <param name="fadeTime">クロスフェード時間</param>
        /// <param name="layer">レイヤー</param>
        public void CrossFade(string stateName, float fadeTime = -1f, int layer = 0)
        {
            if (!IsValid)
            {
                Debug.LogWarning($"[VRMAnimationController] Animator not available for {characterName}");
                return;
            }

            if (string.IsNullOrEmpty(stateName))
            {
                Debug.LogWarning($"[VRMAnimationController] State name is null or empty");
                return;
            }

            float duration = fadeTime < 0 ? defaultFadeTime : fadeTime;
            animator.CrossFade(stateName, duration, layer);

            if (debugMode)
            {
                Debug.Log($"[VRMAnimationController] {characterName}: CrossFade state={stateName}, fadeTime={duration}");
            }
        }

        /// <summary>
        /// Bool パラメータを設定
        /// </summary>
        public void SetBool(string paramName, bool value)
        {
            if (!IsValid) return;
            animator.SetBool(paramName, value);

            if (debugMode)
            {
                Debug.Log($"[VRMAnimationController] {characterName}: SetBool {paramName}={value}");
            }
        }

        /// <summary>
        /// Float パラメータを設定
        /// </summary>
        public void SetFloat(string paramName, float value)
        {
            if (!IsValid) return;
            animator.SetFloat(paramName, value);

            if (debugMode)
            {
                Debug.Log($"[VRMAnimationController] {characterName}: SetFloat {paramName}={value}");
            }
        }

        /// <summary>
        /// Integer パラメータを設定
        /// </summary>
        public void SetInteger(string paramName, int value)
        {
            if (!IsValid) return;
            animator.SetInteger(paramName, value);

            if (debugMode)
            {
                Debug.Log($"[VRMAnimationController] {characterName}: SetInteger {paramName}={value}");
            }
        }

        /// <summary>
        /// 移動中フラグを設定（CharacterNavigatorから呼ばれる）
        /// </summary>
        public void SetIsMoving(bool isMoving)
        {
            if (!IsValid) return;
            if (!string.IsNullOrEmpty(isMovingParam))
            {
                animator.SetBool(isMovingParam, isMoving);
            }
        }

        /// <summary>
        /// 移動速度を設定（CharacterNavigatorから呼ばれる）
        /// </summary>
        public void SetMoveSpeed(float speed)
        {
            if (!IsValid) return;
            if (!string.IsNullOrEmpty(moveSpeedParam))
            {
                animator.SetFloat(moveSpeedParam, speed);
            }
        }

        /// <summary>
        /// 現在のステート名を取得
        /// </summary>
        /// <param name="layer">レイヤー</param>
        public string GetCurrentStateName(int layer = 0)
        {
            if (!IsValid) return string.Empty;

            var stateInfo = animator.GetCurrentAnimatorStateInfo(layer);
            // Note: Animator doesn't directly expose state name
            // This returns the hash, actual name lookup requires additional setup
            return stateInfo.shortNameHash.ToString();
        }

        /// <summary>
        /// 現在のステートがnormalizedTime=1に達するまで待機
        /// </summary>
        public IEnumerator WaitForAnimationComplete(int layer = 0)
        {
            if (!IsValid) yield break;

            // 遷移が始まるまで少し待つ
            yield return null;

            // 遷移中なら完了を待つ
            while (animator.IsInTransition(layer))
            {
                yield return null;
            }

            // アニメーション終了まで待機
            var stateInfo = animator.GetCurrentAnimatorStateInfo(layer);
            while (stateInfo.normalizedTime < 1f)
            {
                yield return null;
                stateInfo = animator.GetCurrentAnimatorStateInfo(layer);
            }

            if (debugMode)
            {
                Debug.Log($"[VRMAnimationController] {characterName}: Animation complete");
            }
        }

        /// <summary>
        /// 指定時間だけアニメーション再生を待機
        /// </summary>
        public IEnumerator WaitForSeconds(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        /// <summary>
        /// 遷移完了まで待機
        /// </summary>
        public IEnumerator WaitForTransition(int layer = 0)
        {
            if (!IsValid) yield break;

            while (animator.IsInTransition(layer))
            {
                yield return null;
            }

            if (debugMode)
            {
                Debug.Log($"[VRMAnimationController] {characterName}: Transition complete");
            }
        }

        /// <summary>
        /// アニメーション速度を設定
        /// </summary>
        public void SetSpeed(float speed)
        {
            if (!IsValid) return;
            animator.speed = speed;
        }

        /// <summary>
        /// Animatorをリセット
        /// </summary>
        public void ResetAnimator()
        {
            if (!IsValid) return;
            animator.Rebind();
            animator.Update(0f);

            if (debugMode)
            {
                Debug.Log($"[VRMAnimationController] {characterName}: Animator reset");
            }
        }
    }
}
