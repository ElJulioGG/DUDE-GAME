using UnityEngine;
using UnityEngine.UI;

public class WeaponBase : MonoBehaviour
{
    private Rigidbody2D playerRb;
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float shootCooldown = 0.25f;
    private float lastShootTime;


    [Header("Recoil")]
    [SerializeField] private float recoilForce = 5f;

    [Header("Ammo Settings")]
    [SerializeField] private int clipSize = 10;
    [SerializeField] private int totalAmmo = 30;
    [SerializeField] private float reloadTime = 2f;
    private int currentAmmo;
    private bool isReloading = false;
    [SerializeField] private int shownCurrentAmmo;
    private Vector2 aimDirection = Vector2.right;

    [Header("Aim Sight")]
    [SerializeField] private GameObject aimSight;
    [SerializeField] private bool sniperSights = false;

    [Header("UI")]
    [SerializeField] private Image reloadFillImage;
    [SerializeField] private Text ammoText; // Optional ammo counter
    [Header("Firing Mode")]
    [SerializeField] private bool fullAutoMode = false;
    [SerializeField] private float autoFireRate = 0.1f;
    private bool isFiring = false;
    private float nextShotTime = 0f;

    public void StartFiring()
    {
        isFiring = true;
        if (!fullAutoMode) Shoot(); // Single-shot fires immediately
    }

    public void StopFiring()
    {
        isFiring = false;
    }

    void Update()
    {
       // UpdateAimSight();

       

        // Update ammo UI
        if (ammoText != null)
        {
            ammoText.text = $"{shownCurrentAmmo}/{totalAmmo}";
        }
        if (isFiring && fullAutoMode && Time.time >= nextShotTime)
        {
            Shoot();
            nextShotTime = Time.time + autoFireRate;
        }
    }
    private void OnEnable()
    {
        UpdateAimSight();
        InitializeWeapon();
    }

    private void Start()
    {
        playerRb = GetComponentInParent<Rigidbody2D>();
        InitializeWeapon();
    }

   

    public void InitializeWeapon()
    {
        currentAmmo = Mathf.Min(clipSize, totalAmmo);
        shownCurrentAmmo = currentAmmo;
        isReloading = false;

        if (reloadFillImage != null)
        {
            reloadFillImage.fillAmount = 0f;
            reloadFillImage.gameObject.SetActive(false);
        }
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

    private void TryShoot()
    {
        if (CanShoot()) Shoot();
    }

    private bool CanShoot()
    {
        return !isReloading &&
               Time.time >= lastShootTime + shootCooldown &&
               currentAmmo > 0;
    }

    public virtual void Shoot()
    {
        lastShootTime = Time.time;
        nextShotTime = Time.time + autoFireRate;

        if (!GameManager.instance.unlimitedBullets)
        {
            currentAmmo--;
            totalAmmo--;
            shownCurrentAmmo = currentAmmo;
        }

        // Spawn bullet
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        Instantiate(bulletPrefab, firePoint.position, Quaternion.Euler(0, 0, angle));
        SoundFXManager.instance.PlaySoundByName("RailGunShot", transform, 0.5f, 1.1f);

        // Recoil
        if (playerRb != null)
        {
            playerRb.AddForce(-aimDirection * recoilForce, ForceMode2D.Impulse);
        }

        // Auto-reload if empty
        if (currentAmmo <= 0 && totalAmmo > 0)
        {
            StartCoroutine(Reload());
        }
    }

    private System.Collections.IEnumerator Reload()
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

        currentAmmo = Mathf.Min(clipSize, totalAmmo);
        shownCurrentAmmo = currentAmmo;
        isReloading = false;

        if (reloadFillImage != null)
        {
            reloadFillImage.fillAmount = 0f;
            reloadFillImage.gameObject.SetActive(false);
        }

        UpdateAimSight();
    }

    private void UpdateAimSight()
    {
        if (aimSight == null) return;
        bool canShow = sniperSights && !isReloading && (Time.time >= lastShootTime + shootCooldown);
        aimSight.SetActive(canShow);
    }

    // Public getters
    public int GetCurrentAmmo() => currentAmmo;
    public int GetTotalAmmo() => totalAmmo;
    public bool IsReloading() => isReloading;
    public bool IsFullAuto() => fullAutoMode;

    // Call this to toggle fire mode
    public void ToggleFireMode() => fullAutoMode = !fullAutoMode;
}