using UnityEngine;
using UnityEngine.VFX;

public class WeaponBox : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private GameObject[] weaponPickups;
    [SerializeField] private bool random = false;
    [SerializeField] private int selectedWeapon = 0;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Bullet")|| collision.CompareTag("Melee"))
        {
            spawnWeapon();
            destroyBox();
        }
    }
    private void spawnWeapon()
    {
        if (random)
        {
            int randomWeapon = Random.Range(0, weaponPickups.Length);
            GameObject weapon = Instantiate(weaponPickups[randomWeapon], transform.position, Quaternion.identity);
            weapon.transform.SetParent(null);
        }
        else
        {
            GameObject weapon = Instantiate(weaponPickups[selectedWeapon], transform.position, Quaternion.identity);
            weapon.transform.SetParent(null);
        }
    }
    private void destroyBox()
    {
        //Intantiate box particles
        SoundFXManager.instance.PlaySoundByName("BoxBreak",transform,1,1,false);
        Destroy(gameObject);
    }
}
