using UnityEngine;
using RPG.Action;
using RPGDefete.Character;

namespace RPG.Testing
{
    /// <summary>
    /// KissActionとHugActionのテスト用スクリプト
    /// Kキー: Kiss, Hキー: Hug, Tキー: Touch, Gキー: Grab
    /// </summary>
    public class ActionTester : MonoBehaviour
    {
        [Header("設定")]
        [SerializeField] private GameCharacter actor;  // megu
        [SerializeField] private GameCharacter target; // ken
        [SerializeField] private bool showDebugLog = true;
        [SerializeField] private bool showGizmos = true;

        [Header("表示設定")]
        [SerializeField] private bool showInteractionPoints = true;
        [SerializeField] private bool showDistanceInfo = true;

        [Header("キーバインド")]
        [SerializeField] private KeyCode kissKey = KeyCode.K;
        [SerializeField] private KeyCode hugKey = KeyCode.H;
        [SerializeField] private KeyCode touchKey = KeyCode.T;
        [SerializeField] private KeyCode grabKey = KeyCode.G;
        [SerializeField] private KeyCode sitKey = KeyCode.S;
        [SerializeField] private KeyCode standKey = KeyCode.U;

        private ActionExecutor _actionExecutor;

        private void Start()
        {
            // 自動検出
            if (actor == null)
            {
                actor = GameObject.FindWithTag("Player")?.GetComponent<GameCharacter>();
            }

            if (actor != null)
            {
                _actionExecutor = actor.GetComponent<ActionExecutor>();
            }

            if (_actionExecutor == null)
            {
                Debug.LogError("[ActionTester] ActionExecutor not found on actor!");
            }

            if (showDebugLog)
            {
                Debug.Log($"[ActionTester] Initialized. Actor: {actor?.CharacterName}, Target: {target?.CharacterName}");
                Debug.Log($"[ActionTester] Controls: K=Kiss, H=Hug, T=Touch, G=Grab, S=Sit, U=Stand");
            }
        }

        private void Update()
        {
            if (_actionExecutor == null || target == null) return;

            // Kiss Action (K)
            if (UnityEngine.Input.GetKeyDown(kissKey))
            {
                ExecuteAction("Kiss");
            }

            // Hug Action (H)
            if (UnityEngine.Input.GetKeyDown(hugKey))
            {
                ExecuteAction("Hug");
            }

            // Touch Action (T)
            if (UnityEngine.Input.GetKeyDown(touchKey))
            {
                ExecuteAction("Touch");
            }

            // Grab Action (G)
            if (UnityEngine.Input.GetKeyDown(grabKey))
            {
                ExecuteAction("Grab");
            }

            // Sit Action (S)
            if (UnityEngine.Input.GetKeyDown(sitKey))
            {
                ExecuteAction("Sit");
            }

            // Stand Action (U)
            if (UnityEngine.Input.GetKeyDown(standKey))
            {
                ExecuteAction("Stand");
            }
        }

        private void ExecuteAction(string actionName)
        {
            if (showDebugLog)
            {
                Debug.Log($"[ActionTester] Executing {actionName} action...");
            }

            // ActionContextを作成
            var context = new ActionContext
            {
                Actor = actor,
                Target = target.gameObject,
                TargetPosition = target.transform.position,
                Distance = Vector3.Distance(actor.transform.position, target.transform.position)
            };

            // アクションを実行
            bool success = _actionExecutor.TryExecuteAction(actionName, context);

            if (showDebugLog)
            {
                if (success)
                {
                    Debug.Log($"[ActionTester] {actionName} action started successfully!");
                }
                else
                {
                    Debug.LogWarning($"[ActionTester] Failed to execute {actionName} action!");
                }
            }
        }

        private void OnGUI()
        {
            // 画面上部に操作説明を表示
            GUILayout.BeginArea(new Rect(10, 10, 450, 300));
            GUILayout.BeginVertical("box");

            GUILayout.Label("=== Action Tester ===", new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });
            GUILayout.Space(5);

