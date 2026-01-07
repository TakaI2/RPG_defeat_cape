using UnityEngine;
using TMPro;

namespace RPG.Combat
{
    /// <summary>
    /// ダメージ数値表示UI
    /// ワールド空間に表示され、上に浮かびながらフェードアウト
    /// </summary>
    public class DamageNumber : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private TextMeshPro textMesh;
        [SerializeField] private TextMeshProUGUI textMeshUI; // Canvas用（オプション）

        [Header("アニメーション")]
        [SerializeField] private float floatSpeed = 1f;
        [SerializeField] private float floatHeight = 1f;
        [SerializeField] private AnimationCurve floatCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.Linear(0, 1, 1, 0);
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0.5f, 0.2f, 1f);

        [Header("クリティカル演出")]
        [SerializeField] private float criticalScale = 1.5f;
        [SerializeField] private Color criticalColor = new Color(1f, 0.8f, 0f); // 金色

        [Header("設定")]
        [SerializeField] private float lifetime = 1f;
        [SerializeField] private bool faceCamera = true;
        [SerializeField] private Vector3 randomOffset = new Vector3(0.3f, 0, 0.3f);

        private float _startTime;
        private Vector3 _startPosition;
        private Color _baseColor = Color.white;
        private bool _isCritical;
        private float _baseScale = 1f;

        private void Awake()
        {
            // TextMeshProコンポーネントの自動取得
            if (textMesh == null)
            {
                textMesh = GetComponent<TextMeshPro>();
            }
            if (textMesh == null && textMeshUI == null)
            {
                textMeshUI = GetComponent<TextMeshProUGUI>();
            }
        }

        /// <summary>
        /// 初期化
        /// </summary>
        public void Initialize(int damage, bool isCritical = false, Color? color = null)
        {
            _startTime = Time.time;
            _isCritical = isCritical;

            // ランダムオフセット追加
            Vector3 offset = new Vector3(
                Random.Range(-randomOffset.x, randomOffset.x),
                0,
                Random.Range(-randomOffset.z, randomOffset.z)
            );
            transform.position += offset;
            _startPosition = transform.position;

            // 色設定
            _baseColor = color ?? Color.white;
            if (isCritical)
            {
                _baseColor = criticalColor;
                _baseScale = criticalScale;
            }

            // テキスト設定
            string text = damage.ToString();
            if (isCritical)
            {
                text = $"<size=120%>CRIT!</size>\n{damage}";
            }

            SetText(text);
            SetColor(_baseColor);

            // 自動破棄
            Destroy(gameObject, lifetime);
        }

        /// <summary>
        /// シンプルな初期化（後方互換）
        /// </summary>
        public void SetValue(int damage)
        {
            Initialize(damage, false, Color.white);
        }

        private void Update()
        {
            float elapsed = Time.time - _startTime;
            float normalizedTime = Mathf.Clamp01(elapsed / lifetime);

            // 上に浮かぶ
            float floatOffset = floatCurve.Evaluate(normalizedTime) * floatHeight;
            transform.position = _startPosition + Vector3.up * floatOffset;

            // スケール
            float scale = scaleCurve.Evaluate(normalizedTime) * _baseScale;
            transform.localScale = Vector3.one * scale;

            // フェードアウト
            float alpha = fadeCurve.Evaluate(normalizedTime);
            Color color = _baseColor;
            color.a = alpha;
            SetColor(color);

            // カメラに向ける
            if (faceCamera && UnityEngine.Camera.main != null)
            {
                transform.LookAt(UnityEngine.Camera.main.transform);
                transform.Rotate(0, 180, 0);
            }
        }

        private void SetText(string text)
        {
            if (textMesh != null)
            {
                textMesh.text = text;
            }
            if (textMeshUI != null)
            {
                textMeshUI.text = text;
            }
        }

        private void SetColor(Color color)
        {
            if (textMesh != null)
            {
                textMesh.color = color;
            }
            if (textMeshUI != null)
            {
                textMeshUI.color = color;
            }
        }
    }
}
