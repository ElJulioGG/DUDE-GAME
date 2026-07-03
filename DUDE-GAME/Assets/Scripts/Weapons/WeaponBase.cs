using FishNet;
using FishNet.Object;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using FMODUnity;
using FMOD.Studio;

public class WeaponBase : MonoBehaviour
{
    private Rigidbody2D playerRb;

    [Header("References")]
    public GameObject bulletPrefab;
    public Transform firePoint;

    [Header("Firing Settings")]
    public float shootCooldown = 0.25f;
    public bool fullAutoMode = false;
    public float autoFireRate = 0.1f;

    [Header("Recoil")]
    [SerializeField] private float recoilForce = 5f;

    [Header("Ammo Settings")]
    [SerializeField] public int clipSize = 10;
    [SerializeField] public int maxAmmo = 30;
    [SerializeField] private float reloadTime = 2f;
    [SerializeField] public int startingClipAmmo = 10;
    [SerializeField] public int startingReserveAmmo = 20;
    public int currentClipAmmo;
    public int reserveAmmo;
    private bool isReloading = false;

    [Header("Aim Sight")]
    [SerializeField] private GameObject aimSight;
    [SerializeField] private bool sniperSights = false;

    [Header("UI")]
    [SerializeField] private Image reloadFillImage;
    [SerializeField] private Image noAmmoImage;
    [SerializeField] private Text ammoText;
    private bool emptySoundPlayed = false;

    [Header("Spread Fire Settings")]
    [SerializeField] private bool spreadFireEnabled = false;
    [SerializeField] private int bulletsPerShot = 5;
    [SerializeField] private float spreadAngle = 15f;
    [SerializeField] private bool randomSpread = true;

    [Header("Out of Ammo Shake Settings")]
    [SerializeField] private float noAmmoShakeDuration = 0.3f;
    [SerializeField] private float shakeStrength = 0.2f;
    [SerializeField] private int shakeVibrato = 40;
    [SerializeField] private float shakeRandomness = 90f;
    [SerializeField] private bool shakeFadeOut = true;
    [SerializeField] private bool shakeSnapping = false;
    [Header("Shoot Camera Shake")]
    [SerializeField] private float shootShakeStrength = 0.1f;
    [SerializeField] private float shootShakeDuration = 0.12f;

    [Header("SFX")]
    [SerializeField] private EventReference shootSoundName;
    [SerializeField] private EventReference reloadSoundName;

    private Tween shakeTween;
    private Tween rotateTween;

    // Managed handle for the reload SFX so a dropped/destroyed weapon can cut it
    // short. The reload one-shot is created through AudioManager.PlayStoppableSound
    // (PlayOneShot is fire-and-forget and can't be stopped) and stopped in OnDestroy,
    // which fires on every drop path (all of them Destroy the weapon GameObject).
    private EventInstance _reloadSoundInstance;
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;


    private float lastShootTime = 0f;
    private float nextAutoShotTime = 0f;
    private bool isFiring = false;
    private Vector2 aimDirection = Vector2.right;
    private bool initialized = false;
    private NetworkPlayerController _netCtrl;

    // True on machines that only render this weapon (online, not the server, not the
    // owner). Their ammo state is meaningless, so cosmetic shots ignore and never
    // mutate it — real ammo lives on the server and is synced to the owner only.
    private bool IsCosmeticOnly =>
        GameSession.IsOnline && !InstanceFinder.IsServerStarted && (_netCtrl == null || !_netCtrl.IsOwner);

    private void Start()
    {
        reloadFillImage.gameObject.SetActive(false);
        noAmmoImage.gameObject.SetActive(false);
        playerRb = GetComponentInParent<Rigidbody2D>();
        _netCtrl = GetComponentInParent<NetworkPlayerController>();

        originalLocalPos = transform.localPosition;
        originalLocalRot = transform.localRotation;

        InitializeWeapon();
    }

    // True while the local Reload() coroutine is animating the bar — the server's
    // synced reload state must not stomp it (their timings differ slightly).
    private bool _localReloadActive;

    // Called by the server's NetworkPlayerController TargetRpc on the owning client
    // to display the reload progress (real ammo + reload state are server-authoritative).
    public void SetExternalReloadingState(bool reloading)
    {
        if (_localReloadActive) return;
        isReloading = reloading;
        if (reloadFillImage != null)
            reloadFillImage.gameObject.SetActive(reloading);
    }

