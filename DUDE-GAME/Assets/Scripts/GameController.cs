using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private PlayerStats [] playerStats;
    [SerializeField] private GameObject[] players;
    [SerializeField] private Animator transitionAnim;
    public GameObject[] UIIntroObjects; // Assign in inspector
    public GameObject[] maps;
    public bool matchEnded = false;
    [SerializeField] public int pointsToGive = 1;
    [SerializeField] private GameObject pointsCanvasPrefab; // Assign in inspector
    private GameObject activePointsCanvas; // Keep track of current active canvas
    private int currentPowerUp;

    public void NextMatch()
    {
        pointsToGive = 1;
        SelectRandomMap();
        GameManager.instance.playersCanMove = false;
        RemovePointsCanvas();
        // Destroy all blood splatters
        foreach (GameObject splatter in PlayerStats.allSplatters)
        {
            if (splatter != null)
                Destroy(splatter);
        }
        PlayerStats.allSplatters.Clear();
        
        // Respawn players
        foreach (PlayerStats player in playerStats)
        {
            player.Respawn();
        }

        matchEnded = false;
        AssignPlayerPositions();
        Invoke("StartGame", 0.5f);
    }


    // Only if you’re using TextMeshPro

public void ShowPointsCanvas(Transform winnerTransform, int points)
{
    if (pointsCanvasPrefab == null)
    {
        Debug.LogWarning("Points Canvas Prefab is not assigned!");
        return;
    }

    activePointsCanvas = Instantiate(pointsCanvasPrefab, winnerTransform);
    activePointsCanvas.transform.localPosition = Vector3.up * 1f; // Offset above player

    // If using TMPro
    TMP_Text tmp = activePointsCanvas.GetComponentInChildren<TMP_Text>();
    if (tmp != null)
    {
        tmp.text = $"+{points}";
    }
}

public void RemovePointsCanvas()
{
    if (activePointsCanvas != null)
    {
        Destroy(activePointsCanvas);
        activePointsCanvas = null;
    }
}

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
        transitionAnim.SetTrigger("FadeOut");
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
                SoundFXManager.instance.PlaySoundByName("Fight", transform, 0.7f, 0.9f);
                if (!SoundFXManager.instance.IsSoundPlaying("BattleLoop"))
                {
                    SoundFXManager.instance.PlaySoundByName("BattleLoop", transform, 0.1f, 1f,true);
                }
               
                GameManager.instance.playersCanMove = true;

            }
            if (i == UIIntroObjects.Length - 2)
            {
                SoundFXManager.instance.PlaySoundByName("1", transform, 0.6f, 0.9f);
            }
            if (i == UIIntroObjects.Length - 3)
            {
                SoundFXManager.instance.PlaySoundByName("2", transform, 0.6f, 0.9f);
            }
            if (i == UIIntroObjects.Length - 4)
            {
                SoundFXManager.instance.PlaySoundByName("3", transform, 0.6f, 0.9f);
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
        if (!SoundFXManager.instance.IsSoundPlaying("BattleLoop"))
        {
            SoundFXManager.instance.PlaySoundByName("FadeInIntro", transform, 0.1f, 1f);
        }
        //SoundFXManager.instance.PlaySoundByName("FadeInIntro", transform, 0.6f, 1f);
        SelectRandomMap();
        GameManager.instance.playersCanMove = false;
        
        AssignPlayerPositions();
        Invoke("StartGame", 0.5f);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            // Correct method call
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

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
                }
            }

            if (aliveCount == 1 && lastAlivePlayer != null)
            {
                matchEnded = true; // Set this FIRST to avoid repeated triggers
                StartCoroutine(HandleLastPlayerWin(lastAlivePlayer));
                
            }

            

        }
    }
    private void FixedUpdate()
    {
        if (playerStats[0].usingPowerUp)
        {
            TriggerPowerUp(playerStats[0].playerIndex);
            playerStats[0].usingPowerUp = false;
        }
        if (playerStats[1].usingPowerUp)
        {
            TriggerPowerUp(playerStats[1].playerIndex);
            playerStats[1].usingPowerUp = false;
        }
        if (playerStats[2].usingPowerUp)
        {
            TriggerPowerUp(playerStats[2].playerIndex);
            playerStats[2].usingPowerUp = false;
        }
        if (playerStats[3].usingPowerUp)
        {
            TriggerPowerUp(playerStats[3].playerIndex);
            playerStats[3].usingPowerUp = false;
        }
    }
    private void TriggerPowerUp(int playerIndex)
    {
        switch (playerIndex)
        {
            case 0:
                currentPowerUp = GameManager.instance.player1PowerUp;
                GameManager.instance.player1PowerUp = 0;
                break;
            case 1:
                currentPowerUp = GameManager.instance.player2PowerUp;
                GameManager.instance.player2PowerUp = 0;
                break;
            case 2:
                currentPowerUp = GameManager.instance.player3PowerUp;
                GameManager.instance.player3PowerUp = 0;
                break;
            case 3:
                currentPowerUp = GameManager.instance.player4PowerUp;
                GameManager.instance.player4PowerUp = 0;
                break;
        }
        switch (currentPowerUp)//0 = no power, 1 = instakill, 2 = doublePoints, 3 = OpenFire, 4 = MaxAmmo, 5 = fireSale, 6 = kaboom, 7 = carpinter, 8 = death machine
        {
            case 0:
                //play funny sound
                SoundFXManager.instance.PlaySoundByName("NoPowerUp", transform, 1f, 1f);
                break;
            case 1:
                Instakill();
                break;
            case 2:
                DoublePoints();
                break;
            case 3:
                break;
            case 4:
                break;
            case 5:
                break;
            case 6:
                break;
            case 7:
                break;
            case 8:
                break;

        }
    }
    IEnumerator HandleLastPlayerWin(PlayerStats winner)
    {
        yield return winner.AddPointsAfterDelay(pointsToGive);
        ShowPointsCanvas(winner.transform, pointsToGive);

        Debug.Log($"Match ended. {winner.name} awarded {pointsToGive} point(s).");
        yield return new WaitForSeconds(2f);
        transitionAnim.SetTrigger("FadeIn");
        yield return new WaitForSeconds(0.5f);
        NextMatch();
        // You can trigger end screen or restart logic here if needed
    }
    public void SelectRandomMap()
    {
        if (maps == null || maps.Length == 0)
        {
            Debug.LogWarning("No maps assigned");
            return;
        }

        int randomIndex = Random.Range(0, maps.Length);

        for (int i = 0; i < maps.Length; i++)
        {
            bool shouldBeActive = (i == randomIndex);
            if (maps[i].activeSelf != shouldBeActive)
            {
                maps[i].SetActive(shouldBeActive);
            }
        }

        Debug.Log($"Random map selected: {maps[randomIndex].name}");
    }
    public void Instakill()
    {
        SoundFXManager.instance.PlaySoundByName("Instakill", transform, 0.9f, 0.9f);
        foreach (PlayerStats player in playerStats)
        {
            if (player.playerAlive)
            {
                player.SetPlayerHealth(1);
            }
        }
    }
    public void DoublePoints()
    {
        SoundFXManager.instance.PlaySoundByName("DoublePoints", transform, 0.9f, 0.9f);
        pointsToGive = pointsToGive*2;
    }
}
