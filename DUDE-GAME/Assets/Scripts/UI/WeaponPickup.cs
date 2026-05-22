using FishNet;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class WeaponPickup : MonoBehaviour
{
    // Enable the GRENADE_DEBUG scripting define to inspect pickup adoption flows.

    public string weaponName;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    public int savedClipAmmo = -1;
    public int savedReserveAmmo = -1;

    [Header("Throw Settings")]
    public float throwSpeed = 10f;
    public float minDamageSpeed = 4f;
    public int damageOnHit = 10;

    private bool hasBeenThrown = false;

    private Collider2D physicsCollider;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

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
        RefreshSprite();

        Sequence anim = DOTween.Sequence();
        anim.Append(transform.DORotate(new Vector3(0, 0, 360), 0.5f, RotateMode.FastBeyond360).SetEase(Ease.OutCubic));
        anim.Join(transform.DOScale(1.5f, 0.25f).SetEase(Ease.OutBack))
            .Append(transform.DOScale(1f, 0.1f).SetEase(Ease.InBack));
    }

    private void Update()
    {
        if (GameSession.IsOnline && !InstanceFinder.IsServerStarted) return;
        if (physicsCollider == null) return;

        float speed = rb.linearVelocity.magnitude;
        if (speed <= minDamageSpeed && physicsCollider.enabled)
            physicsCollider.enabled = false;
        else if (speed > minDamageSpeed && !physicsCollider.enabled)
            physicsCollider.enabled = true;
    }


    public void SetWeapon(string name)
    {
        weaponName = name;
        RefreshSprite();

#if GRENADE_DEBUG
        Debug.Log($"[PICKUP] WeaponPickup {gameObject.name} SetWeapon weaponName={weaponName} clip={savedClipAmmo} reserve={savedReserveAmmo}");
#endif
    }

    public void RefreshSprite()
    {
        if (spriteRenderer == null) return;
        Sprite weaponSprite = Resources.Load<Sprite>("WeaponIcons/" + weaponName);
        if (weaponSprite != null)
            spriteRenderer.sprite = weaponSprite;
        else
            Debug.LogWarning($"Sprite for weapon '{weaponName}' not found in Resources/WeaponIcons/");
    }

    public void Throw(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
        {
            AudioManager.Instance.PlaySound(FMODEvents.Instance.Throw, transform.position);
            rb.linearVelocity = direction.normalized * throwSpeed;
            rb.AddTorque(Random.Range(-100f, 100f));
            hasBeenThrown = true;
        }
        else
        {
            AudioManager.Instance.PlaySound(FMODEvents.Instance.Reload, transform.position);
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.Sleep();
            if (physicsCollider != null) physicsCollider.enabled = false;
            hasBeenThrown = false;
        }
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (GameSession.IsOnline && !InstanceFinder.IsServerStarted) return;
        if (!hasBeenThrown) return;

        float impactSpeed = collision.relativeVelocity.magnitude;
        hasBeenThrown = false;

        if (impactSpeed < minDamageSpeed) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (GameSession.IsOnline)
            {
                var netCtrl = collision.collider.GetComponentInParent<NetworkPlayerController>();
                if (netCtrl != null) netCtrl.ServerTakeDamage(damageOnHit);
            }
            else
            {
                var target = collision.collider.GetComponent<PlayerStats>();
                if (target != null) target.TakeDamage(damageOnHit);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (GameSession.IsOnline && !InstanceFinder.IsServerStarted) return;

        var grenade = other.GetComponentInParent<Grenade>();
        if (grenade != null) return;

        GunHolder gunHolder = other.GetComponent<GunHolder>();
        if (gunHolder != null)
        {
            gunHolder.SetNearbyPickup(this);
#if GRENADE_DEBUG
            Debug.Log($"[PICKUP] Taking WeaponPickup {name} holder={gunHolder.name} weapon={weaponName} clip={savedClipAmmo} reserve={savedReserveAmmo}");
#endif
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (GameSession.IsOnline && !InstanceFinder.IsServerStarted) return;

        var grenade = other.GetComponentInParent<Grenade>();
        if (grenade != null) return;

        GunHolder gunHolder = other.GetComponent<GunHolder>();
        if (gunHolder != null)
        {
            gunHolder.ClearNearbyPickup(this);
#if GRENADE_DEBUG
            Debug.Log($"[PICKUP] {name} exit trigger holder={gunHolder.name} weapon={weaponName}");
#endif
        }
    }
}
