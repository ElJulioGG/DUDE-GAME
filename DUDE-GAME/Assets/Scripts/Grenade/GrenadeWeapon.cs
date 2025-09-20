using System.Collections;
using UnityEngine;

public class GrenadeWeapon : WeaponBase
{
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

    // Flag: la granada se consumió (explotó) en la mano recientemente
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

    /// Shoot AHORA SOLO ARMA (no lanza)
    public override void Shoot()
    {
        if (cooking != null) return; // ya hay una cocinándose

        if (currentClipAmmo <= 0 || grenadePrefab == null || definition == null)
            return;

        Transform parent = handPoint != null ? handPoint : (firePoint != null ? firePoint : transform);
        cooking = Instantiate(grenadePrefab, parent.position, Quaternion.identity, parent);
        cooking.Init(definition, GetOwnerIndexSafe());
        cooking.SetOwner(this);          
        cooking.Arm();                   // empieza fuse en la mano

        // ocultar el cuerpo del arma para que se vea solo la granada
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
    }

    /// Llamado por el botón "lanzar cosas"
    public bool TryThrowCooked(Vector2 dir)
    {
        if (cooking == null) return false;

        cooking.Throw(dir, throwCharge01);
        cooking = null;

        if (weaponBodyRenderer) weaponBodyRenderer.enabled = true;

        currentClipAmmo = Mathf.Max(0, currentClipAmmo - 1);
        if (currentClipAmmo <= 0)
            StartCoroutine(AutoDestroyThisWeaponNextFrame());

        return true;
    }


    private IEnumerator AutoDestroyThisWeaponNextFrame()
    {
        yield return null; // evita pelear con el flujo del holder
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
            StartCoroutine(ClearConsumedFlagEndOfFrame()); // se limpia al final del frame

            if (weaponBodyRenderer) weaponBodyRenderer.enabled = true;

            currentClipAmmo = Mathf.Max(0, currentClipAmmo - 1);
            if (currentClipAmmo <= 0)
                StartCoroutine(AutoDestroyThisWeaponNextFrame());
        }
    }

}
