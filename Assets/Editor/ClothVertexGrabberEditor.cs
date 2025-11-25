using UnityEngine;
using UnityEditor;
using MagicaCloth2;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Custom Editor for ClothVertexGrabber
/// Allows visual vertex selection in Scene view by clicking
/// </summary>
[CustomEditor(typeof(ClothVertexGrabber))]
public class ClothVertexGrabberEditor : Editor
{
    private ClothVertexGrabber grabber;
    private bool vertexSelectionMode = false;
    private int selectedGrabPointIndex = 0;
    private float vertexDisplayRadius = 0.03f;
    private float vertexClickRadius = 50f; // In screen pixels

    // Colors for each grab point
    private readonly Color[] grabPointColors = new Color[]
    {
        new Color(1f, 0.3f, 0.3f, 0.8f),  // Red for GrabPoint 1
        new Color(0.3f, 1f, 0.3f, 0.8f),  // Green for GrabPoint 2
        new Color(0.3f, 0.3f, 1f, 0.8f),  // Blue for GrabPoint 3
        new Color(1f, 1f, 0.3f, 0.8f),    // Yellow for GrabPoint 4
        new Color(1f, 0.3f, 1f, 0.8f),    // Magenta for GrabPoint 5
    };

    private readonly Color unassignedColor = new Color(0.7f, 0.7f, 0.7f, 0.4f);
    private readonly Color hoverColor = new Color(1f, 1f, 1f, 1f);

    private SerializedProperty grabPointsProp;
    private SerializedProperty magicaClothProp;

    private VertexAssignmentData assignmentData;
    private const string AssignmentDataPath = "Assets/Editor/ClothVertexAssignments.asset";

