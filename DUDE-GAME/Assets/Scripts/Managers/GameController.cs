using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public static GameController instance { get; private set; }
    // Enable the GRENADE_DEBUG scripting define to log round resets impacting grenades.
    [SerializeField] private LevelTimer levelTimer;
    [SerializeField] private PlayerStats[] playerStats;
    [SerializeField] private PlayerVisuals[] playerVisuals;
    [SerializeField] private GameObject[] playerCircles;
    [SerializeField] private GameObject[] players;
    [SerializeField] private Animator transitionAnim;
    [SerializeField] private GameObject TimerText;

    [SerializeField] private GameObject[] UIPowerUps;
    [SerializeField] private GameObject[] Mutators;
    [SerializeField] private int mutatorsPerMatch = 1; // how many to spawn each match
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 8f;
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private Camera matchCamera; // assign your main or match camera

    private List<GameObject> activeMutators = new List<GameObject>();
    [SerializeField] private int mutator1InChance = 10;
    [SerializeField] private GameObject mutatorSpawnIndicator;
    [SerializeField] private PoisonStormController poisonStormController;

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

        // Resetear tormenta al iniciar un nuevo round
        if (poisonStormController != null)
        {
            poisonStormController.OnMatchReset();
        }

        //#if GRENADE_DEBUG
        //        // DEBUG:
        //        Debug.Log("[ROUND] NextMatch start resetting round state");
        //#endif
        levelTimer.ResetTimer();
        ClearAllMutators();
        ClearAllWeaponPickups(); // Clear weapons before map change
        SelectRandomMap();
        GameManager.instance.playersCanMove = false;
        GameManager.instance.destroyProyectiles = true;
        RemovePointsCanvas();

        // Destroy all blood splatters
        foreach (GameObject splatter in PlayerVisuals.allSplatters)
        {
            if (splatter != null)
                Destroy(splatter);
        }
        PlayerVisuals.allSplatters.Clear();

        // Respawn players
        foreach (PlayerStats player in playerStats)
        {
            player.Respawn();
        }

        matchEnded = false;
        AssignPlayerPositions();
        Invoke("StartGame", 0.5f);
    }


    private void SpawnMutators()
    {
        StartCoroutine(SpawnMutatorsCoroutine());
    }

    private IEnumerator SpawnMutatorsCoroutine()
    {
        if (Mutators == null || Mutators.Length == 0 || matchCamera == null)
        {
            Debug.LogWarning("Missing Mutators or Camera!");
            yield break;
        }

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = 500;
        Camera cam = matchCamera;

        while (spawned < mutatorsPerMatch && attempts < maxAttempts)
        {
            attempts++;

            // Random point inside camera view (slightly inset to avoid edges)
            float viewportX = Random.Range(0.05f, 0.95f);
            float viewportY = Random.Range(0.05f, 0.95f);

            // Convert viewport to world
            Vector3 viewportPos = new Vector3(viewportX, viewportY, cam.nearClipPlane);
            Vector3 worldPos = cam.ViewportToWorldPoint(viewportPos);
            worldPos.z = 0f;

            // Optional: small random offset to spread them more
            worldPos += (Vector3)Random.insideUnitCircle * 0.5f;

            // Check if area is free (no overlap with walls)
            if (Physics2D.OverlapCircle(worldPos, 1.0f, collisionMask) == null)
            {
                GameObject prefab = Mutators[Random.Range(0, Mutators.Length)];

                //  Spawn indicator first (optional visual feedback)
                if (mutatorSpawnIndicator != null)
                {
                    Instantiate(mutatorSpawnIndicator, worldPos, Quaternion.identity);
                    yield return new WaitForSeconds(1.5f); // small delay before spawning mutator
                }

                //  Spawn actual mutator
                GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity);
                activeMutators.Add(instance);
                spawned++;

                // Wait 1.5 seconds before next spawn
                yield return new WaitForSeconds(.5f);
            }
            else
            {
                // Slight delay even if it failed (keeps rhythm)
                yield return new WaitForSeconds(0.1f);
            }
        }

        Debug.Log($"Spawned {spawned} mutators out of {mutatorsPerMatch} after {attempts} attempts.");
    }

   

    public void ClearAllMutators()
    {
        foreach (GameObject mutator in activeMutators)
        {
            if (mutator != null)
                Destroy(mutator);
        }

        activeMutators.Clear();
        Debug.Log("Cleared all mutators.");
    }
    private void ClearAllWeaponPickups()
    {
#if GRENADE_DEBUG
        // DEBUG:
        Debug.Log("[ROUND] ClearAllWeaponPickups â€“ destroying pickups in world");
#endif
        WeaponPickup[] existingPickups = FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None);
        foreach (WeaponPickup pickup in existingPickups)
        {
            if (pickup != null && pickup.gameObject != null)
            {
                Destroy(pickup.gameObject);
            }
        }