    // Server-side: push current ammo + reload state to the owning client's HUD.
    private void SyncAmmoToOwner()
    {
        if (!GameSession.IsOnline || !InstanceFinder.IsServerStarted) return;
        if (_netCtrl == null) return;
        _netCtrl.ServerSyncAmmo(currentClipAmmo, reserveAmmo, isReloading);
        SyncIndicatorsToObservers();
    }

    // Server-side: tell every OTHER machine when this weapon's visible indicator
    // state changes (reloading / completely dry), so players can read remote guns.
    // Piggybacks on SyncAmmoToOwner's call sites, sends only on change.
    private bool _lastSentReloading;
    private bool _lastSentClipEmpty;
    private bool _lastSentDry;
    private bool _indicatorsSentOnce;

    private void SyncIndicatorsToObservers()
    {
        bool clipEmpty = currentClipAmmo <= 0;
        bool dry = clipEmpty && reserveAmmo <= 0;
        // Always send once per weapon instance so cosmetic copies start from the
        // holder's real state (a re-picked-up dry gun would otherwise stay silent).
        if (_indicatorsSentOnce && isReloading == _lastSentReloading
            && clipEmpty == _lastSentClipEmpty && dry == _lastSentDry) return;
        _indicatorsSentOnce = true;
        _lastSentReloading = isReloading;
        _lastSentClipEmpty = clipEmpty;
        _lastSentDry = dry;
        _netCtrl.ServerSyncWeaponIndicators(isReloading, clipEmpty, dry);
    }

    // Remote machines: the holder's real ammo state, mirrored from the server's
    // indicator broadcasts. Cosmetic copies have no real ammo of their own, so this
    // is what gates their fire feedback — no gunshot sound/camera shake while the
    // real gun has an empty chamber or is reloading.
    private bool _remoteReloading;
    private bool _remoteClipEmpty;

    // The reload bar animates locally over reloadTime (same prefab, same duration).
    private Coroutine _remoteReloadCo;

    // Reload bar progress: fill + color ramp red -> yellow -> green (t = 0..1).
    // Single writer for both the local Reload() bar and the remote observer bar.
    private void SetReloadFill(float t)
    {
        if (reloadFillImage == null) return;
        reloadFillImage.fillAmount = t;
        reloadFillImage.color = t < 0.5f
            ? Color.Lerp(Color.red, Color.yellow, t * 2f)
            : Color.Lerp(Color.yellow, Color.green, (t - 0.5f) * 2f);
    }

