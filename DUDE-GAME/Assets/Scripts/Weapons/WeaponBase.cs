using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class WeaponBase : MonoBehaviour
{
    private Rigidbody2D playerRb;

    [Header("References")]
    public GameObject bulletPrefab;
    public Transform firePoint;

    [Header("Firing Settings")]
    public float shootCooldown = 0.25f; // cooldown for semi-auto
    public bool fullAutoMode = false;
    public float autoFireRate = 0.1f;   // rate for full auto

    [Header("Recoil")]
    [SerializeField] private float recoilForce = 5f;

    [Header("Ammo Settings")]
    [SerializeField] public int clipSize = 10;
    [SerializeField] public int maxAmmo = 30;
    [SerializeField] private float reloadTime = 2f;
    [SerializeField] public int startingClipAmmo = 10; // New field for starting clip
    [SerializeField] public int startingReserveAmmo = 20; // New field for starting reserve
    public int currentClipAmmo;
    public int reserveAmmo;
    private bool isReloading = false;

    [Header("Aim Sight")]
    [SerializeField] private GameObject aimSight;
    [SerializeField] private bool sniperSights = false;

    [Header("UI")]
    [SerializeField] private Image reloadFillImage;
    [SerializeField] private Text ammoText;

    private float lastShootTime = 0f;
    private float nextAutoShotTime = 0f;
    private bool isFiring = false;
    private Vector2 aimDirection = Vector2.right;

    // Add this at the top of your WeaponBase class
    private bool initialized = false;

    public void InitializeWeapon(bool forceDefaults = false)
    {
        if (forceDefaults)
        {
            // Use the inspector defaults
            currentClipAmmo = startingClipAmmo;
            reserveAmmo = startingReserveAmmo;
        }
        // Otherwise keep existing values

        isReloading = false;

        if (reloadFillImage != null)
        {
            reloadFillImage.fillAmount = 0f;
            reloadFillImage.gameObject.SetActive(false);
        }

        UpdateUI();
    }

    // Modify OnEnable and Start
    private void OnEnable()
    {
        //InitializeWeapon();
        UpdateAimSight();
    }

    private void Start()
    {
        reloadFillImage.gameObject.SetActive(false);
        playerRb = GetComponentInParent<Rigidbody2D>();
        //currentClipAmmo = startingClipAmmo;
        //reserveAmmo = startingReserveAmmo;
        // InitializeWeapon();
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
        if (!fullAutoMode)
        {
            TryShoot(false); // fire once for semi-auto
        }
    }

    public void StopFiring()
    {
        isFiring = false;
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

    private void TryShoot(bool isAuto)
    {
        if (CanShoot(isAuto))
        {
            Shoot();
        }
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

        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        Instantiate(bulletPrefab, firePoint.position, Quaternion.Euler(0, 0, angle));
        SoundFXManager.instance.PlaySoundByName("RailGunShot", transform, 0.5f, 1.1f);

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

    // Ammo management methods
    public int GetCurrentClipAmmo() => currentClipAmmo;
    public int GetReserveAmmo() => reserveAmmo;
    public int GetTotalAmmo() => currentClipAmmo + reserveAmmo;
    public int GetMaxAmmo() => maxAmmo;
    public bool IsReloading() => isReloading;
    public bool IsFullAuto() => fullAutoMode;

    // Ammo modification methods
    public void AddAmmo(int amount)
    {
        reserveAmmo = Mathf.Min(reserveAmmo + amount, maxAmmo - currentClipAmmo);
        UpdateUI();
    }

    public void SetAmmo(int clipAmount, int reserveAmount)
    {
        currentClipAmmo = Mathf.Clamp(clipAmount, 0, clipSize);
        reserveAmmo = Mathf.Clamp(reserveAmount, 0, maxAmmo - currentClipAmmo);
        UpdateUI();
    }

    public void ToggleFireMode() => fullAutoMode = !fullAutoMode;
}