    void OnEnable()
    {
        grabber = (ClothVertexGrabber)target;
        grabPointsProp = serializedObject.FindProperty("grabPoints");
        magicaClothProp = serializedObject.FindProperty("magicaCloth");

        // Load or create assignment data asset
        LoadOrCreateAssignmentData();

        // Subscribe to play mode state changes
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    void OnDisable()
    {
        // Unsubscribe from play mode state changes
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Vertex Assignment Tool", EditorStyles.boldLabel);

        // Vertex Selection Mode Toggle
        EditorGUILayout.BeginHorizontal();
        bool newSelectionMode = GUILayout.Toggle(vertexSelectionMode,
            vertexSelectionMode ? "Selection Mode: ON" : "Selection Mode: OFF",
            "Button", GUILayout.Height(30));

        if (newSelectionMode != vertexSelectionMode)
        {
            vertexSelectionMode = newSelectionMode;
            SceneView.RepaintAll();
        }
        EditorGUILayout.EndHorizontal();

        if (vertexSelectionMode)
        {
            EditorGUILayout.HelpBox(
                "Click vertices in Scene view to assign/unassign them to the selected Grab Point.\n" +
                "• Left Click: Toggle vertex assignment\n" +
                "• Hold Shift: Add to selection\n" +
                "• Hold Ctrl: Remove from selection",
                MessageType.Info);

            EditorGUILayout.Space(5);

            // Grab Point Selection
            if (grabPointsProp != null && grabPointsProp.arraySize > 0)
            {
                string[] grabPointNames = new string[grabPointsProp.arraySize];
                for (int i = 0; i < grabPointsProp.arraySize; i++)
                {
                    var gpProp = grabPointsProp.GetArrayElementAtIndex(i);
                    var nameProp = gpProp.FindPropertyRelative("name");
                    grabPointNames[i] = $"Grab Point {i}: {(nameProp != null ? nameProp.stringValue : "Unnamed")}";
                }

                EditorGUILayout.LabelField("Active Grab Point for Assignment:", EditorStyles.boldLabel);
                selectedGrabPointIndex = GUILayout.SelectionGrid(
                    selectedGrabPointIndex,
                    grabPointNames,
                    1,
                    GUILayout.Height(25 * grabPointNames.Length));

                // Show color indicator
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Color:", GUILayout.Width(50));
                Color gpColor = GetGrabPointColor(selectedGrabPointIndex);
                EditorGUI.DrawRect(EditorGUILayout.GetControlRect(GUILayout.Width(30), GUILayout.Height(20)), gpColor);
                EditorGUILayout.EndHorizontal();

                // Show current assignment count
                if (selectedGrabPointIndex < grabPointsProp.arraySize)
                {
                    var gpProp = grabPointsProp.GetArrayElementAtIndex(selectedGrabPointIndex);
                    var allowedIndicesProp = gpProp.FindPropertyRelative("allowedVertexIndices");
                    if (allowedIndicesProp != null)
                    {
                        EditorGUILayout.LabelField($"Assigned Vertices: {allowedIndicesProp.arraySize}");
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No grab points configured!", MessageType.Warning);
            }

            EditorGUILayout.Space(5);

            // Visualization Settings
            EditorGUILayout.LabelField("Visualization Settings:", EditorStyles.boldLabel);
            vertexDisplayRadius = EditorGUILayout.Slider("Vertex Display Size", vertexDisplayRadius, 0.01f, 0.1f);
            vertexClickRadius = EditorGUILayout.Slider("Click Detection Radius (pixels)", vertexClickRadius, 10f, 100f);

            EditorGUILayout.Space(5);

            // Utility Buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Current Grab Point", GUILayout.Height(25)))
            {
                ClearGrabPointAssignments(selectedGrabPointIndex);
            }
            if (GUILayout.Button("Clear All", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Clear All Assignments",
                    "Are you sure you want to clear all vertex assignments for all grab points?",
                    "Yes", "Cancel"))
                {
                    ClearAllAssignments();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        // Persistence UI
        EditorGUILayout.Space(10);
        DrawPersistenceUI();

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }

    void DrawPersistenceUI()
    {
        EditorGUILayout.LabelField("Vertex Assignment Persistence", EditorStyles.boldLabel);

        if (assignmentData != null)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Status:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Last Saved: {(string.IsNullOrEmpty(assignmentData.lastSavedTime) ? "Never" : assignmentData.lastSavedTime)}");
            EditorGUILayout.LabelField($"Total Assigned Vertices: {assignmentData.totalAssignedVertices}");

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Save Current Assignments", GUILayout.Height(25)))
            {
                SaveAssignments();
            }

            if (GUILayout.Button("Load Saved Assignments", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Load Assignments",
                    "This will overwrite current vertex assignments. Continue?",
                    "Yes", "Cancel"))
                {
                    LoadAssignments();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Clear All Assignments", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Clear Assignments",
                    "Are you sure you want to clear all vertex assignments?",
                    "Yes", "Cancel"))
                {
                    ClearAllAssignments();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "Auto-Save: Assignments are automatically saved when exiting Play mode.\n" +
                "Auto-Load: Assignments are automatically loaded when entering Play mode.",
                MessageType.Info);

            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.HelpBox("Assignment data asset not found. Click 'Create Asset' to create one.", MessageType.Warning);
            if (GUILayout.Button("Create Asset", GUILayout.Height(25)))
            {
                LoadOrCreateAssignmentData();
            }
        }
    }

    void OnSceneGUI()
    {
        if (!vertexSelectionMode || !Application.isPlaying)
            return;

        var magicaCloth = GetMagicaCloth();
        if (magicaCloth == null || !magicaCloth.IsValid())
            return;

        // Get vertex data
        var proxyMesh = magicaCloth.Process.ProxyMeshContainer.shareVirtualMesh;
        var localPositions = proxyMesh.localPositions.GetNativeArray();
        var attributes = proxyMesh.attributes.GetNativeArray();

        // Get transform for coordinate conversion
        Transform clothTransform = magicaCloth.ClothTransform;
        Matrix4x4 localToWorld = clothTransform.localToWorldMatrix;

        // Build assignment map
        Dictionary<int, int> vertexToGrabPoint = BuildVertexAssignmentMap();

        // Keep scene view interactive - IMPORTANT: Do this early!
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        // Handle mouse input
        Event e = Event.current;
        Vector2 mousePos = e.mousePosition;
        // NOTE: Event.mousePosition is already in the correct GUI coordinate system
        // No need to flip Y axis

        int hoveredVertexIndex = -1;
        float closestDistance = vertexClickRadius;

        // Draw all vertices and detect hover
        for (int i = 0; i < localPositions.Length; i++)
        {
            var attr = attributes[i];
            if (!attr.IsMove())
                continue;

            Vector3 localPos = localPositions[i];
            Vector3 worldPos = localToWorld.MultiplyPoint3x4(localPos);

            // Determine color
            Color vertexColor;
            if (vertexToGrabPoint.TryGetValue(i, out int gpIndex))
            {
                vertexColor = GetGrabPointColor(gpIndex);
            }
            else
            {
                vertexColor = unassignedColor;
            }

            // Check if mouse is hovering over this vertex
            Vector2 screenPos = HandleUtility.WorldToGUIPoint(worldPos);
            float distance = Vector2.Distance(screenPos, mousePos);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                hoveredVertexIndex = i;
            }

            // Draw vertex
            if (hoveredVertexIndex == i)
            {
                Handles.color = hoverColor;
                Handles.DrawSolidDisc(worldPos, SceneView.currentDrawingSceneView.camera.transform.forward, vertexDisplayRadius * 1.5f);
            }
            else
            {
                Handles.color = vertexColor;
                Handles.DrawSolidDisc(worldPos, SceneView.currentDrawingSceneView.camera.transform.forward, vertexDisplayRadius);
            }
        }

        // Handle click
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Debug.Log($"[ClothVertexGrabberEditor] Mouse clicked! Hovered vertex: {hoveredVertexIndex}, Closest distance: {closestDistance}");

            if (hoveredVertexIndex >= 0)
            {
                bool isShiftHeld = e.shift;
                bool isCtrlHeld = e.control;

                Debug.Log($"[ClothVertexGrabberEditor] Vertex {hoveredVertexIndex} clicked! Shift: {isShiftHeld}, Ctrl: {isCtrlHeld}");

                if (isCtrlHeld)
                {
                    // Remove from current assignment
                    RemoveVertexFromGrabPoint(hoveredVertexIndex, selectedGrabPointIndex);
                    Debug.Log($"[ClothVertexGrabberEditor] Removed vertex {hoveredVertexIndex} from grab point {selectedGrabPointIndex}");
                }
                else if (isShiftHeld)
                {
                    // Add to current grab point
                    AddVertexToGrabPoint(hoveredVertexIndex, selectedGrabPointIndex);
                    Debug.Log($"[ClothVertexGrabberEditor] Added vertex {hoveredVertexIndex} to grab point {selectedGrabPointIndex}");
                }
                else
                {
                    // Toggle assignment
                    ToggleVertexAssignment(hoveredVertexIndex, selectedGrabPointIndex);
                    Debug.Log($"[ClothVertexGrabberEditor] Toggled vertex {hoveredVertexIndex} for grab point {selectedGrabPointIndex}");
                }

                e.Use();
                SceneView.RepaintAll();
            }
            else
            {
                Debug.Log($"[ClothVertexGrabberEditor] No vertex hovered (closest distance: {closestDistance}, threshold: {vertexClickRadius})");
            }
        }

        // Display hover info
        if (hoveredVertexIndex >= 0)
        {
            Vector3 worldPos = localToWorld.MultiplyPoint3x4(localPositions[hoveredVertexIndex]);
            Handles.Label(worldPos + Vector3.up * 0.1f,
                $"Vertex {hoveredVertexIndex}\nClick to assign",
                new GUIStyle(EditorStyles.helpBox) { fontSize = 10 });
        }
    }

    private MagicaCloth GetMagicaCloth()
    {
        serializedObject.Update();
        return magicaClothProp?.objectReferenceValue as MagicaCloth;
    }

    private Dictionary<int, int> BuildVertexAssignmentMap()
    {
        Dictionary<int, int> map = new Dictionary<int, int>();

        serializedObject.Update();

        for (int gpIndex = 0; gpIndex < grabPointsProp.arraySize; gpIndex++)
        {
            var gpProp = grabPointsProp.GetArrayElementAtIndex(gpIndex);
            var allowedIndicesProp = gpProp.FindPropertyRelative("allowedVertexIndices");

            if (allowedIndicesProp != null)
            {
                for (int i = 0; i < allowedIndicesProp.arraySize; i++)
                {
                    int vertexIndex = allowedIndicesProp.GetArrayElementAtIndex(i).intValue;
                    map[vertexIndex] = gpIndex;
                }
            }
        }

        return map;
    }

    private Color GetGrabPointColor(int index)
    {
        if (index < 0 || index >= grabPointColors.Length)
            return unassignedColor;
        return grabPointColors[index];
    }

    private void ToggleVertexAssignment(int vertexIndex, int grabPointIndex)
    {
        serializedObject.Update();

        // First, remove from all grab points
        bool wasAssigned = false;
        for (int gpIndex = 0; gpIndex < grabPointsProp.arraySize; gpIndex++)
        {
            var gpProp = grabPointsProp.GetArrayElementAtIndex(gpIndex);
            var allowedIndicesProp = gpProp.FindPropertyRelative("allowedVertexIndices");

            if (allowedIndicesProp != null)
            {
                for (int i = allowedIndicesProp.arraySize - 1; i >= 0; i--)
                {
                    if (allowedIndicesProp.GetArrayElementAtIndex(i).intValue == vertexIndex)
                    {
                        allowedIndicesProp.DeleteArrayElementAtIndex(i);
                        wasAssigned = true;
                    }
                }
            }
        }

        // If wasn't assigned, add to selected grab point
        if (!wasAssigned)
        {
            AddVertexToGrabPoint(vertexIndex, grabPointIndex);
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    private void AddVertexToGrabPoint(int vertexIndex, int grabPointIndex)
    {
        serializedObject.Update();

        if (grabPointIndex >= grabPointsProp.arraySize)
            return;

        var gpProp = grabPointsProp.GetArrayElementAtIndex(grabPointIndex);
        var allowedIndicesProp = gpProp.FindPropertyRelative("allowedVertexIndices");

        if (allowedIndicesProp != null)
        {
            // Check if already exists
            for (int i = 0; i < allowedIndicesProp.arraySize; i++)
            {
                if (allowedIndicesProp.GetArrayElementAtIndex(i).intValue == vertexIndex)
                    return; // Already assigned
            }

            // Add new index
            allowedIndicesProp.InsertArrayElementAtIndex(allowedIndicesProp.arraySize);
            allowedIndicesProp.GetArrayElementAtIndex(allowedIndicesProp.arraySize - 1).intValue = vertexIndex;
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    private void RemoveVertexFromGrabPoint(int vertexIndex, int grabPointIndex)
    {
        serializedObject.Update();

        if (grabPointIndex >= grabPointsProp.arraySize)
            return;

        var gpProp = grabPointsProp.GetArrayElementAtIndex(grabPointIndex);
        var allowedIndicesProp = gpProp.FindPropertyRelative("allowedVertexIndices");

        if (allowedIndicesProp != null)
        {
            for (int i = allowedIndicesProp.arraySize - 1; i >= 0; i--)
            {
                if (allowedIndicesProp.GetArrayElementAtIndex(i).intValue == vertexIndex)
                {
                    allowedIndicesProp.DeleteArrayElementAtIndex(i);
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    private void ClearGrabPointAssignments(int grabPointIndex)
    {
        serializedObject.Update();

        if (grabPointIndex >= grabPointsProp.arraySize)
            return;

        var gpProp = grabPointsProp.GetArrayElementAtIndex(grabPointIndex);
        var allowedIndicesProp = gpProp.FindPropertyRelative("allowedVertexIndices");

        if (allowedIndicesProp != null)
        {
            allowedIndicesProp.ClearArray();
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
        SceneView.RepaintAll();
    }

    private void ClearAllAssignments()
    {
        serializedObject.Update();

        for (int gpIndex = 0; gpIndex < grabPointsProp.arraySize; gpIndex++)
        {
            var gpProp = grabPointsProp.GetArrayElementAtIndex(gpIndex);
            var allowedIndicesProp = gpProp.FindPropertyRelative("allowedVertexIndices");

            if (allowedIndicesProp != null)
            {
                allowedIndicesProp.ClearArray();
            }
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
        SceneView.RepaintAll();
    }

    // ========== Persistence Methods ==========

    void LoadOrCreateAssignmentData()
    {
        // Try to load existing asset
        assignmentData = AssetDatabase.LoadAssetAtPath<VertexAssignmentData>(AssignmentDataPath);

        if (assignmentData == null)
        {
            // Create new asset
            assignmentData = ScriptableObject.CreateInstance<VertexAssignmentData>();

            // Ensure directory exists
            string directory = Path.GetDirectoryName(AssignmentDataPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            AssetDatabase.CreateAsset(assignmentData, AssignmentDataPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[ClothVertexGrabberEditor] Created new VertexAssignmentData at {AssignmentDataPath}");
        }
        else
        {
            Debug.Log($"[ClothVertexGrabberEditor] Loaded VertexAssignmentData from {AssignmentDataPath}");
        }
    }

    void SaveAssignments()
    {
        if (assignmentData == null)
        {
            Debug.LogError("[ClothVertexGrabberEditor] Assignment data is null!");
            return;
        }

        assignmentData.SaveFromGrabber(grabber);
        AssetDatabase.SaveAssets();

        Debug.Log($"[ClothVertexGrabberEditor] Saved vertex assignments. Total assigned vertices: {assignmentData.totalAssignedVertices}");
        EditorUtility.DisplayDialog("Save Complete",
            $"Vertex assignments saved successfully!\n\nTotal assigned vertices: {assignmentData.totalAssignedVertices}\nTime: {assignmentData.lastSavedTime}",
            "OK");

        Repaint();
    }

    void LoadAssignments()
    {
        if (assignmentData == null)
        {
            Debug.LogError("[ClothVertexGrabberEditor] Assignment data is null!");
            return;
        }

        assignmentData.LoadToGrabber(grabber);
        serializedObject.Update();

        Debug.Log($"[ClothVertexGrabberEditor] Loaded vertex assignments. Total assigned vertices: {assignmentData.totalAssignedVertices}");
        EditorUtility.DisplayDialog("Load Complete",
            $"Vertex assignments loaded successfully!\n\nTotal assigned vertices: {assignmentData.totalAssignedVertices}\nLast saved: {assignmentData.lastSavedTime}",
            "OK");

        Repaint();
        SceneView.RepaintAll();
    }

    void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        switch (state)
        {
            case PlayModeStateChange.ExitingPlayMode:
                // Auto-save when exiting play mode
                Debug.Log("[ClothVertexGrabberEditor] Exiting Play mode - Auto-saving vertex assignments...");
                SaveAssignments();
                break;

            case PlayModeStateChange.EnteredPlayMode:
                // Auto-load when entering play mode
                Debug.Log("[ClothVertexGrabberEditor] Entered Play mode - Auto-loading vertex assignments...");
                LoadAssignments();
                break;
        }
    }
}
