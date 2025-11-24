using UnityEngine;
using MagicaCloth2;

/// <summary>
/// Grabs cloth using MagicaCloth2 by sandwiching vertices between two sphere colliders
/// </summary>
public class ClothGrabber : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject clothPlainObject;
    [SerializeField] private Transform grabPoint;

    [Header("Grab Settings")]
    [SerializeField] private float colliderRadius = 0.3f;
    [SerializeField] private float colliderSeparation = 0.1f;
    [SerializeField] private float searchRadius = 5.0f;

    private MagicaCloth magicaClothComponent;
    private Renderer clothRenderer;
    private GameObject collider1Object;
    private GameObject collider2Object;
    private MagicaSphereCollider collider1;
    private MagicaSphereCollider collider2;
    private bool isGrabbing = false;

void Start()
    {
        // Auto-find objects if not assigned
        if (clothPlainObject == null)
        {
            clothPlainObject = GameObject.Find("cape2");
            if (clothPlainObject == null)
            {
                Debug.LogError("cape2 object not found in scene!");
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
                Debug.LogError("grabpoint object not found in scene!");
                return;
            }
        }

        // Find MagicaCloth component (it's on the child object)
        magicaClothComponent = clothPlainObject.GetComponentInChildren<MagicaCloth>();

        // Find the Plane child object which has the renderer
        Transform planeTransform = clothPlainObject.transform.Find("Plane");
        if (planeTransform != null)
        {
            clothRenderer = planeTransform.GetComponent<Renderer>();
        }

        if (magicaClothComponent == null)
        {
            Debug.LogError("MagicaCloth component not found on cape2 or its children!");
        }
        if (clothRenderer == null)
        {
            Debug.LogError("Renderer component not found on Plane child object!");
        }

        Debug.Log($"ClothGrabber initialized successfully! MagicaCloth: {magicaClothComponent != null}, Renderer: {clothRenderer != null}");
    }

    void Update()
    {
        if (magicaClothComponent == null || grabPoint == null || clothRenderer == null)
            return;

        // Space key pressed - grab cloth
        if (Input.GetKeyDown(KeyCode.Space) && !isGrabbing)
        {
            GrabCloth();
        }
        // Space key released - release cloth
        else if (Input.GetKeyUp(KeyCode.Space) && isGrabbing)
        {
            ReleaseCloth();
        }

        // Update collider positions while grabbing
        if (isGrabbing && collider1Object != null && collider2Object != null)
        {
            UpdateColliderPositions();
        }
    }

    private void GrabCloth()
    {
        // Get the mesh from the renderer
        Mesh mesh = null;
        Transform meshTransform = clothRenderer.transform;

        if (clothRenderer is SkinnedMeshRenderer skinnedMeshRenderer)
        {
            mesh = new Mesh();
            skinnedMeshRenderer.BakeMesh(mesh);
        }
        else if (clothRenderer is MeshRenderer)
        {
            MeshFilter meshFilter = clothRenderer.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                mesh = meshFilter.mesh;
            }
        }

        if (mesh == null)
        {
            Debug.LogError("Could not get mesh from renderer!");
            return;
        }

        // Find the closest vertex to grabPoint
        Vector3[] vertices = mesh.vertices;
        float closestDistance = float.MaxValue;
        Vector3 closestVertex = Vector3.zero;
        bool foundVertex = false;

        Debug.Log($"Searching for vertex near grabPoint at {grabPoint.position}, searchRadius: {searchRadius}, total vertices: {vertices.Length}");

        for (int i = 0; i < vertices.Length; i++)
        {
            // Transform vertex to world space using the mesh's transform (Plane object)
            Vector3 worldVertex = meshTransform.TransformPoint(vertices[i]);
            float distance = Vector3.Distance(worldVertex, grabPoint.position);

            // Track absolute closest for debugging
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestVertex = worldVertex;
            }

            if (distance < searchRadius)
            {
                foundVertex = true;
            }
        }

        Debug.Log($"Closest vertex distance: {closestDistance}, within searchRadius: {foundVertex}");

        if (!foundVertex)
        {
            Debug.LogWarning($"No vertex found within search radius of {searchRadius}! Closest was {closestDistance} units away.");
            return;
        }

        Debug.Log($"Found vertex at distance: {closestDistance}, position: {closestVertex}");

        // Create two sphere colliders at the grabbed position
        CreateColliderPair(closestVertex);

        isGrabbing = true;
    }

    private void CreateColliderPair(Vector3 grabPosition)
    {
        // Calculate direction (using grabPoint's up direction)
        Vector3 offset = grabPoint.up * (colliderSeparation / 2f);

        // Create first collider (above)
        collider1Object = new GameObject("GrabCollider_1");
        collider1Object.transform.position = grabPosition + offset;
        collider1Object.transform.SetParent(grabPoint);
        collider1 = collider1Object.AddComponent<MagicaSphereCollider>();
        collider1.SetSize(colliderRadius);
        collider1.center = Vector3.zero;

        // Create second collider (below)
        collider2Object = new GameObject("GrabCollider_2");
        collider2Object.transform.position = grabPosition - offset;
        collider2Object.transform.SetParent(grabPoint);
        collider2 = collider2Object.AddComponent<MagicaSphereCollider>();
        collider2.SetSize(colliderRadius);
        collider2.center = Vector3.zero;

        // Ensure collision mode is enabled
        var sdata = magicaClothComponent.SerializeData;
        if (sdata.colliderCollisionConstraint.mode == ColliderCollisionConstraint.Mode.None)
        {
            Debug.LogWarning("Collider Collision mode was None, setting to Point mode");
            sdata.colliderCollisionConstraint.mode = ColliderCollisionConstraint.Mode.Point;
        }

        // Add colliders to MagicaCloth's collider list
        sdata.colliderCollisionConstraint.colliderList.Add(collider1);
        sdata.colliderCollisionConstraint.colliderList.Add(collider2);

        // Update collider parameters
        collider1.UpdateParameters();
        collider2.UpdateParameters();

        // Notify MagicaCloth of parameter changes
        magicaClothComponent.SetParameterChange();

        Debug.Log($"Collider pair created at {grabPosition}, Mode: {sdata.colliderCollisionConstraint.mode}, Radius: {colliderRadius}");
    }

    private void UpdateColliderPositions()
    {
        // Colliders are parented to grabPoint, so they move automatically
        // This method is here in case we need additional position updates
    }

    private void ReleaseCloth()
    {
        if (collider1 == null || collider2 == null)
            return;

        // Remove colliders from MagicaCloth's collider list
        var sdata = magicaClothComponent.SerializeData;
        sdata.colliderCollisionConstraint.colliderList.Remove(collider1);
        sdata.colliderCollisionConstraint.colliderList.Remove(collider2);

        // Notify MagicaCloth of parameter changes
        magicaClothComponent.SetParameterChange();

        // Destroy collider GameObjects
        Destroy(collider1Object);
        Destroy(collider2Object);

        collider1 = null;
        collider2 = null;
        collider1Object = null;
        collider2Object = null;
        isGrabbing = false;

        Debug.Log("Cloth released");
    }

    void OnDestroy()
    {
        // Clean up if script is destroyed while grabbing
        if (isGrabbing)
        {
            ReleaseCloth();
        }
    }

    // Visualize grab point and search radius in editor
    void OnDrawGizmos()
    {
        if (grabPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(grabPoint.position, searchRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(grabPoint.position, 0.05f);
        }

        // Draw active colliders
        if (isGrabbing && collider1Object != null && collider2Object != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(collider1Object.transform.position, colliderRadius);
            Gizmos.DrawSphere(collider1Object.transform.position, colliderRadius * 0.5f);

            Gizmos.DrawWireSphere(collider2Object.transform.position, colliderRadius);
            Gizmos.DrawSphere(collider2Object.transform.position, colliderRadius * 0.5f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(collider1Object.transform.position, collider2Object.transform.position);
        }
    }
}
