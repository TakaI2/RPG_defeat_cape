using System.Collections;
using UnityEngine;
using RPGDefete.Story;

namespace RPGDefete.Character
{
    /// <summary>
    /// VRMFinalIKController / VRMIKController のテスト用コンポーネント
    /// キーボード入力で各種IKテストを実行
    /// FinalIKが優先されます
    /// </summary>
    public class IKTester : MonoBehaviour
    {
        [Header("参照 - FinalIK (優先)")]
        [SerializeField] private VRMFinalIKController finalIKController;

        [Header("参照 - Animation Rigging (フォールバック)")]
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

        /// <summary>FinalIKを使用するか</summary>
        private bool UseFinalIK => finalIKController != null && finalIKController.IsValid;

        /// <summary>Animation Riggingを使用するか</summary>
        private bool UseAnimationRigging => !UseFinalIK && ikController != null && ikController.IsValid;

        /// <summary>いずれかのIKシステムが有効か</summary>
        private bool IsAnyIKValid => UseFinalIK || UseAnimationRigging;

        /// <summary>現在のLookAt Weight</summary>
        private float CurrentLookAtWeight
        {
            get
            {
                if (UseFinalIK) return finalIKController.LookAtWeight;
                if (UseAnimationRigging) return ikController.LookAtWeight;
                return 0f;
            }
        }

