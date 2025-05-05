using UnityEngine;

public class WeaponBase : MonoBehaviour
{
    private Rigidbody2D playerRb;
    public GameObject bulletPrefab;
    public Transform firePoint; // Create an empty child called "FirePoint" where bullets come from
    public float shootCooldown = 0.25f;
    private float lastShootTime;
    [SerializeField] private float recoilForce = 5f; // Amount of recoil force applied to the player when shooting

    private Vector2 aimDirection = Vector2.right; // Default aim direction

    public void SetAimDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude > 0.1f)
        {
            aimDirection = dir.normalized;

            // Rotate the weapon
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            // Flip vertically when aiming left
            Vector3 scale = transform.localScale;
            scale.y = dir.x < 0 ? -Mathf.Abs(scale.y) : Mathf.Abs(scale.y);
            transform.localScale = scale;
        }
    }


    public virtual void Shoot()
    {
        playerRb = GetComponentInParent<Rigidbody2D>();
        if (Time.time < lastShootTime + shootCooldown) return;

        lastShootTime = Time.time;

        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        Quaternion bulletRotation = Quaternion.Euler(0, 0, angle);

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, bulletRotation);

        SoundFXManager.instance.PlaySoundByName("RailGunShot", transform, 0.8f, 1.1f);

        if (playerRb != null)
        {
            playerRb.AddForce(-aimDirection * recoilForce, ForceMode2D.Impulse);
            print("shot");
        }
    }



}
