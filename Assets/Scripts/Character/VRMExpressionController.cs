using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniVRM10;

namespace RPGDefete.Character
{
    /// <summary>
    /// VRMキャラクターの表情を制御するコンポーネント
    /// Vrm10Instanceから表情を設定・遷移させる
    /// </summary>
    public class VRMExpressionController : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private Vrm10Instance vrm10Instance;

        [Header("設定")]
        [SerializeField] private string characterName = "Character";
        [SerializeField] private float defaultTransitionDuration = 0.3f;

        [Header("デバッグ")]
        [SerializeField] private bool debugMode = false;

        private Vrm10RuntimeExpression _expression;
        private Coroutine _transitionCoroutine;
        private Dictionary<string, ExpressionKey> _expressionKeyCache;

        /// <summary>キャラクター名</summary>
        public string CharacterName => characterName;

        /// <summary>VRM表情制御が有効か</summary>
        public bool IsValid => _expression != null;

        /// <summary>現在遷移中か</summary>
        public bool IsTransitioning { get; private set; }

        private void Awake()
        {
            if (vrm10Instance == null)
            {
                vrm10Instance = GetComponent<Vrm10Instance>();
            }

            InitializeExpressionKeyCache();
        }

        private void Start()
        {
            if (vrm10Instance != null && vrm10Instance.Runtime != null)
            {
                _expression = vrm10Instance.Runtime.Expression;
                if (debugMode)
                {
                    Debug.Log($"[VRMExpressionController] Initialized for {characterName}");
                }
            }
            else
            {
                Debug.LogWarning($"[VRMExpressionController] Vrm10Instance not found on {gameObject.name}");
            }
        }

        /// <summary>
        /// ExpressionKeyのキャッシュを初期化
        /// </summary>
        private void InitializeExpressionKeyCache()
        {
            _expressionKeyCache = new Dictionary<string, ExpressionKey>(StringComparer.OrdinalIgnoreCase)
            {
                // 感情表現
                { "happy", ExpressionKey.Happy },
                { "angry", ExpressionKey.Angry },
                { "sad", ExpressionKey.Sad },
                { "relaxed", ExpressionKey.Relaxed },
                { "surprised", ExpressionKey.Surprised },
                { "neutral", ExpressionKey.Neutral },

                // リップシンク
                { "aa", ExpressionKey.Aa },
                { "ih", ExpressionKey.Ih },
                { "ou", ExpressionKey.Ou },
                { "ee", ExpressionKey.Ee },
                { "oh", ExpressionKey.Oh },

                // 瞬き
                { "blink", ExpressionKey.Blink },
                { "blinkleft", ExpressionKey.BlinkLeft },
                { "blinkright", ExpressionKey.BlinkRight },

                // 視線（表情として使用する場合）
                { "lookup", ExpressionKey.LookUp },
                { "lookdown", ExpressionKey.LookDown },
                { "lookleft", ExpressionKey.LookLeft },
                { "lookright", ExpressionKey.LookRight }
            };
        }

        /// <summary>
        /// 表情名からExpressionKeyを取得
        /// </summary>
        private bool TryGetExpressionKey(string expressionName, out ExpressionKey key)
        {
            if (_expressionKeyCache.TryGetValue(expressionName, out key))
            {
                return true;
            }

            // カスタム表情として試行
            key = ExpressionKey.CreateCustom(expressionName);
            return true;
        }

        /// <summary>
        /// 表情を即座に設定
        /// </summary>
        /// <param name="expressionName">表情名</param>
        /// <param name="weight">表情の強さ (0-1)</param>
        public void SetExpression(string expressionName, float weight = 1f)
        {
            if (!IsValid)
            {
                Debug.LogWarning($"[VRMExpressionController] Expression not available for {characterName}");
                return;
            }

            if (!TryGetExpressionKey(expressionName, out var key))
            {
                Debug.LogWarning($"[VRMExpressionController] Unknown expression: {expressionName}");
                return;
            }

            weight = Mathf.Clamp01(weight);
            _expression.SetWeight(key, weight);

            if (debugMode)
            {
                Debug.Log($"[VRMExpressionController] {characterName}: Set {expressionName} = {weight}");
            }
        }

