using System.Collections;
using UnityEngine;

public class GameController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private PlayerStats [] playerStats;
    [SerializeField] private GameObject[] players;
    [SerializeField] private Animator transitionAnim;
    public GameObject[] UIIntroObjects; // Assign in inspector
    public bool matchEnded = false;
    [SerializeField] public int pointsToGive = 1;

 

    public void AssignPlayerPositions()
    {
        for (int i = 0; i < players.Length; i++)
        {
            string spawnName = "SpawnPosP" + (i + 1);
            GameObject spawnPoint = GameObject.Find(spawnName);


            if (spawnPoint != null && players[i] != null)
            {
                players[i].transform.position = spawnPoint.transform.position;
            }
            else
            {
                Debug.LogWarning($"Missing player or spawn point: {players[i]} / {spawnName}");
            }
        }
    }
    public void StartGame()
    {
        StartCoroutine(MatchBegin());
    }

    IEnumerator MatchBegin()
    {
        foreach (PlayerStats player in playerStats)
        {
            player.SetPlayerHealth(player.baseHealth);
        }
        AssignPlayerPositions();
        print("MATCH BEGIN");
        for (int i = 0; i < UIIntroObjects.Length; i++)
        {
            UIIntroObjects[i].SetActive(true);

           
            if (i == UIIntroObjects.Length - 1)
            {
                GameManager.instance.playersCanMove = true;
            }
            float waitTime = (i == UIIntroObjects.Length - 1) ? 1.5f : 1f;
            yield return new WaitForSeconds(waitTime);

            // Deactivate the current object
            UIIntroObjects[i].SetActive(false);
        }
        GameManager.instance.playersCanMove = true;
    }
    void Start()
    {

        GameManager.instance.playersCanMove = false;
        transitionAnim.SetTrigger("FadeOut");
        AssignPlayerPositions();
        Invoke("StartGame", 0.5f);
    }

    // Update is called once per frame
    void Update()
    {
        if (!matchEnded)
        {
            int aliveCount = 0;
            PlayerStats lastAlivePlayer = null;

            foreach (PlayerStats player in playerStats)
            {
                if (player.playerAlive)
                {
                    aliveCount++;
                    lastAlivePlayer = player;
                    print(lastAlivePlayer);
                    print(aliveCount);
                }
            }

            // If only one player is alive, give points and end the match
            if (aliveCount == 1 && lastAlivePlayer != null)
            {
                StartCoroutine(lastAlivePlayer.AddPointsAfterDelay(pointsToGive));
                matchEnded = true;
                Debug.Log("Match ended. Points awarded.");
            }
        }
    }

}