        private void Start()
        {
            // FinalIKを優先して検索
            if (finalIKController == null)
            {
                finalIKController = GetComponent<VRMFinalIKController>();
            }

            // フォールバックでAnimation Rigging
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

            // 使用するIKシステムをログ出力
            if (UseFinalIK)
            {
                Debug.Log("[IKTester] Using FinalIK");
            }
            else if (UseAnimationRigging)
            {
                Debug.Log("[IKTester] Using Animation Rigging");
            }
            else
            {
                Debug.LogWarning("[IKTester] No valid IK system found");
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
                Debug.Log($"[IKTester] Registered Animation Rigging IK controller: {characterName}");
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
            if (lookAtEnabled && lookAtTarget != null)
            {
                if (UseFinalIK)
                {
                    finalIKController.UpdateLookAtTarget(lookAtTarget);
                }
                else if (UseAnimationRigging)
                {
                    ikController.UpdateLookAtTarget(lookAtTarget);
                }
            }

            // Hand IKターゲット追従
            if (handIKEnabled && handIKTarget != null && UseFinalIK)
            {
                finalIKController.UpdateHandIKTarget(HandType.Right, handIKTarget);
            }
        }

        private void ToggleLookAt()
        {
            if (!IsAnyIKValid)
            {
                Debug.LogWarning("[IKTester] No valid IK system");
                return;
            }

            lookAtEnabled = !lookAtEnabled;

            if (UseFinalIK)
            {
                if (lookAtEnabled && lookAtTarget != null)
                {
                    Debug.Log($"[IKTester] FinalIK: Enabling LookAt to {lookAtTarget.name}");
                    finalIKController.SetLookAtTarget(lookAtTarget);
                    StartCoroutine(finalIKController.SetLookAtWeight(1f));
                }
                else
                {
                    Debug.Log("[IKTester] FinalIK: Disabling LookAt");
                    StartCoroutine(finalIKController.SetLookAtWeight(0f));
                }
            }
            else if (UseAnimationRigging)
            {
                if (lookAtEnabled && lookAtTarget != null)
                {
                    Debug.Log($"[IKTester] AnimRig: Enabling LookAt to {lookAtTarget.name}");
                    ikController.SetLookAtTarget(lookAtTarget);
                    StartCoroutine(ikController.SetLookAtWeight(1f));
                }
                else
                {
                    Debug.Log("[IKTester] AnimRig: Disabling LookAt");
                    StartCoroutine(ikController.SetLookAtWeight(0f));
                }
            }
        }

        private void ToggleHandIK()
        {
            if (!IsAnyIKValid)
            {
                Debug.LogWarning("[IKTester] No valid IK system");
                return;
            }

            handIKEnabled = !handIKEnabled;

            if (UseFinalIK)
            {
                if (handIKEnabled && handIKTarget != null)
                {
                    Debug.Log("[IKTester] FinalIK: Enabling Right Hand IK");
                    finalIKController.SetHandIKTarget(HandType.Right, handIKTarget);
                    StartCoroutine(finalIKController.SetHandIKWeight(HandType.Right, 1f));
                }
                else
                {
                    Debug.Log("[IKTester] FinalIK: Disabling Right Hand IK");
                    StartCoroutine(finalIKController.SetHandIKWeight(HandType.Right, 0f));
                }
            }
            else if (UseAnimationRigging)
            {
                if (handIKEnabled && handIKTarget != null)
                {
                    Debug.Log("[IKTester] AnimRig: Enabling Right Hand IK");
                    ikController.SetHandIKTarget(HandType.Right, handIKTarget);
                    StartCoroutine(ikController.SetHandIKWeight(HandType.Right, 1f));
                }
                else
                {
                    Debug.Log("[IKTester] AnimRig: Disabling Right Hand IK");
                    StartCoroutine(ikController.SetHandIKWeight(HandType.Right, 0f));
                }
            }
        }

        private void ToggleFootIK()
        {
            if (!IsAnyIKValid)
            {
                Debug.LogWarning("[IKTester] No valid IK system");
                return;
            }

            footIKEnabled = !footIKEnabled;

            if (UseFinalIK)
            {
                if (footIKEnabled && footIKTarget != null)
                {
                    Debug.Log("[IKTester] FinalIK: Enabling Right Foot IK");
                    finalIKController.SetFootIKTarget(FootType.Right, footIKTarget);
                    StartCoroutine(finalIKController.SetFootIKWeight(FootType.Right, 1f));
                }
                else
                {
                    Debug.Log("[IKTester] FinalIK: Disabling Right Foot IK");
                    StartCoroutine(finalIKController.SetFootIKWeight(FootType.Right, 0f));
                }
            }
            else if (UseAnimationRigging)
            {
                if (footIKEnabled && footIKTarget != null)
                {
                    Debug.Log("[IKTester] AnimRig: Enabling Right Foot IK");
                    ikController.SetFootIKTarget(FootType.Right, footIKTarget);
                    StartCoroutine(ikController.SetFootIKWeight(FootType.Right, 1f));
                }
                else
                {
                    Debug.Log("[IKTester] AnimRig: Disabling Right Foot IK");
                    StartCoroutine(ikController.SetFootIKWeight(FootType.Right, 0f));
                }
            }
        }

        private void ToggleHipIK()
        {
            if (!IsAnyIKValid)
            {
                Debug.LogWarning("[IKTester] No valid IK system");
                return;
            }

            hipIKEnabled = !hipIKEnabled;

            if (UseFinalIK)
            {
                if (hipIKEnabled && hipIKTarget != null)
                {
                    Debug.Log($"[IKTester] FinalIK: Enabling Body/Hip IK to {hipIKTarget.name}");
                    finalIKController.SetHipIKTarget(hipIKTarget);
                    StartCoroutine(finalIKController.SetHipIKWeight(1f));
                }
                else
                {
                    Debug.Log("[IKTester] FinalIK: Disabling Body/Hip IK");
                    StartCoroutine(finalIKController.SetHipIKWeight(0f));
                }
            }
            else if (UseAnimationRigging)
            {
                if (hipIKEnabled && hipIKTarget != null)
                {
                    Debug.Log($"[IKTester] AnimRig: Enabling Hip IK to {hipIKTarget.name}");
                    ikController.SetHipIKTarget(hipIKTarget);
                    StartCoroutine(ikController.SetHipIKWeight(1f));
                }
                else
                {
                    Debug.Log("[IKTester] AnimRig: Disabling Hip IK");
                    StartCoroutine(ikController.SetHipIKWeight(0f));
                }
            }
        }

        private void ClearAllIK()
        {
            if (!IsAnyIKValid)
            {
                Debug.LogWarning("[IKTester] No valid IK system");
                return;
            }

            Debug.Log("[IKTester] Clearing all IK");

            if (UseFinalIK)
            {
                StartCoroutine(finalIKController.DisableAllIK());
            }
            else if (UseAnimationRigging)
            {
                StartCoroutine(ikController.DisableAllIK());
            }

            lookAtEnabled = false;
            handIKEnabled = false;
            footIKEnabled = false;
            hipIKEnabled = false;
        }

        private void OnGUI()
        {
            if (!showGUI) return;

            GUILayout.BeginArea(new Rect(630, 10, 280, 380));
            GUILayout.BeginVertical("box");

            GUILayout.Label("IK Tester", GUI.skin.box);

            // IK System 状態
            GUILayout.Label("--- IK System ---");
            if (UseFinalIK)
            {
                GUILayout.Label("System: FinalIK");
                GUILayout.Label($"FBBIK: {(finalIKController.IsValid ? "Valid" : "Invalid")}");
                GUILayout.Label($"LookAt: {(finalIKController.HasLookAt ? "Valid" : "Not Set")}");
            }
            else if (UseAnimationRigging)
            {
                GUILayout.Label("System: Animation Rigging");
                GUILayout.Label($"Character: {characterName}");
            }
            else
            {
                GUILayout.Label("System: None");
                GUILayout.Label("Add FinalIK or AnimRig components");
            }

            GUILayout.Label($"LookAt Weight: {CurrentLookAtWeight:F2}");

            GUILayout.Space(5);

            // IK状態
            GUILayout.Label("--- IK States ---");
            GUILayout.Label($"LookAt: {(lookAtEnabled ? "ON" : "OFF")}");
            GUILayout.Label($"Hand IK: {(handIKEnabled ? "ON" : "OFF")}");
            GUILayout.Label($"Foot IK: {(footIKEnabled ? "ON" : "OFF")}");
            GUILayout.Label($"Hip/Body IK: {(hipIKEnabled ? "ON" : "OFF")}");

            GUILayout.Space(10);
            GUILayout.Label("Controls:");
            GUILayout.Label($"  {lookAtTestKey}: Toggle LookAt");
            GUILayout.Label($"  {handIKTestKey}: Toggle Hand IK");
            GUILayout.Label($"  {footIKTestKey}: Toggle Foot IK");
            GUILayout.Label($"  {hipIKTestKey}: Toggle Hip/Body IK");
            GUILayout.Label($"  {clearAllIKKey}: Clear All IK");
            GUILayout.Label($"  {registerKey}: Register to StoryPlayer");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
