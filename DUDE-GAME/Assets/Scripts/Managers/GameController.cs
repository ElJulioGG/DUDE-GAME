using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Object;
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
    [SerializeField] private  int pointsToWin = 10;
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

        if (poisonStormController != null)
            poisonStormController.OnMatchReset();

        if (levelTimer != null) levelTimer.ResetTimer();
        ClearAllMutators();
        ClearAllWeaponPickups();
        ClearAllGrenades();

        // Map selection: only the server picks the map so all machines load the same one.
        // Non-host clients run the whole round reset via RoundStartBroadcast → OnNetworkRoundStart().
        if (!GameSession.IsOnline || InstanceFinder.IsServerStarted)
            SelectRandomMap();

        if (GameSession.IsOnline && InstanceFinder.IsServerStarted)
            NetworkGameManager.Instance?.ServerBroadcastRoundStart(CurrentMapIndex());

        GameManager.instance.playersCanMove = false;
        GameManager.instance.destroyProyectiles = true;
        RemovePointsCanvas();

        foreach (GameObject splatter in PlayerVisuals.allSplatters)
        {
            if (splatter != null) Destroy(splatter);
        }
        PlayerVisuals.allSplatters.Clear();

        // Respawn: server issues an RPC that re-enables players on all machines.
        // Non-server clients skip the local call — they receive RpcOnPlayerRespawned instead.
        if (GameSession.IsOnline)
        {
            if (InstanceFinder.IsServerStarted)
            {
                foreach (PlayerStats player in playerStats)
                {
                    if (player == null) continue;
                    // Only respawn slots that are actually in the match — re-activating
                    // unowned slots made all 4 players appear on client machines.
                    if (!IsSlotPlayable(player.playerIndex)) continue;
                    var netCtrl = player.GetComponent<NetworkPlayerController>();
                    if (netCtrl != null) netCtrl.ServerRespawn();
                    else                player.Respawn();
                }
            }
        }
        else
        {
            foreach (PlayerStats player in playerStats)
            {
                if (player == null) continue;
                player.Respawn();
            }
        }

        matchEnded = false;
        AssignPlayerPositions();
        Invoke("StartGame", 0.5f);
    }

    // Removes all live grenades from the scene, server-despawning NetworkObjects online.
    private void ClearAllGrenades()
    {
        var grenades = FindObjectsByType<Grenade>(FindObjectsSortMode.None);
        foreach (var g in grenades)
        {
            if (g == null) continue;
            if (GameSession.IsOnline && InstanceFinder.IsServerStarted)
            {
                if (g.IsSpawned) InstanceFinder.ServerManager.Despawn(g.NetworkObject);
                else             Destroy(g.gameObject);
            }
            else if (GameSession.IsOnline)
            {
                // Clients destroy local-only grenades; spawned ones despawn via server.
                if (!g.IsSpawned) Destroy(g.gameObject);
            }
            else
            {
                Destroy(g.gameObject);
            }
        }
    }


    // -------------------------------------------------------------------------
    // Network round flow — non-host clients mirror the server's round lifecycle
    // through these handlers instead of running their own timers / win detection.
    // -------------------------------------------------------------------------

    // True when this player slot is part of the current match.
    public bool IsSlotPlayable(int slot)
    {
        if (GameManager.instance == null) return false;
        switch (slot)
        {
            case 0: return GameManager.instance.player1Playable;
            case 1: return GameManager.instance.player2Playable;
            case 2: return GameManager.instance.player3Playable;
            case 3: return GameManager.instance.player4Playable;
            default: return false;
        }
    }

    public Transform GetPlayerTransform(int slot) =>
        (players != null && slot >= 0 && slot < players.Length && players[slot] != null)
            ? players[slot].transform : null;

    // Client-side NextMatch: same reset as the host but the map comes from the server
    // and respawns arrive via NetworkPlayerController RPCs.
    public void OnNetworkRoundStart(int mapIndex)
    {
        if (!GameSession.IsOnline || InstanceFinder.IsServerStarted) return;

        pointsToGive = 1;

        if (poisonStormController != null)
            poisonStormController.OnMatchReset();

        if (levelTimer != null) levelTimer.ResetTimer();
        ClearAllMutators();
        ClearAllWeaponPickups();
        ClearAllGrenades();

        GameManager.instance.playersCanMove = false;
        GameManager.instance.destroyProyectiles = true;
        RemovePointsCanvas();

        foreach (GameObject splatter in PlayerVisuals.allSplatters)
        {
            if (splatter != null) Destroy(splatter);
        }
        PlayerVisuals.allSplatters.Clear();

        SetMapByIndex(mapIndex);

        matchEnded = false;
        AssignPlayerPositions();
        CancelInvoke(nameof(StartGame));
        Invoke(nameof(StartGame), 0.5f);
    }

    // Client-side round end: play the same points/transition sequence as the host.
    public void OnNetworkRoundEnd(int winnerSlot, int points, bool matchOver)
    {
        if (!GameSession.IsOnline || InstanceFinder.IsServerStarted) return;
        if (matchEnded) return;
        matchEnded = true;
        StartCoroutine(HandleNetworkRoundEnd(winnerSlot, points, matchOver));
    }

    private IEnumerator HandleNetworkRoundEnd(int winnerSlot, int points, bool matchOver)
    {
        if (levelTimer != null) levelTimer.StopTimer();
        GameManager.instance.playersCanPowerUp = false;

        if (winnerSlot >= 0 && winnerSlot < playerStats.Length && playerStats[winnerSlot] != null)
        {
            PlayerStats winner = playerStats[winnerSlot];
            winner.PlayPointsSound();
            ShowPointsCanvas(winner.transform, points);
        }

        yield return new WaitForSeconds(2f);
        if (transitionAnim != null) transitionAnim.SetTrigger("FadeIn");
        if (AudioManager.Instance != null && FMODEvents.Instance != null)
            AudioManager.Instance.PlaySound(FMODEvents.Instance.DoorClose, transform.position);
        yield return new WaitForSeconds(0.5f);

        if (matchOver)
            SceneManager.LoadScene("VictoryScene");
        // Otherwise wait for the server's RoundStartBroadcast → OnNetworkRoundStart().
    }

    public void OnNetworkTimerSync(float serverTimeLeft)
    {
        if (levelTimer != null) levelTimer.NetworkSyncTime(serverTimeLeft);
    }

    // Client-side mutator spawn indicator (cosmetic, mirrors the server's 1.5s warning).
    public void OnNetworkMutatorIndicator(Vector2 pos)
    {
        if (!GameSession.IsOnline || InstanceFinder.IsServerStarted) return;
        if (mutatorSpawnIndicator != null)
            Instantiate(mutatorSpawnIndicator, pos, Quaternion.identity);
    }

    // Client-side mutator spawn: instantiate the same prefab as a puppet that
    // follows the server's streamed state instead of simulating its own AI.
    public void OnNetworkMutatorSpawn(int mutatorId, int prefabIndex, Vector2 pos, bool golden)
    {
        if (!GameSession.IsOnline || InstanceFinder.IsServerStarted) return;
        if (Mutators == null || prefabIndex < 0 || prefabIndex >= Mutators.Length) return;
        if (Mutators[prefabIndex] == null) return;

        GameObject instance = Instantiate(Mutators[prefabIndex], pos, Quaternion.identity);
        activeMutators.Add(instance);

        var chicken = instance.GetComponent<RainbowChicken>();
        if (chicken != null) chicken.InitPuppet(mutatorId, golden);
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
                int prefabIndex = Random.Range(0, Mutators.Length);
                GameObject prefab = Mutators[prefabIndex];
                bool isServerOnline = GameSession.IsOnline && InstanceFinder.IsServerStarted;

                //  Spawn indicator first (optional visual feedback)
                if (mutatorSpawnIndicator != null)
                {
                    if (isServerOnline)
                        NetworkGameManager.Instance?.ServerBroadcastMutatorIndicator(worldPos);
                    Instantiate(mutatorSpawnIndicator, worldPos, Quaternion.identity);
                    yield return new WaitForSeconds(1.5f); // small delay before spawning mutator
                }

                //  Spawn actual mutator
                GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity);
                activeMutators.Add(instance);
                spawned++;

                // Tell clients to spawn the matching puppet (server simulates, clients render).
                if (isServerOnline)
                {
                    var chicken = instance.GetComponent<RainbowChicken>();
                    NetworkGameManager.Instance?.ServerBroadcastMutatorSpawn(
                        chicken != null ? chicken.NetId : 0,
                        prefabIndex,
                        worldPos,
                        chicken != null && chicken.IsGolden);
                }

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
        WeaponPickup[] existingPickups = FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None);
        foreach (WeaponPickup pickup in existingPickups)
        {
            if (pickup == null || pickup.gameObject == null) continue;

            if (GameSession.IsOnline)
            {
                var no = pickup.GetComponent<NetworkObject>();
                bool spawned = no != null && no.IsSpawned;
                if (InstanceFinder.IsServerStarted)
                {
                    if (spawned) InstanceFinder.ServerManager.Despawn(no);
                    else         Destroy(pickup.gameObject);
                }
                else if (!spawned)
                {
                    // Clients destroy local-only leftovers (chicken drops, strays);
                    // spawned pickups are removed by the server's Despawn.
                    Destroy(pickup.gameObject);
                }
            }
            else
            {
                Destroy(pickup.gameObject);
            }
        }
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

                // Flag the jump as a teleport so other machines snap players to their
                // spawn instead of smoothly sliding them across the map. Teleport()
                // no-ops on machines that don't control the player, so it's safe to
                // call for all of them.
                if (GameSession.IsOnline)
                    players[i].GetComponent<FishNet.Component.Transforming.NetworkTransform>()?.Teleport();
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
        if (transitionAnim != null) transitionAnim.SetTrigger("FadeOut");
        if (AudioManager.Instance != null && FMODEvents.Instance != null)
            AudioManager.Instance.PlaySound(FMODEvents.Instance.DoorOpen, transform.position);
        foreach (PlayerStats player in playerStats)
        {
            if (player == null) continue;
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
                if (levelTimer != null) levelTimer.StartTimer();
                if (AudioManager.Instance != null && FMODEvents.Instance != null)
                    AudioManager.Instance.PlaySound(FMODEvents.Instance.VoiceSayFight, transform.position);
                TimerText.SetActive(true);

                if (poisonStormController != null)
                    poisonStormController.OnMatchStarted();

                GameManager.instance.playersCanMove = true;
                GameManager.instance.playersCanPowerUp = true;
            }
            if (i == UIIntroObjects.Length - 2)
            {
                if (AudioManager.Instance != null && FMODEvents.Instance != null)
                    AudioManager.Instance.PlaySound(FMODEvents.Instance.VoiceSay1, transform.position);
            }
            if (i == UIIntroObjects.Length - 3)
            {
                if (AudioManager.Instance != null && FMODEvents.Instance != null)
                    AudioManager.Instance.PlaySound(FMODEvents.Instance.VoiceSay2, transform.position);
            }
            if (i == UIIntroObjects.Length - 4)
            {
                StartCoroutine(PlayerCirclesSpawn());
                if (AudioManager.Instance != null && FMODEvents.Instance != null)
                    AudioManager.Instance.PlaySound(FMODEvents.Instance.VoiceSay3, transform.position);
            }

            float waitTime = (i == UIIntroObjects.Length - 1) ? 0.9f : 1f;
            yield return new WaitForSeconds(waitTime);
            UIIntroObjects[i].SetActive(false);
        }
        // Safety: ensure movement is always re-enabled even if the intro loop was empty or short.
        GameManager.instance.playersCanMove  = true;
        GameManager.instance.playersCanPowerUp = true;

        // Mutator roll is server-only online — each machine rolling independently
        // meant host and clients disagreed on whether a chicken spawned at all.
        // Clients receive MutatorIndicator/MutatorSpawn broadcasts instead.
        if (!GameSession.IsOnline || InstanceFinder.IsServerStarted)
        {
            int randomMutatorID = Random.Range(1, mutator1InChance+1); // 1 en 10
            if(randomMutatorID == 1)
            {
                SpawnMutators();
            }
        }
    }

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;

        // Players are placed in the scene on layer 0 (Default) by default.
        // Bullet damageableMask targets layer 6 ("Player"), so set correct layer/tag at runtime.
        int playerLayer = LayerMask.NameToLayer("Player");
        foreach (var go in players)
        {
            if (go == null) continue;
            go.layer = playerLayer;
            go.tag = "Player";
        }

        // Catch-all: a match starting with no client AND no server is a local match,
        // whatever IsOnline claims (e.g., the user opened the online menu and backed
        // out). A stale true here disables ammo consumption, damage and box breaking.
        if (GameSession.IsOnline && !InstanceFinder.IsClientStarted && !InstanceFinder.IsServerStarted)
        {
            Debug.LogWarning("[GameController] IsOnline was true with no network running — forcing offline mode.");
            GameSession.IsOnline = false;
        }

        GameManager.instance.assignController = true;
        GameManager.instance.playersCanMove = false;

        // Online clients wait for the server's MatchStartBroadcast / RoundStartBroadcast
        // to tell them which map to load — picking randomly here desyncs from the host.
        if (!GameSession.IsOnline || InstanceFinder.IsServerStarted)
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
        
