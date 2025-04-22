using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private int health = 200;
    [SerializeField] private int points = 0;
    void Start()
    {
        
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
        gameObject.SetActive(false);
    }
    public void Respawn()
    {
        health = 200;
        gameObject.SetActive(true);
    }
}
