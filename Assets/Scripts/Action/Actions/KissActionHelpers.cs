using System;
using UnityEngine;
using RPGDefete.Character;

namespace RPG.Action
{
    /// <summary>
    /// キャラクターの姿勢
    /// </summary>
    public enum CharacterPosture
    {
        Standing,   // 立っている
        Sitting,    // 座っている
        Crouching,  // しゃがんでいる
        Kneeling,   // 膝立ち
        LyingDown,  // 寝ている（仰向け）
        LyingFace,  // 寝ている（うつ伏せ）
        LyingSide   // 寝ている（横向き）
    }

    /// <summary>
    /// 身長差カテゴリ
    /// </summary>
    public enum HeightDifferenceCategory
    {
        MuchTaller,   // 対象がずっと高い（+30cm以上）
        Taller,       // 対象が高い（+10〜30cm）
        Similar,      // ほぼ同じ（±10cm）
        Shorter,      // 対象が低い（-10〜30cm）
        MuchShorter   // 対象がずっと低い（-30cm以上）
    }

    /// <summary>
    /// キスアプローチタイプ
    /// </summary>
    public enum KissApproachType
    {
        FrontStanding,      // 正面から立って
        BendDown,           // かがんで
        KneelDown,          // 膝をついて
        LeanOver,           // 覆いかぶさって
        TipToe,             // つま先立ちで
        SitBeside,          // 横に座って
        FromBehind          // 後ろから
    }

    /// <summary>
    /// 姿勢検出器
    /// キャラクターの現在の姿勢を判定する
    /// </summary>
    public class PostureDetector : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform hipsTransform;
        [SerializeField] private Transform headTransform;

        [Header("判定閾値")]
        [SerializeField] private float lyingHeightThreshold = 0.5f;   // この高さ以下なら寝ている
        [SerializeField] private float sittingHeightRatio = 0.6f;     // 立位の60%以下なら座っている
        [SerializeField] private float kneelingHeightRatio = 0.75f;   // 立位の75%以下なら膝立ち

        private float _standingHipsHeight;  // 立っている時の腰の高さ
        private bool _initialized = false;

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// 初期化
        /// </summary>
        public void Initialize()
        {
            if (_initialized) return;

            // 自動検出
            if (animator == null)
                animator = GetComponent<Animator>();

            if (hipsTransform == null && animator != null)
                hipsTransform = animator.GetBoneTransform(HumanBodyBones.Hips);

            if (headTransform == null && animator != null)
                headTransform = animator.GetBoneTransform(HumanBodyBones.Head);

            // 立位時の腰の高さを記録（現在の高さをベースに）
            if (hipsTransform != null)
            {
                _standingHipsHeight = hipsTransform.position.y;
                if (_standingHipsHeight < 0.5f) // あまりに低い場合は推定値を使用
                    _standingHipsHeight = 1.0f;
            }

            _initialized = true;
        }

        /// <summary>
        /// 現在の姿勢を判定
        /// </summary>
        public CharacterPosture GetCurrentPosture()
        {
            if (!_initialized) Initialize();
            if (hipsTransform == null || headTransform == null)
                return CharacterPosture.Standing;

            float currentHipsHeight = hipsTransform.position.y;

            // 寝ている判定（腰が非常に低い）
            if (currentHipsHeight < lyingHeightThreshold)
            {
                return DetermineLyingDirection();
            }

            // 座っている判定
            if (currentHipsHeight < _standingHipsHeight * sittingHeightRatio)
            {
                return CharacterPosture.Sitting;
            }

            // 膝立ち判定
            if (currentHipsHeight < _standingHipsHeight * kneelingHeightRatio)
            {
                return CharacterPosture.Kneeling;
            }

            // しゃがんでいる判定（腰が中間）
            if (currentHipsHeight < _standingHipsHeight * 0.85f)
            {
                return CharacterPosture.Crouching;
            }

            return CharacterPosture.Standing;
        }

        /// <summary>
        /// 寝ている方向を判定
        /// </summary>
        private CharacterPosture DetermineLyingDirection()
        {
            if (headTransform == null || hipsTransform == null)
                return CharacterPosture.LyingDown;

            Vector3 headToHips = (hipsTransform.position - headTransform.position).normalized;
            float dotForward = Vector3.Dot(transform.forward, Vector3.up);

            if (Mathf.Abs(dotForward) > 0.7f)
            {
                // 顔が上または下を向いている
                return dotForward > 0 ? CharacterPosture.LyingFace : CharacterPosture.LyingDown;
            }

            return CharacterPosture.LyingSide;
        }

