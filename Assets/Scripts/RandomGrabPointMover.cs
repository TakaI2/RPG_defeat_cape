using UnityEngine;

/// <summary>
/// Moves grab points randomly in all directions (forward/back/left/right/up/down)
/// for testing cloth behavior with multiple dynamic grab points
/// </summary>
public class RandomGrabPointMover : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2.0f;
    [SerializeField] private float moveRange = 1.5f;
    [SerializeField] private bool enableVerticalMovement = true;

    [Header("Random Pattern Settings")]
    [SerializeField] private float directionChangeInterval = 2.0f;
    [SerializeField] private bool usePerlinNoise = true;
    [SerializeField] private float perlinNoiseScale = 0.5f;

    [Header("Movement Constraints")]
    [SerializeField] private Vector3 minBounds = new Vector3(-5f, 0f, -5f);
    [SerializeField] private Vector3 maxBounds = new Vector3(5f, 3f, 5f);

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = Color.yellow;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float nextDirectionChangeTime;
    private Vector3 perlinOffset;

    void Start()
    {
        startPosition = transform.position;
        targetPosition = startPosition;

        // Random offset for Perlin noise to make each grab point unique
        perlinOffset = new Vector3(
            Random.Range(0f, 1000f),
            Random.Range(0f, 1000f),
            Random.Range(0f, 1000f)
        );

        SetNewRandomTarget();
    }

    void Update()
    {
        if (usePerlinNoise)
        {
            MoveWithPerlinNoise();
        }
        else
        {
            MoveToRandomTarget();
        }
    }

    void MoveWithPerlinNoise()
    {
        // Use Perlin noise for smooth, organic movement
        float time = Time.time * perlinNoiseScale;

        float xNoise = Mathf.PerlinNoise(time + perlinOffset.x, 0f) * 2f - 1f;
        float zNoise = Mathf.PerlinNoise(time + perlinOffset.z, 1000f) * 2f - 1f;
        float yNoise = enableVerticalMovement ?
            Mathf.PerlinNoise(time + perlinOffset.y, 2000f) * 2f - 1f : 0f;

        Vector3 offset = new Vector3(xNoise, yNoise, zNoise) * moveRange;
        targetPosition = startPosition + offset;

        // Clamp to bounds
        targetPosition = ClampToBounds(targetPosition);

        // Smooth movement
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );
    }

    void MoveToRandomTarget()
    {
        // Move to target
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        // Check if we reached the target or it's time to change direction
        if (Time.time >= nextDirectionChangeTime ||
            Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            SetNewRandomTarget();
        }
    }

    void SetNewRandomTarget()
    {
        // Generate random direction
        Vector3 randomDirection = new Vector3(
            Random.Range(-1f, 1f),
            enableVerticalMovement ? Random.Range(-0.5f, 1f) : 0f,
            Random.Range(-1f, 1f)
        ).normalized;

        // Calculate new target position
        targetPosition = startPosition + randomDirection * Random.Range(moveRange * 0.5f, moveRange);

        // Clamp to bounds
        targetPosition = ClampToBounds(targetPosition);

        // Set next direction change time
        nextDirectionChangeTime = Time.time + directionChangeInterval + Random.Range(-0.5f, 0.5f);
    }

    Vector3 ClampToBounds(Vector3 position)
    {
        return new Vector3(
            Mathf.Clamp(position.x, minBounds.x, maxBounds.x),
            Mathf.Clamp(position.y, minBounds.y, maxBounds.y),
            Mathf.Clamp(position.z, minBounds.z, maxBounds.z)
        );
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Vector3 center = Application.isPlaying ? startPosition : transform.position;

        // Draw movement range sphere
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.2f);
        Gizmos.DrawWireSphere(center, moveRange);

        // Draw bounds box
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.5f);
        Vector3 boundsCenter = (minBounds + maxBounds) / 2f;
        Vector3 boundsSize = maxBounds - minBounds;
        Gizmos.DrawWireCube(boundsCenter, boundsSize);

        // Draw current target
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition, 0.1f);
            Gizmos.DrawLine(transform.position, targetPosition);
        }
    }

    void OnDisable()
    {
        // Return to start position when disabled
        if (Application.isPlaying)
        {
            transform.position = startPosition;
        }
    }
}
