using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class WeaponPickup : MonoBehaviour
{
    // Enable the GRENADE_DEBUG scripting define to inspect pickup adoption flows.
    public string weaponName;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    //public GameObject weaponPrefab;

    // Estas variables guardan el estado de munición
    public int savedClipAmmo = -1;
    public int savedReserveAmmo = -1;

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
        if (speed <= minDamageSpeed && physicsCollider.enabled)
        {
            physicsCollider.enabled = false;
        }
        else if (speed > minDamageSpeed && !physicsCollider.enabled)
        {
            physicsCollider.enabled = true;
        }
    }


    public void SetWeapon(string name)
    {
        weaponName = name;
        UpdateSprite();

#if GRENADE_DEBUG
        // DEBUG:
        Debug.Log($"[PICKUP] WeaponPickup {gameObject.name} SetWeapon weaponName={weaponName} clip={savedClipAmmo} reserve={savedReserveAmmo}");
#endif
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
            SoundFXManager.instance.PlaySoundByName("Throw", transform, 0.5f, 1f, false);
            rb.linearVelocity = direction.normalized * throwSpeed;
            rb.AddTorque(Random.Range(-100f, 100f)); // adds some spin
            hasBeenThrown = true;
        }
        else
        {
            SoundFXManager.instance.PlaySoundByName("Reload", transform, 0.5f, 1f, false);
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
    private void OnTriggerEnter2D(Collider2D other)
    {
        var grenade = other.GetComponentInParent<Grenade>();
        if (grenade != null)
        {
#if GRENADE_DEBUG
            // DEBUG:
            Debug.Log($"[PICKUP] IGNORE live grenade id={grenade.Id} state={grenade.CurrentState} fuseLeft={grenade.FuseLeft:F2} def={grenade.Definition?.name ?? "null"}");
#endif
            return;
        }

        GunHolder gunHolder = other.GetComponent<GunHolder>();

        if (gunHolder != null)
        {
            gunHolder.SetNearbyPickup(this);
#if GRENADE_DEBUG
        // DEBUG:
            Debug.Log($"[PICKUP] Taking WeaponPickup {name} holder={gunHolder.name} weapon={weaponName} clip={savedClipAmmo} reserve={savedReserveAmmo}");
#endif
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var grenade = other.GetComponentInParent<Grenade>();
        if (grenade != null)
        {
#if GRENADE_DEBUG
            // DEBUG:
            Debug.Log($"[PICKUP] IGNORE exit for live grenade id={grenade.Id} state={grenade.CurrentState}");
#endif
            return;
        }

        GunHolder gunHolder = other.GetComponent<GunHolder>();

        if (gunHolder != null)
        {
            gunHolder.ClearNearbyPickup(this);
#if GRENADE_DEBUG
            // DEBUG:
            Debug.Log($"[PICKUP] {name} exit trigger holder={gunHolder.name} weapon={weaponName}");
#endif
        }
    }


}



