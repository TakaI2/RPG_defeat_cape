using UnityEngine;
using UnityEditor;
using RPGDefete.Character;

namespace RPGDefete.Editor
{
    /// <summary>
    /// InteractionPointを自動配置するエディタツール
    /// Humanoidリグのボーン情報から各InteractionPointを生成します
    /// </summary>
    public class InteractionPointAutoSetup : EditorWindow
    {
        private GameObject targetCharacter;
        private bool createMouth = true;
        private bool createEye = true;
        private bool createHead = true;
        private bool createShoulder = true;
        private bool createHand = true;
        private bool createHip = true;
        private bool createChest = true;
        private bool createFoot = true;

        [MenuItem("Tools/VRM/Auto Setup Interaction Points")]
        public static void ShowWindow()
        {
            GetWindow<InteractionPointAutoSetup>("Interaction Point Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Interaction Point Auto Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            targetCharacter = (GameObject)EditorGUILayout.ObjectField(
                "Target Character",
                targetCharacter,
                typeof(GameObject),
                true
            );

            EditorGUILayout.Space();
            GUILayout.Label("Create Points:", EditorStyles.boldLabel);

            createMouth = EditorGUILayout.Toggle("Mouth (口)", createMouth);
            createEye = EditorGUILayout.Toggle("Eye (目)", createEye);
            createHead = EditorGUILayout.Toggle("Head (頭)", createHead);
            createShoulder = EditorGUILayout.Toggle("Shoulder (肩)", createShoulder);
            createHand = EditorGUILayout.Toggle("Hand (手)", createHand);
            createChest = EditorGUILayout.Toggle("Chest (胸)", createChest);
            createHip = EditorGUILayout.Toggle("Hip (腰)", createHip);
            createFoot = EditorGUILayout.Toggle("Foot (足)", createFoot);

            EditorGUILayout.Space();

            GUI.enabled = targetCharacter != null;
            if (GUILayout.Button("Auto Setup Interaction Points", GUILayout.Height(40)))
            {
                SetupInteractionPoints();
            }
            GUI.enabled = true;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Humanoidリグのボーン情報から自動的にInteractionPointを配置します。\n" +
                "配置後、微調整が必要な場合はInspectorで調整してください。",
                MessageType.Info
            );
        }

        private void SetupInteractionPoints()
        {
            if (targetCharacter == null)
            {
                EditorUtility.DisplayDialog("Error", "Target Characterを指定してください", "OK");
                return;
            }

            Animator animator = targetCharacter.GetComponent<Animator>();
            if (animator == null || !animator.isHuman)
            {
                EditorUtility.DisplayDialog("Error", "ターゲットはHumanoidリグを持つキャラクターである必要があります", "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(targetCharacter, "Setup Interaction Points");

            int createdCount = 0;

            // Mouth (口元)
            if (createMouth)
            {
                Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
                if (head != null)
                {
                    createdCount += CreateInteractionPoint(
                        head,
                        "mouth_point",
                        InteractionPointType.Mouth,
                        new Vector3(0, 0.05f, 0.08f),  // 頭から前方・やや上
                        Color.red
                    );
                }
            }

            // Eye (目)
            if (createEye)
            {
                Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
                if (head != null)
                {
                    createdCount += CreateInteractionPoint(
                        head,
                        "eye_point",
                        InteractionPointType.Eye,
                        new Vector3(0, 0.08f, 0.06f),  // 頭から前方・目の位置
                        Color.blue
                    );
                }
            }

            // Head (頭頂部)
            if (createHead)
            {
                Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
                if (head != null)
                {
                    createdCount += CreateInteractionPoint(
                        head,
                        "head_point",
                        InteractionPointType.Head,
                        new Vector3(0, 0.12f, 0),  // 頭から上
                        Color.magenta
                    );
                }
            }

            // Shoulder (左右)
            if (createShoulder)
            {
                Transform leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
                Transform rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);

                if (leftShoulder != null)
                {
                    createdCount += CreateInteractionPoint(
                        leftShoulder,
                        "shoulder_left_point",
                        InteractionPointType.Shoulder,
                        new Vector3(0.05f, 0, 0),  // 肩から外側
                        Color.green
                    );
                }

                if (rightShoulder != null)
                {
                    createdCount += CreateInteractionPoint(
                        rightShoulder,
                        "shoulder_right_point",
                        InteractionPointType.Shoulder,
                        new Vector3(-0.05f, 0, 0),  // 肩から外側
                        Color.green
                    );
                }
            }

            // Hand (左右)
            if (createHand)
            {
                Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);

                if (leftHand != null)
                {
                    createdCount += CreateInteractionPoint(
                        leftHand,
                        "hand_left_point",
                        InteractionPointType.Hand,
                        new Vector3(0.05f, 0, 0),  // 手のひら中央
                        Color.yellow
                    );
                }

                if (rightHand != null)
                {
                    createdCount += CreateInteractionPoint(
                        rightHand,
                        "hand_right_point",
                        InteractionPointType.Hand,
                        new Vector3(-0.05f, 0, 0),  // 手のひら中央
                        Color.yellow
                    );
                }
            }

            // Chest (胸)
            if (createChest)
            {
                Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest);
                if (chest != null)
                {
                    createdCount += CreateInteractionPoint(
                        chest,
                        "chest_point",
                        InteractionPointType.Chest,
                        new Vector3(0, 0.1f, 0.05f),  // 胸の中央
                        Color.cyan
                    );
                }
            }

