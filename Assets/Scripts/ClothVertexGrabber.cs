using UnityEngine;
using MagicaCloth2;
using System.Collections.Generic;

/// <summary>
/// MagicaCloth2 Multi-Point Vertex Grabber
/// - Supports multiple grab points with individual vertex constraints
/// - Each grab point can grab specific vertices defined by index ranges
/// - Fixed vertices are excluded from constraint calculations, preventing oscillation
/// - Uses OnPreSimulation, OnPostSimulation, and Camera.onPreRender for smooth grabbing
/// </summary>
public class ClothVertexGrabber : MonoBehaviour
{
    [System.Serializable]
    public class GrabPointInfo
    {
        [Header("Grab Point Settings")]
        public string name = "GrabPoint";
        public Transform transform;
        public KeyCode keyCode = KeyCode.Space;

        [Header("Vertex Constraints")]
        [Tooltip("Indices of vertices that this grab point can grab. Empty = can grab any vertex")]
        public List<int> allowedVertexIndices = new List<int>();

        [Header("Grab Settings")]
        public int maxGrabbedVertices = 2;

        [Header("Runtime State (Read Only)")]
        [SerializeField] private bool isGrabbing = false;
        [SerializeField] private int[] grabbedVertexIndices = null;
        [SerializeField] private VertexAttribute[] originalAttributes = null;

        public bool IsGrabbing => isGrabbing;
        public int[] GrabbedVertexIndices => grabbedVertexIndices;
        public VertexAttribute[] OriginalAttributes => originalAttributes;

        public void StartGrab(int[] indices, VertexAttribute[] attrs)
        {
            isGrabbing = true;
            grabbedVertexIndices = indices;
            originalAttributes = attrs;
        }

        public void StopGrab()
        {
            isGrabbing = false;
            grabbedVertexIndices = null;
            originalAttributes = null;
        }

        public bool CanGrabVertex(int vertexIndex)
        {
            // If allowedVertexIndices is empty, can grab any vertex
            if (allowedVertexIndices == null || allowedVertexIndices.Count == 0)
                return true;

            return allowedVertexIndices.Contains(vertexIndex);
        }
    }

    [Header("References")]
    [SerializeField] private MagicaCloth magicaCloth;

    [Header("Grab Points")]
    [SerializeField] private GrabPointInfo[] grabPoints = new GrabPointInfo[3];

    [Header("Direct Mesh Control")]
    [SerializeField] private bool useDirectMeshControl = true;

    private bool isInitialized = false;
    private int teamId = -1;

    void Start()
    {
        // Auto-find MagicaCloth if not assigned
        if (magicaCloth == null)
        {
            GameObject cape2 = GameObject.Find("cape2");
            if (cape2 != null)
            {
                magicaCloth = cape2.GetComponentInChildren<MagicaCloth>();
            }

            if (magicaCloth == null)
            {
                Debug.LogError("[ClothVertexGrabber] MagicaCloth component not found!");
                return;
            }
        }

        // Auto-find grab points if not assigned
        for (int i = 0; i < grabPoints.Length; i++)
        {
            if (grabPoints[i] == null)
            {
                grabPoints[i] = new GrabPointInfo();
            }

            if (grabPoints[i].transform == null)
            {
                string grabPointName = $"grabpoint{i + 1}";
                GameObject grabPointObj = GameObject.Find(grabPointName);
                if (grabPointObj != null)
                {
                    grabPoints[i].transform = grabPointObj.transform;
                    grabPoints[i].name = grabPointName;
                }
                else
                {
                    Debug.LogWarning($"[ClothVertexGrabber] {grabPointName} not found! Please assign manually.");
                }
            }
        }

        // Adjust constraints for vertex grabbing
        var sdata = magicaCloth.SerializeData;

        sdata.motionConstraint.useMaxDistance = false;
        sdata.motionConstraint.useBackstop = false;
        sdata.tetherConstraint.distanceCompression = 0.0f;

        magicaCloth.SetParameterChange();

        Debug.Log($"[ClothVertexGrabber] Initialized - MagicaCloth: {magicaCloth.name}");
        Debug.Log($"[ClothVertexGrabber] {grabPoints.Length} grab points configured");
    }

