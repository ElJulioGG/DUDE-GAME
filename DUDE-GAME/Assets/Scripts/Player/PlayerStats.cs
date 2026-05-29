using System.Collections;
using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [SerializeField] public int playerIndex = 0;
    [SerializeField] private int health = 200;
    public int baseHealth = 200;
    [SerializeField] private int points = 0;
    public bool playerAlive = true;
    [SerializeField] public bool usingPowerUp;
    [SerializeField] private GunHolder gunHolder;
    [SerializeField] private PlayerVisuals playerVisuals;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        if (playerVisuals == null) playerVisuals = GetComponent<PlayerVisuals>();
        if (gunHolder == null) gunHolder = GetComponentInChildren<GunHolder>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Start()
    {
        health = baseHealth;
        gameObject.SetActive(true);
    }

    public void SetPlayerIndex(int index)
    {
        playerIndex = index;
    }

    public IEnumerator AddPointsAfterDelay(int pointsToAdd)
    {
        yield return new WaitForSeconds(1.5f);

        if (!playerAlive)
        {
            Debug.Log($"Player {playerIndex} is not alive. No points awarded.");
            yield break;
        }

        switch (playerIndex)
        {
            case 0:
                GameManager.instance.player1Score += pointsToAdd;
                break;
            case 1:
                GameManager.instance.player2Score += pointsToAdd;
                break;
            case 2:
                GameManager.instance.player3Score += pointsToAdd;
                break;
            case 3:
                GameManager.instance.player4Score += pointsToAdd;
                break;
            default:
                Debug.LogWarning("Invalid playerIndex. Points not awarded.");
                yield break;
        }

        points += pointsToAdd;
        if (AudioManager.Instance != null && FMODEvents.Instance != null)
            AudioManager.Instance.PlaySound(FMODEvents.Instance.PointGain, transform.position);
        Debug.Log($"Player {playerIndex} received {pointsToAdd} points. Total: {points}");
    }

    public void TakeDamage(int damageAmount = 1)
    {
        if (!playerAlive) return;

        health -= damageAmount;

        if (AudioManager.Instance != null && FMODEvents.Instance != null)
            AudioManager.Instance.PlaySound(FMODEvents.Instance.PlayerGetHit, transform.position);
        if (playerVisuals != null)
        {
            playerVisuals.PlayDamageShake();
            playerVisuals.SpawnDamageParticles(damageAmount, baseHealth);
        }

        if (health <= 0 && playerAlive)
        {
            KillPlayer();
        }
    }

    public void SetPlayerHealth(int newHealth)
    {
        health = newHealth;
    }

    public int GetPlayerHealth()
    {
        return health;
    }

    public int GetPlayerIndex()
    {
        return playerIndex;
    }

    public void UsePowerUp()
    {
        if (playerAlive && !usingPowerUp)
        {
            usingPowerUp = true;
        }
    }

    // Plays the points-earned sound — used by online mode where score is awarded via SyncVar.
    public void PlayPointsSound()
    {
        if (AudioManager.Instance != null && FMODEvents.Instance != null)
            AudioManager.Instance.PlaySound(FMODEvents.Instance.PointGain, transform.position);
    }

    // Plays blood/death particles and death sound — safe to call on any machine.
    public void PlayDeathEffects()
    {
        if (playerVisuals != null) playerVisuals.SpawnBloodSplatter(playerIndex, transform.position);
        if (playerVisuals != null) playerVisuals.SpawnDeathEffect();
        if (AudioManager.Instance != null && FMODEvents.Instance != null)
            AudioManager.Instance.PlaySound(FMODEvents.Instance.PlayerDeath, transform.position);
    }

    public void KillPlayer()
    {
        PlayDeathEffects();
        playerAlive = false;
        if (gunHolder != null) gunHolder.DropCurrentWeapon();
        gameObject.SetActive(false);

        if (GameController.instance != null)
            GameController.instance.OnPlayerDied(this);
    }

    // Applies damage feedback (shake, particles, sound) without triggering death.
    // Used by NetworkPlayerController.ServerTakeDamage so health/death are server-authoritative.
    public void ApplyDamageWithoutKill(int damageAmount)
    {
        if (!playerAlive) return;
        health -= damageAmount;
        if (AudioManager.Instance != null && FMODEvents.Instance != null)
            AudioManager.Instance.PlaySound(FMODEvents.Instance.PlayerGetHit, transform.position);
        if (playerVisuals != null)
        {
            playerVisuals.PlayDamageShake();
            playerVisuals.SpawnDamageParticles(damageAmount, baseHealth);
        }
    }

    public void ApplyKnockback(Vector2 origin, float force)
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 knockDirection = (transform.position - (Vector3)origin).normalized;
            rb.AddForce(knockDirection * force, ForceMode2D.Impulse);
            playerMovement?.SetKnockbackWindow();
        }
    }

    public void ApplyKnockbackDirection(Vector2 direction, float force)
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
            playerMovement?.SetKnockbackWindow();
        }
    }

    public void Respawn()
    {
        switch (playerIndex)
        {
            case 0:
                if (GameManager.instance.player1Playable)
                {
                    if (playerVisuals != null) playerVisuals.ResetShake();
                    if (gunHolder != null) gunHolder.DestroyCurrentWeapon();
                    health = baseHealth;
                    playerAlive = true;
                    gameObject.SetActive(true);
                }
                break;
            case 1:
                if (GameManager.instance.player2Playable)
                {
                    if (playerVisuals != null) playerVisuals.ResetShake();
                    if (gunHolder != null) gunHolder.DestroyCurrentWeapon();
                    health = baseHealth;
                    playerAlive = true;
                    gameObject.SetActive(true);
                }
                break;
            case 2:
                if (GameManager.instance.player3Playable)
                {
                    if (playerVisuals != null) playerVisuals.ResetShake();
                    if (gunHolder != null) gunHolder.DestroyCurrentWeapon();
                    health = baseHealth;
                    playerAlive = true;
                    gameObject.SetActive(true);
                }
                break;
            case 3:
                if (GameManager.instance.player4Playable)
                {
                    if (playerVisuals != null) playerVisuals.ResetShake();
                    if (gunHolder != null) gunHolder.DestroyCurrentWeapon();
                    health = baseHealth;
                    playerAlive = true;
                    gameObject.SetActive(true);
                }
                break;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void OnEnable()
    {
        health = baseHealth;
        playerAlive = true;
    }
}
