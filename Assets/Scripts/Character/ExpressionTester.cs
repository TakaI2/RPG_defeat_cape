using System.Collections;
using UnityEngine;
using RPGDefete.Story;

namespace RPGDefete.Character
{
    /// <summary>
    /// VRMExpressionControllerのテスト用コンポーネント
    /// キーボード入力で各種表情テストを実行
    /// </summary>
    public class ExpressionTester : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private VRMExpressionController expressionController;
        [SerializeField] private StoryPlayer storyPlayer;

        [Header("テスト設定")]
        [SerializeField] private string characterName = "hero";
        [SerializeField] private float transitionDuration = 0.3f;

        [Header("キー設定")]
        [SerializeField] private KeyCode happyKey = KeyCode.Alpha1;
        [SerializeField] private KeyCode angryKey = KeyCode.Alpha2;
        [SerializeField] private KeyCode sadKey = KeyCode.Alpha3;
        [SerializeField] private KeyCode surprisedKey = KeyCode.Alpha4;
        [SerializeField] private KeyCode relaxedKey = KeyCode.Alpha5;
        [SerializeField] private KeyCode neutralKey = KeyCode.Alpha6;
        [SerializeField] private KeyCode resetKey = KeyCode.R;
        [SerializeField] private KeyCode blinkKey = KeyCode.B;
        [SerializeField] private KeyCode transitionTestKey = KeyCode.T;
        [SerializeField] private KeyCode registerKey = KeyCode.F1;

        [Header("デバッグ")]
        [SerializeField] private bool showGUI = true;

        private void Start()
        {
            if (expressionController == null)
            {
                expressionController = GetComponent<VRMExpressionController>();
            }

            if (storyPlayer == null)
            {
                storyPlayer = FindObjectOfType<StoryPlayer>();
            }

            // 自動登録
            if (storyPlayer != null && expressionController != null)
            {
                storyPlayer.RegisterCharacter(characterName, expressionController);
                Debug.Log($"[ExpressionTester] Registered character: {characterName}");
            }
        }

        private void Update()
        {
            if (expressionController == null || !expressionController.IsValid) return;

            // 表情テスト
            if (Input.GetKeyDown(happyKey))
            {
                TestExpression("happy");
            }
            else if (Input.GetKeyDown(angryKey))
            {
                TestExpression("angry");
            }
            else if (Input.GetKeyDown(sadKey))
            {
                TestExpression("sad");
            }
            else if (Input.GetKeyDown(surprisedKey))
            {
                TestExpression("surprised");
            }
            else if (Input.GetKeyDown(relaxedKey))
            {
                TestExpression("relaxed");
            }
            else if (Input.GetKeyDown(neutralKey))
            {
                TestExpression("neutral");
            }
            else if (Input.GetKeyDown(resetKey))
            {
                TestReset();
            }
            else if (Input.GetKeyDown(blinkKey))
            {
                StartCoroutine(TestBlink());
            }
            else if (Input.GetKeyDown(transitionTestKey))
            {
                StartCoroutine(TestTransition());
            }
            else if (Input.GetKeyDown(registerKey))
            {
                TestRegister();
            }
        }

        private void TestExpression(string expressionName)
        {
            Debug.Log($"[ExpressionTester] Testing expression: {expressionName}");
            expressionController.SetExpression(expressionName, 1f);
        }

        private void TestReset()
        {
            Debug.Log("[ExpressionTester] Testing reset");
            expressionController.ResetExpression();
        }

        private IEnumerator TestBlink()
        {
            Debug.Log("[ExpressionTester] Testing blink");
            expressionController.SetExpression("blink", 1f);
            yield return new WaitForSeconds(0.15f);
            expressionController.SetExpression("blink", 0f);
        }

        private IEnumerator TestTransition()
        {
            Debug.Log("[ExpressionTester] Testing transition: neutral -> happy -> angry -> neutral");

            // neutral -> happy
            yield return expressionController.SetExpressionWithTransition("happy", 1f, transitionDuration);
            yield return new WaitForSeconds(0.5f);

            // happy -> angry
            expressionController.SetExpression("happy", 0f);
            yield return expressionController.SetExpressionWithTransition("angry", 1f, transitionDuration);
            yield return new WaitForSeconds(0.5f);

            // angry -> neutral
            yield return expressionController.ResetExpressionWithTransition(transitionDuration);

            Debug.Log("[ExpressionTester] Transition test complete");
        }

        private void TestRegister()
        {
            if (storyPlayer == null)
            {
                Debug.LogWarning("[ExpressionTester] StoryPlayer not found");
                return;
            }

            storyPlayer.RegisterCharacter(characterName, expressionController);
            Debug.Log($"[ExpressionTester] Registered character: {characterName}");

            // 登録確認
            var context = storyPlayer.GetContext();
            if (context != null && context.TryGetCharacter(characterName, out var controller))
            {
                Debug.Log($"[ExpressionTester] Character '{characterName}' found in context: {controller != null}");
            }
        }

        private void OnGUI()
        {
            if (!showGUI) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.BeginVertical("box");

            GUILayout.Label("Expression Tester", GUI.skin.box);

            if (expressionController == null)
            {
                GUILayout.Label("ExpressionController: Not Set", GUI.skin.box);
            }
            else if (!expressionController.IsValid)
            {
                GUILayout.Label("ExpressionController: Not Valid (waiting for VRM)", GUI.skin.box);
            }
            else
            {
                GUILayout.Label($"Character: {characterName}");
                GUILayout.Label($"IsTransitioning: {expressionController.IsTransitioning}");

                GUILayout.Space(10);
                GUILayout.Label("Current Weights:");

                // 主要な表情のWeightを表示
                string[] expressions = { "happy", "angry", "sad", "surprised", "relaxed", "neutral", "blink" };
                foreach (var expr in expressions)
                {
                    float weight = expressionController.GetExpressionWeight(expr);
                    if (weight > 0.01f)
                    {
                        GUILayout.Label($"  {expr}: {weight:F2}");
                    }
                }
            }

            GUILayout.Space(10);
            GUILayout.Label("Controls:");
            GUILayout.Label($"  {happyKey}: Happy");
            GUILayout.Label($"  {angryKey}: Angry");
            GUILayout.Label($"  {sadKey}: Sad");
            GUILayout.Label($"  {surprisedKey}: Surprised");
            GUILayout.Label($"  {relaxedKey}: Relaxed");
            GUILayout.Label($"  {neutralKey}: Neutral");
            GUILayout.Label($"  {resetKey}: Reset");
            GUILayout.Label($"  {blinkKey}: Blink");
            GUILayout.Label($"  {transitionTestKey}: Transition Test");
            GUILayout.Label($"  {registerKey}: Register Character");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
