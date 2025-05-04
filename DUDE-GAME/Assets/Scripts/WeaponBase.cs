using UnityEngine;

public class WeaponBase : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint; // Create an empty child called "FirePoint" where bullets come from
    public float shootCooldown = 0.25f;
    private float lastShootTime;

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
        if (Time.time < lastShootTime + shootCooldown) return;

        lastShootTime = Time.time;

        // Calculate rotation based on aim direction
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        Quaternion bulletRotation = Quaternion.Euler(0, 0, angle);

        // Instantiate bullet facing the direction it's aiming
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, bulletRotation);

        //// Set its movement direction
        //Bullet1 bulletScript = bullet.GetComponent<Bullet1>();
        //bulletScript.SetDirection(aimDirection);
    }


}
