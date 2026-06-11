using System.Collections;
using UnityEngine;

public class PowerUpTrigger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] private int powerUpType = 0;  //0 = no power, 1 = instakill, 2 = doublePoints, 3 = OpenFire, 4 = MaxAmmo, 5 = fireSale, 6 = kaboom, 7 = carpinter, 8 = death machine
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        // The icon never changes after spawn — assigning it every frame (the old
        // Update) re-dirtied the renderer for nothing.
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (GameManager.instance != null && powerUpType >= 0
            && powerUpType < GameManager.instance.powerUpIcons.Length)
            spriteRenderer.sprite = GameManager.instance.powerUpIcons[powerUpType];
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerStats player = collision.GetComponent<PlayerStats>();
            AudioManager.Instance.PlaySound(FMODEvents.Instance.PickUpPowerUp, transform.position);
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
            DetachAndFadeParticles();
            Destroy(gameObject);
        }
    }

    private void DetachAndFadeParticles()
    {
        foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>())
        {
            ps.transform.SetParent(null);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            ps.gameObject.AddComponent<ParticleSelfDestruct>();
        }
    }
}

[RequireComponent(typeof(ParticleSystem))]
public class ParticleSelfDestruct : MonoBehaviour
{
    [SerializeField] private float lingerDuration = 0.5f;

    private IEnumerator Start()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        yield return new WaitForSeconds(lingerDuration);
        yield return new WaitWhile(() => ps != null && ps.IsAlive(true));
        Destroy(gameObject);
    }
}
