using UnityEngine;

public class WeaponPickup : MonoBehaviour
{

    public string weaponName;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        //UpdateSprite();
    }
    private void Start()
    {
        UpdateSprite();
    }
    public void SetWeapon(string name)
    {
        weaponName = name;
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (spriteRenderer == null) return;

        // Load the sprite from Resources/WeaponIcons folder
        //print(weaponName);
        Sprite weaponSprite = Resources.Load<Sprite>("WeaponIcons/" + weaponName);
        if (weaponSprite != null)
        {
            spriteRenderer.sprite = weaponSprite;
        }
        else
        {
            Debug.LogWarning($"Sprite for weapon '{weaponName}' not found in Resources/WeaponIcons/");
        }
    }
}



