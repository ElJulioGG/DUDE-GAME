using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private int playerIndex = 0;
    [SerializeField] private int health = 200;
    public int baseHealth = 200;
    [SerializeField] private int points = 0;
    [SerializeField] private bool playerAlive;
    void Start()
    {
        health = 200;
        gameObject.SetActive(true);
    }
    public void SetPlayerIndex(int index)
    {
        playerIndex = index;
    }
    public void AddPoints(int pointsToAdd)
    {
        points = points + pointsToAdd;
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
    // Update is called once per frame
    void Update()
    {
        if(health <= 0)
        {
            KillPlayer();
        }
    }

    public void KillPlayer()
    {
        playerAlive = false;
        gameObject.SetActive(false);
    }
    public void Respawn()
    {
        health = 200;
        gameObject.SetActive(true);
    }
    private void OnEnable()
    {
        health = baseHealth;
    }
}
