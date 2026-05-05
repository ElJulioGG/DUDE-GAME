using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

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


    private Tween shakeTween;
    private Tween rotateTween;
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;


    private float lastShootTime = 0f;
    private float nextAutoShotTime = 0f;
    private bool isFiring = false;
    private Vector2 aimDirection = Vector2.right;
    private bool initialized = false;

    private void Start()
    {
        reloadFillImage.gameObject.SetActive(false);
        noAmmoImage.gameObject.SetActive(false);
        playerRb = GetComponentInParent<Rigidbody2D>();

        originalLocalPos = transform.localPosition;
        originalLocalRot = transform.localRotation;

        InitializeWeapon();
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
        else if (currentClipAmmo <= 0 && !isReloading&&!emptySoundPlayed)
        {
            // Play empty clip sound
            if (SoundFXManager.instance != null)
            {
                SoundFXManager.instance.PlaySoundByName("EmptyMag", transform, 0.5f, 1f, false);
                noAmmoImage.gameObject.SetActive(currentClipAmmo <= 0 && reserveAmmo <= 0);

                emptySoundPlayed = true;

                TriggerOutOfAmmoShake();
            }

            // Auto-reload if possible
            if (reserveAmmo > 0)
            {
                StartCoroutine(Reload());
            }
        }
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
        return !isReloading &&
               Time.time >= lastShootTime + cooldown &&
               currentClipAmmo > 0;
    }

    public virtual void Shoot()
    {
        lastShootTime = Time.time;
        nextAutoShotTime = Time.time + autoFireRate;
        currentClipAmmo--;

        float baseAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

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
                    float currentAngle = baseAngle + randomAngle;
                    Instantiate(bulletPrefab, firePoint.position, Quaternion.Euler(0, 0, currentAngle));
                }
            }
            else
            {
                float angleStep = spreadAngle * 2 / (bulletsPerShot - 1);
                float startAngle = baseAngle - spreadAngle;

                for (int i = 0; i < bulletsPerShot; i++)
                {
                    float currentAngle = startAngle + angleStep * i;
                    Instantiate(bulletPrefab, firePoint.position, Quaternion.Euler(0, 0, currentAngle));
                }
            }
        }
        else
        {
            Instantiate(bulletPrefab, firePoint.position, Quaternion.Euler(0, 0, baseAngle));
        }

        // Play shoot sound
        if ( SoundFXManager.instance != null)
        {
            SoundFXManager.instance.PlaySoundByName("RailGunShot", transform, 0.7f, 1f, false);
        }

        if (playerRb != null)
        {
            playerRb.AddForce(-aimDirection * recoilForce, ForceMode2D.Impulse);
        }

        UpdateUI();

        if (currentClipAmmo <= 0 && reserveAmmo > 0)
        {
            StartCoroutine(Reload());
        }
    }

    public IEnumerator Reload()
    {
        isReloading = true;
        UpdateAimSight();

        // Play reload sound
        if ( SoundFXManager.instance != null)
        {
            SoundFXManager.instance.PlaySoundByName("Reload", transform, 0.5f, 1f, false);
        }

        if (reloadFillImage != null)
        {
            reloadFillImage.gameObject.SetActive(true);
            reloadFillImage.fillAmount = 0f;
        }

        float timer = 0f;
        while (timer < reloadTime)
        {
            timer += Time.deltaTime;
            if (reloadFillImage != null)
            {
                reloadFillImage.fillAmount = timer / reloadTime;
            }
            yield return null;
        }

        int ammoNeeded = clipSize - currentClipAmmo;
        int ammoToLoad = Mathf.Min(ammoNeeded, reserveAmmo);

        currentClipAmmo += ammoToLoad;
        reserveAmmo -= ammoToLoad;
        isReloading = false;

        if (reloadFillImage != null)
        {
            reloadFillImage.fillAmount = 0f;
            reloadFillImage.gameObject.SetActive(false);
        }

        UpdateAimSight();
        UpdateUI();
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

    private void UpdateUI()
    {
        if (ammoText != null)
        {
            ammoText.text = $"{currentClipAmmo}/{reserveAmmo}";
        }
    }

    // Public methods
    public int GetCurrentClipAmmo() => currentClipAmmo;
    public int GetReserveAmmo() => reserveAmmo;
    public bool IsReloading() => isReloading;
    public void AddAmmo(int amount) => reserveAmmo = Mathf.Min(reserveAmmo + amount, maxAmmo - currentClipAmmo);
    public void SetAmmo(int clip, int reserve) { currentClipAmmo = clip; reserveAmmo = reserve; UpdateUI(); }
    public void ToggleFireMode() => fullAutoMode = !fullAutoMode;
    public void SetRandomSpread(bool useRandom) => randomSpread = useRandom;
    public void SetSpreadFire(bool enabled) => spreadFireEnabled = enabled;
    public void SetBulletsPerShot(int count) => bulletsPerShot = Mathf.Max(1, count);
    public void SetSpreadAngle(float angle) => spreadAngle = Mathf.Clamp(angle, 0f, 90f);

    void OnDestroy()
    {
        StopAllCoroutines();
    }
}