        /// <summary>
        /// 表情を遷移付きで設定
        /// </summary>
        /// <param name="expressionName">表情名</param>
        /// <param name="weight">目標の強さ (0-1)</param>
        /// <param name="duration">遷移時間（秒）</param>
        /// <returns>コルーチン</returns>
        public IEnumerator SetExpressionWithTransition(string expressionName, float weight, float duration)
        {
            if (!IsValid)
            {
                Debug.LogWarning($"[VRMExpressionController] Expression not available for {characterName}");
                yield break;
            }

            if (!TryGetExpressionKey(expressionName, out var key))
            {
                Debug.LogWarning($"[VRMExpressionController] Unknown expression: {expressionName}");
                yield break;
            }

            weight = Mathf.Clamp01(weight);

            // 即時変更の場合
            if (duration <= 0)
            {
                _expression.SetWeight(key, weight);
                yield break;
            }

            // 遷移中の場合は停止
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
            }

            IsTransitioning = true;

            float startWeight = _expression.GetWeight(key);
            float elapsed = 0f;

            if (debugMode)
            {
                Debug.Log($"[VRMExpressionController] {characterName}: Transitioning {expressionName} from {startWeight} to {weight} over {duration}s");
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float currentWeight = Mathf.Lerp(startWeight, weight, t);
                _expression.SetWeight(key, currentWeight);
                yield return null;
            }

            _expression.SetWeight(key, weight);
            IsTransitioning = false;

            if (debugMode)
            {
                Debug.Log($"[VRMExpressionController] {characterName}: Transition complete for {expressionName}");
            }
        }

        /// <summary>
        /// 表情を遷移付きで設定（非コルーチン版）
        /// </summary>
        public void StartExpressionTransition(string expressionName, float weight, float duration)
        {
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
            }
            _transitionCoroutine = StartCoroutine(SetExpressionWithTransition(expressionName, weight, duration));
        }

        /// <summary>
        /// 全ての表情をリセット（Neutralに戻す）
        /// </summary>
        public void ResetExpression()
        {
            if (!IsValid) return;

            // 全ての表情をリセット
            foreach (var key in _expressionKeyCache.Values)
            {
                _expression.SetWeight(key, 0f);
            }

            // Neutralを設定
            _expression.SetWeight(ExpressionKey.Neutral, 1f);

            if (debugMode)
            {
                Debug.Log($"[VRMExpressionController] {characterName}: Expression reset to Neutral");
            }
        }

        /// <summary>
        /// 全ての表情をリセット（遷移付き）
        /// </summary>
        public IEnumerator ResetExpressionWithTransition(float duration)
        {
            if (!IsValid) yield break;

            IsTransitioning = true;

            // 現在の表情値を取得
            var currentWeights = new Dictionary<ExpressionKey, float>();
            foreach (var key in _expressionKeyCache.Values)
            {
                currentWeights[key] = _expression.GetWeight(key);
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                foreach (var kvp in currentWeights)
                {
                    if (kvp.Key.Equals(ExpressionKey.Neutral))
                    {
                        _expression.SetWeight(kvp.Key, Mathf.Lerp(kvp.Value, 1f, t));
                    }
                    else
                    {
                        _expression.SetWeight(kvp.Key, Mathf.Lerp(kvp.Value, 0f, t));
                    }
                }

                yield return null;
            }

            ResetExpression();
            IsTransitioning = false;
        }

        /// <summary>
        /// 現在の表情の強さを取得
        /// </summary>
        public float GetExpressionWeight(string expressionName)
        {
            if (!IsValid) return 0f;

            if (TryGetExpressionKey(expressionName, out var key))
            {
                return _expression.GetWeight(key);
            }

            return 0f;
        }

        /// <summary>
        /// 利用可能な表情名一覧を取得
        /// </summary>
        public IReadOnlyList<string> GetAvailableExpressions()
        {
            return new List<string>(_expressionKeyCache.Keys);
        }

        /// <summary>
        /// 遷移を中断
        /// </summary>
        public void StopTransition()
        {
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }
            IsTransitioning = false;
        }

        private void OnDestroy()
        {
            StopTransition();
        }
    }
}
