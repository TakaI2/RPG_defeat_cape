using System.Collections;
using UnityEngine;
using RPGDefete.Story;

namespace RPGDefete.Character
{
    /// <summary>
    /// VRMIKController のテスト用コンポーネント
    /// キーボード入力で各種IKテストを実行
    /// </summary>
    public class IKTester : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private VRMIKController ikController;
        [SerializeField] private VRMRigSetup rigSetup;
        [SerializeField] private StoryPlayer storyPlayer;

        [Header("テスト設定")]
        [SerializeField] private string characterName = "hero";

        [Header("IKターゲット")]
        [SerializeField] private Transform lookAtTarget;
        [SerializeField] private Transform handIKTarget;
        [SerializeField] private Transform footIKTarget;
        [SerializeField] private Transform hipIKTarget;

        [Header("キー設定")]
        [SerializeField] private KeyCode lookAtTestKey = KeyCode.Alpha4;
        [SerializeField] private KeyCode handIKTestKey = KeyCode.Alpha5;
        [SerializeField] private KeyCode footIKTestKey = KeyCode.Alpha6;
        [SerializeField] private KeyCode hipIKTestKey = KeyCode.Alpha7;
        [SerializeField] private KeyCode clearAllIKKey = KeyCode.Alpha0;
        [SerializeField] private KeyCode registerKey = KeyCode.F3;

        [Header("デバッグ")]
        [SerializeField] private bool showGUI = true;

        private bool lookAtEnabled = false;
        private bool handIKEnabled = false;
        private bool footIKEnabled = false;
        private bool hipIKEnabled = false;

