using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] public int playerIndex = 0;
    [SerializeField] private int health = 200;
    public int baseHealth = 200;
    [SerializeField] private int points = 0;
    public bool playerAlive = true;
    [SerializeField] private GameObject[] bloodSplatterPrefabs; // Size 4, one for each player
    public static List<GameObject> allSplatters = new List<GameObject>();
    [SerializeField] public bool usingPowerUp;
    [SerializeField] private GunHolder gunHolder;


    void Start()
    {
        health = baseHealth;
        playerAlive = true;
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
        SoundFXManager.instance.PlaySoundByName("Bell", transform, 0.7f, 1.3f);
        Debug.Log($"Player {playerIndex} received {pointsToAdd} points. Total: {points}");
    }

    public void TakeDamage(int damageAmount)
    {
        if (!playerAlive) return;

        health -= damageAmount;
        SoundFXManager.instance.PlaySoundByName("NoPowerUp", transform, 1f, 0.8f);
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
    void Update()
    {
        //if (health <= 0 && playerAlive)
        //{
        //    KillPlayer();
        //}
    }

    public void KillPlayer()
    {
        playerAlive = false;
        gunHolder.DropCurrentWeapon();
        gameObject.SetActive(false);

        // Play death sound
        SoundFXManager.instance.PlaySoundByName("Death", transform, 0.6f, 1.5f);

        // Instantiate blood splatter specific to the player index
        if (playerIndex >= 0 && playerIndex < bloodSplatterPrefabs.Length)
        {
            Quaternion randomRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            GameObject splatter = Instantiate(bloodSplatterPrefabs[playerIndex], transform.position, randomRotation);
            allSplatters.Add(splatter);
        }
        else
        {
            Debug.LogWarning($"No blood splatter prefab assigned for playerIndex {playerIndex}.");
        }
        // instantiate corpse
        // instantiate particles
    }

    public void ApplyKnockback(Vector2 origin, float force)
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 knockDirection = (transform.position - (Vector3)origin).normalized;
            rb.AddForce(knockDirection * force, ForceMode2D.Impulse);
        }
    }
    public void Respawn()
    {
        gunHolder.DestroyCurrentWeapon();
        health = baseHealth;
        playerAlive = true;
        gameObject.SetActive(true);
    }

    private void OnEnable()
    {
        health = baseHealth;
        playerAlive = true;
    }
}
