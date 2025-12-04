#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace RPGDefete.Story.Editor
{
    /// <summary>
    /// Custom inspector for StoryData ScriptableObject
    /// </summary>
    [CustomEditor(typeof(StoryData))]
    public class StoryDataEditor : UnityEditor.Editor
    {
        private StoryData storyData;
        private ReorderableList commandList;
        private SerializedProperty storyIdProperty;
        private SerializedProperty commandsProperty;

        private readonly string[] opTypes = { "say", "bg", "bgm.play", "bgm.stop", "se.play", "wait", "end" };

        private void OnEnable()
        {
            storyData = (StoryData)target;
            storyIdProperty = serializedObject.FindProperty("storyId");
            commandsProperty = serializedObject.FindProperty("commands");

            SetupReorderableList();
        }

        private void SetupReorderableList()
        {
            commandList = new ReorderableList(serializedObject, commandsProperty, true, true, true, true);

            commandList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, $"Commands ({commandsProperty.arraySize})");
            };

            commandList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = commandsProperty.GetArrayElementAtIndex(index);
                var opProperty = element.FindPropertyRelative("op");

                rect.y += 2;
                float opWidth = 100f;
                float spacing = 5f;

                // Op type
                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, opWidth, EditorGUIUtility.singleLineHeight),
                    opProperty, GUIContent.none);

                // Summary based on op type
                string summary = GetCommandSummary(element, opProperty.stringValue);
                EditorGUI.LabelField(
                    new Rect(rect.x + opWidth + spacing, rect.y, rect.width - opWidth - spacing, EditorGUIUtility.singleLineHeight),
                    summary, EditorStyles.miniLabel);
            };

            commandList.onAddDropdownCallback = (Rect buttonRect, ReorderableList list) =>
            {
                var menu = new GenericMenu();
                foreach (var op in opTypes)
                {
                    menu.AddItem(new GUIContent(op), false, () => AddCommand(op));
                }
                menu.ShowAsContext();
            };

            commandList.elementHeight = EditorGUIUtility.singleLineHeight + 4;
        }

        private string GetCommandSummary(SerializedProperty element, string op)
        {
            switch (op)
            {
                case "say":
                    var name = element.FindPropertyRelative("characterName").stringValue;
                    var lines = element.FindPropertyRelative("lines");
                    int lineCount = lines.arraySize;
                    return $"{name} ({lineCount} lines)";

                case "bg":
                    return element.FindPropertyRelative("backgroundName").stringValue;

                case "bgm.play":
                    return element.FindPropertyRelative("bgmName").stringValue;

                case "bgm.stop":
                    return $"fade: {element.FindPropertyRelative("fadeOutDuration").intValue}ms";

                case "se.play":
                    return element.FindPropertyRelative("seName").stringValue;

                case "wait":
                    return $"{element.FindPropertyRelative("waitDuration").intValue}ms";

                case "end":
                    return element.FindPropertyRelative("returnTo").stringValue;

                default:
                    return "";
            }
        }

        private void AddCommand(string op)
        {
            serializedObject.Update();
            int index = commandsProperty.arraySize;
            commandsProperty.InsertArrayElementAtIndex(index);

            var newElement = commandsProperty.GetArrayElementAtIndex(index);
            newElement.FindPropertyRelative("op").stringValue = op;

            // Set defaults
            newElement.FindPropertyRelative("loop").boolValue = true;
            newElement.FindPropertyRelative("volume").floatValue = 1f;
            newElement.FindPropertyRelative("seVolume").floatValue = 1f;
            newElement.FindPropertyRelative("portraitScale").floatValue = 1f;
            newElement.FindPropertyRelative("portraitPosition").enumValueIndex = 1; // Center

            serializedObject.ApplyModifiedProperties();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Story ID
            EditorGUILayout.PropertyField(storyIdProperty);
            EditorGUILayout.Space();

            // Toolbar
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Open in Story Editor", GUILayout.Height(25)))
            {
                StoryEditorWindow.Open(storyData);
            }

            if (GUILayout.Button("Import JSON", GUILayout.Width(100)))
            {
                ImportJson();
            }

            if (GUILayout.Button("Export JSON", GUILayout.Width(100)))
            {
                ExportJson();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Command list
            commandList.DoLayoutList();

            // Selected command details
            if (commandList.index >= 0 && commandList.index < commandsProperty.arraySize)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Command Details", EditorStyles.boldLabel);

                var selected = commandsProperty.GetArrayElementAtIndex(commandList.index);
                DrawCommandDetails(selected);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCommandDetails(SerializedProperty element)
        {
            string op = element.FindPropertyRelative("op").stringValue;

            EditorGUI.indentLevel++;

            switch (op)
            {
                case "say":
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("characterName"), new GUIContent("Character Name"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("portrait"), new GUIContent("Portrait"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("portraitPosition"), new GUIContent("Position"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("portraitScale"), new GUIContent("Scale"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("lines"), new GUIContent("Lines"), true);
                    break;

                case "bg":
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("backgroundName"), new GUIContent("Background"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("fadeDuration"), new GUIContent("Fade (ms)"));
                    break;

                case "bgm.play":
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("bgmName"), new GUIContent("BGM"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("loop"), new GUIContent("Loop"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("volume"), new GUIContent("Volume"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("fadeInDuration"), new GUIContent("Fade In (ms)"));
                    break;

                case "bgm.stop":
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("fadeOutDuration"), new GUIContent("Fade Out (ms)"));
                    break;

                case "se.play":
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("seName"), new GUIContent("SE"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("seVolume"), new GUIContent("Volume"));
                    break;

                case "wait":
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("waitDuration"), new GUIContent("Duration (ms)"));
                    break;

                case "end":
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("returnTo"), new GUIContent("Return To"));
                    break;
            }

            EditorGUI.indentLevel--;
        }

        private void ImportJson()
        {
            string path = EditorUtility.OpenFilePanel("Import Story JSON", "", "json");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                string json = System.IO.File.ReadAllText(path);
                Undo.RecordObject(storyData, "Import Story JSON");
                storyData.ImportFromJson(json);
                EditorUtility.SetDirty(storyData);
                Debug.Log($"[StoryDataEditor] Imported story from {path}");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Import Error", e.Message, "OK");
            }
        }

        private void ExportJson()
        {
            string defaultName = string.IsNullOrEmpty(storyData.StoryId) ? "story" : storyData.StoryId;
            string path = EditorUtility.SaveFilePanel("Export Story JSON", "", defaultName, "json");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                string json = storyData.ExportToJson();
                System.IO.File.WriteAllText(path, json);
                Debug.Log($"[StoryDataEditor] Exported story to {path}");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Export Error", e.Message, "OK");
            }
        }
    }
}
#endif
