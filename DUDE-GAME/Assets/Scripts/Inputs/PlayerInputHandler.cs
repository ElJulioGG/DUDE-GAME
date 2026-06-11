using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

public class PlayerInputHandler : MonoBehaviour
{
    public PlayerMovement playerMovement;
    public PlayerStats playerStats;
    public GunHolder gunHolder;
    public PlayerInput playerInput;
    public int index;

    private NetworkPlayerController _netController;

    // Aim replication throttle: gamepad sticks fire OnAim every frame, which was
    // ~60 reliable RPCs/sec per aiming player (each relayed back out by the server).
    // Local aim stays full-rate for responsiveness; the network only needs ~20 Hz.
    private const float AimSendInterval = 0.05f;
    private float _nextAimSendTime;
    private Vector2 _pendingAim;
    private bool _aimDirty;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        index = playerInput.playerIndex;
        LinkComponents(index);
        DetectAndSetControllerType();
    }

    void Update()
    {
        // Flush the latest aim at the throttled rate so the final direction always
        // reaches the server even when the last input event landed inside the window.
        if (!_aimDirty || _netController == null) return;
        if (Time.unscaledTime < _nextAimSendTime) return;
        _aimDirty = false;
        _nextAimSendTime = Time.unscaledTime + AimSendInterval;
        _netController.RpcAim(_pendingAim);
    }

    public void reasignController(int newIndex)
    {
        // Zero out movement on the old target before re-linking
        if (playerMovement != null)
            playerMovement.SetInputVector(Vector2.zero);

        index = newIndex;
        LinkComponents(newIndex);

        _netController = GameSession.IsOnline
            ? NetworkGameManager.Instance?.GetPlayerSlot(newIndex)
            : null;

        DetectAndSetControllerType();
    }

    private void LinkComponents(int targetIndex)
    {
        // PlayerStats.playerIndex is [SerializeField] — reliable even when the GO is inactive.
        // PlayerMovement.playerIndex is set in Start(), so it's wrong for inactive objects.
        // We anchor on PlayerStats and grab siblings from the same hierarchy.
        var allStats = FindObjectsByType<PlayerStats>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        playerStats = allStats.FirstOrDefault(s => s.GetPlayerIndex() == targetIndex);

        if (playerStats != null)
        {
            playerMovement = playerStats.GetComponent<PlayerMovement>()
                          ?? playerStats.GetComponentInParent<PlayerMovement>(true)
                          ?? playerStats.GetComponentInChildren<PlayerMovement>(true);

            gunHolder = playerStats.GetComponent<GunHolder>()
                     ?? playerStats.GetComponentInParent<GunHolder>(true)
                     ?? playerStats.GetComponentInChildren<GunHolder>(true);
        }
        else
        {
            playerMovement = null;
            gunHolder      = null;
        }
    }

    private void DetectAndSetControllerType()
    {
        var device = playerInput.devices.FirstOrDefault();
        if (device == null) return;

        int controllerType = 0;
        string deviceName = device.name.ToLower();

        if (deviceName.Contains("xbox"))
            controllerType = 0;
        else if (deviceName.Contains("switch") || deviceName.Contains("joycon"))
            controllerType = 1;
        else if (deviceName.Contains("dualshock") || deviceName.Contains("dualsense") || deviceName.Contains("ps"))
            controllerType = 2;

        if (GameManager.instance == null) return;
        switch (index)
        {
            case 0: GameManager.instance.player1ControllerType = controllerType; break;
            case 1: GameManager.instance.player2ControllerType = controllerType; break;
            case 2: GameManager.instance.player3ControllerType = controllerType; break;
            case 3: GameManager.instance.player4ControllerType = controllerType; break;
        }

        Debug.Log($"Player {index} is using controller: {device.displayName}, type: {controllerType}, raw name: {device.name}");
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (playerStats != null && !playerStats.playerAlive)
        {
            if (playerMovement != null) playerMovement.SetInputVector(Vector2.zero);
            return;
        }
        var input = context.ReadValue<Vector2>();
        if (playerMovement != null)
            playerMovement.SetInputVector(input);
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        if (GameManager.instance == null || !GameManager.instance.playersCanAim) return;
        if (playerStats == null || !playerStats.playerAlive) return;
        var dir = context.ReadValue<Vector2>();
        if (gunHolder != null) gunHolder.SetAimDirection(dir); // local: full rate
        if (GameSession.IsOnline && _netController != null)
        {
            _pendingAim = dir;     // network: throttled flush in Update()
            _aimDirty = true;
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (GameManager.instance == null || !GameManager.instance.playersCanPickDrop) return;
        if (playerStats == null || !playerStats.playerAlive) return;
        if (!context.performed) return;
        if (GameSession.IsOnline && _netController != null)
            _netController.RpcInteract();
        else if (gunHolder != null)
            gunHolder.HandlePickDrop();
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        if (GameManager.instance == null || !GameManager.instance.playersCanShoot) return;
        if (playerStats == null || !playerStats.playerAlive) return;
        if (GameSession.IsOnline && _netController != null)
        {
            // Run HandleShoot locally for instant feedback (sound, recoil, camera shake,
            // melee swing). Real bullets/damage stay server-side: WeaponBase.SpawnBullet
            // early-returns on non-server machines, and the owner's ammo is kept accurate
            // by the server via TargetSyncAmmo, so the local cosmetic shot stays in sync.
            if (context.performed)
            {
                if (gunHolder != null) gunHolder.HandleShoot();
                _netController.RpcShootStart();
            }
            else if (context.canceled)
            {
                if (gunHolder != null) gunHolder.HandleStopShoot();
                _netController.RpcShootStop();
            }
        }
        else if (gunHolder != null)
        {
            if (context.performed)     gunHolder.HandleShoot();
            else if (context.canceled) gunHolder.HandleStopShoot();
        }
    }

    public void OnReload(InputAction.CallbackContext context)
    {
        if (GameManager.instance == null || !GameManager.instance.playersCanReload) return;
        if (playerStats == null || !playerStats.playerAlive) return;
        if (!context.performed) return;
        if (GameSession.IsOnline && _netController != null)
        {
            // Run the reload locally too — the owner's ammo is server-synced, and the
            // local coroutine is what animates the reload bar (the server's sync only
            // toggles it, leaving a frozen empty bar otherwise).
            if (gunHolder != null) gunHolder.HandleReload();
            _netController.RpcReload();
        }
        else if (gunHolder != null)
            gunHolder.HandleReload();
    }

    public void OnPowerUp(InputAction.CallbackContext context)
    {
        if (GameManager.instance == null || !GameManager.instance.playersCanPowerUp)
        {
            if (AudioManager.Instance != null && FMODEvents.Instance != null)
                AudioManager.Instance.PlaySound(FMODEvents.Instance.NoPowerUp, transform.position);
            return;
        }
        if (playerStats == null || !playerStats.playerAlive) return;
        if (!context.performed) return;
        if (GameSession.IsOnline && _netController != null)
            _netController.RpcPowerUp();
        else if (GameManager.instance.playersCanMove)
            playerStats.usingPowerUp = true;
    }
}
