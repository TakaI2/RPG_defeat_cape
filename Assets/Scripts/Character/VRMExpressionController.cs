using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniVRM10;

namespace RPGDefete.Character
{
    /// <summary>
    /// 表情ウェイト（表情名と強さのペア）
    /// </summary>
    [System.Serializable]
    public class ExpressionWeight
    {
        [Tooltip("表情名（happy, sad, angry など）")]
        public string expressionName;

        [Range(0f, 1f)]
        [Tooltip("表情の強さ（0〜1）")]
        public float weight = 1f;

        public ExpressionWeight() { }

        public ExpressionWeight(string name, float w)
        {
            expressionName = name;
            weight = w;
        }
    }

    /// <summary>
    /// 表情プリセット（複数の表情を組み合わせた定義）
    /// </summary>
    [System.Serializable]
    public class ExpressionPreset
    {
        [Tooltip("プリセット名（呼び出し時に使用）")]
        public string presetName;

        [Tooltip("このプリセットに含まれる表情の組み合わせ")]
        public List<ExpressionWeight> weights = new List<ExpressionWeight>();

        public ExpressionPreset() { }

        public ExpressionPreset(string name, params ExpressionWeight[] expressionWeights)
        {
            presetName = name;
            weights = expressionWeights.ToList();
        }
    }

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

        [Header("表情プリセット")]
        [SerializeField] private bool useDefaultPresets = true;
        [SerializeField] private List<ExpressionPreset> customPresets = new List<ExpressionPreset>();

        [Header("デバッグ")]
        [SerializeField] private bool debugMode = false;

        private Vrm10RuntimeExpression _expression;
        private Coroutine _transitionCoroutine;
        private Dictionary<string, ExpressionKey> _expressionKeyCache;
        private Dictionary<string, ExpressionPreset> _presetCache;

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
            InitializePresetCache();
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
        /// プリセットキャッシュを初期化
        /// </summary>
        private void InitializePresetCache()
        {
            _presetCache = new Dictionary<string, ExpressionPreset>(StringComparer.OrdinalIgnoreCase);

            // デフォルトプリセットを追加
            if (useDefaultPresets)
            {
                AddDefaultPresets();
            }

            // カスタムプリセットを追加（Inspectorで設定したもの）
            foreach (var preset in customPresets)
            {
                if (!string.IsNullOrEmpty(preset.presetName))
                {
                    _presetCache[preset.presetName] = preset;
                }
            }

            if (debugMode)
            {
                Debug.Log($"[VRMExpressionController] Loaded {_presetCache.Count} expression presets");
            }
        }

        /// <summary>
        /// デフォルトプリセットを追加
        /// </summary>
        private void AddDefaultPresets()
        {
            // 微笑み（軽いhappy）
            RegisterPreset(new ExpressionPreset("smile",
                new ExpressionWeight("happy", 0.4f)
            ));

            // 爆笑
            RegisterPreset(new ExpressionPreset("laugh",
                new ExpressionWeight("happy", 1.0f),
                new ExpressionWeight("aa", 0.6f)
            ));

            // くすくす笑い
            RegisterPreset(new ExpressionPreset("giggle",
                new ExpressionWeight("happy", 0.6f),
                new ExpressionWeight("ih", 0.3f)
            ));

            // 困惑
            RegisterPreset(new ExpressionPreset("confused",
                new ExpressionWeight("sad", 0.3f),
                new ExpressionWeight("surprised", 0.3f)
            ));

            // 照れ
            RegisterPreset(new ExpressionPreset("embarrassed",
                new ExpressionWeight("happy", 0.4f),
                new ExpressionWeight("sad", 0.2f)
            ));

            // 不機嫌
            RegisterPreset(new ExpressionPreset("grumpy",
                new ExpressionWeight("angry", 0.5f),
                new ExpressionWeight("sad", 0.2f)
            ));

            // 激怒
            RegisterPreset(new ExpressionPreset("furious",
                new ExpressionWeight("angry", 1.0f),
                new ExpressionWeight("aa", 0.4f)
            ));

            // 泣き
            RegisterPreset(new ExpressionPreset("crying",
                new ExpressionWeight("sad", 1.0f),
                new ExpressionWeight("blink", 0.3f)
            ));

            // しょんぼり
            RegisterPreset(new ExpressionPreset("dejected",
                new ExpressionWeight("sad", 0.6f),
                new ExpressionWeight("lookdown", 0.3f)
            ));

            // 驚愕
            RegisterPreset(new ExpressionPreset("shocked",
                new ExpressionWeight("surprised", 1.0f),
                new ExpressionWeight("aa", 0.5f)
            ));

            // キョトン
            RegisterPreset(new ExpressionPreset("puzzled",
                new ExpressionWeight("surprised", 0.4f),
                new ExpressionWeight("neutral", 0.6f)
            ));

            // ウインク（右目）
            RegisterPreset(new ExpressionPreset("wink",
                new ExpressionWeight("happy", 0.3f),
                new ExpressionWeight("blinkright", 1.0f)
            ));

            // ウインク（左目）
            RegisterPreset(new ExpressionPreset("winkleft",
                new ExpressionWeight("happy", 0.3f),
                new ExpressionWeight("blinkleft", 1.0f)
            ));

            // ドヤ顔
            RegisterPreset(new ExpressionPreset("smug",
                new ExpressionWeight("happy", 0.5f),
                new ExpressionWeight("blinkleft", 0.7f)
            ));

            // 眠い
            RegisterPreset(new ExpressionPreset("sleepy",
                new ExpressionWeight("relaxed", 0.7f),
                new ExpressionWeight("blink", 0.5f)
            ));

            // リラックス笑顔
            RegisterPreset(new ExpressionPreset("content",
                new ExpressionWeight("happy", 0.3f),
                new ExpressionWeight("relaxed", 0.5f)
            ));

            // 悔しい
            RegisterPreset(new ExpressionPreset("frustrated",
                new ExpressionWeight("angry", 0.4f),
                new ExpressionWeight("sad", 0.5f)
            ));

            // 感動
            RegisterPreset(new ExpressionPreset("moved",
                new ExpressionWeight("happy", 0.6f),
                new ExpressionWeight("sad", 0.3f),
                new ExpressionWeight("surprised", 0.2f)
            ));

            // 疑惑
            RegisterPreset(new ExpressionPreset("suspicious",
                new ExpressionWeight("angry", 0.2f),
                new ExpressionWeight("blinkleft", 0.5f)
            ));

            // 期待
            RegisterPreset(new ExpressionPreset("expectant",
                new ExpressionWeight("happy", 0.4f),
                new ExpressionWeight("surprised", 0.3f)
            ));
        }

