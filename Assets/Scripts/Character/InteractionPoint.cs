using UnityEngine;

namespace RPGDefete.Character
{
    /// <summary>
    /// インタラクションポイントタイプ
    /// </summary>
    public enum InteractionPointType
    {
        // キャラクター用
        Eye,            // 目
        Mouth,          // 口
        Head,           // 頭
        Shoulder,       // 肩
        Chest,          // 胸
        Hip,            // 腰
        Hand,           // 手
        Foot,           // 足

        // オブジェクト用
        Grab,           // 掴む位置
        Sit,            // 座る位置
        Touch,          // 触る位置
        Stomp,          // 踏む位置
        Look,           // 注目位置

        // 戦闘用
        Target,         // 攻撃目標
        WeakPoint,      // 弱点
        Drag            // 引きずり位置
    }

    /// <summary>
    /// インタラクションポイント
    /// キャラクターやオブジェクトの特定位置を示すマーカー
    /// </summary>
    public class InteractionPoint : MonoBehaviour
    {
        [Header("ポイント設定")]
        [SerializeField] private InteractionPointType pointType = InteractionPointType.Look;
        [SerializeField] private string pointName;
        [SerializeField] private Vector3 localOffset;

        [Header("表示設定")]
        [SerializeField] private bool showGizmo = true;
        [SerializeField] private Color gizmoColor = Color.cyan;
        [SerializeField] private float gizmoSize = 0.05f;

        // プロパティ
        public InteractionPointType PointType => pointType;
        public string PointName => string.IsNullOrEmpty(pointName) ? gameObject.name : pointName;

        /// <summary>
        /// ワールド座標を取得
        /// </summary>
        public Vector3 GetWorldPosition()
        {
            return transform.TransformPoint(localOffset);
        }

        /// <summary>
        /// ワールド座標を設定（localOffsetを更新）
        /// </summary>
        public void SetWorldPosition(Vector3 worldPos)
        {
            localOffset = transform.InverseTransformPoint(worldPos);
        }

        /// <summary>
        /// 方向を取得（forward）
        /// </summary>
        public Vector3 GetDirection()
        {
            return transform.forward;
        }

        /// <summary>
        /// 回転を取得
        /// </summary>
        public Quaternion GetRotation()
        {
            return transform.rotation;
        }

        /// <summary>
        /// 距離を計算
        /// </summary>
        public float DistanceTo(Vector3 position)
        {
            return Vector3.Distance(GetWorldPosition(), position);
        }

        /// <summary>
        /// 距離を計算（他のInteractionPoint）
        /// </summary>
        public float DistanceTo(InteractionPoint other)
        {
            return Vector3.Distance(GetWorldPosition(), other.GetWorldPosition());
        }

        private void OnDrawGizmos()
        {
            if (!showGizmo) return;

            Gizmos.color = gizmoColor;
            Vector3 pos = GetWorldPosition();
            Gizmos.DrawWireSphere(pos, gizmoSize);

            // 方向を示す線
            Gizmos.DrawLine(pos, pos + transform.forward * gizmoSize * 2);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 pos = GetWorldPosition();
            Gizmos.DrawSphere(pos, gizmoSize);

            // ラベル表示用（Handlesを使う場合はEditorスクリプトで）
        }

#if UNITY_EDITOR
        /// <summary>
        /// エディタでタイプに応じた色を設定
        /// </summary>
        private void OnValidate()
        {
            gizmoColor = pointType switch
            {
                InteractionPointType.Eye => Color.blue,
                InteractionPointType.Mouth => Color.red,
                InteractionPointType.Head => Color.magenta,
                InteractionPointType.Hand => Color.green,
                InteractionPointType.Foot => Color.yellow,
                InteractionPointType.Target => new Color(1f, 0.5f, 0f),
                InteractionPointType.WeakPoint => Color.red,
                _ => Color.cyan
            };
        }
#endif
    }
}
