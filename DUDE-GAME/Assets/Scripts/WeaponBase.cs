using UnityEngine;

public class WeaponBase : MonoBehaviour
{
    private Rigidbody2D playerRb;
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float shootCooldown = 0.25f;
    private float lastShootTime;

    [SerializeField] private float recoilForce = 5f;

    [Header("Ammo Settings")]
    [SerializeField] private int maxAmmo = 10;
    [SerializeField] private float reloadTime = 2f;
    private int currentAmmo;
    private bool isReloading = false;

    private Vector2 aimDirection = Vector2.right;

    private void OnEnable()
    {
        currentAmmo = maxAmmo;
        isReloading = false;
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
        if (isReloading) return;
        if (Time.time < lastShootTime + shootCooldown) return;
        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        lastShootTime = Time.time;
        if (!GameManager.instance.unlimitedBullets)
        {
            currentAmmo--;
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
        Debug.Log("Reloading...");
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = maxAmmo;
        isReloading = false;
        Debug.Log("Reloaded!");
    }

    public int GetCurrentAmmo() => currentAmmo;
    public bool IsReloading() => isReloading;
}
