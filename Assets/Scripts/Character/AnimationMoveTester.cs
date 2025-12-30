using System.Collections;
using UnityEngine;
using RPGDefete.Story;

namespace RPGDefete.Character
{
    /// <summary>
    /// VRMAnimationController と CharacterNavigator のテスト用コンポーネント
    /// キーボード入力で各種アニメーション・移動テストを実行
    /// </summary>
    public class AnimationMoveTester : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private VRMAnimationController animationController;
        [SerializeField] private CharacterNavigator navigator;
        [SerializeField] private StoryPlayer storyPlayer;

        [Header("テスト設定")]
        [SerializeField] private string characterName = "hero";

        [Header("移動テスト用ポイント")]
        [SerializeField] private Transform[] testMovePoints;

        [Header("キー設定")]
        [SerializeField] private KeyCode triggerTestKey = KeyCode.Alpha1;
        [SerializeField] private KeyCode crossFadeTestKey = KeyCode.Alpha2;
        [SerializeField] private KeyCode moveTestKey = KeyCode.Alpha3;
        [SerializeField] private KeyCode stopKey = KeyCode.S;
        [SerializeField] private KeyCode registerKey = KeyCode.F2;

        [Header("デバッグ")]
        [SerializeField] private bool showGUI = true;

        private int currentMovePointIndex = 0;

        private void Start()
        {
            if (animationController == null)
            {
                animationController = GetComponent<VRMAnimationController>();
            }

            if (navigator == null)
            {
                navigator = GetComponent<CharacterNavigator>();
            }

            if (storyPlayer == null)
            {
                storyPlayer = FindObjectOfType<StoryPlayer>();
            }

            // 自動登録
            RegisterToStoryPlayer();
        }

        private void RegisterToStoryPlayer()
        {
            if (storyPlayer == null) return;

            if (animationController != null)
            {
                storyPlayer.RegisterAnimationController(characterName, animationController);
                Debug.Log($"[AnimationMoveTester] Registered animation controller: {characterName}");
            }

            if (navigator != null)
            {
                storyPlayer.RegisterNavigator(characterName, navigator);
                Debug.Log($"[AnimationMoveTester] Registered navigator: {characterName}");
            }

            // 移動ポイントを登録
            for (int i = 0; i < testMovePoints.Length; i++)
            {
                if (testMovePoints[i] != null)
                {
                    string pointName = $"test_point_{i}";
                    storyPlayer.RegisterMovePoint(pointName, testMovePoints[i]);
                    Debug.Log($"[AnimationMoveTester] Registered move point: {pointName}");
                }
            }
        }

        private void Update()
        {
            // Trigger テスト
            if (Input.GetKeyDown(triggerTestKey))
            {
                TestTriggerAnimation();
            }
            // CrossFade テスト
            else if (Input.GetKeyDown(crossFadeTestKey))
            {
                StartCoroutine(TestCrossFadeAnimation());
            }
            // 移動テスト
            else if (Input.GetKeyDown(moveTestKey))
            {
                TestMove();
            }
            // 停止
            else if (Input.GetKeyDown(stopKey))
            {
                TestStop();
            }
            // 再登録
            else if (Input.GetKeyDown(registerKey))
            {
                RegisterToStoryPlayer();
            }
        }

        private void TestTriggerAnimation()
        {
            if (animationController == null || !animationController.IsValid)
            {
                Debug.LogWarning("[AnimationMoveTester] AnimationController not valid");
                return;
            }

            // CombatAnimator用: Attack トリガー
            Debug.Log("[AnimationMoveTester] Testing trigger animation: Attack");
            animationController.PlayAnimation("Attack");
        }

        private IEnumerator TestCrossFadeAnimation()
        {
            if (animationController == null || !animationController.IsValid)
            {
                Debug.LogWarning("[AnimationMoveTester] AnimationController not valid");
                yield break;
            }

            // CombatAnimator用: Speed パラメータで Walk/Idle 切替
            Debug.Log("[AnimationMoveTester] Testing Speed parameter: Idle -> Walk -> Idle");

            // Walk (Speed = 0.3)
            animationController.SetFloat("Speed", 0.3f);
            yield return new WaitForSeconds(2f);

            // Idle (Speed = 0)
            animationController.SetFloat("Speed", 0f);
            Debug.Log("[AnimationMoveTester] Speed test complete");
        }

        private void TestMove()
        {
            if (navigator == null || !navigator.IsValid)
            {
                Debug.LogWarning("[AnimationMoveTester] Navigator not valid");
                return;
            }

            if (testMovePoints == null || testMovePoints.Length == 0)
            {
                Debug.LogWarning("[AnimationMoveTester] No move points configured");
                return;
            }

            // 次のポイントへ移動
            var targetPoint = testMovePoints[currentMovePointIndex];
            if (targetPoint != null)
            {
                Debug.Log($"[AnimationMoveTester] Moving to point {currentMovePointIndex}: {targetPoint.name}");
                navigator.MoveTo(targetPoint.position);
            }

            currentMovePointIndex = (currentMovePointIndex + 1) % testMovePoints.Length;
        }

        private void TestStop()
        {
            if (navigator != null)
            {
                Debug.Log("[AnimationMoveTester] Stopping movement");
                navigator.Stop();
            }
        }

        private void OnGUI()
        {
            if (!showGUI) return;

            GUILayout.BeginArea(new Rect(320, 10, 300, 350));
            GUILayout.BeginVertical("box");

            GUILayout.Label("Animation/Move Tester", GUI.skin.box);

            // Animation Controller 状態
            GUILayout.Label("--- Animation ---");
            if (animationController == null)
            {
                GUILayout.Label("AnimationController: Not Set");
            }
            else if (!animationController.IsValid)
            {
                GUILayout.Label("AnimationController: Not Valid");
            }
            else
            {
                GUILayout.Label($"Character: {characterName}");
                GUILayout.Label($"IsTransitioning: {animationController.IsTransitioning}");
            }

            GUILayout.Space(5);

            // Navigator 状態
            GUILayout.Label("--- Navigator ---");
            if (navigator == null)
            {
                GUILayout.Label("Navigator: Not Set");
            }
            else if (!navigator.IsValid)
            {
                GUILayout.Label("Navigator: Not Valid (not on NavMesh?)");
            }
            else
            {
                GUILayout.Label($"IsMoving: {navigator.IsMoving}");
                GUILayout.Label($"Position: {navigator.GetPosition():F1}");
            }

            GUILayout.Space(10);
            GUILayout.Label("Controls (CombatAnimator):");
            GUILayout.Label($"  {triggerTestKey}: Attack Trigger");
            GUILayout.Label($"  {crossFadeTestKey}: Walk/Idle (Speed)");
            GUILayout.Label($"  {moveTestKey}: Move to Next Point");
            GUILayout.Label($"  {stopKey}: Stop Movement");
            GUILayout.Label($"  {registerKey}: Register to StoryPlayer");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