        /// <summary>
        /// mouth_pointのワールド位置を取得
        /// </summary>
        public Vector3 GetMouthPosition()
        {
            var gameChar = GetComponent<GameCharacter>();
            if (gameChar != null)
            {
                var mouthPoint = gameChar.GetInteractionPoint(InteractionPointType.Mouth);
                if (mouthPoint != null)
                    return mouthPoint.GetWorldPosition();
            }

            // フォールバック：頭の前方
            if (headTransform != null)
                return headTransform.position + headTransform.forward * 0.1f;

            return transform.position + Vector3.up * 1.5f + transform.forward * 0.1f;
        }
    }

    /// <summary>
    /// 身長差計算ユーティリティ
    /// </summary>
    public static class HeightDifferenceCalculator
    {
        /// <summary>
        /// 身長差を計算（プラス = 対象が高い、マイナス = 対象が低い）
        /// </summary>
        public static float CalculateHeightDifference(GameCharacter actor, GameCharacter target)
        {
            float actorMouthHeight = GetMouthHeight(actor);
            float targetMouthHeight = GetMouthHeight(target);

            return targetMouthHeight - actorMouthHeight;
        }

        /// <summary>
        /// 口の高さを取得（姿勢を考慮）
        /// </summary>
        private static float GetMouthHeight(GameCharacter character)
        {
            if (character == null) return 1.5f;

            var mouthPoint = character.GetInteractionPoint(InteractionPointType.Mouth);
            if (mouthPoint != null)
                return mouthPoint.GetWorldPosition().y;

            // フォールバック
            return character.transform.position.y + 1.5f;
        }

        /// <summary>
        /// 身長差カテゴリを判定
        /// </summary>
        public static HeightDifferenceCategory CategorizeHeightDifference(float difference)
        {
            if (difference > 0.3f) return HeightDifferenceCategory.MuchTaller;
            if (difference > 0.1f) return HeightDifferenceCategory.Taller;
            if (difference > -0.1f) return HeightDifferenceCategory.Similar;
            if (difference > -0.3f) return HeightDifferenceCategory.Shorter;
            return HeightDifferenceCategory.MuchShorter;
        }
    }

    /// <summary>
    /// キスアプローチ戦略
    /// </summary>
    public static class KissApproachStrategy
    {
        /// <summary>
        /// 最適なアプローチ方法を決定
        /// </summary>
        public static KissApproachType DetermineApproach(
            CharacterPosture targetPosture,
            HeightDifferenceCategory heightDiff)
        {
            // 対象が寝ている場合
            if (targetPosture == CharacterPosture.LyingDown ||
                targetPosture == CharacterPosture.LyingSide)
            {
                return KissApproachType.LeanOver;
            }

            if (targetPosture == CharacterPosture.LyingFace)
            {
                return KissApproachType.KneelDown; // うつ伏せなら横から
            }

            // 対象が座っている場合
            if (targetPosture == CharacterPosture.Sitting)
            {
                return heightDiff switch
                {
                    HeightDifferenceCategory.MuchShorter => KissApproachType.KneelDown,
                    _ => KissApproachType.BendDown
                };
            }

            // 対象が立っている場合
            return heightDiff switch
            {
                HeightDifferenceCategory.MuchTaller => KissApproachType.TipToe,
                HeightDifferenceCategory.MuchShorter => KissApproachType.BendDown,
                HeightDifferenceCategory.Shorter => KissApproachType.BendDown,
                _ => KissApproachType.FrontStanding
            };
        }

        /// <summary>
        /// アプローチ位置を計算
        /// </summary>
        public static Vector3 CalculateApproachPosition(
            GameCharacter target,
            KissApproachType approachType)
        {
            if (target == null) return Vector3.zero;

            Vector3 targetPos = target.transform.position;
            Vector3 targetForward = target.transform.forward;

            var postureDetector = target.GetComponent<PostureDetector>();
            CharacterPosture posture = postureDetector != null
                ? postureDetector.GetCurrentPosture()
                : CharacterPosture.Standing;

            switch (approachType)
            {
                case KissApproachType.FrontStanding:
                case KissApproachType.TipToe:
                    // 正面から適切な距離
                    return targetPos + targetForward * 0.4f;

                case KissApproachType.BendDown:
                    // やや近め
                    return targetPos + targetForward * 0.3f;

                case KissApproachType.KneelDown:
                    // 膝をつく位置
                    return targetPos + targetForward * 0.35f;

                case KissApproachType.LeanOver:
                    // 寝ている人の横
                    if (posture == CharacterPosture.LyingDown)
                    {
                        // 仰向け：頭の横
                        var mouthPoint = target.GetInteractionPoint(InteractionPointType.Mouth);
                        Vector3 headPos = mouthPoint != null
                            ? mouthPoint.GetWorldPosition()
                            : targetPos + Vector3.up * 0.3f;
                        return headPos + target.transform.right * 0.4f;
                    }
                    else
                    {
                        // 横向き：顔の正面
                        return targetPos + targetForward * 0.3f;
                    }

                case KissApproachType.SitBeside:
                    // 横に座る
                    return targetPos + target.transform.right * 0.5f;

                default:
                    return targetPos + targetForward * 0.4f;
            }
        }
    }
}