    void OnEnable()
    {
        // Register to simulation events
        MagicaManager.OnPreSimulation += UpdateGrabbedVertices;
        MagicaManager.OnPostSimulation += ForceUpdateDisplayPositions;

        // Register to camera pre-render event for direct mesh control
        if (useDirectMeshControl)
        {
            Camera.onPreRender += OnCameraPreRender;
        }

        Debug.Log("[ClothVertexGrabber] Registered to simulation and render events");
    }

    void OnDisable()
    {
        // Unregister from simulation events
        MagicaManager.OnPreSimulation -= UpdateGrabbedVertices;
        MagicaManager.OnPostSimulation -= ForceUpdateDisplayPositions;

        // Unregister from camera event
        Camera.onPreRender -= OnCameraPreRender;

        Debug.Log("[ClothVertexGrabber] Unregistered from all events");
    }

    void Update()
    {
        // Handle input for each grab point
        foreach (var grabPoint in grabPoints)
        {
            if (grabPoint == null || grabPoint.transform == null)
                continue;

            if (Input.GetKeyDown(grabPoint.keyCode))
            {
                StartGrabbing(grabPoint);
            }

            if (Input.GetKeyUp(grabPoint.keyCode))
            {
                StopGrabbing(grabPoint);
            }
        }
    }

    void OnCameraPreRender(Camera cam)
    {
        // Direct mesh control before rendering
        if (!useDirectMeshControl || !magicaCloth.IsValid() || !isInitialized)
            return;

        foreach (var grabPoint in grabPoints)
        {
            if (grabPoint != null && grabPoint.IsGrabbing)
            {
                DirectlyControlMeshVertices(grabPoint);
            }
        }
    }

    void DirectlyControlMeshVertices(GrabPointInfo grabPoint)
    {
        if (grabPoint.GrabbedVertexIndices == null)
            return;

        var proxyMesh = magicaCloth.Process.ProxyMeshContainer.shareVirtualMesh;
        var positions = proxyMesh.localPositions.GetNativeArray();

        Vector3 grabPointLocalPos = magicaCloth.ClothTransform.InverseTransformPoint(grabPoint.transform.position);

        foreach (int vertexIndex in grabPoint.GrabbedVertexIndices)
        {
            if (vertexIndex < positions.Length)
            {
                positions[vertexIndex] = grabPointLocalPos;
            }
        }
    }

    void StartGrabbing(GrabPointInfo grabPoint)
    {
        if (!magicaCloth.IsValid())
            return;

        // Initialize team ID if needed
        if (!isInitialized)
        {
            var clothProcess = magicaCloth.Process;
            if (clothProcess == null)
                return;

            teamId = clothProcess.TeamId;
            isInitialized = true;
            Debug.Log($"[ClothVertexGrabber] Initialized with TeamId: {teamId}");
        }

        // Find closest vertices that this grab point can grab
        var proxyMesh = magicaCloth.Process.ProxyMeshContainer.shareVirtualMesh;
        var attributes = proxyMesh.attributes.GetNativeArray();

        ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);
        var basePosArray = MagicaManager.Simulation.basePosArray;

        Vector3 grabPointLocalPos = magicaCloth.ClothTransform.InverseTransformPoint(grabPoint.transform.position);

        // Find candidate vertices
        var candidates = new List<(int index, float distance)>();

