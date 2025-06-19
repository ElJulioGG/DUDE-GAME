using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    [SerializeField] private PlayerStats[] playerStats;
    [SerializeField] private GameObject[] players;
    [SerializeField] private Animator transitionAnim;
    public GameObject[] UIIntroObjects;
    public GameObject[] maps;
    public bool matchEnded = false;
    [SerializeField] public int pointsToGive = 1;
    [SerializeField] private GameObject pointsCanvasPrefab;
    private GameObject activePointsCanvas;
    private int currentPowerUp;

    public void NextMatch()
    {
        pointsToGive = 1;
        ClearAllWeaponPickups(); // Clear weapons before map change
        SelectRandomMap();
        GameManager.instance.playersCanMove = false;
        GameManager.instance.destroyProyectiles = true;
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

    private void ClearAllWeaponPickups()
    {
        WeaponPickup[] existingPickups = FindObjectsOfType<WeaponPickup>(true);
        foreach (WeaponPickup pickup in existingPickups)
        {
            if (pickup != null && pickup.gameObject != null)
            {
                Destroy(pickup.gameObject);
            }
        }
        Debug.Log($"Cleared {existingPickups.Length} weapon pickups");
    }

    public void ShowPointsCanvas(Transform winnerTransform, int points)
    {
        if (pointsCanvasPrefab == null)
        {
            Debug.LogWarning("Points Canvas Prefab is not assigned!");
            return;
        }

        activePointsCanvas = Instantiate(pointsCanvasPrefab, winnerTransform);
        activePointsCanvas.transform.localPosition = Vector3.up * 1f;

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
        GameManager.instance.destroyProyectiles = false;
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
                if (!SoundFXManager.instance.IsSoundPlaying("BattleTheme"))
                {
                    SoundFXManager.instance.PlaySoundByName("BattleTheme", transform, 0.5f, 1f, true);
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
        SelectRandomMap();
        GameManager.instance.playersCanMove = false;
        AssignPlayerPositions();
        Invoke("StartGame", 0.5f);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
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
                matchEnded = true;
                StartCoroutine(HandleLastPlayerWin(lastAlivePlayer));
            }
            else if (aliveCount == 0)
            {
                matchEnded = true;
                StartCoroutine(HandleDraw());
            }
        }
    }

    IEnumerator HandleDraw()
    {
        Debug.Log("Draw! No players alive.");
        yield return new WaitForSeconds(2f); // Optional delay
        transitionAnim.SetTrigger("FadeIn");
        yield return new WaitForSeconds(0.5f);
        NextMatch(); // Restart match without awarding points
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < playerStats.Length; i++)
        {
            if (playerStats[i].usingPowerUp)
            {
                TriggerPowerUp(playerStats[i].playerIndex);
                playerStats[i].usingPowerUp = false;
            }
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

        switch (currentPowerUp)
        {
            case 0:
                SoundFXManager.instance.PlaySoundByName("NoPowerUp", transform, 1f, 1f);
                break;
            case 1:
                Instakill();
                break;
            case 2:
                DoublePoints();
                break;
                // Other powerup cases...
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
    }

    public void SelectRandomMap()
    {
        if (maps == null || maps.Length == 0)
        {
            Debug.LogWarning("No maps assigned");
            return;
        }

        foreach (GameObject map in maps)
        {
            map.SetActive(false);
        }

        int randomIndex = Random.Range(0, maps.Length);
        maps[randomIndex].SetActive(true);
        Debug.Log($"Map reloaded: {maps[randomIndex].name}");
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
        pointsToGive *= 2;
    }
}