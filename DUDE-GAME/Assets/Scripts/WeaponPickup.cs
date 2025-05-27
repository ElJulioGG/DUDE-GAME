using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class WeaponPickup : MonoBehaviour
{
    public string weaponName;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;


    [Header("Throw Settings")]
    public float throwSpeed = 10f;
    public float minDamageSpeed = 4f;
    public int damageOnHit = 10;

    private bool hasBeenThrown = false;

    private Collider2D physicsCollider; // Non-trigger collider

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        // Find the non-trigger collider
        foreach (var c in GetComponents<Collider2D>())
        {
            if (!c.isTrigger)
            {
                physicsCollider = c;
                break;
            }
        }

        rb.gravityScale = 0f;
        rb.linearDamping = 3f;
        rb.angularDamping = 5f;
    }


    private void Start()
    {
        UpdateSprite();

        Sequence anim = DOTween.Sequence();
        anim.Append(transform.DORotate(new Vector3(0, 0, 360), 0.5f, RotateMode.FastBeyond360).SetEase(Ease.OutCubic));
        anim.Join(transform.DOScale(1.5f, 0.25f).SetEase(Ease.OutBack))
            .Append(transform.DOScale(1f, 0.1f).SetEase(Ease.InBack));
    }

    private void Update()
    {
        if (physicsCollider == null) return;

        float speed = rb.linearVelocity.magnitude;
        if (speed <= 0.01f && physicsCollider.enabled)
        {
            physicsCollider.enabled = false;
        }
        else if (speed > 0.01f && !physicsCollider.enabled)
        {
            physicsCollider.enabled = true;
        }
    }


    public void SetWeapon(string name)
    {
        weaponName = name;
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (spriteRenderer == null) return;

        Sprite weaponSprite = Resources.Load<Sprite>("WeaponIcons/" + weaponName);
        if (weaponSprite != null)
        {
            spriteRenderer.sprite = weaponSprite;
        }
        else
        {
            Debug.LogWarning($"Sprite for weapon '{weaponName}' not found in Resources/WeaponIcons/");
        }
    }

    public void Throw(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
        {
            rb.linearVelocity = direction.normalized * throwSpeed;
            rb.AddTorque(Random.Range(-100f, 100f)); // adds some spin
            hasBeenThrown = true;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.Sleep(); // freeze physics to avoid jitter
            if (physicsCollider != null)
            {
                physicsCollider.enabled = false;
            }
            hasBeenThrown = false;
        }
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!hasBeenThrown) return;

        float impactSpeed = collision.relativeVelocity.magnitude;

        // Stop being able to do damage after the first valid collision
        hasBeenThrown = false;

        if (impactSpeed < minDamageSpeed) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            var target = collision.collider.GetComponent<PlayerStats>();
            if (target != null)
            {
                target.TakeDamage(damageOnHit);
            }
        }
    }


}