            // キャラクター情報
            GUILayout.Label($"Actor: {actor?.CharacterName ?? "None"}");
            GUILayout.Label($"Target: {target?.CharacterName ?? "None"}");

            if (actor != null && target != null)
            {
                float distance = Vector3.Distance(actor.transform.position, target.transform.position);
                GUILayout.Label($"Distance: {distance:F2}m");

                // 高さ情報
                if (showDistanceInfo)
                {
                    var actorMouth = actor.GetInteractionPoint(InteractionPointType.Mouth);
                    var targetMouth = target.GetInteractionPoint(InteractionPointType.Mouth);
                    if (actorMouth != null && targetMouth != null)
                    {
                        float heightDiff = actorMouth.GetWorldPosition().y - targetMouth.GetWorldPosition().y;
                        GUILayout.Label($"Height Diff: {heightDiff:F2}m ({(heightDiff > 0 ? "Actor taller" : "Target taller")})");
                    }
                }

                // ActionExecutorの状態
                if (_actionExecutor != null)
                {
                    GUILayout.Label($"ActionExecutor: Ready");
                }
            }

            GUILayout.Space(10);
            GUILayout.Label("Controls:", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.Label($"  K - Kiss Action");
            GUILayout.Label($"  H - Hug Action");
            GUILayout.Label($"  T - Touch Action");
            GUILayout.Label($"  G - Grab Action");
            GUILayout.Label($"  S - Sit Action");
            GUILayout.Label($"  U - Stand Action");

            GUILayout.Space(10);
            GUILayout.Label("Tips:", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.Label($"  Console shows detailed logs");
            GUILayout.Label($"  Gizmos show InteractionPoints");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        // Gizmosで視覚的にデバッグ
        private void OnDrawGizmos()
        {
            if (!showGizmos || actor == null || target == null) return;

            // ActorからTargetへの線
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(actor.transform.position + Vector3.up, target.transform.position + Vector3.up);

            // 距離表示用
            Vector3 midPoint = (actor.transform.position + target.transform.position) / 2f + Vector3.up * 1.5f;
            float distance = Vector3.Distance(actor.transform.position, target.transform.position);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(midPoint, $"{distance:F2}m");

            // InteractionPointsを視覚化
            if (showInteractionPoints)
            {
                DrawInteractionPoints(actor, Color.green);
                DrawInteractionPoints(target, Color.yellow);

                // 口同士の距離を表示
                var actorMouth = actor.GetInteractionPoint(InteractionPointType.Mouth);
                var targetMouth = target.GetInteractionPoint(InteractionPointType.Mouth);
                if (actorMouth != null && targetMouth != null)
                {
                    Vector3 actorMouthPos = actorMouth.GetWorldPosition();
                    Vector3 targetMouthPos = targetMouth.GetWorldPosition();

                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(actorMouthPos, targetMouthPos);

                    Vector3 mouthMidPoint = (actorMouthPos + targetMouthPos) / 2f;
                    float mouthDistance = Vector3.Distance(actorMouthPos, targetMouthPos);
                    UnityEditor.Handles.Label(mouthMidPoint, $"Mouth: {mouthDistance:F3}m");
                }
            }
#endif
        }

#if UNITY_EDITOR
        private void DrawInteractionPoints(GameCharacter character, Color color)
        {
            if (character == null) return;

            var points = new[]
            {
                character.GetInteractionPoint(InteractionPointType.Mouth),
                character.GetInteractionPoint(InteractionPointType.Eye),
                character.GetInteractionPoint(InteractionPointType.Head),
                character.GetInteractionPoint(InteractionPointType.Shoulder),
                character.GetInteractionPoint(InteractionPointType.Hand),
                character.GetInteractionPoint(InteractionPointType.Chest),
                character.GetInteractionPoint(InteractionPointType.Hip)
            };

            Gizmos.color = color;
            foreach (var point in points)
            {
                if (point != null)
                {
                    Vector3 pos = point.GetWorldPosition();
                    Gizmos.DrawWireSphere(pos, 0.03f);
                    UnityEditor.Handles.Label(pos + Vector3.up * 0.05f, point.PointType.ToString());
                }
            }
        }
#endif
    }
}