#if UNITY_EDITOR
        // Editor-only debug shortcuts. Never ship these: reloading the scene
        // mid-session desyncs every connected machine in online play.
        if (Input.GetKeyDown(KeyCode.L))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (Input.GetKeyDown(KeyCode.Alpha0) && GameManager.instance.assignController)
        {
            foreach (var cursor in PlayerCursor.All)
                if (cursor != null && cursor.IsAssigned) cursor.UnassignPlayer();
        }
#endif

        // Win condition is now driven by PlayerStats.KillPlayer() calling OnPlayerDied(),
        // so no per-frame polling is needed here. aliveCount is initialized in
        // ActivateAssignedPlayers() and decremented in OnPlayerDied().
    }

    // Called by PlayerStats.KillPlayer() whenever a player dies.
    // Decrements aliveCount and triggers the win/draw flow when appropriate.
    public void OnPlayerDied(PlayerStats deadPlayer)
    {
        if (matchEnded) return;

        // Online clients only mirror the alive count — round end is decided by the
        // server and arrives via RoundEndBroadcast → OnNetworkRoundEnd().
        if (GameSession.IsOnline && !InstanceFinder.IsServerStarted)
        {
            aliveCount = Mathf.Max(0, aliveCount - 1);
            return;
        }

        if (GameManager.instance == null || !GameManager.instance.playersCanMove) return;

        aliveCount--;

        if (aliveCount <= 0)
        {
            matchEnded = true;
            StartCoroutine(HandleDraw());
            return;
        }

        if (aliveCount == 1)
        {
            PlayerStats lastAlive = null;
            foreach (var p in playerStats)
            {
                if (p != null && p.playerAlive)
                {
                    lastAlive = p;
                    break;
                }
            }
            if (lastAlive != null)
            {
                matchEnded = true;
                StartCoroutine(HandleLastPlayerWin(lastAlive));
            }
        }
    }

    public IEnumerator HandleDraw()
    {
        // Round end is server-authoritative — clients receive it via RoundEndBroadcast.
        if (GameSession.IsOnline && !InstanceFinder.IsServerStarted) yield break;

        matchEnded = true;
        if (GameSession.IsOnline && InstanceFinder.IsServerStarted)
            NetworkGameManager.Instance?.ServerBroadcastRoundEnd(-1, 0, false);

        Debug.Log("Draw! No players alive.");
        yield return new WaitForSeconds(2f);
        if (transitionAnim != null) transitionAnim.SetTrigger("FadeIn");
        if (AudioManager.Instance != null && FMODEvents.Instance != null)
            AudioManager.Instance.PlaySound(FMODEvents.Instance.DoorClose, transform.position);
        yield return new WaitForSeconds(0.5f);
        NextMatch();
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < playerStats.Length; i++)
        {
            if (playerStats[i] == null) continue;
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

        // Mirror the activation to clients (banner UI, voice, particles) — this
        // only ever runs on the server online, since usingPowerUp is set via
        // ServerRpc there.
        if (GameSession.IsOnline && InstanceFinder.IsServerStarted)
            NetworkGameManager.Instance?.ServerBroadcastPowerUpUse(playerIndex, currentPowerUp);

        ApplyPowerUp(playerIndex, currentPowerUp);
    }

    // Client mirror of a pickup the server detected: consume this machine's copy of
    // the pickup object (same spawn coordinates on every machine) and grant the slot.
    public void OnNetworkPowerUpPickup(int slot, int type, Vector2 pos)
    {
        if (!GameSession.IsOnline || InstanceFinder.IsServerStarted) return;

        PowerUpTrigger best = null;
        float bestSqr = 4f; // within 2 units
        foreach (var p in FindObjectsByType<PowerUpTrigger>(FindObjectsSortMode.None))
        {
            float sqr = ((Vector2)p.transform.position - pos).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = p; }
        }

        if (best != null)
        {
            best.ApplyPickup(slot, type);
        }
        else
        {
            // No local copy found — still grant it so the HUD icon stays correct.
            PowerUpTrigger.Grant(slot, type);
            if (AudioManager.Instance != null && FMODEvents.Instance != null)
                AudioManager.Instance.PlaySound(FMODEvents.Instance.PickUpPowerUp, pos);
        }
    }

    // Client mirror of an activation: clear the HUD slot and play the same feedback.
    // Gameplay inside the effects is authority-gated (Instakill damages nothing on
    // clients), so only the banner/voice/particles run here.
    public void OnNetworkPowerUpUse(int slot, int type)
    {
        if (!GameSession.IsOnline || InstanceFinder.IsServerStarted) return;
        PowerUpTrigger.Grant(slot, 0); // consume — the server already decided the type
        ApplyPowerUp(slot, type);
    }

    private void ApplyPowerUp(int playerIndex, int type)
    {
        switch (type)
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

    // Win Condition Test -------------------------------------------------------------------------
    private int GetGlobalScore(int playerIndex)
    {
        switch (playerIndex)
        {
            case 0: return GameManager.instance.player1Score;
            case 1: return GameManager.instance.player2Score;
            case 2: return GameManager.instance.player3Score;
            case 3: return GameManager.instance.player4Score;
            default: return 0;
        }
    }

    IEnumerator HandleLastPlayerWin(PlayerStats winner)
    {
        if (levelTimer != null) levelTimer.StopTimer();
        GameManager.instance.playersCanPowerUp = false;

        // Award points once — server-authoritative online, local for offline.
        if (GameSession.IsOnline)
        {
            if (InstanceFinder.IsServerStarted)
            {
                // Mirror the offline pacing: the point lands 1.5s after the win, and
                // only if the winner is still alive (same rules as AddPointsAfterDelay).
                yield return new WaitForSeconds(1.5f);

                bool awardPoints = winner.playerAlive;
                if (awardPoints && winner.TryGetComponent(out NetworkPlayerController netCtrl))
                    netCtrl.ServerAddScore(pointsToGive);

                // Broadcast AFTER the award decision so MatchOver (and the points the
                // clients display) are facts, not predictions — clients then play their
                // own canvas/sound immediately on receive, matching this timing.
                bool matchOver = GetGlobalScore(winner.playerIndex) >= pointsToWin;
                NetworkGameManager.Instance?.ServerBroadcastRoundEnd(
                    winner.playerIndex, awardPoints ? pointsToGive : 0, matchOver);

                if (awardPoints) winner.PlayPointsSound();
            }
        }
        else
        {
            yield return winner.AddPointsAfterDelay(pointsToGive);
        }

        ShowPointsCanvas(winner.transform, pointsToGive);
        Debug.Log($"Match ended. {winner.name} awarded {pointsToGive} point(s).");

        yield return new WaitForSeconds(2f);
        if (transitionAnim != null) transitionAnim.SetTrigger("FadeIn");
        if (AudioManager.Instance != null && FMODEvents.Instance != null)
            AudioManager.Instance.PlaySound(FMODEvents.Instance.DoorClose, transform.position);
        yield return new WaitForSeconds(0.5f);

        int finalScore = GetGlobalScore(winner.playerIndex);

        if (finalScore >= pointsToWin)
        {
            Debug.Log($"Player {winner.playerIndex} won with {finalScore} points! Loading Victory Screen.");
            SceneManager.LoadScene("VictoryScene");
        }
        else
        {
            Debug.Log("No winner yet. Starting next round.");
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
        if (AudioManager.Instance != null && FMODEvents.Instance != null)
            AudioManager.Instance.PlaySound(FMODEvents.Instance.VoiceInstakill, transform.position);
        UIPowerUps[0].SetActive(true);
        foreach (PlayerStats player in playerStats)
        {
            if (player == null || !player.playerAlive) continue;
            if (GameSession.IsOnline && InstanceFinder.IsServerStarted)
            {
                var netCtrl = player.GetComponent<NetworkPlayerController>();
                if (netCtrl != null) netCtrl.ServerSetHealth(1);
            }
            else if (!GameSession.IsOnline)
            {
                player.SetPlayerHealth(1);
            }
        }
    }
    void Awake()
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


