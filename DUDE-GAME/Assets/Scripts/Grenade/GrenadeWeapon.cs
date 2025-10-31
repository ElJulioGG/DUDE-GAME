using System.Collections;
using UnityEngine;

public class GrenadeWeapon : WeaponBase
{
    // Enable the GRENADE_DEBUG scripting define to surface grenade lifecycle diagnostics.
    [Header("Grenade Setup")]
    [SerializeField] private Grenade grenadePrefab;
    [SerializeField] private GrenadeDefinition definition;
    [SerializeField] private Transform handPoint;
    [SerializeField, Range(0f, 1f)] private float throwCharge01 = 0f;

    [Header("Visual (arma)")]
    [SerializeField] private SpriteRenderer weaponBodyRenderer;
    [SerializeField] private bool forceGrenadeOnTop = true;

    private Grenade cooking;
    private GunHolder holder;

    private bool consumedInHandThisFrame = false;
    public bool WasConsumedInHand => consumedInHandThisFrame;

    private IEnumerator ClearConsumedFlagEndOfFrame()
    {
        yield return null; // al final del frame
        consumedInHandThisFrame = false;
    }

    public bool HasCooking => cooking != null;

    private void Awake()
    {
        holder = GetComponentInParent<GunHolder>();
        if (!weaponBodyRenderer) weaponBodyRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        reserveAmmo = 0; // las granadas no recargan
        if (weaponBodyRenderer) weaponBodyRenderer.enabled = true;
    }

    private Vector2 GetAimDir()
    {
        if (firePoint != null) return firePoint.right.normalized;
        return transform.right.normalized;
    }

    public override void Shoot()
    {
        if (cooking != null) return; 

        if (currentClipAmmo <= 0 || grenadePrefab == null || definition == null)
            return;

        Transform parent = handPoint != null ? handPoint : (firePoint != null ? firePoint : transform);
        cooking = Instantiate(grenadePrefab, parent.position, Quaternion.identity, parent);
        cooking.Init(definition, GetOwnerIndexSafe());
        cooking.SetOwner(this);
        cooking.Arm();                 

        if (weaponBodyRenderer) weaponBodyRenderer.enabled = false;

        if (forceGrenadeOnTop)
        {
            var gsr = cooking.GetComponentInChildren<SpriteRenderer>();
            if (gsr && weaponBodyRenderer)
            {
                gsr.sortingLayerID = weaponBodyRenderer.sortingLayerID;
                gsr.sortingOrder = weaponBodyRenderer.sortingOrder + 1;
            }
        }

#if GRENADE_DEBUG
        // DEBUG:
        Debug.Log($"[GRENADE] Weapon {name} Shoot spawned {cooking.name} state={cooking.CurrentState} fuseLeft={cooking.FuseLeft:F2} def={definition?.name ?? "null"} owner={GetOwnerIndexSafe()}");
#endif
    }

    public bool TryThrowCooked(Vector2 dir)
    {
        if (cooking == null) return false;

        var thrown = cooking;

        cooking.Throw(dir, throwCharge01);
        cooking = null;

        if (weaponBodyRenderer) weaponBodyRenderer.enabled = true;

        currentClipAmmo = Mathf.Max(0, currentClipAmmo - 1);
        if (currentClipAmmo <= 0)
            StartCoroutine(AutoDestroyThisWeaponNextFrame());

#if GRENADE_DEBUG
        // DEBUG:
        Debug.Log($"[GRENADE] Weapon {name} TryThrowCooked threw {thrown.name} state={thrown.CurrentState} fuseLeft={thrown.FuseLeft:F2} def={thrown.Definition?.name ?? "null"}");
#endif

        return true;
    }


    private IEnumerator AutoDestroyThisWeaponNextFrame()
    {
        yield return null; 
        if (holder != null) holder.DestroyCurrentWeapon();
        else Destroy(gameObject);
    }

    private void OnDisable()
    {
        if (cooking != null)
        {
            cooking.transform.SetParent(null, true);
        }

        if (weaponBodyRenderer) weaponBodyRenderer.enabled = true;
    }

    private void OnDestroy()
    {
        if (cooking != null)
        {
            cooking.transform.SetParent(null, true);
        }

        if (weaponBodyRenderer) weaponBodyRenderer.enabled = true;
    }

    private int GetOwnerIndexSafe()
    {
        if (holder != null) return holder.GetPlayerIndex();
        var ps = GetComponentInParent<PlayerStats>();
        return ps != null ? ps.GetPlayerIndex() : -1;
    }

    public void NotifyHeldGrenadeExploded(Grenade g)
    {
        if (cooking == g)
        {
            cooking = null;

            consumedInHandThisFrame = true;
            StartCoroutine(ClearConsumedFlagEndOfFrame());

            if (weaponBodyRenderer) weaponBodyRenderer.enabled = true;

            currentClipAmmo = Mathf.Max(0, currentClipAmmo - 1);
            if (currentClipAmmo <= 0)
                StartCoroutine(AutoDestroyThisWeaponNextFrame());
        }
    }

    public bool TryPickupExisting(Grenade g)
    {
        if (g == null) return false;
        if (cooking != null) return false; 

        Transform parent = handPoint != null ? handPoint : (firePoint != null ? firePoint : transform);

#if GRENADE_DEBUG
        // DEBUG:
        Debug.Log($"[GRENADE] Weapon {name} TryPickupExisting target={g.name} state={g.CurrentState} fuseLeft={g.FuseLeft:F2} ticking={g.IsTicking} def={g.Definition?.name ?? "null"} owner={g.OwnerIndex}");
#endif

        if (g.Definition != null && g.Definition != definition)
        {
#if GRENADE_DEBUG
            // DEBUG:
            Debug.Log($"[GRENADE] Weapon {name} adopting definition {g.Definition.name} (previous={definition?.name ?? "null"})");
#endif
            definition = g.Definition;
        }

        g.AttachToHand(this, parent);
        cooking = g;

        if (weaponBodyRenderer) weaponBodyRenderer.enabled = false;

#if GRENADE_DEBUG
        // DEBUG:
        Debug.Log($"[GRENADE] Weapon {name} Adopted {g.name} state={g.CurrentState} fuseLeft={g.FuseLeft:F2} def={g.Definition?.name ?? "null"}");
#endif

        return true;
    }


}