        int particleIndex = tdata.particleChunk.startIndex;
        for (int i = 0; i < tdata.particleChunk.dataLength; i++, particleIndex++)
        {
            var attr = attributes[i];
            if (attr.IsMove() && grabPoint.CanGrabVertex(i))
            {
                Vector3 vertexPos = basePosArray[particleIndex];
                float distance = Vector3.Distance(vertexPos, grabPointLocalPos);
                candidates.Add((i, distance));
            }
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"[ClothVertexGrabber] {grabPoint.name}: No grabbable vertices found!");
            return;
        }

        // Sort by distance and select closest vertices
        candidates.Sort((a, b) => a.distance.CompareTo(b.distance));

        int numToGrab = Mathf.Min(grabPoint.maxGrabbedVertices, candidates.Count);
        int[] grabbedIndices = new int[numToGrab];
        VertexAttribute[] originalAttrs = new VertexAttribute[numToGrab];

        // Change vertices to Fixed attribute
        for (int i = 0; i < numToGrab; i++)
        {
            int vertexIndex = candidates[i].index;
            grabbedIndices[i] = vertexIndex;
            originalAttrs[i] = attributes[vertexIndex];
            attributes[vertexIndex] = VertexAttribute.Fixed;
        }

        grabPoint.StartGrab(grabbedIndices, originalAttrs);
        Debug.Log($"[ClothVertexGrabber] {grabPoint.name} grabbed {numToGrab} vertices (key: {grabPoint.keyCode})");
    }

    void StopGrabbing(GrabPointInfo grabPoint)
    {
        if (!grabPoint.IsGrabbing || !magicaCloth.IsValid())
            return;

        // Restore original attributes
        var proxyMesh = magicaCloth.Process.ProxyMeshContainer.shareVirtualMesh;
        var attributes = proxyMesh.attributes.GetNativeArray();

        var indices = grabPoint.GrabbedVertexIndices;
        var originalAttrs = grabPoint.OriginalAttributes;

        for (int i = 0; i < indices.Length; i++)
        {
            attributes[indices[i]] = originalAttrs[i];
        }

        grabPoint.StopGrab();
        Debug.Log($"[ClothVertexGrabber] {grabPoint.name} released");
    }

    void UpdateGrabbedVertices()
    {
        if (!magicaCloth.IsValid() || !isInitialized)
            return;

        ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);
        var basePosArray = MagicaManager.Simulation.basePosArray;
        var nextPosArray = MagicaManager.Simulation.nextPosArray;
        var oldPosArray = MagicaManager.Simulation.oldPosArray;
        var velocityArray = MagicaManager.Simulation.velocityArray;

        foreach (var grabPoint in grabPoints)
        {
            if (grabPoint == null || !grabPoint.IsGrabbing || grabPoint.transform == null)
                continue;

            Vector3 grabPointLocalPos = magicaCloth.ClothTransform.InverseTransformPoint(grabPoint.transform.position);

            foreach (int vertexIndex in grabPoint.GrabbedVertexIndices)
            {
                int particleIndex = tdata.particleChunk.startIndex + vertexIndex;

                basePosArray[particleIndex] = grabPointLocalPos;
                nextPosArray[particleIndex] = grabPointLocalPos;
                oldPosArray[particleIndex] = grabPointLocalPos;
                velocityArray[particleIndex] = Vector3.zero;
            }
        }
    }

    void ForceUpdateDisplayPositions()
    {
        if (!magicaCloth.IsValid() || !isInitialized)
            return;

        ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);
        var dispPosArray = MagicaManager.Simulation.dispPosArray;

        var proxyMesh = magicaCloth.Process.ProxyMeshContainer.shareVirtualMesh;
        var positions = proxyMesh.localPositions.GetNativeArray();

        foreach (var grabPoint in grabPoints)
        {
            if (grabPoint == null || !grabPoint.IsGrabbing || grabPoint.transform == null)
                continue;

            Vector3 grabPointLocalPos = magicaCloth.ClothTransform.InverseTransformPoint(grabPoint.transform.position);

            foreach (int vertexIndex in grabPoint.GrabbedVertexIndices)
            {
                int particleIndex = tdata.particleChunk.startIndex + vertexIndex;

                dispPosArray[particleIndex] = grabPointLocalPos;
                positions[vertexIndex] = grabPointLocalPos;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        foreach (var grabPoint in grabPoints)
        {
            if (grabPoint == null || grabPoint.transform == null)
                continue;

            // Draw grab point location
            Gizmos.color = grabPoint.IsGrabbing ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(grabPoint.transform.position, 0.1f);

            if (grabPoint.IsGrabbing)
            {
                Gizmos.DrawSphere(grabPoint.transform.position, 0.05f);

                // Draw grabbed vertices
                if (grabPoint.GrabbedVertexIndices != null && magicaCloth != null && magicaCloth.IsValid() && isInitialized)
                {
                    ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);
                    var dispPosArray = MagicaManager.Simulation.dispPosArray;

                    Gizmos.color = Color.cyan;
                    foreach (int vertexIndex in grabPoint.GrabbedVertexIndices)
                    {
                        int particleIndex = tdata.particleChunk.startIndex + vertexIndex;
                        Vector3 localPos = dispPosArray[particleIndex];
                        Vector3 worldPos = magicaCloth.ClothTransform.TransformPoint(localPos);

                        Gizmos.DrawSphere(worldPos, 0.03f);
                        Gizmos.DrawLine(worldPos, grabPoint.transform.position);
                    }
                }
            }
        }
    }
}
