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

        [Header("Neighbor Vertices")]
        [Tooltip("Include neighboring vertices when grabbing to reduce vibration")]
        public bool includeNeighborVertices = true;

        [Tooltip("How many layers of neighbors to include (1=direct neighbors, 2=neighbors of neighbors, etc.)")]
        [Range(1, 3)]
        public int neighborDepth = 1;

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

    // Cache for vertex connectivity (built from mesh triangles)
    private Dictionary<int, HashSet<int>> vertexConnectivity = null;

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

        Debug.Log($"[ClothVertexGrabber] Initialized - MagicaCloth: {magicaCloth.name}");
        Debug.Log($"[ClothVertexGrabber] {grabPoints.Length} grab points configured");
        Debug.Log($"[ClothVertexGrabber] ClothTransform: {magicaCloth.ClothTransform.name}");

        // Build vertex connectivity for neighbor grabbing
        BuildVertexConnectivity();
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

        // Use the CURRENT ClothTransform coordinate system
        Vector3 grabPointLocalPos = WorldToClothLocal(grabPoint.transform.position);

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

        // Use the CURRENT ClothTransform coordinate system for vertex operations
        Vector3 grabPointWorldPos = grabPoint.transform.position;
        Vector3 grabPointLocalPos = WorldToClothLocal(grabPointWorldPos);

        Debug.Log($"[StartGrabbing] {grabPoint.name}:");
        Debug.Log($"  GrabPoint World: {grabPointWorldPos}");
        Debug.Log($"  GrabPoint Local (Current ClothTransform): {grabPointLocalPos}");
        Debug.Log($"  ClothTransform World: {magicaCloth.ClothTransform.position}");

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

                // Debug: Log first 3 vertices
                if (candidates.Count <= 3)
                {
                    Vector3 vertexWorldPos = ClothLocalToWorld(vertexPos);
                    Debug.Log($"    Vertex {i}: Local: {vertexPos}, World: {vertexWorldPos}, Distance: {distance:F3}");
                }
            }
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"[ClothVertexGrabber] {grabPoint.name}: No grabbable vertices found!");
            return;
        }

        Debug.Log($"  Found {candidates.Count} candidate vertices");

        // Sort by distance and select closest vertices
        candidates.Sort((a, b) => a.distance.CompareTo(b.distance));

        int numToGrab = Mathf.Min(grabPoint.maxGrabbedVertices, candidates.Count);

        // Collect vertex indices to grab (including neighbors if enabled)
        HashSet<int> verticesToGrab = new HashSet<int>();

        // Add the closest vertices
        for (int i = 0; i < numToGrab; i++)
        {
            verticesToGrab.Add(candidates[i].index);
        }

        // Add neighboring vertices if enabled
        if (grabPoint.includeNeighborVertices && vertexConnectivity != null)
        {
            HashSet<int> coreVertices = new HashSet<int>(verticesToGrab);

            foreach (int vertexIndex in coreVertices)
            {
                HashSet<int> neighbors = GetNeighborVertices(vertexIndex, grabPoint.neighborDepth);

                foreach (int neighborIndex in neighbors)
                {
                    // Check if neighbor is allowed to be grabbed
                    var attr = attributes[neighborIndex];
                    if (attr.IsMove() && grabPoint.CanGrabVertex(neighborIndex))
                    {
                        verticesToGrab.Add(neighborIndex);
                    }
                }
            }

            Debug.Log($"  Including neighbors: {coreVertices.Count} core vertices -> {verticesToGrab.Count} total vertices (depth: {grabPoint.neighborDepth})");
        }

        int[] grabbedIndices = new int[verticesToGrab.Count];
        VertexAttribute[] originalAttrs = new VertexAttribute[verticesToGrab.Count];

        Debug.Log($"  Grabbing {verticesToGrab.Count} vertices (core: {numToGrab}, neighbors: {verticesToGrab.Count - numToGrab}):");

        // Change vertices to Fixed attribute
        int idx = 0;
        foreach (int vertexIndex in verticesToGrab)
        {
            int particleIdx = tdata.particleChunk.startIndex + vertexIndex;
            Vector3 vertexLocalPos = basePosArray[particleIdx];
            Vector3 vertexWorldPos = ClothLocalToWorld(vertexLocalPos);

            grabbedIndices[idx] = vertexIndex;
            originalAttrs[idx] = attributes[vertexIndex];
            attributes[vertexIndex] = VertexAttribute.Fixed;

            // Only log first few for brevity
            if (idx < 5)
            {
                Debug.Log($"    [{idx}] Vertex {vertexIndex}: Local: {vertexLocalPos}, World: {vertexWorldPos}");
            }
            idx++;
        }

        if (verticesToGrab.Count > 5)
        {
            Debug.Log($"    ... and {verticesToGrab.Count - 5} more vertices");
        }

        grabPoint.StartGrab(grabbedIndices, originalAttrs);
        Debug.Log($"[ClothVertexGrabber] {grabPoint.name} grabbed {verticesToGrab.Count} total vertices ({numToGrab} core + {verticesToGrab.Count - numToGrab} neighbors)");
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

            Vector3 grabPointWorldPos = grabPoint.transform.position;

            // Use the CURRENT ClothTransform coordinate system
            Vector3 grabPointLocalPos = WorldToClothLocal(grabPointWorldPos);

            // Debug log (first grab point only, every 30 frames to avoid spam)
            if (grabPoint == grabPoints[0] && Time.frameCount % 30 == 0)
            {
                // Check actual vertex position in basePosArray for comparison
                if (grabPoint.GrabbedVertexIndices != null && grabPoint.GrabbedVertexIndices.Length > 0)
                {
                    int firstVertexIdx = grabPoint.GrabbedVertexIndices[0];
                    int particleIdx = tdata.particleChunk.startIndex + firstVertexIdx;
                    Vector3 currentVertexLocal = basePosArray[particleIdx];
                    Vector3 currentVertexWorld = ClothLocalToWorld(currentVertexLocal);

                    Debug.Log($"[DEBUG] ========== Coordinate Debug ==========");
                    Debug.Log($"  GrabPoint World: {grabPointWorldPos}");
                    Debug.Log($"  GrabPoint -> ClothLocal: {grabPointLocalPos}");
                    Debug.Log($"  Vertex[{firstVertexIdx}] Local (before update): {currentVertexLocal}");
                    Debug.Log($"  Vertex[{firstVertexIdx}] World (before update): {currentVertexWorld}");
                    Debug.Log($"  ClothTransform.position: {magicaCloth.ClothTransform.position}");
                    Debug.Log($"  ClothTransform.rotation: {magicaCloth.ClothTransform.rotation.eulerAngles}");
                    Debug.Log($"  Distance (World): {Vector3.Distance(grabPointWorldPos, currentVertexWorld):F4}");
                    Debug.Log($"  Distance (Local): {Vector3.Distance(grabPointLocalPos, currentVertexLocal):F4}");
                }
            }

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

            // Use the CURRENT ClothTransform coordinate system
            Vector3 grabPointLocalPos = WorldToClothLocal(grabPoint.transform.position);

            foreach (int vertexIndex in grabPoint.GrabbedVertexIndices)
            {
                int particleIndex = tdata.particleChunk.startIndex + vertexIndex;

                dispPosArray[particleIndex] = grabPointLocalPos;
                positions[vertexIndex] = grabPointLocalPos;
            }
        }
    }

    /// <summary>
    /// Convert world position to MagicaCloth2's internal coordinate system
    /// Testing: MagicaCloth2 may use world coordinates directly
    /// </summary>
    Vector3 WorldToClothLocal(Vector3 worldPos)
    {
        // MagicaCloth2 seems to use world coordinates in its internal arrays
        return worldPos;
    }

    /// <summary>
    /// Convert from MagicaCloth2's internal coordinate system to world position
    /// </summary>
    Vector3 ClothLocalToWorld(Vector3 localPos)
    {
        // MagicaCloth2 seems to use world coordinates in its internal arrays
        return localPos;
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
                        // Convert from cloth local space to world space
                        Vector3 worldPos = ClothLocalToWorld(localPos);

                        Gizmos.DrawSphere(worldPos, 0.03f);
                        Gizmos.DrawLine(worldPos, grabPoint.transform.position);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Get grab points array (for editor access)
    /// </summary>
    public GrabPointInfo[] GetGrabPoints()
    {
        return grabPoints;
    }

    #region Vertex Connectivity

    /// <summary>
    /// Build vertex connectivity graph from mesh triangles
    /// This allows us to find neighboring vertices
    /// </summary>
    void BuildVertexConnectivity()
    {
        if (!magicaCloth.IsValid())
            return;

        vertexConnectivity = new Dictionary<int, HashSet<int>>();

        // Get mesh from the cloth object
        Mesh mesh = null;

        // Try to get mesh from SkinnedMeshRenderer
        var skinnedMeshRenderer = magicaCloth.GetComponent<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer != null)
        {
            mesh = skinnedMeshRenderer.sharedMesh;
        }
        else
        {
            // Try to get mesh from MeshFilter
            var meshFilter = magicaCloth.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                mesh = meshFilter.sharedMesh;
            }
        }

        if (mesh != null)
        {
            int[] triangles = mesh.triangles;

            // Build connectivity from triangles
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v0 = triangles[i];
                int v1 = triangles[i + 1];
                int v2 = triangles[i + 2];

                // Add bidirectional connections
                AddConnection(v0, v1);
                AddConnection(v0, v2);
                AddConnection(v1, v0);
                AddConnection(v1, v2);
                AddConnection(v2, v0);
                AddConnection(v2, v1);
            }

            Debug.Log($"[ClothVertexGrabber] Built vertex connectivity: {vertexConnectivity.Count} vertices from {triangles.Length / 3} triangles");
        }
        else
        {
            Debug.LogWarning("[ClothVertexGrabber] Could not find mesh for building vertex connectivity");
        }
    }

    /// <summary>
    /// Add a connection between two vertices
    /// </summary>
    void AddConnection(int vertexA, int vertexB)
    {
        if (!vertexConnectivity.ContainsKey(vertexA))
        {
            vertexConnectivity[vertexA] = new HashSet<int>();
        }
        vertexConnectivity[vertexA].Add(vertexB);
    }

    /// <summary>
    /// Get neighboring vertices up to specified depth
    /// </summary>
    /// <param name="vertexIndex">Starting vertex index</param>
    /// <param name="depth">How many layers of neighbors (1=direct neighbors, 2=neighbors of neighbors, etc.)</param>
    /// <returns>HashSet of neighbor vertex indices</returns>
    HashSet<int> GetNeighborVertices(int vertexIndex, int depth)
    {
        if (vertexConnectivity == null || depth <= 0)
            return new HashSet<int>();

        HashSet<int> neighbors = new HashSet<int>();
        HashSet<int> currentLayer = new HashSet<int> { vertexIndex };
        HashSet<int> visited = new HashSet<int> { vertexIndex };

        for (int d = 0; d < depth; d++)
        {
            HashSet<int> nextLayer = new HashSet<int>();

            foreach (int vertex in currentLayer)
            {
                if (vertexConnectivity.ContainsKey(vertex))
                {
                    foreach (int neighbor in vertexConnectivity[vertex])
                    {
                        if (!visited.Contains(neighbor))
                        {
                            neighbors.Add(neighbor);
                            nextLayer.Add(neighbor);
                            visited.Add(neighbor);
                        }
                    }
                }
            }

            currentLayer = nextLayer;
        }

        return neighbors;
    }

    #endregion

    #region Public API for External Control

    /// <summary>
    /// Start grabbing at the specified grab point index
    /// Can be called from animations, timeline, or other scripts
    /// </summary>
    /// <param name="grabPointIndex">Index of the grab point (0-based)</param>
    public void StartGrabbingAtPoint(int grabPointIndex)
    {
        if (grabPointIndex < 0 || grabPointIndex >= grabPoints.Length)
        {
            Debug.LogWarning($"[ClothVertexGrabber] Invalid grab point index: {grabPointIndex}");
            return;
        }

        var grabPoint = grabPoints[grabPointIndex];
        if (grabPoint == null)
        {
            Debug.LogWarning($"[ClothVertexGrabber] Grab point at index {grabPointIndex} is null");
            return;
        }

        if (grabPoint.IsGrabbing)
        {
            Debug.LogWarning($"[ClothVertexGrabber] {grabPoint.name} is already grabbing");
            return;
        }

        StartGrabbing(grabPoint);
    }

    /// <summary>
    /// Stop grabbing at the specified grab point index
    /// Can be called from animations, timeline, or other scripts
    /// </summary>
    /// <param name="grabPointIndex">Index of the grab point (0-based)</param>
    public void StopGrabbingAtPoint(int grabPointIndex)
    {
        if (grabPointIndex < 0 || grabPointIndex >= grabPoints.Length)
        {
            Debug.LogWarning($"[ClothVertexGrabber] Invalid grab point index: {grabPointIndex}");
            return;
        }

        var grabPoint = grabPoints[grabPointIndex];
        if (grabPoint == null)
        {
            Debug.LogWarning($"[ClothVertexGrabber] Grab point at index {grabPointIndex} is null");
            return;
        }

        if (!grabPoint.IsGrabbing)
        {
            Debug.LogWarning($"[ClothVertexGrabber] {grabPoint.name} is not currently grabbing");
            return;
        }

        StopGrabbing(grabPoint);
    }

    /// <summary>
    /// Start grabbing at the grab point with the specified name
    /// Can be called from animations, timeline, or other scripts
    /// </summary>
    /// <param name="grabPointName">Name of the grab point</param>
    public void StartGrabbingAtPoint(string grabPointName)
    {
        var grabPoint = FindGrabPointByName(grabPointName);
        if (grabPoint == null)
        {
            Debug.LogWarning($"[ClothVertexGrabber] Grab point '{grabPointName}' not found");
            return;
        }

        if (grabPoint.IsGrabbing)
        {
            Debug.LogWarning($"[ClothVertexGrabber] {grabPoint.name} is already grabbing");
            return;
        }

        StartGrabbing(grabPoint);
    }

    /// <summary>
    /// Stop grabbing at the grab point with the specified name
    /// Can be called from animations, timeline, or other scripts
    /// </summary>
    /// <param name="grabPointName">Name of the grab point</param>
    public void StopGrabbingAtPoint(string grabPointName)
    {
        var grabPoint = FindGrabPointByName(grabPointName);
        if (grabPoint == null)
        {
            Debug.LogWarning($"[ClothVertexGrabber] Grab point '{grabPointName}' not found");
            return;
        }

        if (!grabPoint.IsGrabbing)
        {
            Debug.LogWarning($"[ClothVertexGrabber] {grabPoint.name} is not currently grabbing");
            return;
        }

        StopGrabbing(grabPoint);
    }

    /// <summary>
    /// Toggle grabbing at the specified grab point index
    /// Useful for Animation Events
    /// </summary>
    /// <param name="grabPointIndex">Index of the grab point (0-based)</param>
    public void ToggleGrabbingAtPoint(int grabPointIndex)
    {
        if (grabPointIndex < 0 || grabPointIndex >= grabPoints.Length)
        {
            Debug.LogWarning($"[ClothVertexGrabber] Invalid grab point index: {grabPointIndex}");
            return;
        }

        var grabPoint = grabPoints[grabPointIndex];
        if (grabPoint == null)
        {
            Debug.LogWarning($"[ClothVertexGrabber] Grab point at index {grabPointIndex} is null");
            return;
        }

        if (grabPoint.IsGrabbing)
        {
            StopGrabbing(grabPoint);
        }
        else
        {
            StartGrabbing(grabPoint);
        }
    }

    /// <summary>
    /// Toggle grabbing at the grab point with the specified name
    /// Useful for Animation Events
    /// </summary>
    /// <param name="grabPointName">Name of the grab point</param>
    public void ToggleGrabbingAtPoint(string grabPointName)
    {
        var grabPoint = FindGrabPointByName(grabPointName);
        if (grabPoint == null)
        {
            Debug.LogWarning($"[ClothVertexGrabber] Grab point '{grabPointName}' not found");
            return;
        }

        if (grabPoint.IsGrabbing)
        {
            StopGrabbing(grabPoint);
        }
        else
        {
            StartGrabbing(grabPoint);
        }
    }

    /// <summary>
    /// Check if a grab point is currently grabbing
    /// </summary>
    /// <param name="grabPointIndex">Index of the grab point (0-based)</param>
    /// <returns>True if grabbing, false otherwise</returns>
    public bool IsGrabbingAtPoint(int grabPointIndex)
    {
        if (grabPointIndex < 0 || grabPointIndex >= grabPoints.Length)
        {
            return false;
        }

        var grabPoint = grabPoints[grabPointIndex];
        return grabPoint != null && grabPoint.IsGrabbing;
    }

    /// <summary>
    /// Check if a grab point is currently grabbing
    /// </summary>
    /// <param name="grabPointName">Name of the grab point</param>
    /// <returns>True if grabbing, false otherwise</returns>
    public bool IsGrabbingAtPoint(string grabPointName)
    {
        var grabPoint = FindGrabPointByName(grabPointName);
        return grabPoint != null && grabPoint.IsGrabbing;
    }

    /// <summary>
    /// Find grab point by name
    /// </summary>
    private GrabPointInfo FindGrabPointByName(string name)
    {
        foreach (var grabPoint in grabPoints)
        {
            if (grabPoint != null && grabPoint.name == name)
            {
                return grabPoint;
            }
        }
        return null;
    }

    #endregion
}
