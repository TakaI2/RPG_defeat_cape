using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody _rb;
    private Camera _mainCamera;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        MoveAndRotate();
    }

    private void MoveAndRotate()
    {
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            Vector3 targetPosition = hit.point;
            targetPosition.y = transform.position.y; // Keep same height

            // Rotate
            transform.LookAt(targetPosition);

            // Move
            Vector3 direction = (targetPosition - transform.position).normalized;
            
            // Stop if close to cursor to prevent jitter
            if (Vector3.Distance(transform.position, targetPosition) > 0.5f)
            {
                _rb.MovePosition(_rb.position + direction * moveSpeed * Time.deltaTime);
            }
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            GameManager.Instance.EndGame();
        }
    }
}