    public void ApplyRemoteIndicators(bool reloading, bool clipEmpty, bool outOfAmmo)
    {
        Debug.Log($"[IND-DIAG] {name} ApplyRemoteIndicators reloading={reloading} clipEmpty={clipEmpty} outOfAmmo={outOfAmmo}");

        _remoteReloading = reloading;
        _remoteClipEmpty = clipEmpty;

        // Only CLEAR the icon here (holder regained ammo). Showing it happens in
        // TryShoot's dry-fire branch — the icon appears when the holder pulls the
        // trigger on a dry gun, exactly like on their own machine, not the moment
        // their ammo count hits zero.
        if (!outOfAmmo && noAmmoImage != null)
            noAmmoImage.gameObject.SetActive(false);

        if (reloading && !outOfAmmo)
        {
            if (_remoteReloadCo == null)
                _remoteReloadCo = StartCoroutine(RemoteReloadBar());
        }
        else if (_remoteReloadCo != null)
        {
            StopCoroutine(_remoteReloadCo);
            _remoteReloadCo = null;
            if (reloadFillImage != null)
            {
                SetReloadFill(0f);
                reloadFillImage.gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator RemoteReloadBar()
    {
        if (reloadFillImage != null)
        {
            reloadFillImage.gameObject.SetActive(true);
            SetReloadFill(0f);
        }

        float timer = 0f;
        while (timer < reloadTime)
        {
            timer += Time.deltaTime;
            SetReloadFill(timer / reloadTime);
            yield return null;
        }

        if (reloadFillImage != null)
        {
            SetReloadFill(0f);
            reloadFillImage.gameObject.SetActive(false);
        }
        _remoteReloadCo = null;
    }


    public void InitializeWeapon(bool forceDefaults = false)
    {
        if (forceDefaults)
        {
            currentClipAmmo = startingClipAmmo;
            reserveAmmo = startingReserveAmmo;
        }
        isReloading = false;

        if (reloadFillImage != null)
        {
            reloadFillImage.fillAmount = 0f;
            reloadFillImage.gameObject.SetActive(false);
            noAmmoImage.gameObject.SetActive(false);
        }
        UpdateUI();
        // Fresh pickups initialize on the server — push the starting ammo to the
        // owning client so their local (cosmetic) weapon doesn't think it's empty.
        SyncAmmoToOwner();
    }

    private void OnEnable()
    {
        UpdateAimSight();
    }

    void Update()
    {
        if (isFiring && fullAutoMode && !isReloading && Time.time >= nextAutoShotTime)
        {
            TryShoot(fullAutoMode);
        }
        UpdateUI();
    }

    public void StartFiring()
    {
        isFiring = true;
        emptySoundPlayed = false; // reset when starting a new firing action
        if (!fullAutoMode)
        {
            TryShoot(false);
        }
    }

    public void StopFiring()
    {
        isFiring = false;
        emptySoundPlayed = false; // reset so next press can play the sound again
    }

    private void TryShoot(bool isAuto)
    {
        if (CanShoot(isAuto))
        {
            Shoot();
            emptySoundPlayed = false;
        }
        // Cosmetic copies don't detect dry-fire themselves (their ammo fields are
        // meaningless) — their feedback arrives as a server event (BroadcastDryFire),
        // so it mirrors the holder's real attempts exactly.
        else if (!IsCosmeticOnly && currentClipAmmo <= 0 && !isReloading && !emptySoundPlayed)
        {
            emptySoundPlayed = true;
            PlayDryFireFeedback(reserveAmmo <= 0);

            // Mirror the dry-fire (click + shake, icon when fully dry) to observers.
            if (GameSession.IsOnline && InstanceFinder.IsServerStarted && _netCtrl != null)
                _netCtrl.ServerDryFire(reserveAmmo <= 0);

            // Auto-reload if possible
            if (reserveAmmo > 0)
            {
                StartCoroutine(Reload());
            }
        }
    }

    // Empty-chamber feedback: click + gun shake; the no-ammo icon only when the gun
    // is completely dry (clip AND reserve). Runs locally for the shooter/server and
    // on remote machines via the server's dry-fire broadcast. Deliberately NOT gated
    // on SoundFXManager — that legacy check silently swallowed the whole feedback
    // block (no click, no shake) when the manager wasn't in the scene, even though
    // the sound actually plays through AudioManager.
    public void PlayDryFireFeedback(bool outOfAmmo)
    {
        if (AudioManager.Instance != null && FMODEvents.Instance != null)
            AudioManager.Instance.PlaySound(FMODEvents.Instance.EmptyMag, transform.position);
        if (noAmmoImage != null)
            noAmmoImage.gameObject.SetActive(outOfAmmo);
        TriggerOutOfAmmoShake();
    }
    private void TriggerOutOfAmmoShake()
    {
        shakeTween?.Kill();
        transform.localPosition = originalLocalPos;

        shakeTween = transform.DOShakePosition(
            noAmmoShakeDuration,
            strength: shakeStrength,
            vibrato: shakeVibrato,
            randomness: shakeRandomness,
            snapping: shakeSnapping,
            fadeOut: shakeFadeOut
        ).SetRelative(true);
    }


    private bool CanShoot(bool isAuto)
    {
        float cooldown = isAuto ? autoFireRate : shootCooldown;
        if (isReloading || Time.time < lastShootTime + cooldown) return false;
        // Cosmetic copies gate on the holder's broadcast state — their own ammo is
        // meaningless, and firing the gunshot sound + camera shake while the real
        // gun was empty or reloading was a visible desync on remote machines.
        if (IsCosmeticOnly) return !_remoteReloading && !_remoteClipEmpty;
        return currentClipAmmo > 0;
    }

    // Reused per shot to avoid allocations.
    private readonly List<float> _shotAngles = new List<float>();

    public virtual void Shoot()
    {
        lastShootTime = Time.time;
        nextAutoShotTime = Time.time + autoFireRate;
        if (!IsCosmeticOnly) currentClipAmmo--;

        float baseAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

        // Build the exact shot first (per-bullet angles). Online, the server relays
        // origin + angles to non-owner machines (ServerSendShotData) so their cosmetic
        // bullets start from identical state — bounce trajectories and spread then
        // match the authoritative bullets instead of each machine rolling its own.
        _shotAngles.Clear();
        if (spreadFireEnabled && bulletsPerShot > 1)
        {
            if (randomSpread)
            {
                float minAngleBetweenBullets = spreadAngle * 2 / bulletsPerShot;
                List<float> usedAngles = new List<float>();

                for (int i = 0; i < bulletsPerShot; i++)
                {
                    float randomAngle;
                    int attempts = 0;
                    do
                    {
                        randomAngle = Random.Range(-spreadAngle, spreadAngle);
                        attempts++;
                        if (attempts >= 100) break;
                    }
                    while (usedAngles.Any(a => Mathf.Abs(a - randomAngle) < minAngleBetweenBullets));

                    usedAngles.Add(randomAngle);
                    _shotAngles.Add(baseAngle + randomAngle);
                }
            }
            else
            {
                float angleStep = spreadAngle * 2 / (bulletsPerShot - 1);
                float startAngle = baseAngle - spreadAngle;

                for (int i = 0; i < bulletsPerShot; i++)
                    _shotAngles.Add(startAngle + angleStep * i);
            }
        }
        else
        {
            _shotAngles.Add(baseAngle);
        }

        // Cosmetic-only machines (online, neither server nor owner) skip spawning —
        // their bullets arrive via the server's shot data so they match exactly.
        // They still play the sound/shake feedback below.
        if (!IsCosmeticOnly)
        {
            Vector3 origin = firePoint.position;
            bool isOnlineServer = GameSession.IsOnline && InstanceFinder.IsServerStarted;
            bool isRemoteOwner  = GameSession.IsOnline && !InstanceFinder.IsServerStarted
                                  && _netCtrl != null && _netCtrl.IsOwner;

            // Server: adopt the owner's exact shot (sent just before RpcShootStart) so
            // the authoritative bullets equal the shooter's instant local ones. Clamp
            // the origin so lag (or tampering) can't shoot from across the map.
            if (isOnlineServer && _netCtrl != null
                && _netCtrl.TryConsumeShotData(out Vector2 ownerOrigin, out float[] ownerAngles))
            {
                if (((Vector2)origin - ownerOrigin).sqrMagnitude <= 9f)
                    origin = new Vector3(ownerOrigin.x, ownerOrigin.y, firePoint.position.z);
                _shotAngles.Clear();
                _shotAngles.AddRange(ownerAngles);
            }

            foreach (float angle in _shotAngles)
                SpawnBullet(bulletPrefab, origin, Quaternion.Euler(0, 0, angle));

            if (isOnlineServer && _netCtrl != null)
                _netCtrl.ServerSendShotData(origin, _shotAngles.ToArray());
            else if (isRemoteOwner)
                _netCtrl.RpcSubmitShotData(origin, _shotAngles.ToArray());
        }

        // Play shoot sound
        /* if ( SoundFXManager.instance != null)
        {
            SoundFXManager.instance.PlaySoundByName("RailGunShot", transform, 0.7f, 1f, false);
        } */
        AudioManager.Instance?.PlaySound(shootSoundName, transform.position);
        if (CameraShakeManager.Instance != null) CameraShakeManager.Instance.Shake(shootShakeStrength, shootShakeDuration);

        if (playerRb != null)
        {
            playerRb.AddForce(-aimDirection * recoilForce, ForceMode2D.Impulse);
        }

        if (!IsCosmeticOnly)
        {
            UpdateUI();
            SyncAmmoToOwner();
            // No auto-reload here: the reload starts on the NEXT trigger pull, via
            // TryShoot's dry-fire branch, so the player gets the click + shake
            // feedback that an instant post-shot reload was swallowing.
        }
    }

    public IEnumerator Reload()
    {
        isReloading = true;
        _localReloadActive = true;
        UpdateAimSight();
        SyncAmmoToOwner();

        // Play reload sound
        /* if ( SoundFXManager.instance != null)
        {
            SoundFXManager.instance.PlaySoundByName("Reload", transform, 0.5f, 1f, false);
        } */
        // Keep the handle so dropping the weapon mid-reload can cut the sound.
        StopReloadSound();
        if (AudioManager.Instance != null && !reloadSoundName.IsNull)
            _reloadSoundInstance = AudioManager.Instance.PlayStoppableSound(reloadSoundName, transform.position);

        if (reloadFillImage != null)
        {
            reloadFillImage.gameObject.SetActive(true);
            SetReloadFill(0f);
        }

        float timer = 0f;
        while (timer < reloadTime)
        {
            timer += Time.deltaTime;
            SetReloadFill(timer / reloadTime);
            yield return null;
        }

        int ammoNeeded = clipSize - currentClipAmmo;
        int ammoToLoad = Mathf.Min(ammoNeeded, reserveAmmo);

        currentClipAmmo += ammoToLoad;
        reserveAmmo -= ammoToLoad;
        isReloading = false;
        _localReloadActive = false;

        if (reloadFillImage != null)
        {
            SetReloadFill(0f); // resetea tambien el color a rojo para la proxima recarga
            reloadFillImage.gameObject.SetActive(false);
        }

        UpdateAimSight();
        UpdateUI();
        SyncAmmoToOwner();
    }

    public void SetAimDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude > 0.1f)
        {
            aimDirection = dir.normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            Vector3 scale = transform.localScale;
            scale.y = dir.x < 0 ? -Mathf.Abs(scale.y) : Mathf.Abs(scale.y);
            transform.localScale = scale;
        }
    }

    private void UpdateAimSight()
    {
        if (aimSight == null) return;
        bool canShow = sniperSights && !isReloading && (Time.time >= lastShootTime + shootCooldown);
        aimSight.SetActive(canShow);
    }

    // Cached last-displayed values — Update() calls this every frame, and setting
    // UI text allocates a string and dirties the canvas even when nothing changed.
    private int _uiClip = int.MinValue;
    private int _uiReserve = int.MinValue;

    private void UpdateUI()
    {
        if (ammoText == null) return;
        if (currentClipAmmo == _uiClip && reserveAmmo == _uiReserve) return;
        _uiClip = currentClipAmmo;
        _uiReserve = reserveAmmo;
        ammoText.text = $"{currentClipAmmo}/{reserveAmmo}";
    }

    // Public methods
    public int GetCurrentClipAmmo() => currentClipAmmo;
    public int GetReserveAmmo() => reserveAmmo;
    public bool IsReloading() => isReloading;
    public void AddAmmo(int amount)
    {
        reserveAmmo = Mathf.Min(reserveAmmo + amount, maxAmmo - currentClipAmmo);
        UpdateUI();
        SyncAmmoToOwner(); // keeps owner HUD and observer indicators current
    }
    public void SetAmmo(int clip, int reserve) { currentClipAmmo = clip; reserveAmmo = reserve; UpdateUI(); SyncAmmoToOwner(); }

    // Applies ammo received FROM the server without re-triggering the owner sync.
    // Using SetAmmo for that on a host ping-pongs TargetSyncAmmo with itself forever
    // (thousands of RPCs, and the loop re-applies stale values so the clip never
    // empties — the "unlimited ammo on the host" bug).
    public void ApplySyncedAmmo(int clip, int reserve) { currentClipAmmo = clip; reserveAmmo = reserve; UpdateUI(); }
    public void ToggleFireMode() => fullAutoMode = !fullAutoMode;
    public void SetRandomSpread(bool useRandom) => randomSpread = useRandom;
    public void SetSpreadFire(bool enabled) => spreadFireEnabled = enabled;
    public void SetBulletsPerShot(int count) => bulletsPerShot = Mathf.Max(1, count);
    public void SetSpreadAngle(float angle) => spreadAngle = Mathf.Clamp(angle, 0f, 90f);

    // Cuts the reload SFX immediately. Safe to call when nothing is playing.
    private void StopReloadSound()
    {
        if (_reloadSoundInstance.isValid())
            _reloadSoundInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        _reloadSoundInstance = default;
    }

    void OnDestroy()
    {
        // OnDestroy is the single catch-all for "weapon dropped in any way" — every
        // drop/swap/death path Destroys this GameObject — so stop the reload sound here.
        StopReloadSound();
        StopAllCoroutines();
    }

    // Bullets are LOCAL simulations on every machine — the shot event itself is what
    // replicates (owner input → ServerRpc → ObserversRpc, same pattern as melee).
    // Replicating fast short-lived bullets through NetworkTransform looked stuttery
    // and arrived a round-trip late; local sims are instant and perfectly smooth.
    // Only the server's bullets apply damage (gated inside BulletBehavior).
    protected void SpawnBullet(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        Instantiate(prefab, pos, rot);
    }

    // Spawns this machine's cosmetic bullets from the server's authoritative shot
    // data (exact origin + angles), so trajectories match the server's bullets.
    public void SpawnShotData(Vector2 origin, float[] angles)
    {
        if (bulletPrefab == null || angles == null) return;
        foreach (float angle in angles)
            Instantiate(bulletPrefab, origin, Quaternion.Euler(0, 0, angle));
    }
}