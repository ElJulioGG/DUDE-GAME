using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MiniChickenRevenge : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private int damage = 8;
    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float turnSpeed = 8f;

    private Transform _target;
    private Rigidbody2D _rb;
    private SpriteRenderer _sprite;
    private Vector2 _moveDir;
    private float _destroyTime;

    public void SetTarget(Transform target)
    {
        _target = target;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        _sprite = GetComponentInChildren<SpriteRenderer>();
        _moveDir = Random.insideUnitCircle.normalized;
    }

    private void OnEnable()
    {
        _destroyTime = Time.time + lifetime;
    }

    private void FixedUpdate()
    {
        if (Time.time >= _destroyTime)
        {
            Destroy(gameObject);
            return;
        }

        // Homing toward target
        if (_target != null)
        {
            Vector2 toTarget = ((Vector2)_target.position - _rb.position).normalized;
            _moveDir = Vector2.Lerp(_moveDir, toTarget, turnSpeed * Time.fixedDeltaTime).normalized;
        }

        _rb.MovePosition(_rb.position + _moveDir * speed * Time.fixedDeltaTime);

        // Rotate sprite in movement direction
        if (_sprite != null)
        {
            float angle = Mathf.Atan2(_moveDir.y, _moveDir.x) * Mathf.Rad2Deg;
            _sprite.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var stats = other.GetComponentInParent<PlayerStats>();
        if (stats != null && stats.playerAlive)
        {
            stats.TakeDamage(damage);
            stats.ApplyKnockback(_rb.position, knockbackForce);
        }

        Destroy(gameObject);
    }
}
