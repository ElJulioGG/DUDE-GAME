using UnityEngine;
using UnityEngine.InputSystem;

public class GunHolder : MonoBehaviour
{
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private GameObject[] allGuns;
    [SerializeField] private GameObject dropPrefab;

    private GameObject currentGun;
    private WeaponPickup nearbyPickup;
    private bool hasGun = false;

    void Update()
    {
        if (Gamepad.current == null) return;

        if (Gamepad.current.buttonNorth.wasPressedThisFrame)
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

    public void EquipGun(string gunName)
    {
        if (currentGun != null)
            currentGun.SetActive(false);

        foreach (GameObject gun in allGuns)
        {
            print(gun.name);
            print(gunName);
            if (gun.name == gunName)
            {
                currentGun = gun;
                currentGun.SetActive(true);
                currentGun.transform.SetParent(weaponHolder, false);
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
        drop.GetComponent<WeaponPickup>().weaponName = currentGun.name;

        currentGun.SetActive(false);
        currentGun = null;
        hasGun = false;
    }
}

