using UnityEngine;

public class WeaponBox : MonoBehaviour
{
    [SerializeField] private GameObject[] weaponPickups;
    [SerializeField] private bool random = false;
    [SerializeField] private int selectedWeapon = 0;

    private bool isBroken = false; // Flag to prevent multiple breaks

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Early exit conditions
        if (isBroken) return;
        if (!collision.CompareTag("Bullet") && !collision.CompareTag("Melee")) return;

        BreakBox();
    }

    private void BreakBox()
    {
        isBroken = true; // Set flag first to prevent re-entry

        SpawnWeapon();
        DestroyBox();
    }

    private void SpawnWeapon()
    {
        int weaponIndex = random ? Random.Range(0, weaponPickups.Length) : selectedWeapon;

        if (weaponPickups.Length == 0 || weaponPickups[weaponIndex] == null)
        {
            Debug.LogError("Missing weapon pickup prefab!", this);
            return;
        }

        Instantiate(weaponPickups[weaponIndex], transform.position, Quaternion.identity);
    }

    private void DestroyBox()
    {
        SoundFXManager.instance?.PlaySoundByName("BoxBreak", transform, 0.5f, 1, false);
        Destroy(gameObject);
    }
}