        private void Start()
        {
            if (ikController == null)
            {
                ikController = GetComponent<VRMIKController>();
            }

            if (rigSetup == null)
            {
                rigSetup = GetComponent<VRMRigSetup>();
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

            if (ikController != null)
            {
                storyPlayer.RegisterIKController(characterName, ikController);
                Debug.Log($"[IKTester] Registered IK controller: {characterName}");
            }

            // IKターゲットを登録
            if (lookAtTarget != null)
            {
                storyPlayer.RegisterIKTarget("lookat_target", lookAtTarget);
                Debug.Log("[IKTester] Registered IK target: lookat_target");
            }

            if (handIKTarget != null)
            {
                storyPlayer.RegisterIKTarget("hand_target", handIKTarget);
                Debug.Log("[IKTester] Registered IK target: hand_target");
            }

            if (footIKTarget != null)
            {
                storyPlayer.RegisterIKTarget("foot_target", footIKTarget);
                Debug.Log("[IKTester] Registered IK target: foot_target");
            }

            if (hipIKTarget != null)
            {
                storyPlayer.RegisterIKTarget("hip_target", hipIKTarget);
                Debug.Log("[IKTester] Registered IK target: hip_target");
            }
        }

        private void Update()
        {
            // LookAt テスト
            if (Input.GetKeyDown(lookAtTestKey))
            {
                ToggleLookAt();
            }
            // Hand IK テスト
            else if (Input.GetKeyDown(handIKTestKey))
            {
                ToggleHandIK();
            }
            // Foot IK テスト
            else if (Input.GetKeyDown(footIKTestKey))
            {
                ToggleFootIK();
            }
            // Hip IK テスト
            else if (Input.GetKeyDown(hipIKTestKey))
            {
                ToggleHipIK();
            }
            // 全IK解除
            else if (Input.GetKeyDown(clearAllIKKey))
            {
                ClearAllIK();
            }
            // 再登録
            else if (Input.GetKeyDown(registerKey))
            {
                RegisterToStoryPlayer();
            }

            // LookAtターゲット追従
            if (lookAtEnabled && lookAtTarget != null && ikController != null)
            {
                ikController.UpdateLookAtTarget(lookAtTarget);
            }
        }

        private void ToggleLookAt()
        {
            Debug.Log($"[IKTester] ToggleLookAt called. ikController={ikController != null}, IsValid={ikController?.IsValid}, lookAtTarget={lookAtTarget != null}");

            if (ikController == null || !ikController.IsValid)
            {
                Debug.LogWarning("[IKTester] IK Controller not valid");
                return;
            }

            lookAtEnabled = !lookAtEnabled;

            if (lookAtEnabled && lookAtTarget != null)
            {
                Debug.Log($"[IKTester] Enabling LookAt to {lookAtTarget.name} at {lookAtTarget.position}");
                ikController.SetLookAtTarget(lookAtTarget);
                StartCoroutine(ikController.SetLookAtWeight(1f));
            }
            else
            {
                Debug.Log("[IKTester] Disabling LookAt");
                StartCoroutine(ikController.SetLookAtWeight(0f));
            }
        }

        private void ToggleHandIK()
        {
            if (ikController == null || !ikController.IsValid)
            {
                Debug.LogWarning("[IKTester] IK Controller not valid");
                return;
            }

            handIKEnabled = !handIKEnabled;

            if (handIKEnabled && handIKTarget != null)
            {
                Debug.Log("[IKTester] Enabling Hand IK (Right)");
                ikController.SetHandIKTarget(HandType.Right, handIKTarget);
                StartCoroutine(ikController.SetHandIKWeight(HandType.Right, 1f));
            }
            else
            {
                Debug.Log("[IKTester] Disabling Hand IK (Right)");
                StartCoroutine(ikController.SetHandIKWeight(HandType.Right, 0f));
            }
        }

        private void ToggleFootIK()
        {
            if (ikController == null || !ikController.IsValid)
            {
                Debug.LogWarning("[IKTester] IK Controller not valid");
                return;
            }

            footIKEnabled = !footIKEnabled;

            if (footIKEnabled && footIKTarget != null)
            {
                Debug.Log("[IKTester] Enabling Foot IK (Right)");
                ikController.SetFootIKTarget(FootType.Right, footIKTarget);
                StartCoroutine(ikController.SetFootIKWeight(FootType.Right, 1f));
            }
            else
            {
                Debug.Log("[IKTester] Disabling Foot IK (Right)");
                StartCoroutine(ikController.SetFootIKWeight(FootType.Right, 0f));
            }
        }

        private void ToggleHipIK()
        {
            Debug.Log($"[IKTester] ToggleHipIK called. hipIKTarget={hipIKTarget != null}");

            if (ikController == null || !ikController.IsValid)
            {
                Debug.LogWarning("[IKTester] IK Controller not valid");
                return;
            }

            hipIKEnabled = !hipIKEnabled;

            if (hipIKEnabled && hipIKTarget != null)
            {
                Debug.Log($"[IKTester] Enabling Hip IK to {hipIKTarget.name} at {hipIKTarget.position}");
                ikController.SetHipIKTarget(hipIKTarget);
                StartCoroutine(ikController.SetHipIKWeight(1f));
            }
            else
            {
                Debug.Log("[IKTester] Disabling Hip IK");
                StartCoroutine(ikController.SetHipIKWeight(0f));
            }
        }

        private void ClearAllIK()
        {
            if (ikController == null || !ikController.IsValid)
            {
                Debug.LogWarning("[IKTester] IK Controller not valid");
                return;
            }

            Debug.Log("[IKTester] Clearing all IK");
            StartCoroutine(ikController.DisableAllIK());

            lookAtEnabled = false;
            handIKEnabled = false;
            footIKEnabled = false;
            hipIKEnabled = false;
        }

        private void OnGUI()
        {
            if (!showGUI) return;

            GUILayout.BeginArea(new Rect(630, 10, 280, 350));
            GUILayout.BeginVertical("box");

            GUILayout.Label("IK Tester", GUI.skin.box);

            // IK Controller 状態
            GUILayout.Label("--- IK Controller ---");
            if (ikController == null)
            {
                GUILayout.Label("IKController: Not Set");
            }
            else if (!ikController.IsValid)
            {
                GUILayout.Label("IKController: Not Valid");
                GUILayout.Label("Run 'Setup Rig Structure'");
            }
            else
            {
                GUILayout.Label($"Character: {characterName}");
                GUILayout.Label($"LookAt Weight: {ikController.LookAtWeight:F2}");
            }

            GUILayout.Space(5);

            // IK状態
            GUILayout.Label("--- IK States ---");
            GUILayout.Label($"LookAt: {(lookAtEnabled ? "ON" : "OFF")}");
            GUILayout.Label($"Hand IK: {(handIKEnabled ? "ON" : "OFF")}");
            GUILayout.Label($"Foot IK: {(footIKEnabled ? "ON" : "OFF")}");
            GUILayout.Label($"Hip IK: {(hipIKEnabled ? "ON" : "OFF")}");

            GUILayout.Space(10);
            GUILayout.Label("Controls:");
            GUILayout.Label($"  {lookAtTestKey}: Toggle LookAt");
            GUILayout.Label($"  {handIKTestKey}: Toggle Hand IK");
            GUILayout.Label($"  {footIKTestKey}: Toggle Foot IK");
            GUILayout.Label($"  {hipIKTestKey}: Toggle Hip IK");
            GUILayout.Label($"  {clearAllIKKey}: Clear All IK");
            GUILayout.Label($"  {registerKey}: Register to StoryPlayer");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