#if GRENADE_DEBUG
        // DEBUG:
        Debug.Log($"[ROUND] Cleared {existingPickups.Length} weapon pickups");
#endif


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

        // Re-link now that player GameObjects are guaranteed active.
        // Needed when FishNet disabled scene NetworkObjects before CSS ran.
        var handlers = FindObjectsByType<PlayerInputHandler>(FindObjectsSortMode.None);
        foreach (var h in handlers)
            h.reasignController(h.index);

        // For online play the generic re-link above uses device index as global index, which is
        // wrong. Re-apply the correct server-assigned global indices from LocalPlayerRegistry.
        if (GameSession.IsOnline)
            OnlineLobbyManager.Instance?.ApplyNetworkAssignment();

        GameManager.instance.destroyProyectiles = false;
        transitionAnim.SetTrigger("FadeOut");
        AudioManager.Instance.PlaySound(FMODEvents.Instance.DoorOpen, transform.position);
        foreach (PlayerStats player in playerStats)
        {
            player.SetPlayerHealth(player.baseHealth);
        }
        AssignPlayerPositions();
        print("MATCH BEGIN");

        for (int i = 0; i < UIIntroObjects.Length; i++)
        {
            UIIntroObjects[i].SetActive(true);
            TimerText.SetActive(false);

            if (i == UIIntroObjects.Length - 1)
            {
                levelTimer.StartTimer();
                AudioManager.Instance.PlaySound(FMODEvents.Instance.VoiceSayFight, transform.position);
                TimerText.SetActive(true);
    
                // Avisar a la tormenta que la partida ha comenzado
                if (poisonStormController != null)
                {
                    poisonStormController.OnMatchStarted();
                }

                // Enable movement immediately when FIGHT appears
                GameManager.instance.playersCanMove = true;
                GameManager.instance.playersCanPowerUp = true;
            }
            if (i == UIIntroObjects.Length - 2)
            {
                AudioManager.Instance.PlaySound(FMODEvents.Instance.VoiceSay1, transform.position);
            }
            if (i == UIIntroObjects.Length - 3)
            {
                AudioManager.Instance.PlaySound(FMODEvents.Instance.VoiceSay2, transform.position);
            }
            if (i == UIIntroObjects.Length - 4)
            {
                StartCoroutine(PlayerCirclesSpawn());
                AudioManager.Instance.PlaySound(FMODEvents.Instance.VoiceSay3, transform.position);
            }

            float waitTime = (i == UIIntroObjects.Length - 1) ? 0.9f : 1f;
            yield return new WaitForSeconds(waitTime);
            UIIntroObjects[i].SetActive(false);
        }
        // Safety: ensure movement is always re-enabled even if the intro loop was empty or short.
        GameManager.instance.playersCanMove  = true;
        GameManager.instance.playersCanPowerUp = true;

        int randomMutatorID = Random.Range(1, mutator1InChance+1); // 1 en 10
        if(randomMutatorID == 1)
        {
            SpawnMutators();
        }
    }

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;

        GameManager.instance.assignController = true;
        GameManager.instance.playersCanMove = false;
        
        SelectRandomMap();
       
        AssignPlayerPositions();
        Invoke("StartGame", 1.35f);
    }
    void firstStart()
    {

        AudioManager.Instance.SetMusicArea(MusicTracks.FIGHT);
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
        Invoke("firstStart", 0.1f);
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
                GameManager.instance.ResetAllPlayerPlayable();
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

        if (Input.GetKeyDown(KeyCode.Alpha0) && GameManager.instance.assignController)
        {
            foreach (var cursor in PlayerCursor.All)
                if (cursor != null && cursor.IsAssigned) cursor.UnassignPlayer();
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
        AudioManager.Instance.PlaySound(FMODEvents.Instance.DoorClose, transform.position);
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
                AudioManager.Instance.PlaySound(FMODEvents.Instance.NoPowerUp, transform.position);
                break;
            case 1:
                AudioManager.Instance.PlaySound(FMODEvents.Instance.PowerUp2, transform.position);
                playerVisuals[playerIndex].ActivateParticlesPowerUP();
                Instakill();
                break;
            case 2:
                AudioManager.Instance.PlaySound(FMODEvents.Instance.PowerUp1, transform.position);
                playerVisuals[playerIndex].ActivateParticlesPowerUP();
                DoublePoints();
                break;
            
                // Other powerup cases...
        }
    }

    IEnumerator HandleLastPlayerWin(PlayerStats winner)
    {
        levelTimer.StopTimer();
        GameManager.instance.playersCanPowerUp = false;
        yield return winner.AddPointsAfterDelay(pointsToGive);
        ShowPointsCanvas(winner.transform, pointsToGive);
        if(levelTimer.timeLeft > 0){
        Debug.Log($"Match ended. {winner.name} awarded {pointsToGive} point(s).");
        yield return new WaitForSeconds(2f);
        transitionAnim.SetTrigger("FadeIn");
        AudioManager.Instance.PlaySound(FMODEvents.Instance.DoorClose, transform.position);
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

        // Apagar todos los mapas
        foreach (GameObject map in maps)
            map.SetActive(false);

        // Elegir uno al azar
        int randomIndex = Random.Range(0, maps.Length);
        GameObject selectedMap = maps[randomIndex];
        selectedMap.SetActive(true);

        Debug.Log($"Map reloaded: {selectedMap.name}");

        // Buscar automáticamente el PoisonStorm en el mapa activo
        poisonStormController = selectedMap.GetComponentInChildren<PoisonStormController>(true);

        if (poisonStormController == null)
            Debug.LogWarning($"El mapa {selectedMap.name} NO tiene PoisonStormController");
        //else
        //    Debug.Log($"PoisonStorm asignado desde {selectedMap.name}: {poisonStormController.name}");
    }


    public void Instakill()
    {
        AudioManager.Instance.PlaySound(FMODEvents.Instance.VoiceInstakill, transform.position);
        UIPowerUps[0].SetActive(true);
        foreach (PlayerStats player in playerStats)
        {
            if (player.playerAlive)
            {
                player.SetPlayerHealth(1);
            }
        }
    }
    public void Awake()
    {
        instance = this;
        PauseGame();
        Cursor.visible = false;
    }

    // Returns the index of the currently active map (used by server to tell clients which map to load).
    public int CurrentMapIndex()
    {
        for (int i = 0; i < maps.Length; i++)
            if (maps[i] != null && maps[i].activeSelf) return i;
        return 0;
    }

    // Called on clients to match the server's map selection before the match begins.
    public void SetMapByIndex(int index)
    {
        if (maps == null || index < 0 || index >= maps.Length) return;
        foreach (var map in maps) map.SetActive(false);
        maps[index].SetActive(true);
        poisonStormController = maps[index].GetComponentInChildren<PoisonStormController>(true);
        AssignPlayerPositions();
        Debug.Log($"[GameController] Map synced to: {maps[index].name}");
    }
    public void DoublePoints()
    {
        AudioManager.Instance.PlaySound(FMODEvents.Instance.VoiceDoublePoints, transform.position);
        UIPowerUps[1].SetActive(true);
        pointsToGive *= 2;
    }
}


