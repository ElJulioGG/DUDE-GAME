using UnityEngine;
using UnityEngine.UI;

public class WeaponBase : MonoBehaviour
{
    private Rigidbody2D playerRb;
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float shootCooldown = 0.25f;
    private float lastShootTime;

    [SerializeField] private float recoilForce = 5f;

    [Header("Ammo Settings")]
    [SerializeField] private int clipSize = 10;
    [SerializeField] private int totalAmmo = 30; // Max overall ammo
    [SerializeField] private float reloadTime = 2f;
    private int currentAmmo;
    private bool isReloading = false;
    [SerializeField] private int shownCurrentAmmo;
    private Vector2 aimDirection = Vector2.right;

    [Header("Aim Sight Settings")]
    [SerializeField] private GameObject aimSight;
    [SerializeField] private bool sniperSights = false;

    [Header("UI")]
    [SerializeField] private Image reloadFillImage; // Reference to UI fill image

    private void OnEnable()
    {
        UpdateAimSight();

        if (reloadFillImage != null)
        {
            reloadFillImage.fillAmount = 0f;
            reloadFillImage.gameObject.SetActive(false);
        }
    }
    private void Start()
    {
     
        InitializeWeapon();
    }
    private void Update()
    {
        UpdateAimSight();
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

    public virtual void Shoot()
    {
        if (isReloading || Time.time < lastShootTime + shootCooldown) return;

        if (currentAmmo <= 0)
        {
            if (totalAmmo > 0)
            {
                StartCoroutine(Reload());
            }
            else
            {
                Debug.Log("Out of total ammo!");
            }
            return;
        }

        lastShootTime = Time.time;

        if (!GameManager.instance.unlimitedBullets)
        {
            currentAmmo--;
            totalAmmo--;
            shownCurrentAmmo = currentAmmo;
        }

        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        Quaternion bulletRotation = Quaternion.Euler(0, 0, angle);

        Instantiate(bulletPrefab, firePoint.position, bulletRotation);
        SoundFXManager.instance.PlaySoundByName("RailGunShot", transform, 0.5f, 1.1f);

        playerRb = GetComponentInParent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.AddForce(-aimDirection * recoilForce, ForceMode2D.Impulse);
        }
    }

    private System.Collections.IEnumerator Reload()
    {
        isReloading = true;
        UpdateAimSight();

        Debug.Log("Reloading...");

        if (reloadFillImage != null)
        {
            reloadFillImage.gameObject.SetActive(true);
            reloadFillImage.fillAmount = 0f;
        }

        float timer = 0f;
        while (timer < reloadTime)
        {
            timer += Time.deltaTime;
            float fill = timer / reloadTime;

            if (reloadFillImage != null)
            {
                reloadFillImage.fillAmount = fill;
            }

            yield return null;
        }

        int bulletsToLoad = Mathf.Min(clipSize, totalAmmo);
        currentAmmo = bulletsToLoad;
        shownCurrentAmmo = currentAmmo;
        isReloading = false;

        Debug.Log("Reloaded!");
        UpdateAimSight();

        if (reloadFillImage != null)
        {
            reloadFillImage.fillAmount = 0f;
            reloadFillImage.gameObject.SetActive(false);
        }
    }

    private void UpdateAimSight()
    {
        if (aimSight == null) return;

        bool canShowSight = sniperSights && !isReloading && (Time.time >= lastShootTime + shootCooldown);
        aimSight.SetActive(canShowSight);
    }

    public int GetCurrentAmmo() => currentAmmo;
    public int GetTotalAmmo() => totalAmmo;
    public bool IsReloading() => isReloading;
}
