using UnityEngine;

public class PowerUpTrigger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] private int powerUpType = 0;  //0 = no power, 1 = instakill, 2 = doublePoints, 3 = OpenFire, 4 = MaxAmmo, 5 = fireSale, 6 = kaboom, 7 = carpinter, 8 = death machine
    private SpriteRenderer spriteRenderer;
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
    }

    // Update is called once per frame
    void Update()
    {
        spriteRenderer.sprite = GameManager.instance.powerUpIcons[powerUpType];
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerStats player = collision.GetComponent<PlayerStats>();
           SoundFXManager.instance.PlaySoundByName("PickupPowerUp", transform, 1f, 1f);
            switch (player.playerIndex)
            {
                case 0:
                    GameManager.instance.player1PowerUp = powerUpType;
                    break;
                case 1:
                    GameManager.instance.player2PowerUp = powerUpType;
                    break;
                case 2:
                    GameManager.instance.player3PowerUp = powerUpType;
                    break;
                case 3:
                    GameManager.instance.player4PowerUp = powerUpType;
                    break;
            }
            Destroy(gameObject);
        }
    }
}
