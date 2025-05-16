using UnityEngine;
using UnityEngine.InputSystem;

public class GunHolder : MonoBehaviour
{
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private GameObject[] allWeapons;
    [SerializeField] private GameObject dropPrefab;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private GameObject defaultMeleePrefab;

    private GameObject currentWeapon;
    private GameObject defaultMeleeWeapon;

    private WeaponBase currentGunScript;
    private MeleeWeaponBase currentMeleeScript;
    private MeleeWeaponBase defaultMeleeScript;

    private WeaponPickup nearbyPickup;
    private bool hasWeapon = false;
    private int playerIndex;

    public void Start()
    {
        playerIndex = playerStats.GetPlayerIndex();

        if (defaultMeleePrefab != null)
        {
            defaultMeleeWeapon = Instantiate(defaultMeleePrefab, weaponHolder.position, weaponHolder.rotation, weaponHolder);
            defaultMeleeScript = defaultMeleeWeapon.GetComponent<MeleeWeaponBase>();
            hasWeapon = false; // Start with default weapon, but don't count as "equipped"
        }
    }

    public void SetPlayerIndex(int index)
    {
        playerIndex = index;
    }

    public int GetPlayerIndex()
    {
        return playerIndex;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        WeaponPickup pickup = other.GetComponent<WeaponPickup>();
        if (pickup != null)
        {
            nearbyPickup = pickup;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (nearbyPickup != null && other.gameObject == nearbyPickup.gameObject)
        {
            nearbyPickup = null;
        }
    }

    public void EquipWeapon(string weaponName)
    {
        // Destroy current weapon
        if (currentWeapon != null)
        {
            Destroy(currentWeapon);
        }

        currentWeapon = null;
        currentGunScript = null;
        currentMeleeScript = null;

        foreach (GameObject weaponPrefab in allWeapons)
        {
            if (weaponPrefab.name == weaponName)
            {
                currentWeapon = Instantiate(weaponPrefab, weaponHolder.position, weaponHolder.rotation, weaponHolder);
                currentGunScript = currentWeapon.GetComponent<WeaponBase>();
                currentMeleeScript = currentWeapon.GetComponent<MeleeWeaponBase>();

                hasWeapon = true;

                if (defaultMeleeWeapon != null) defaultMeleeWeapon.SetActive(false);
                return;
            }
        }

        Debug.LogWarning("Weapon not found: " + weaponName);
    }

    public void DropCurrentWeapon()
    {
        if (currentWeapon == null) return;

        GameObject drop = Instantiate(dropPrefab, transform.position, Quaternion.identity);
        drop.GetComponent<WeaponPickup>().weaponName = currentWeapon.name.Replace("(Clone)", "").Trim();

        Destroy(currentWeapon);
        currentWeapon = null;
        currentGunScript = null;
        currentMeleeScript = null;
        hasWeapon = false;

        if (defaultMeleeWeapon != null) defaultMeleeWeapon.SetActive(true);
    }

    public void HandlePickDrop()
    {
        if (nearbyPickup != null && !hasWeapon)
        {
            EquipWeapon(nearbyPickup.weaponName);
            Destroy(nearbyPickup.gameObject);
            nearbyPickup = null;
        }
        else if (hasWeapon)
        {
            DropCurrentWeapon();
        }
    }

    public void HandleShoot()
    {
       
        if (hasWeapon)
        {
            if (currentGunScript != null)
                currentGunScript.Shoot();
            else if (currentMeleeScript != null)
                currentMeleeScript.Attack();
        }
        else if (defaultMeleeScript != null)
        {
            defaultMeleeScript.Attack();
            
        }
    }

    public void SetAimDirection(Vector2 direction)
    {
        if (hasWeapon)
        {
            if (currentGunScript != null)
                currentGunScript.SetAimDirection(direction);
            else if (currentMeleeScript != null)
                currentMeleeScript.SetAimDirection(direction);
        }
        else if (defaultMeleeScript != null)
        {
            defaultMeleeScript.SetAimDirection(direction);
        }
    }
}
