using System;
using System.Collections.Generic;
using UnityEngine;
using RPGDefete.Character;

namespace RPG.Interaction
{
    /// <summary>
    /// インタラクタブル属性フラグ
    /// </summary>
    [Flags]
    public enum InteractableAttribute
    {
        None = 0,
        Grabbable = 1 << 0,    // 掴める
        Eatable = 1 << 1,      // 食べられる
        Sittable = 1 << 2,     // 座れる
        Touchable = 1 << 3,    // 触れる
        Stompable = 1 << 4,    // 踏める
        Talkable = 1 << 5,     // 話せる
        Kissable = 1 << 6,     // キスできる
        Huggable = 1 << 7,     // 抱きしめられる
        Pushable = 1 << 8,     // 押せる
        Pullable = 1 << 9      // 引ける
    }

    /// <summary>
    /// サイズカテゴリ
    /// </summary>
    public enum SizeCategory
    {
        Tiny,      // < 0.1m  - 摘む
        Small,     // 0.1-0.3m - 片手
        Medium,    // 0.3-0.8m - 両手
        Large      // > 0.8m  - 特殊
    }

    /// <summary>
    /// インタラクション可能なオブジェクト
    /// 属性、サイズ、インタラクションポイントを管理
    /// </summary>
    public class InteractableObject : MonoBehaviour
    {
        [Header("基本設定")]
        [SerializeField] private string displayName;
        [SerializeField] private InteractableAttribute attributes;
        [SerializeField] private bool isEnabled = true;

        [Header("サイズ")]
        [SerializeField] private SizeCategory sizeCategory = SizeCategory.Small;
        [SerializeField] private bool autoDetectSize = true;

        [Header("インタラクションポイント")]
        [SerializeField] private List<InteractionPoint> interactionPoints = new List<InteractionPoint>();

        [Header("状態")]
        [SerializeField] private bool isBeingHeld = false;

        [Header("ビジュアルフィードバック")]
        [SerializeField] private bool enableHighlight = true;
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 0f, 0.3f);

        // 状態
        private GameCharacter _heldBy;
        private bool _isHovered;
        private Renderer[] _renderers;
        private Dictionary<Renderer, Material[]> _originalMaterials;

        // プロパティ
        public string DisplayName => string.IsNullOrEmpty(displayName) ? gameObject.name : displayName;
        public InteractableAttribute Attributes => attributes;
        public SizeCategory Size => sizeCategory;
        public bool IsEnabled => isEnabled;
        public bool IsBeingHeld => isBeingHeld;
        public GameCharacter HeldBy => _heldBy;
        public IReadOnlyList<InteractionPoint> InteractionPoints => interactionPoints;

        // イベント
        public event Action<GameCharacter> OnGrabbed;
        public event Action<GameCharacter> OnReleased;
        public event Action<GameCharacter> OnInteracted;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _originalMaterials = new Dictionary<Renderer, Material[]>();

            foreach (var r in _renderers)
            {
                _originalMaterials[r] = r.materials;
            }
        }

        private void Start()
        {
            if (autoDetectSize)
            {
                DetectSize();
            }
            CollectInteractionPoints();
        }

        /// <summary>
        /// Boundsからサイズカテゴリを自動判定
        /// </summary>
        private void DetectSize()
        {
            if (_renderers == null || _renderers.Length == 0) return;

            Bounds bounds = _renderers[0].bounds;
            foreach (var r in _renderers)
            {
                bounds.Encapsulate(r.bounds);
            }

            float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

            sizeCategory = maxDimension switch
            {
                < 0.1f => SizeCategory.Tiny,
                < 0.3f => SizeCategory.Small,
                < 0.8f => SizeCategory.Medium,
                _ => SizeCategory.Large
            };
        }

        /// <summary>
        /// 子要素のInteractionPointを収集
        /// </summary>
        private void CollectInteractionPoints()
        {
            var points = GetComponentsInChildren<InteractionPoint>();
            interactionPoints = new List<InteractionPoint>(points);
        }

        /// <summary>
        /// 指定タイプのポイントを取得
        /// </summary>
        public InteractionPoint GetPoint(InteractionPointType type)
        {
            return interactionPoints.Find(p => p.PointType == type);
        }

        /// <summary>
        /// 指定属性を持っているか
        /// </summary>
        public bool HasAttribute(InteractableAttribute attr)
        {
            return (attributes & attr) != 0;
        }

        /// <summary>
        /// 掴まれる
        /// </summary>
        public void OnGrab(GameCharacter grabber)
        {
            if (!HasAttribute(InteractableAttribute.Grabbable)) return;

            isBeingHeld = true;
            _heldBy = grabber;
            OnGrabbed?.Invoke(grabber);

            // Rigidbodyがあればキネマティックに
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }

        /// <summary>
        /// 離される
        /// </summary>
        public void OnRelease(GameCharacter releaser)
        {
            isBeingHeld = false;
            _heldBy = null;
            OnReleased?.Invoke(releaser);

            // Rigidbodyを元に戻す
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }
        }

        /// <summary>
        /// インタラクト実行
        /// </summary>
        public void Interact(GameCharacter interactor)
        {
            if (!isEnabled) return;
            OnInteracted?.Invoke(interactor);
        }

        /// <summary>
        /// ホバー開始
        /// </summary>
        public void OnHoverEnter()
        {
            if (!enableHighlight) return;

            _isHovered = true;
            SetHighlight(true);
        }

        /// <summary>
        /// ホバー終了
        /// </summary>
        public void OnHoverExit()
        {
            _isHovered = false;
            SetHighlight(false);
        }

        /// <summary>
        /// ハイライト設定
        /// </summary>
        private void SetHighlight(bool enabled)
        {
            // シンプルなアウトライン/色変更
            // 実際のプロジェクトではシェーダーを使用
            foreach (var r in _renderers)
            {
                if (r == null) continue;

                foreach (var mat in r.materials)
                {
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.SetColor("_EmissionColor", enabled ? highlightColor : Color.black);
                        mat.EnableKeyword("_EMISSION");
                    }
                }
            }
        }

        /// <summary>
        /// 有効/無効を設定
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
        }

        private void OnDrawGizmosSelected()
        {
            // サイズ表示
            Gizmos.color = sizeCategory switch
            {
                SizeCategory.Tiny => Color.cyan,
                SizeCategory.Small => Color.green,
                SizeCategory.Medium => Color.yellow,
                SizeCategory.Large => Color.red,
                _ => Color.white
            };

            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (var r in renderers)
                {
                    bounds.Encapsulate(r.bounds);
                }
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }
    }
}
