using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BulletBounce : MonoBehaviour
{
    private Rigidbody2D rb;
    [SerializeField] int life =10;

    private Vector2 direction;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Wall"))
        {
            life--;
            if (life < 0)
            {
                Destroy(gameObject);
                return;
            }
            var firstContactPoint = collision.contacts[0];
            //Vector2 newVelocity = Vector2.Reflect(Direction.normalized, firstContactPoint.normal);
            Vector2 inDirection = rb.linearVelocity;
            
        }
    }
}
