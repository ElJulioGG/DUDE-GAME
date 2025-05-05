using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

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
    

    public void NextMatch()
    {
        GameManager.instance.playersCanMove = false;
        RemovePointsCanvas();
        // Destroy all blood splatters
        foreach (GameObject splatter in PlayerStats.allSplatters)
        {
            if (splatter != null)
                Destroy(splatter);
        }
        PlayerStats.allSplatters.Clear();
        SelectRandomMap();
        // Respawn players
        foreach (PlayerStats player in playerStats)
        {
            player.Respawn();
        }

        matchEnded = false;

        transitionAnim.SetTrigger("FadeOut");
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
        SelectRandomMap();
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
                }
            }

            if (aliveCount == 1 && lastAlivePlayer != null)
            {
                matchEnded = true; // Set this FIRST to avoid repeated triggers
                StartCoroutine(HandleLastPlayerWin(lastAlivePlayer));
                
            }
        }
    }

    IEnumerator HandleLastPlayerWin(PlayerStats winner)
    {
        yield return winner.AddPointsAfterDelay(pointsToGive);
        ShowPointsCanvas(winner.transform, pointsToGive);

        Debug.Log($"Match ended. {winner.name} awarded {pointsToGive} point(s).");
        yield return new WaitForSeconds(2f);
        transitionAnim.SetTrigger("FadeIn");
        yield return new WaitForSeconds(1f);
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
        foreach (PlayerStats player in playerStats)
        {
            if (player.playerAlive)
            {
                player.SetPlayerHealth(1);
            }
        }
    }
}
