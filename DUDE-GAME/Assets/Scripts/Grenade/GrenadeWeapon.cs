using System.Collections;
using FishNet;
using UnityEngine;

public class GrenadeWeapon : WeaponBase
{
    // Enable the GRENADE_DEBUG scripting define to surface grenade lifecycle diagnostics.
    [Header("Grenade Setup")]
    [SerializeField] private Grenade grenadePrefab;
    [SerializeField] private GrenadeDefinition definition;
    [SerializeField] private Transform handPoint;
    [SerializeField, Range(0f, 1f)] private float throwCharge01 = 0f;
    [SerializeField] private GameObject grenadePinPrefab;

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
    public int OwnerPlayerIndex => GetOwnerIndexSafe();

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

        // Network-spawn so all remote clients see the grenade.
        if (GameSession.IsOnline && InstanceFinder.IsServerStarted)
            InstanceFinder.ServerManager.Spawn(cooking.gameObject);

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

        if (grenadePinPrefab != null) Instantiate(grenadePinPrefab, transform.position, transform.rotation);
        AudioManager.Instance.PlaySound(FMODEvents.Instance.GranadePin, transform.position);
//#if GRENADE_DEBUG
//        // DEBUG:
//        Debug.Log($"[GRENADE][WEAPON {name}] Shoot spawned id={cooking.Id} state={cooking.CurrentState} fuseLeft={cooking.FuseLeft:F2} def={definition?.name ?? "null"} owner={GetOwnerIndexSafe()}");
//#endif
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

//#if GRENADE_DEBUG
//        // DEBUG:
//        Debug.Log($"[GRENADE][WEAPON {name}] TryThrowCooked threw id={thrown.Id} state={thrown.CurrentState} fuseLeft={thrown.FuseLeft:F2} def={thrown.Definition?.name ?? "null"}");
//#endif

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

        AdoptExistingGrenade(g);
        return cooking != null;
    }

    public Grenade DetachHeldGrenadeForTransfer()
    {
        if (cooking == null) return null;
        if (cooking.CurrentState == Grenade.State.Exploded)
        {
#if GRENADE_DEBUG
            // DEBUG:
            Debug.Log($"[GRENADE][WEAPON {name}] DETACH_HELD aborted exploded grenade");
#endif
            cooking = null;
            return null;
        }

        Grenade g = cooking;
        cooking = null;

        g.DetachFromHand();
        g.SetOwner(null);

        consumedInHandThisFrame = true;
        StartCoroutine(ClearConsumedFlagEndOfFrame());

        if (weaponBodyRenderer) weaponBodyRenderer.enabled = true;

        currentClipAmmo = Mathf.Max(0, currentClipAmmo - 1);
        if (currentClipAmmo <= 0)
            StartCoroutine(AutoDestroyThisWeaponNextFrame());

#if GRENADE_DEBUG
        // DEBUG:
        Debug.Log($"[GRENADE][WEAPON {name}] DETACH_HELD id={g.Id} state={g.CurrentState} fuseLeft={g.FuseLeft:F2} def={g.Definition?.name ?? "null"}");
#endif

        return g;
    }

    public void AdoptExistingGrenade(Grenade g)
    {
        if (g == null) return;
        if (g.CurrentState == Grenade.State.Exploded) return;
        if (cooking != null && cooking != g)
        {
#if GRENADE_DEBUG
            Debug.LogWarning($"[GRENADE][WEAPON {name}] AdoptExisting refused because already cooking id={cooking.Id}");
#endif
            return;
        }

        Transform parent = handPoint != null ? handPoint : (firePoint != null ? firePoint : transform);
        if (parent == null) parent = transform;

#if GRENADE_DEBUG
        // DEBUG:
        Debug.Log($"[GRENADE][WEAPON {name}] AdoptExisting begin id={g.Id} state={g.CurrentState} fuseLeft={g.FuseLeft:F2} def={g.Definition?.name ?? "null"}");
#endif

        if (g.Definition != null && g.Definition != definition)
        {
#if GRENADE_DEBUG
            // DEBUG:
            Debug.Log($"[GRENADE][WEAPON {name}] adopting definition {g.Definition.name} (previous={definition?.name ?? "null"})");
#endif
            definition = g.Definition;
        }

        g.SetOwner(this);
        g.AttachToHand(parent, GetOwnerIndexSafe());
        cooking = g;

        if (weaponBodyRenderer) weaponBodyRenderer.enabled = false;

        if (forceGrenadeOnTop)
        {
            var gsr = g.GetComponentInChildren<SpriteRenderer>();
            if (gsr && weaponBodyRenderer)
            {
                gsr.sortingLayerID = weaponBodyRenderer.sortingLayerID;
                gsr.sortingOrder = weaponBodyRenderer.sortingOrder + 1;
            }
        }

#if GRENADE_DEBUG
        // DEBUG:
        Debug.Log($"[GRENADE][WEAPON {name}] AdoptExisting end id={g.Id} state={g.CurrentState} fuseLeft={g.FuseLeft:F2}");
#endif
    }

}


