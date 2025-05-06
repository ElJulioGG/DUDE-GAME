using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BulletBounce : MonoBehaviour
{
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Wall"))
        {
            ContactPoint2D contact = collision.contacts[0];
            Vector2 inDirection = rb.linearVelocity;
            Vector2 normal = contact.normal;
            Vector2 reflectedVelocity = Vector2.Reflect(inDirection, normal);

            rb.linearVelocity = reflectedVelocity;

            // Optional: preserve speed
            rb.linearVelocity = reflectedVelocity.normalized * inDirection.magnitude;
        }
    }
}
