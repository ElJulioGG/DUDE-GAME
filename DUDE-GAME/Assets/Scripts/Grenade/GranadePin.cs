using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GranadePin : MonoBehaviour
{
    [SerializeField] private float minForce = 2f;
    [SerializeField] private float maxForce = 5f;
    [SerializeField] private float minTorque = -300f;
    [SerializeField] private float maxTorque = 300f;

    [SerializeField] private float settleThreshold = 0.05f;

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private bool _settled;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float force = Random.Range(minForce, maxForce);
        _rb.AddForce(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * force, ForceMode2D.Impulse);
        _rb.AddTorque(Random.Range(minTorque, maxTorque));
    }
    private void FixedUpdate()
    {
        if (GameManager.instance.destroyProyectiles)
        {
            Destroy(gameObject);
            return;
        }

        if (!_settled && _rb.linearVelocity.magnitude < settleThreshold)
        {
            _settled = true;
            if (_sr != null) _sr.sortingOrder -= 4;
        }
    }
}