            // Hip (腰)
            if (createHip)
            {
                Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                if (hips != null)
                {
                    createdCount += CreateInteractionPoint(
                        hips,
                        "hip_point",
                        InteractionPointType.Hip,
                        Vector3.zero,  // 腰の中心
                        Color.gray
                    );
                }
            }

            // Foot (左右の足)
            if (createFoot)
            {
                Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);

                if (leftFoot != null)
                {
                    createdCount += CreateInteractionPoint(
                        leftFoot,
                        "foot_left_point",
                        InteractionPointType.Foot,
                        new Vector3(0, 0, 0.05f),  // 足の甲の位置
                        new Color(1f, 0.5f, 0f)  // オレンジ色
                    );
                }

                if (rightFoot != null)
                {
                    createdCount += CreateInteractionPoint(
                        rightFoot,
                        "foot_right_point",
                        InteractionPointType.Foot,
                        new Vector3(0, 0, 0.05f),  // 足の甲の位置
                        new Color(1f, 0.5f, 0f)  // オレンジ色
                    );
                }
            }

            EditorUtility.DisplayDialog(
                "完了",
                $"{createdCount}個のInteractionPointを作成しました。\n" +
                "必要に応じてInspectorで微調整してください。",
                "OK"
            );

            // GameCharacterに再度コレクトさせる
            var gameChar = targetCharacter.GetComponent<GameCharacter>();
            if (gameChar != null)
            {
                // プライベートメソッドなので、リフレクションで呼び出す
                var method = gameChar.GetType().GetMethod(
                    "CollectInteractionPoints",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
                );
                if (method != null)
                {
                    method.Invoke(gameChar, null);
                }
            }
        }

        /// <summary>
        /// InteractionPointを作成
        /// </summary>
        private int CreateInteractionPoint(
            Transform parent,
            string pointName,
            InteractionPointType pointType,
            Vector3 localOffset,
            Color gizmoColor)
        {
            // 既に存在する場合はスキップ
            Transform existing = parent.Find(pointName);
            if (existing != null)
            {
                Debug.Log($"[InteractionPointAutoSetup] {pointName} already exists. Skipping.");
                return 0;
            }

            // 新しいGameObjectを作成
            GameObject pointObj = new GameObject(pointName);
            pointObj.transform.SetParent(parent);
            pointObj.transform.localPosition = localOffset;
            pointObj.transform.localRotation = Quaternion.identity;
            pointObj.transform.localScale = Vector3.one;

            // InteractionPointコンポーネントを追加
            InteractionPoint point = pointObj.AddComponent<InteractionPoint>();

            // リフレクションでprivateフィールドを設定
            var pointTypeField = typeof(InteractionPoint).GetField(
                "pointType",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            if (pointTypeField != null)
            {
                pointTypeField.SetValue(point, pointType);
            }

            var gizmoColorField = typeof(InteractionPoint).GetField(
                "gizmoColor",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            if (gizmoColorField != null)
            {
                gizmoColorField.SetValue(point, gizmoColor);
            }

            Debug.Log($"[InteractionPointAutoSetup] Created {pointName} at {pointType}");
            return 1;
        }
    }
}
