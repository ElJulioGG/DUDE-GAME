using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    [SerializeField] private LevelTimer levelTimer;
    [SerializeField] private PlayerStats[] playerStats;
    [SerializeField] private GameObject[] playerCircles;
    [SerializeField] private GameObject[] players;
    [SerializeField] private Animator transitionAnim;
    public GameObject[] UIIntroObjects;
    public GameObject[] maps;
    public bool matchEnded = false;
    [SerializeField] public int pointsToGive = 1;
    [SerializeField] private GameObject pointsCanvasPrefab;
    [SerializeField] public int aliveCount;
    private GameObject activePointsCanvas;
    private int currentPowerUp;

    private bool wasInAssignmentPhase = false;

    [SerializeField]private ControllerMapper controllerMapper;
   
    IEnumerator PlayerCirclesSpawn()
    {
        if(GameManager.instance.player1Playable){
            playerCircles[0].SetActive(true);
        }
        yield return new WaitForSeconds(0.5f);
        if(GameManager.instance.player2Playable){
            playerCircles[1].SetActive(true);
        }
        yield return new WaitForSeconds(0.5f);
            if(GameManager.instance.player3Playable){
            playerCircles[2].SetActive(true);
        }
        yield return new WaitForSeconds(0.5f);
            if(GameManager.instance.player4Playable){
            playerCircles[3].SetActive(true);
        }
    }
    public void NextMatch()
    {
        pointsToGive = 1;
        levelTimer.ResetTimer();
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
        WeaponPickup[] existingPickups = FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None);
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



    public void ActivateAssignedPlayers()
    {
        aliveCount = 0;
        if(GameManager.instance.player1Playable){
            players[0].SetActive(true);
            aliveCount++;
        }else{
            players[0].SetActive(false);
            playerStats[0].playerAlive = false;
        }
        if(GameManager.instance.player2Playable){
            players[1].SetActive(true);
            aliveCount++;
        }else{
            players[1].SetActive(false);
            playerStats[1].playerAlive = false;
        }
        if(GameManager.instance.player3Playable){
            players[2].SetActive(true);
            aliveCount++;
        }else{
            players[2].SetActive(false);
            playerStats[2].playerAlive = false;
        }
        if(GameManager.instance.player4Playable){
            players[3].SetActive(true);
            aliveCount++;
        }else{
            players[3].SetActive(false);
            playerStats[3].playerAlive = false;
        }
    }

    public void StartGame()
    {
        StartCoroutine(MatchBegin());
    }
    private int CountAssignedPlayers()
    {
        var allCursors = FindObjectsByType<PlayerCursor>(FindObjectsSortMode.None);
        int count = 0;
        foreach (var cursor in allCursors)
        {
            if (cursor.IsAssigned)
                count++;
        }
        return count;
    }

    IEnumerator MatchBegin()
    {
        ActivateAssignedPlayers();

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
                levelTimer.StartTimer();
                SoundFXManager.instance.PlaySoundByName("Fight", transform, 0.7f, 0.9f);
                if (!SoundFXManager.instance.IsSoundPlaying("BattleLoop"))
                {
                    SoundFXManager.instance.PlaySoundByName("BattleLoop", transform, 0.2f, 1f, true);
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
                StartCoroutine(PlayerCirclesSpawn());
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
        SoundFXManager.instance.StopSoundByName("BattleTheme");
        SoundFXManager.instance.StopSoundByName("FadeInIntro");
        SoundFXManager.instance.StopSoundByName("BattleLoop");
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;

        if (!SoundFXManager.instance.IsSoundPlaying("BattleTheme"))
        {
            SoundFXManager.instance.PlaySoundByName("BattleTheme", transform, 0.3f, 1f);
        }
        GameManager.instance.assignController = true;
        Invoke("firstStart", 0.01f);
        SelectRandomMap();
        GameManager.instance.playersCanMove = false;
        AssignPlayerPositions();
        Invoke("StartGame", 0.5f);
    }
    void firstStart()
    {
        SoundFXManager.instance.StopSoundByName("BattleTheme");
        if (!SoundFXManager.instance.IsSoundPlaying("FadeInIntro"))
        {
            SoundFXManager.instance.PlaySoundByName("FadeInIntro", transform, 0.2f, 1f);
        }
    }
    private void AssignController()
    {
        controllerMapper.InitializeControllerMapping();
    }
    private void PauseGame()
    {
        Time.timeScale = 0;

        GameManager.instance.gamePaused = true;

        GameManager.instance.playersCanShoot = false;
        GameManager.instance.playersCanPickDrop = false;
        GameManager.instance.playersCanReload = false;
        GameManager.instance.playersCanAim = false;
        GameManager.instance.playersCanPowerUp = false;
       
    }
    private void UnpauseGame()
    {
        Time.timeScale = 1;
         GameManager.instance.gamePaused = false;
        
        GameManager.instance.playersCanShoot = true;
        GameManager.instance.playersCanPickDrop = true;
        GameManager.instance.playersCanReload = true;
        GameManager.instance.playersCanAim = true;
        GameManager.instance.playersCanPowerUp = true;
    }

    void Update()
    {
        // Check if assignController value changed
        if (GameManager.instance.assignController != wasInAssignmentPhase)
        {
            if (GameManager.instance.assignController)
            {
                // Just entered assignment phase
                PauseGame();
                AssignController();
            }
            else
            {
                // Just left assignment phase
                controllerMapper.FinalizeControllerMapping();//quitar despues
                UnpauseGame();
            }
            
            // Update our tracking variable
            wasInAssignmentPhase = GameManager.instance.assignController;
        }
        
        if (Input.GetKeyDown(KeyCode.L))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (!matchEnded)
        {
            aliveCount = 0;
            PlayerStats lastAlivePlayer = null;

            foreach (PlayerStats player in playerStats)
            {
                if (player.playerAlive)
                {
                    aliveCount++;
                    lastAlivePlayer = player;
                }
            }
            print("aliveCount: " + aliveCount);
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

    public IEnumerator HandleDraw()
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
        levelTimer.StopTimer();
        yield return winner.AddPointsAfterDelay(pointsToGive);
        ShowPointsCanvas(winner.transform, pointsToGive);
        if(levelTimer.timeLeft > 0){
        Debug.Log($"Match ended. {winner.name} awarded {pointsToGive} point(s).");
        yield return new WaitForSeconds(2f);
        transitionAnim.SetTrigger("FadeIn");
        yield return new WaitForSeconds(0.5f);
        NextMatch();
        }
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