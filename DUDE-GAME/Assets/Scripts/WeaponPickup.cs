using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    private GunHolder gunHolder;
    public string weaponName;

    private void Awake()
    {
        gunHolder = FindAnyObjectByType<GunHolder>();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
           // gunHolder.EquipGun(weaponName);
           // Destroy(gameObject);
        }
    }
}


