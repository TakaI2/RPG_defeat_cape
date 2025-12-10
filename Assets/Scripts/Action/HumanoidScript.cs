using UnityEngine;

/// <summary>
/// Humanoid character controller using UnityChanLocomotion animator.
/// Controls: WASD/Arrow keys for movement, Space for jump.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class HumanoidScript : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float forwardSpeed = 7f;
    [SerializeField] private float backwardSpeed = 2f;
    [SerializeField] private float rotateSpeed = 2f;
    [SerializeField] private float animationSpeed = 1.5f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpPower = 3f;
    [SerializeField] private float gravity = 20f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundLayer = ~0;

    // Components
    private Animator animator;
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;

    // Animator parameter hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int DirectionHash = Animator.StringToHash("Direction");
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    // State
    private float currentSpeed;
    private float currentDirection;
    private bool isGrounded;
    private Vector3 velocity;

    private void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        // Configure rigidbody
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Set animation speed
        animator.speed = animationSpeed;
    }

    private void Update()
    {
        // Check ground state
        CheckGrounded();

        // Get input
        float horizontal = Input.GetAxis("Horizontal"); // A/D or Left/Right
        float vertical = Input.GetAxis("Vertical");     // W/S or Up/Down

        // Store raw input for movement calculation
        currentSpeed = vertical;
        currentDirection = horizontal;

        // Calculate animator Speed parameter
        // BlendTree expects: 0 = idle, 0.8 = run (range 0 to 0.8)
        // Use absolute value so backward movement also plays walk/run animation
        float animSpeed = Mathf.Abs(vertical) * 0.8f;

        // Update animator parameters
        animator.SetFloat(SpeedHash, animSpeed);
        animator.SetFloat(DirectionHash, horizontal);

        // Handle jump
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
    }

    private void FixedUpdate()
    {
        // Calculate movement
        float moveSpeed = currentSpeed > 0 ? forwardSpeed : backwardSpeed;
        float targetSpeed = currentSpeed * moveSpeed;

        // Rotate character
        if (Mathf.Abs(currentDirection) > 0.1f)
        {
            float rotation = currentDirection * rotateSpeed * 100f * Time.fixedDeltaTime;
            transform.Rotate(0, rotation, 0);
        }

        // Move character
        if (Mathf.Abs(currentSpeed) > 0.1f)
        {
            Vector3 moveDirection = transform.forward * targetSpeed;
            rb.linearVelocity = new Vector3(moveDirection.x, rb.linearVelocity.y, moveDirection.z);
        }
        else
        {
            // Stop horizontal movement when no input
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    private void Jump()
    {
        // Trigger jump animation
        animator.SetTrigger(JumpHash);

        // Apply jump force
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpPower, rb.linearVelocity.z);
    }

    private void CheckGrounded()
    {
        float radius = capsuleCollider.radius * 0.9f;
        Vector3 origin = transform.position + Vector3.up * (capsuleCollider.radius);

        isGrounded = Physics.SphereCast(origin, radius, Vector3.down, out _, groundCheckDistance + capsuleCollider.radius, groundLayer);
    }

    private void OnDrawGizmosSelected()
    {
        if (capsuleCollider == null) return;

        // Visualize ground check
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 origin = transform.position + Vector3.up * capsuleCollider.radius;
        Gizmos.DrawWireSphere(origin + Vector3.down * (groundCheckDistance + capsuleCollider.radius * 0.1f), capsuleCollider.radius * 0.9f);
    }
}
