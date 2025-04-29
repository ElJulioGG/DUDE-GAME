using UnityEngine;

public class GameController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private PlayerStats [] playerStats;
    void Start()
    {
        foreach(PlayerStats player in playerStats)
        {
            player.SetPlayerHealth(player.baseHealth);
        }
        for( int i = 0; i> GameManager.instance.playersActive; i++)
        {
            if (playerStats[i].GetPlayerIndex() == i)
            {

            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
