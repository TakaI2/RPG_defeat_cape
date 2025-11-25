using UnityEngine;

/// <summary>
/// Sample script demonstrating how to control ClothVertexGrabber from animations and other scripts
///
/// Usage Examples:
///
/// 1. Animation Events:
///    - Add this script to the character GameObject
///    - In the Animation window, add an Animation Event at the desired frame
///    - Select the method (e.g., "OnGrabCape", "OnReleaseCape")
///    - The grab will be triggered at that specific frame
///
/// 2. Timeline:
///    - Add Animation Track to Timeline
///    - Add Animation Event markers
///    - Call methods from this script
///
/// 3. From Other Scripts:
///    ```csharp
///    ClothGrabController controller = GetComponent<ClothGrabController>();
///    controller.GrabCapeWithRightHand();
///    ```
///
/// 4. Direct ClothVertexGrabber Control:
///    ```csharp
///    ClothVertexGrabber grabber = GetComponent<ClothVertexGrabber>();
///    grabber.StartGrabbingAtPoint(0);  // Start grabbing at grab point index 0
///    grabber.StopGrabbingAtPoint("GrabPoint1");  // Stop grabbing at grab point named "GrabPoint1"
///    ```
/// </summary>
public class ClothGrabController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ClothVertexGrabber clothGrabber;

    [Header("Grab Point Configuration")]
    [Tooltip("Grab point index for right hand (default: 0)")]
    [SerializeField] private int rightHandGrabPointIndex = 0;

    [Tooltip("Grab point index for left hand (default: 1)")]
    [SerializeField] private int leftHandGrabPointIndex = 1;

    [Tooltip("Grab point name for specific grab (optional)")]
    [SerializeField] private string customGrabPointName = "GrabPoint1";

    void Start()
    {
        // Auto-find ClothVertexGrabber if not assigned
        if (clothGrabber == null)
        {
            clothGrabber = GetComponent<ClothVertexGrabber>();
            if (clothGrabber == null)
            {
                // Try to find in scene
                clothGrabber = FindObjectOfType<ClothVertexGrabber>();
            }

            if (clothGrabber == null)
            {
                Debug.LogError("[ClothGrabController] ClothVertexGrabber not found!");
            }
        }
    }

    #region Animation Event Methods

    /// <summary>
    /// Called from Animation Event: Grab cape with right hand
    /// </summary>
    public void OnGrabCapeWithRightHand()
    {
        if (clothGrabber == null)
        {
            Debug.LogWarning("[ClothGrabController] ClothVertexGrabber is not assigned!");
            return;
        }

        Debug.Log($"[ClothGrabController] Animation Event: Grabbing cape with right hand (index: {rightHandGrabPointIndex})");
        clothGrabber.StartGrabbingAtPoint(rightHandGrabPointIndex);
    }

    /// <summary>
    /// Called from Animation Event: Release cape from right hand
    /// </summary>
    public void OnReleaseCapeFromRightHand()
    {
        if (clothGrabber == null)
        {
            Debug.LogWarning("[ClothGrabController] ClothVertexGrabber is not assigned!");
            return;
        }

        Debug.Log($"[ClothGrabController] Animation Event: Releasing cape from right hand (index: {rightHandGrabPointIndex})");
        clothGrabber.StopGrabbingAtPoint(rightHandGrabPointIndex);
    }

    /// <summary>
    /// Called from Animation Event: Grab cape with left hand
    /// </summary>
    public void OnGrabCapeWithLeftHand()
    {
        if (clothGrabber == null)
        {
            Debug.LogWarning("[ClothGrabController] ClothVertexGrabber is not assigned!");
            return;
        }

        Debug.Log($"[ClothGrabController] Animation Event: Grabbing cape with left hand (index: {leftHandGrabPointIndex})");
        clothGrabber.StartGrabbingAtPoint(leftHandGrabPointIndex);
    }

    /// <summary>
    /// Called from Animation Event: Release cape from left hand
    /// </summary>
    public void OnReleaseCapeFromLeftHand()
    {
        if (clothGrabber == null)
        {
            Debug.LogWarning("[ClothGrabController] ClothVertexGrabber is not assigned!");
            return;
        }

        Debug.Log($"[ClothGrabController] Animation Event: Releasing cape from left hand (index: {leftHandGrabPointIndex})");
        clothGrabber.StopGrabbingAtPoint(leftHandGrabPointIndex);
    }

    /// <summary>
    /// Called from Animation Event: Toggle grab at right hand
    /// </summary>
    public void OnToggleRightHandGrab()
    {
        if (clothGrabber == null)
        {
            Debug.LogWarning("[ClothGrabController] ClothVertexGrabber is not assigned!");
            return;
        }

        Debug.Log($"[ClothGrabController] Animation Event: Toggling right hand grab (index: {rightHandGrabPointIndex})");
        clothGrabber.ToggleGrabbingAtPoint(rightHandGrabPointIndex);
    }

    /// <summary>
    /// Called from Animation Event: Grab by name (uses customGrabPointName)
    /// </summary>
    public void OnGrabByName()
    {
        if (clothGrabber == null)
        {
            Debug.LogWarning("[ClothGrabController] ClothVertexGrabber is not assigned!");
            return;
        }

        Debug.Log($"[ClothGrabController] Animation Event: Grabbing at '{customGrabPointName}'");
        clothGrabber.StartGrabbingAtPoint(customGrabPointName);
    }

    /// <summary>
    /// Called from Animation Event: Release by name (uses customGrabPointName)
    /// </summary>
    public void OnReleaseByName()
    {
        if (clothGrabber == null)
        {
            Debug.LogWarning("[ClothGrabController] ClothVertexGrabber is not assigned!");
            return;
        }

        Debug.Log($"[ClothGrabController] Animation Event: Releasing at '{customGrabPointName}'");
        clothGrabber.StopGrabbingAtPoint(customGrabPointName);
    }

    #endregion

    #region Public Methods (for other scripts)

    /// <summary>
    /// Grab cape with right hand (can be called from other scripts)
    /// </summary>
    public void GrabCapeWithRightHand()
    {
        OnGrabCapeWithRightHand();
    }

    /// <summary>
    /// Release cape from right hand (can be called from other scripts)
    /// </summary>
    public void ReleaseCapeFromRightHand()
    {
        OnReleaseCapeFromRightHand();
    }

    /// <summary>
    /// Grab cape with left hand (can be called from other scripts)
    /// </summary>
    public void GrabCapeWithLeftHand()
    {
        OnGrabCapeWithLeftHand();
    }

    /// <summary>
    /// Release cape from left hand (can be called from other scripts)
    /// </summary>
    public void ReleaseCapeFromLeftHand()
    {
        OnReleaseCapeFromLeftHand();
    }

    /// <summary>
    /// Grab at specific grab point index
    /// </summary>
    /// <param name="grabPointIndex">Index of the grab point</param>
    public void GrabAtPoint(int grabPointIndex)
    {
        if (clothGrabber == null)
        {
            Debug.LogWarning("[ClothGrabController] ClothVertexGrabber is not assigned!");
            return;
        }

        clothGrabber.StartGrabbingAtPoint(grabPointIndex);
    }

    /// <summary>
    /// Release at specific grab point index
    /// </summary>
    /// <param name="grabPointIndex">Index of the grab point</param>
    public void ReleaseAtPoint(int grabPointIndex)
    {
        if (clothGrabber == null)
        {
            Debug.LogWarning("[ClothGrabController] ClothVertexGrabber is not assigned!");
            return;
        }

        clothGrabber.StopGrabbingAtPoint(grabPointIndex);
    }

    /// <summary>
    /// Grab at specific grab point by name
    /// </summary>
    /// <param name="grabPointName">Name of the grab point</param>
    public void GrabAtPoint(string grabPointName)
    {
        if (clothGrabber == null)
        {
            Debug.LogWarning("[ClothGrabController] ClothVertexGrabber is not assigned!");
            return;
        }

        clothGrabber.StartGrabbingAtPoint(grabPointName);
    }

    /// <summary>
    /// Release at specific grab point by name
    /// </summary>
    /// <param name="grabPointName">Name of the grab point</param>
    public void ReleaseAtPoint(string grabPointName)
    {
        if (clothGrabber == null)
        {
            Debug.LogWarning("[ClothGrabController] ClothVertexGrabber is not assigned!");
            return;
        }

        clothGrabber.StopGrabbingAtPoint(grabPointName);
    }

    /// <summary>
    /// Check if grabbing at specific point
    /// </summary>
    /// <param name="grabPointIndex">Index of the grab point</param>
    /// <returns>True if grabbing, false otherwise</returns>
    public bool IsGrabbing(int grabPointIndex)
    {
        if (clothGrabber == null)
            return false;

        return clothGrabber.IsGrabbingAtPoint(grabPointIndex);
    }

    /// <summary>
    /// Release all grab points
    /// </summary>
    public void ReleaseAll()
    {
        if (clothGrabber == null)
        {
            Debug.LogWarning("[ClothGrabController] ClothVertexGrabber is not assigned!");
            return;
        }

        var grabPoints = clothGrabber.GetGrabPoints();
        for (int i = 0; i < grabPoints.Length; i++)
        {
            if (clothGrabber.IsGrabbingAtPoint(i))
            {
                clothGrabber.StopGrabbingAtPoint(i);
            }
        }
    }

    #endregion

    #region Example Usage in Code

    // Example: Call from Update or other methods
    void ExampleUsage()
    {
        // Example 1: Grab when pressing a key
        if (Input.GetKeyDown(KeyCode.G))
        {
            GrabCapeWithRightHand();
        }

        // Example 2: Release when releasing a key
        if (Input.GetKeyUp(KeyCode.G))
        {
            ReleaseCapeFromRightHand();
        }

        // Example 3: Check if grabbing
        if (IsGrabbing(rightHandGrabPointIndex))
        {
            Debug.Log("Right hand is grabbing the cape");
        }
    }

    #endregion
}
