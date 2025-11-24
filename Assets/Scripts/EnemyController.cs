using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    private Transform _target;
    private System.Action<EnemyController> _onDeath;

    public void Initialize(Transform target, System.Action<EnemyController> onDeath)
    {
        _target = target;
        _onDeath = onDeath;
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (_target == null) return;

        // Simple move towards target
        Vector3 direction = (_target.position - transform.position).normalized;
        direction.y = 0; // Keep on ground plane
        transform.position += direction * speed * Time.deltaTime;
        
        // Face target
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    public void Die()
    {
        _onDeath?.Invoke(this);
    }
}
