using System.Collections;
using FishNet;
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
        if (!collision.CompareTag("Player")) return;

        // Online: only the server decides who picked it up — every machine running
        // its own trigger detection desynced who got which powerup. Clients consume
        // their copy via the PowerUpPickupBroadcast mirror instead.
        if (GameSession.IsOnline && !InstanceFinder.IsServerStarted) return;

        PlayerStats player = collision.GetComponent<PlayerStats>();
        if (player == null) return;

        if (GameSession.IsOnline)
            NetworkGameManager.Instance?.ServerBroadcastPowerUpPickup(player.playerIndex, powerUpType, transform.position);

        ApplyPickup(player.playerIndex, powerUpType);
    }

    // Grants the powerup and consumes this pickup — runs on every machine.
    public void ApplyPickup(int slot, int type)
    {
        if (AudioManager.Instance != null && FMODEvents.Instance != null)
            AudioManager.Instance.PlaySound(FMODEvents.Instance.PickUpPowerUp, transform.position);
        Grant(slot, type);
        DetachAndFadeParticles();
        Destroy(gameObject);
    }

    public static void Grant(int slot, int type)
    {
        if (GameManager.instance == null) return;
        switch (slot)
        {
            case 0: GameManager.instance.player1PowerUp = type; break;
            case 1: GameManager.instance.player2PowerUp = type; break;
            case 2: GameManager.instance.player3PowerUp = type; break;
            case 3: GameManager.instance.player4PowerUp = type; break;
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
// ParticleSelfDestruct vivia aqui; ahora esta en su propio archivo
// (Assets/Scripts/Particles/ParticleSelfDestruct.cs) porque tambien lo usa
// DetachParticlePlayer.
