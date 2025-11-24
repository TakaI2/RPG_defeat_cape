using UnityEngine;
using MagicaCloth2;

/// <summary>
/// MagicaCloth2 Vertex Grabber
/// - Finds closest 1-2 vertices to grabPoint and temporarily changes them to Fixed attribute
/// - Fixed vertices are excluded from constraint calculations, preventing oscillation
/// - Uses OnPreSimulation to update positions and OnPostSimulation to force display position
/// - Restores original vertex attributes when released
/// </summary>
public class ClothVertexGrabber : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MagicaCloth magicaCloth;
    [SerializeField] private Transform grabPoint;

    [Header("Grab Control")]
    [SerializeField] private bool isGrabbing = false;
    [SerializeField] private int maxGrabbedVertices = 2;
    [SerializeField] private float grabSpeed = 10f; // 移動速度（高いほど速い）

    private bool isInitialized = false;
    private int teamId = -1;
    private int[] grabbedVertexIndices = null; // 掴んでいる頂点のインデックス
    private VertexAttribute[] originalAttributes = null; // 元の頂点属性を保存

    [Header("Direct Mesh Control")]
    [SerializeField] private bool useDirectMeshControl = true; // メッシュを直接制御

    void Start()
    {
        // Auto-find objects if not assigned
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

        if (grabPoint == null)
        {
            GameObject grabPointObj = GameObject.Find("grabpoint");
            if (grabPointObj != null)
            {
                grabPoint = grabPointObj.transform;
            }
            else
            {
                Debug.LogError("[ClothVertexGrabber] grabpoint object not found!");
                return;
            }
        }

        // Adjust constraints for vertex grabbing
        var sdata = magicaCloth.SerializeData;

        // Motion Constraint - 無効化（掴んだ頂点がgrabpointまで移動できるように）
        Debug.Log($"[ClothVertexGrabber] Motion Constraint - useMaxDistance: {sdata.motionConstraint.useMaxDistance}, useBackstop: {sdata.motionConstraint.useBackstop}");
        sdata.motionConstraint.useMaxDistance = false;
        sdata.motionConstraint.useBackstop = false;

        // Tether Constraint - 緩和（初期位置への引き戻しを弱める）
        Debug.Log($"[ClothVertexGrabber] Tether Constraint distanceCompression before: {sdata.tetherConstraint.distanceCompression}");
        sdata.tetherConstraint.distanceCompression = 0.0f; // 縮小を無効化

        // Distance Constraint - 有効のまま（隣接頂点が引っ張られるように）
        Debug.Log($"[ClothVertexGrabber] Distance Constraint stiffness: {sdata.distanceConstraint.stiffness} (keeping enabled for cloth pull)");
        // stiffnessは変更しない（デフォルトのまま）

        magicaCloth.SetParameterChange();

        Debug.Log($"[ClothVertexGrabber] Initialized - MagicaCloth: {magicaCloth.name}, GrabPoint: {grabPoint.name}");
        Debug.Log($"[ClothVertexGrabber] Constraints adjusted: Motion/Tether disabled, Distance enabled for cloth pull");
    }

    void OnEnable()
    {
        // Register to simulation events
        MagicaManager.OnPreSimulation += UpdateGrabbedVertex;
        MagicaManager.OnPostSimulation += ForceUpdateDisplayPosition;

        // Register to camera pre-render event for direct mesh control
        if (useDirectMeshControl)
        {
            Camera.onPreRender += OnCameraPreRender;
        }

        Debug.Log("[ClothVertexGrabber] Registered to OnPreSimulation, OnPostSimulation, and Camera.onPreRender events");
    }

    void OnDisable()
    {
        // Unregister from simulation events
        MagicaManager.OnPreSimulation -= UpdateGrabbedVertex;
        MagicaManager.OnPostSimulation -= ForceUpdateDisplayPosition;

        // Unregister from camera event
        Camera.onPreRender -= OnCameraPreRender;

        Debug.Log("[ClothVertexGrabber] Unregistered from all events");
    }

    void Update()
    {
        // Space key controls grabbing
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartGrabbing();
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            StopGrabbing();
        }
    }

    void OnCameraPreRender(Camera cam)
    {
        // レンダリング直前（MagicaCloth2のClothUpdate後）にメッシュを直接制御
        if (useDirectMeshControl && isGrabbing && grabbedVertexIndices != null && magicaCloth != null && magicaCloth.IsValid() && isInitialized)
        {
            DirectlyControlMeshVertices();
        }
    }

    void DirectlyControlMeshVertices()
    {
        // ProxyMeshの頂点を直接書き換え（レンダリング直前の最終調整）
        var proxyMesh = magicaCloth.Process.ProxyMeshContainer.shareVirtualMesh;
        var positions = proxyMesh.localPositions.GetNativeArray();

        Vector3 grabPointLocalPos = magicaCloth.ClothTransform.InverseTransformPoint(grabPoint.position);

        // 掴んだ頂点の位置を強制的に固定
        foreach (int vertexIndex in grabbedVertexIndices)
        {
            if (vertexIndex < positions.Length)
            {
                positions[vertexIndex] = grabPointLocalPos;
            }
        }

        // Debug output
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[ClothVertexGrabber] DirectlyControlMeshVertices: Forced {grabbedVertexIndices.Length} vertices to {grabPointLocalPos}");
        }
    }

    void StartGrabbing()
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

        // Find closest Move vertices to grabPoint
        var proxyMesh = magicaCloth.Process.ProxyMeshContainer.shareVirtualMesh;
        var attributes = proxyMesh.attributes.GetNativeArray();
        var localPositions = proxyMesh.localPositions.GetNativeArray();

        ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);
        var basePosArray = MagicaManager.Simulation.basePosArray;

        // grabpointのローカル座標
        Vector3 grabPointLocalPos = magicaCloth.ClothTransform.InverseTransformPoint(grabPoint.position);

        // 距離とインデックスのリスト
        var candidates = new System.Collections.Generic.List<(int index, float distance)>();

        int particleIndex = tdata.particleChunk.startIndex;
        for (int i = 0; i < tdata.particleChunk.dataLength; i++, particleIndex++)
        {
            var attr = attributes[i];
            if (attr.IsMove())
            {
                Vector3 vertexPos = basePosArray[particleIndex];
                float distance = Vector3.Distance(vertexPos, grabPointLocalPos);
                candidates.Add((i, distance));
            }
        }

        // 距離でソートして最も近い頂点を選択
        candidates.Sort((a, b) => a.distance.CompareTo(b.distance));

        int numToGrab = Mathf.Min(maxGrabbedVertices, candidates.Count);
        grabbedVertexIndices = new int[numToGrab];
        originalAttributes = new VertexAttribute[numToGrab];

        // 頂点を選択し、Fixed属性に変更（制約計算から除外）
        for (int i = 0; i < numToGrab; i++)
        {
            int vertexIndex = candidates[i].index;
            grabbedVertexIndices[i] = vertexIndex;

            // 元の属性を保存
            originalAttributes[i] = attributes[vertexIndex];

            // Fixed属性に変更（制約計算から除外される）
            attributes[vertexIndex] = VertexAttribute.Fixed;

            Debug.Log($"[ClothVertexGrabber] Vertex {vertexIndex} changed to Fixed attribute (was {originalAttributes[i]})");
        }

        // NativeArrayへの変更は自動的にExSimpleNativeArrayに反映される
        isGrabbing = true;
        Debug.Log($"[ClothVertexGrabber] Grabbing started - {numToGrab} vertices changed to Fixed attribute (excluded from constraints)");
    }

    void StopGrabbing()
    {
        // 元の属性に戻す
        if (grabbedVertexIndices != null && originalAttributes != null && magicaCloth != null && magicaCloth.IsValid())
        {
            var proxyMesh = magicaCloth.Process.ProxyMeshContainer.shareVirtualMesh;
            var attributes = proxyMesh.attributes.GetNativeArray();

            for (int i = 0; i < grabbedVertexIndices.Length; i++)
            {
                int vertexIndex = grabbedVertexIndices[i];
                VertexAttribute originalAttr = originalAttributes[i];

                // 元の属性に復元
                attributes[vertexIndex] = originalAttr;

                Debug.Log($"[ClothVertexGrabber] Vertex {vertexIndex} restored to {originalAttr} attribute");
            }

            // NativeArrayへの変更は自動的にExSimpleNativeArrayに反映される
        }

        isGrabbing = false;
        grabbedVertexIndices = null;
        originalAttributes = null;
        Debug.Log("[ClothVertexGrabber] Grabbing released - attributes restored");
    }

    void UpdateGrabbedVertex()
    {
        // Skip if not grabbing or not valid
        if (!isGrabbing || magicaCloth == null || grabPoint == null || grabbedVertexIndices == null)
        {
            return;
        }

        // Wait for MagicaCloth to be valid
        if (!magicaCloth.IsValid())
            return;

        // Initialize team ID on first valid frame
        if (!isInitialized)
        {
            var clothProcess = magicaCloth.Process;
            if (clothProcess == null)
                return;

            teamId = clothProcess.TeamId;
            isInitialized = true;
            Debug.Log($"[ClothVertexGrabber] Initialized with TeamId: {teamId}");
        }

        // Get team data
        ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);

        // Get particle arrays
        var basePosArray = MagicaManager.Simulation.basePosArray;
        var nextPosArray = MagicaManager.Simulation.nextPosArray;
        var oldPosArray = MagicaManager.Simulation.oldPosArray;
        var velocityArray = MagicaManager.Simulation.velocityArray;

        // Convert grabPoint world position to cloth local space
        Vector3 grabPointWorldPos = grabPoint.position;
        Vector3 grabPointLocalPos = magicaCloth.ClothTransform.InverseTransformPoint(grabPointWorldPos);

        // Update Fixed vertices position
        // Fixed属性なので制約計算から除外され、強制的にgrabpointに追従
        foreach (int vertexIndex in grabbedVertexIndices)
        {
            int particleIndex = tdata.particleChunk.startIndex + vertexIndex;

            // すべての位置配列を更新して振動を防ぐ
            basePosArray[particleIndex] = grabPointLocalPos;  // 基準位置
            nextPosArray[particleIndex] = grabPointLocalPos;  // 現在位置
            oldPosArray[particleIndex] = grabPointLocalPos;   // 前フレーム位置

            // 速度をゼロにして慣性による振動を防ぐ
            velocityArray[particleIndex] = Vector3.zero;
        }

        // Debug output
        if (Time.frameCount % 30 == 0)
        {
            Debug.Log($"[ClothVertexGrabber] Updating {grabbedVertexIndices.Length} Fixed vertices (no constraint conflict), target: {grabPointLocalPos}");
        }
    }

    void ForceUpdateDisplayPosition()
    {
        // OnPostSimulation: シミュレーション完了後、表示位置を強制的に更新
        if (!isGrabbing || grabbedVertexIndices == null || !magicaCloth.IsValid() || !isInitialized)
            return;

        ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);
        var dispPosArray = MagicaManager.Simulation.dispPosArray;

        // VirtualMeshのpositions配列も更新（補間後の最終的なメッシュ位置）
        var proxyMesh = magicaCloth.Process.ProxyMeshContainer.shareVirtualMesh;
        var positions = proxyMesh.localPositions.GetNativeArray();

        Vector3 grabPointLocalPos = magicaCloth.ClothTransform.InverseTransformPoint(grabPoint.position);

        foreach (int vertexIndex in grabbedVertexIndices)
        {
            int particleIndex = tdata.particleChunk.startIndex + vertexIndex;

            // dispPosArrayとpositions配列の両方を更新
            dispPosArray[particleIndex] = grabPointLocalPos;
            positions[vertexIndex] = grabPointLocalPos;
        }

        // Debug output
        if (Time.frameCount % 30 == 0)
        {
            Debug.Log($"[ClothVertexGrabber] ForceUpdateDisplayPosition: Updated dispPosArray and positions for {grabbedVertexIndices.Length} vertices to {grabPointLocalPos}, blendWeight={tdata.blendWeight}");
        }
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || grabPoint == null)
            return;

        // Draw grabPoint location
        Gizmos.color = isGrabbing ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(grabPoint.position, 0.1f);

        if (isGrabbing)
        {
            Gizmos.DrawSphere(grabPoint.position, 0.05f);

            // Draw grabbed vertices using dispPosArray (actual display position)
            if (grabbedVertexIndices != null && magicaCloth != null && magicaCloth.IsValid() && isInitialized)
            {
                ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);
                var dispPosArray = MagicaManager.Simulation.dispPosArray;

                Gizmos.color = Color.cyan;
                foreach (int vertexIndex in grabbedVertexIndices)
                {
                    int particleIndex = tdata.particleChunk.startIndex + vertexIndex;
                    Vector3 localPos = dispPosArray[particleIndex];
                    Vector3 worldPos = magicaCloth.ClothTransform.TransformPoint(localPos);

                    Gizmos.DrawSphere(worldPos, 0.03f);
                    Gizmos.DrawLine(worldPos, grabPoint.position);
                }
            }
        }
    }
}


