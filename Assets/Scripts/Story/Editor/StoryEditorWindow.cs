#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace RPGDefete.Story.Editor
{
    /// <summary>
    /// Editor window for creating and editing story data
    /// </summary>
    public class StoryEditorWindow : EditorWindow
    {
        private StoryData currentStory;
        private SerializedObject serializedStory;
        private SerializedProperty commandsProperty;
        private ReorderableList commandList;

        private Vector2 listScrollPosition;
        private Vector2 detailScrollPosition;
        private int selectedCommandIndex = -1;

        private readonly string[] opTypes = { "say", "bg", "bgm.play", "bgm.stop", "se.play", "wait", "end" };
        private readonly Color[] opColors = {
            new Color(0.4f, 0.7f, 1f),   // say - blue
            new Color(0.5f, 0.8f, 0.5f), // bg - green
            new Color(1f, 0.7f, 0.4f),   // bgm.play - orange
            new Color(1f, 0.5f, 0.5f),   // bgm.stop - red
            new Color(0.8f, 0.6f, 1f),   // se.play - purple
            new Color(0.7f, 0.7f, 0.7f), // wait - gray
            new Color(1f, 1f, 0.5f)      // end - yellow
        };

        [MenuItem("Tools/RPG Defete/Story Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<StoryEditorWindow>("Story Editor");
            window.minSize = new Vector2(600, 400);
        }

        public static void Open(StoryData story)
        {
            var window = GetWindow<StoryEditorWindow>("Story Editor");
            window.LoadStory(story);
        }

        private void OnEnable()
        {
            // Try to restore last edited story
            string lastStoryPath = EditorPrefs.GetString("StoryEditor_LastStory", "");
            if (!string.IsNullOrEmpty(lastStoryPath))
            {
                var story = AssetDatabase.LoadAssetAtPath<StoryData>(lastStoryPath);
                if (story != null)
                {
                    LoadStory(story);
                }
            }
        }

        private void LoadStory(StoryData story)
        {
            currentStory = story;
            if (currentStory != null)
            {
                serializedStory = new SerializedObject(currentStory);
                commandsProperty = serializedStory.FindProperty("commands");
                SetupReorderableList();

                // Save for restore
                string path = AssetDatabase.GetAssetPath(currentStory);
                EditorPrefs.SetString("StoryEditor_LastStory", path);
            }
            else
            {
                serializedStory = null;
                commandsProperty = null;
                commandList = null;
            }

            selectedCommandIndex = -1;
            Repaint();
        }

        private void SetupReorderableList()
        {
            commandList = new ReorderableList(serializedStory, commandsProperty, true, true, true, true);

            commandList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, $"Commands ({commandsProperty.arraySize})");
            };

            commandList.drawElementCallback = DrawCommandElement;

            commandList.onSelectCallback = (list) =>
            {
                selectedCommandIndex = list.index;
            };

            commandList.onAddDropdownCallback = (Rect buttonRect, ReorderableList list) =>
            {
                var menu = new GenericMenu();
                for (int i = 0; i < opTypes.Length; i++)
                {
                    string op = opTypes[i];
                    menu.AddItem(new GUIContent(op), false, () => AddCommand(op));
                }
                menu.ShowAsContext();
            };

            commandList.elementHeight = EditorGUIUtility.singleLineHeight + 6;
        }

        private void DrawCommandElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = commandsProperty.GetArrayElementAtIndex(index);
            var opProperty = element.FindPropertyRelative("op");
            string op = opProperty.stringValue;

            rect.y += 3;
            rect.height = EditorGUIUtility.singleLineHeight;

            // Color indicator
            int opIndex = System.Array.IndexOf(opTypes, op);
            Color color = opIndex >= 0 ? opColors[opIndex] : Color.white;

            Rect colorRect = new Rect(rect.x, rect.y, 4, rect.height);
            EditorGUI.DrawRect(colorRect, color);

            // Op label
            Rect opRect = new Rect(rect.x + 8, rect.y, 80, rect.height);
            EditorGUI.LabelField(opRect, op, EditorStyles.boldLabel);

            // Summary
            string summary = GetCommandSummary(element, op);
            Rect summaryRect = new Rect(rect.x + 90, rect.y, rect.width - 90, rect.height);
            EditorGUI.LabelField(summaryRect, summary, EditorStyles.miniLabel);
        }

        private string GetCommandSummary(SerializedProperty element, string op)
        {
            switch (op)
            {
                case "say":
                    var name = element.FindPropertyRelative("characterName").stringValue;
                    var lines = element.FindPropertyRelative("lines");
                    string firstLine = lines.arraySize > 0 ? lines.GetArrayElementAtIndex(0).stringValue : "";
                    if (firstLine.Length > 30) firstLine = firstLine.Substring(0, 30) + "...";
                    return $"{name}: {firstLine}";

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
                    var returnTo = element.FindPropertyRelative("returnTo").stringValue;
                    return string.IsNullOrEmpty(returnTo) ? "(no return)" : $"-> {returnTo}";

                default:
                    return "";
            }
        }

        private void AddCommand(string op)
        {
            serializedStory.Update();
            int index = commandsProperty.arraySize;
            commandsProperty.InsertArrayElementAtIndex(index);

            var newElement = commandsProperty.GetArrayElementAtIndex(index);
            newElement.FindPropertyRelative("op").stringValue = op;

            // Set defaults
            newElement.FindPropertyRelative("characterName").stringValue = "";
            newElement.FindPropertyRelative("portrait").stringValue = "";
            newElement.FindPropertyRelative("backgroundName").stringValue = "";
            newElement.FindPropertyRelative("bgmName").stringValue = "";
            newElement.FindPropertyRelative("seName").stringValue = "";
            newElement.FindPropertyRelative("returnTo").stringValue = "";
            newElement.FindPropertyRelative("lines").ClearArray();
            newElement.FindPropertyRelative("loop").boolValue = true;
            newElement.FindPropertyRelative("volume").floatValue = 1f;
            newElement.FindPropertyRelative("seVolume").floatValue = 1f;
            newElement.FindPropertyRelative("portraitScale").floatValue = 1f;
            newElement.FindPropertyRelative("portraitPosition").enumValueIndex = 1; // Center
            newElement.FindPropertyRelative("fadeDuration").intValue = 0;
            newElement.FindPropertyRelative("fadeInDuration").intValue = 0;
            newElement.FindPropertyRelative("fadeOutDuration").intValue = 0;
            newElement.FindPropertyRelative("waitDuration").intValue = 0;

            serializedStory.ApplyModifiedProperties();
            selectedCommandIndex = index;
            commandList.index = index;
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (currentStory == null)
            {
                DrawNoStoryMessage();
                return;
            }

            serializedStory.Update();

            EditorGUILayout.BeginHorizontal();

            // Left panel - Command list
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.45f));
            DrawCommandListPanel();
            EditorGUILayout.EndVertical();

            // Right panel - Command details
            EditorGUILayout.BeginVertical();
            DrawCommandDetailPanel();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            serializedStory.ApplyModifiedProperties();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // New button
            if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                CreateNewStory();
            }

            // Open button
            if (GUILayout.Button("Open", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                OpenStory();
            }

            // Save button
            EditorGUI.BeginDisabledGroup(currentStory == null);
            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                SaveStory();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();

            // Import/Export
            EditorGUI.BeginDisabledGroup(currentStory == null);
            if (GUILayout.Button("Import JSON", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                ImportJson();
            }
            if (GUILayout.Button("Export JSON", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                ExportJson();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            // Story info
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Story:", GUILayout.Width(40));

            EditorGUI.BeginDisabledGroup(currentStory == null);
            var newStory = (StoryData)EditorGUILayout.ObjectField(currentStory, typeof(StoryData), false);
            if (newStory != currentStory)
            {
                LoadStory(newStory);
            }

            if (currentStory != null)
            {
                var storyIdProperty = serializedStory.FindProperty("storyId");
                EditorGUILayout.PropertyField(storyIdProperty, GUIContent.none, GUILayout.Width(150));
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        private void DrawNoStoryMessage()
        {
            EditorGUILayout.Space(50);
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("No story loaded. Create or open a story to begin.", EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        private void DrawCommandListPanel()
        {
            EditorGUILayout.LabelField("Commands", EditorStyles.boldLabel);

            listScrollPosition = EditorGUILayout.BeginScrollView(listScrollPosition);

            if (commandList != null)
            {
                commandList.DoLayoutList();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawCommandDetailPanel()
        {
            EditorGUILayout.LabelField("Details", EditorStyles.boldLabel);

            if (selectedCommandIndex < 0 || selectedCommandIndex >= commandsProperty.arraySize)
            {
                EditorGUILayout.HelpBox("Select a command to edit", MessageType.Info);
                return;
            }

            detailScrollPosition = EditorGUILayout.BeginScrollView(detailScrollPosition);

            var element = commandsProperty.GetArrayElementAtIndex(selectedCommandIndex);
            var opProperty = element.FindPropertyRelative("op");
            string op = opProperty.stringValue;

            // Op selector
            int opIndex = System.Array.IndexOf(opTypes, op);
            int newOpIndex = EditorGUILayout.Popup("Type", opIndex, opTypes);
            if (newOpIndex != opIndex && newOpIndex >= 0)
            {
                opProperty.stringValue = opTypes[newOpIndex];
            }

            EditorGUILayout.Space();

            // Draw op-specific fields
            switch (op)
            {
                case "say":
                    DrawSayDetails(element);
                    break;
                case "bg":
                    DrawBgDetails(element);
                    break;
                case "bgm.play":
                    DrawBgmPlayDetails(element);
                    break;
                case "bgm.stop":
                    DrawBgmStopDetails(element);
                    break;
                case "se.play":
                    DrawSePlayDetails(element);
                    break;
                case "wait":
                    DrawWaitDetails(element);
                    break;
                case "end":
                    DrawEndDetails(element);
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSayDetails(SerializedProperty element)
        {
            EditorGUILayout.PropertyField(element.FindPropertyRelative("characterName"), new GUIContent("Character Name"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("portrait"), new GUIContent("Portrait Resource"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("portraitPosition"), new GUIContent("Portrait Position"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("portraitScale"), new GUIContent("Portrait Scale"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Dialogue Lines", EditorStyles.boldLabel);

            var linesProperty = element.FindPropertyRelative("lines");

            for (int i = 0; i < linesProperty.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();

                var lineProperty = linesProperty.GetArrayElementAtIndex(i);
                lineProperty.stringValue = EditorGUILayout.TextArea(lineProperty.stringValue, GUILayout.Height(40));

                if (GUILayout.Button("-", GUILayout.Width(25)))
                {
                    linesProperty.DeleteArrayElementAtIndex(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Line"))
            {
                linesProperty.InsertArrayElementAtIndex(linesProperty.arraySize);
                linesProperty.GetArrayElementAtIndex(linesProperty.arraySize - 1).stringValue = "";
            }
        }

        private void DrawBgDetails(SerializedProperty element)
        {
            EditorGUILayout.PropertyField(element.FindPropertyRelative("backgroundName"), new GUIContent("Background Resource"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("fadeDuration"), new GUIContent("Fade Duration (ms)"));
        }

        private void DrawBgmPlayDetails(SerializedProperty element)
        {
            EditorGUILayout.PropertyField(element.FindPropertyRelative("bgmName"), new GUIContent("BGM Resource"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("loop"), new GUIContent("Loop"));
            EditorGUILayout.Slider(element.FindPropertyRelative("volume"), 0f, 1f, new GUIContent("Volume"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("fadeInDuration"), new GUIContent("Fade In (ms)"));
        }

        private void DrawBgmStopDetails(SerializedProperty element)
        {
            EditorGUILayout.PropertyField(element.FindPropertyRelative("fadeOutDuration"), new GUIContent("Fade Out (ms)"));
        }

        private void DrawSePlayDetails(SerializedProperty element)
        {
            EditorGUILayout.PropertyField(element.FindPropertyRelative("seName"), new GUIContent("SE Resource"));
            EditorGUILayout.Slider(element.FindPropertyRelative("seVolume"), 0f, 1f, new GUIContent("Volume"));
        }

        private void DrawWaitDetails(SerializedProperty element)
        {
            EditorGUILayout.PropertyField(element.FindPropertyRelative("waitDuration"), new GUIContent("Duration (ms)"));
        }

        private void DrawEndDetails(SerializedProperty element)
        {
            EditorGUILayout.PropertyField(element.FindPropertyRelative("returnTo"), new GUIContent("Return To"));
        }

        private void CreateNewStory()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create New Story",
                "NewStory",
                "asset",
                "Enter a name for the new story asset");

            if (string.IsNullOrEmpty(path)) return;

            var newStory = CreateInstance<StoryData>();
            AssetDatabase.CreateAsset(newStory, path);
            AssetDatabase.SaveAssets();

            LoadStory(newStory);
        }

        private void OpenStory()
        {
            string path = EditorUtility.OpenFilePanel("Open Story", "Assets", "asset");
            if (string.IsNullOrEmpty(path)) return;

            // Convert to relative path
            if (path.StartsWith(Application.dataPath))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
            }

            var story = AssetDatabase.LoadAssetAtPath<StoryData>(path);
            if (story != null)
            {
                LoadStory(story);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Selected file is not a StoryData asset", "OK");
            }
        }

        private void SaveStory()
        {
            if (currentStory == null) return;

            EditorUtility.SetDirty(currentStory);
            AssetDatabase.SaveAssets();
            Debug.Log("[StoryEditor] Story saved");
        }

        private void ImportJson()
        {
            if (currentStory == null) return;

            string path = EditorUtility.OpenFilePanel("Import Story JSON", "", "json");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                string json = System.IO.File.ReadAllText(path);
                Undo.RecordObject(currentStory, "Import Story JSON");
                currentStory.ImportFromJson(json);
                EditorUtility.SetDirty(currentStory);

                // Reload serialized object
                serializedStory = new SerializedObject(currentStory);
                commandsProperty = serializedStory.FindProperty("commands");
                SetupReorderableList();

                Debug.Log($"[StoryEditor] Imported story from {path}");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Import Error", e.Message, "OK");
            }
        }

        private void ExportJson()
        {
            if (currentStory == null) return;

            string defaultName = string.IsNullOrEmpty(currentStory.StoryId) ? "story" : currentStory.StoryId;
            string path = EditorUtility.SaveFilePanel("Export Story JSON", "", defaultName, "json");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                string json = currentStory.ExportToJson();
                System.IO.File.WriteAllText(path, json);
                Debug.Log($"[StoryEditor] Exported story to {path}");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Export Error", e.Message, "OK");
            }
        }
    }
}
#endif
