using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject to persist vertex assignment data
/// Stores which vertices are assigned to which grab points
/// </summary>
[System.Serializable]
public class GrabPointAssignment
{
    public string grabPointName;
    public List<int> assignedVertexIndices = new List<int>();
}

[CreateAssetMenu(fileName = "VertexAssignmentData", menuName = "Cloth/Vertex Assignment Data")]
public class VertexAssignmentData : ScriptableObject
{
    [Header("Assignment Data")]
    public List<GrabPointAssignment> grabPointAssignments = new List<GrabPointAssignment>();

    [Header("Metadata")]
    public string lastSavedTime;
    public int totalAssignedVertices;

    /// <summary>
    /// Save current vertex assignments from ClothVertexGrabber
    /// </summary>
    public void SaveFromGrabber(ClothVertexGrabber grabber)
    {
        grabPointAssignments.Clear();

        var grabPoints = grabber.GetGrabPoints();
        if (grabPoints == null) return;

        foreach (var gp in grabPoints)
        {
            if (gp == null) continue;

            var assignment = new GrabPointAssignment
            {
                grabPointName = gp.name,
                assignedVertexIndices = new List<int>(gp.allowedVertexIndices)
            };

            grabPointAssignments.Add(assignment);
        }

        lastSavedTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        totalAssignedVertices = CalculateTotalAssignedVertices();

        // Mark dirty to ensure it's saved
        UnityEditor.EditorUtility.SetDirty(this);
    }

    /// <summary>
    /// Load vertex assignments into ClothVertexGrabber
    /// </summary>
    public void LoadToGrabber(ClothVertexGrabber grabber)
    {
        var grabPoints = grabber.GetGrabPoints();
        if (grabPoints == null) return;

        for (int i = 0; i < grabPointAssignments.Count && i < grabPoints.Length; i++)
        {
            if (grabPoints[i] == null) continue;

            var assignment = grabPointAssignments[i];

            // Clear existing assignments
            grabPoints[i].allowedVertexIndices.Clear();

            // Load saved assignments
            grabPoints[i].allowedVertexIndices.AddRange(assignment.assignedVertexIndices);
        }

        UnityEditor.EditorUtility.SetDirty(grabber);
    }

    /// <summary>
    /// Clear all assignments
    /// </summary>
    public void Clear()
    {
        grabPointAssignments.Clear();
        lastSavedTime = "";
        totalAssignedVertices = 0;
        UnityEditor.EditorUtility.SetDirty(this);
    }

    private int CalculateTotalAssignedVertices()
    {
        HashSet<int> uniqueVertices = new HashSet<int>();
        foreach (var assignment in grabPointAssignments)
        {
            foreach (var vertexIndex in assignment.assignedVertexIndices)
            {
                uniqueVertices.Add(vertexIndex);
            }
        }
        return uniqueVertices.Count;
    }
}
