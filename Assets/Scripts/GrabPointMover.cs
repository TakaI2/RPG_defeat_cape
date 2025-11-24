using UnityEngine;

/// <summary>
/// Moves the grab point up and down when space key is held
/// </summary>
public class GrabPointMover : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 1.0f;
    [SerializeField] private float moveDistance = 2.0f;

    private Vector3 startPosition;
    private bool isMoving = false;
    private float moveTime = 0f;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            if (!isMoving)
            {
                isMoving = true;
                moveTime = 0f;
            }

            // Move up and down in a sine wave pattern
            moveTime += Time.deltaTime * moveSpeed;
            float yOffset = Mathf.Sin(moveTime * Mathf.PI) * moveDistance;
            transform.position = startPosition + Vector3.up * yOffset;
        }
        else
        {
            if (isMoving)
            {
                isMoving = false;
                // Return to start position
                transform.position = startPosition;
            }
        }
    }

    void OnDestroy()
    {
        // Reset position when destroyed
        if (startPosition != Vector3.zero)
        {
            transform.position = startPosition;
        }
    }
}
