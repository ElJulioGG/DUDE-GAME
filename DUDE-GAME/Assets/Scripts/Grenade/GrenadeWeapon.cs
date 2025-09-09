using System.Collections;
using UnityEngine;

public class GrenadeWeapon : WeaponBase
{
    [Header("Grenade Setup")]
    [SerializeField] private Grenade grenadePrefab;        
    [SerializeField] private GrenadeDefinition definition; 
    [SerializeField] private Transform handPoint;          
    [SerializeField] private float throwCharge01 = 0f;     

    [Header("Visual (arma)")]
    [SerializeField] private SpriteRenderer weaponBodyRenderer; 
    [SerializeField] private bool forceGrenadeOnTop = true;     

    private Grenade cooking;               
    private GunHolder holder;

    private void Awake()
    {
        holder = GetComponentInParent<GunHolder>();
        if (!weaponBodyRenderer) weaponBodyRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        reserveAmmo = 0;
        if (weaponBodyRenderer) weaponBodyRenderer.enabled = true;
    }

    private Vector2 GetAimDir()
    {
        if (firePoint != null) return firePoint.right.normalized;
        return transform.right.normalized;
    }

    public override void Shoot()
    {
        if (cooking == null)
        {
            if (currentClipAmmo <= 0 || grenadePrefab == null || definition == null)
                return;

            Transform parent = handPoint != null ? handPoint : (firePoint != null ? firePoint : transform);
            cooking = Instantiate(grenadePrefab, parent.position, Quaternion.identity, parent);
            cooking.Init(definition, GetOwnerIndexSafe());
            cooking.Arm(); // empieza el fuse en la mano (si te demoras explota)

            // 1) Oculta el sprite del arma mientras cocinas 
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

            return;
        }

        Vector2 dir = GetAimDir();
        cooking.Throw(dir, throwCharge01);
        cooking = null;

        // Volver a mostrar el sprite del arma (si aún hay stock)
        if (weaponBodyRenderer) weaponBodyRenderer.enabled = true;

        // Consumir 1 del stock; el HUD se actualizará en WeaponBase.Update()
        currentClipAmmo = Mathf.Max(0, currentClipAmmo - 1);

        // Sin stock: destruir y volver a melee mediante tu GunHolder
        if (currentClipAmmo <= 0)
            StartCoroutine(AutoDestroyThisWeaponNextFrame());
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
            cooking.DropArmed();              
            cooking = null;
        }

        if (weaponBodyRenderer) weaponBodyRenderer.enabled = true;
    }

    private void OnDestroy()
    {
        if (cooking != null)
            cooking.transform.SetParent(null, true);

        if (weaponBodyRenderer) weaponBodyRenderer.enabled = true;
    }

    private int GetOwnerIndexSafe()
    {
        if (holder != null) return holder.GetPlayerIndex();
        var ps = GetComponentInParent<PlayerStats>();
        return ps != null ? ps.GetPlayerIndex() : -1;
    }

    public void PreDropAdjustAmmoAndCooking()
    {
        if (cooking != null)
        {
            cooking.DropArmed();   
            cooking = null;

            currentClipAmmo = Mathf.Max(0, currentClipAmmo - 1);
        }

        if (weaponBodyRenderer) weaponBodyRenderer.enabled = true;
    }

}
