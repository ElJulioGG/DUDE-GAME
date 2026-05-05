using UnityEngine;

public class BlackHoleTriggerRelay : MonoBehaviour
{
    [SerializeField] private bool isCenter;
    private BlackHoleEntity _parent;

    void Awake()
    {
        _parent = GetComponentInParent<BlackHoleEntity>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (isCenter)
        {
            _parent.KillPlayer(other);
        }
        else
        {
            if (other.TryGetComponent<Rigidbody2D>(out var rb))
                _parent.RegisterPlayer(rb);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (isCenter || !other.CompareTag("Player")) return;
        if (other.TryGetComponent<Rigidbody2D>(out var rb))
            _parent.UnregisterPlayer(rb);
    }
}
