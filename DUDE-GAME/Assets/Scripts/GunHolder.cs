using UnityEngine;
using UnityEngine.InputSystem;

public class GunHolder : MonoBehaviour
{
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private GameObject[] allGuns;
    [SerializeField] private GameObject dropPrefab;
    [SerializeField] private PlayerStats playerStats;

    private GameObject currentGun;
    private WeaponPickup nearbyPickup;
    private bool hasGun = false;
    private int playerIndex;
    public void Start()
    {
        playerIndex = playerStats.GetPlayerIndex();
    }
    public void SetPlayerIndex(int index)
    {
        playerIndex = index;
    }

    public int GetPlayerIndex()
    {
        return playerIndex;
    }
    void Update()
    {
        //if (Gamepad.current == null) return;

        //if (Gamepad.current.buttonNorth.wasPressedThisFrame)
        //{
        //    //if (nearbyPickup != null && !hasGun)
        //    //{
        //    //    EquipGun(nearbyPickup.weaponName);
        //    //    Destroy(nearbyPickup.gameObject);
        //    //    nearbyPickup = null;
        //    //}
        //    //else if (hasGun)
        //    //{
        //    //    DropCurrentGun();
        //    //}
        //}
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

    private WeaponBase currentWeaponScript;

    public void EquipGun(string gunName)
    {
        if (currentGun != null)
        {
            Destroy(currentGun);
            currentWeaponScript = null;
        }

        foreach (GameObject gunPrefab in allGuns)
        {
            if (gunPrefab.name == gunName)
            {
                currentGun = Instantiate(gunPrefab, weaponHolder.position, weaponHolder.rotation, weaponHolder);
                currentWeaponScript = currentGun.GetComponent<WeaponBase>();
                hasGun = true;
                return;
            }
        }

        Debug.LogWarning("Gun not found: " + gunName);
    }



    public void DropCurrentGun()
    {
        if (currentGun == null) return;

        GameObject drop = Instantiate(dropPrefab, transform.position, Quaternion.identity);
        drop.GetComponent<WeaponPickup>().weaponName = currentGun.name.Replace("(Clone)", "").Trim();

        Destroy(currentGun);
        currentGun = null;
        currentWeaponScript = null;
        hasGun = false;
    }

    public void HandlePickDrop()
    {
        if (nearbyPickup != null && !hasGun)
        {
            EquipGun(nearbyPickup.weaponName);
            Destroy(nearbyPickup.gameObject);
            nearbyPickup = null;
        }
        else if (hasGun)
        {
            DropCurrentGun();
        }
    }

    public void HandleShoot()
    {
        if (hasGun && currentWeaponScript != null)
        {
            currentWeaponScript.Shoot();
        }
    }
    public void SetAimDirection(Vector2 direction)
    {
        if (hasGun && currentWeaponScript != null)
        {
            currentWeaponScript.SetAimDirection(direction);
        }
    }

}