        /// <summary>
        /// プリセットを登録
        /// </summary>
        public void RegisterPreset(ExpressionPreset preset)
        {
            if (preset == null || string.IsNullOrEmpty(preset.presetName)) return;
            _presetCache[preset.presetName] = preset;
        }

        /// <summary>
        /// プリセットを削除
        /// </summary>
        public bool RemovePreset(string presetName)
        {
            return _presetCache.Remove(presetName);
        }

        /// <summary>
        /// プリセットを取得
        /// </summary>
        public ExpressionPreset GetPreset(string presetName)
        {
            _presetCache.TryGetValue(presetName, out var preset);
            return preset;
        }

        /// <summary>
        /// 利用可能なプリセット名一覧を取得
        /// </summary>
        public IReadOnlyList<string> GetAvailablePresets()
        {
            return _presetCache.Keys.ToList();
        }

        /// <summary>
        /// プリセットを適用（即座に）
        /// </summary>
        public void ApplyPreset(string presetName)
        {
            if (!IsValid)
            {
                Debug.LogWarning($"[VRMExpressionController] Expression not available for {characterName}");
                return;
            }

            if (!_presetCache.TryGetValue(presetName, out var preset))
            {
                Debug.LogWarning($"[VRMExpressionController] Preset not found: {presetName}");
                return;
            }

            // まず全表情をリセット
            foreach (var key in _expressionKeyCache.Values)
            {
                _expression.SetWeight(key, 0f);
            }

            // プリセットの表情を適用
            foreach (var ew in preset.weights)
            {
                if (TryGetExpressionKey(ew.expressionName, out var key))
                {
                    _expression.SetWeight(key, Mathf.Clamp01(ew.weight));
                }
            }

            if (debugMode)
            {
                Debug.Log($"[VRMExpressionController] {characterName}: Applied preset '{presetName}'");
            }
        }

        /// <summary>
        /// プリセットを適用（遷移付き）
        /// </summary>
        public IEnumerator ApplyPresetWithTransition(string presetName, float duration)
        {
            if (!IsValid)
            {
                Debug.LogWarning($"[VRMExpressionController] Expression not available for {characterName}");
                yield break;
            }

            if (!_presetCache.TryGetValue(presetName, out var preset))
            {
                Debug.LogWarning($"[VRMExpressionController] Preset not found: {presetName}");
                yield break;
            }

            if (duration <= 0)
            {
                ApplyPreset(presetName);
                yield break;
            }

            IsTransitioning = true;

            // 現在の表情値を記録
            var startWeights = new Dictionary<ExpressionKey, float>();
            foreach (var key in _expressionKeyCache.Values)
            {
                startWeights[key] = _expression.GetWeight(key);
            }

            // 目標値を計算
            var targetWeights = new Dictionary<ExpressionKey, float>();
            foreach (var key in _expressionKeyCache.Values)
            {
                targetWeights[key] = 0f;
            }
            foreach (var ew in preset.weights)
            {
                if (TryGetExpressionKey(ew.expressionName, out var key))
                {
                    targetWeights[key] = Mathf.Clamp01(ew.weight);
                }
            }

            // 遷移
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                foreach (var key in _expressionKeyCache.Values)
                {
                    float start = startWeights.ContainsKey(key) ? startWeights[key] : 0f;
                    float target = targetWeights.ContainsKey(key) ? targetWeights[key] : 0f;
                    _expression.SetWeight(key, Mathf.Lerp(start, target, t));
                }

                yield return null;
            }

            // 最終値を設定
            foreach (var key in _expressionKeyCache.Values)
            {
                float target = targetWeights.ContainsKey(key) ? targetWeights[key] : 0f;
                _expression.SetWeight(key, target);
            }

            IsTransitioning = false;

            if (debugMode)
            {
                Debug.Log($"[VRMExpressionController] {characterName}: Preset '{presetName}' transition complete");
            }
        }

        /// <summary>
        /// プリセットを適用（非コルーチン版、遷移付き）
        /// </summary>
        public void StartPresetTransition(string presetName, float duration)
        {
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
            }
            _transitionCoroutine = StartCoroutine(ApplyPresetWithTransition(presetName, duration));
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
