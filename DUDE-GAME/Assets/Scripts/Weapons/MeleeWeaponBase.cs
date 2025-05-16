using UnityEngine;

public class MeleeWeaponBase : MonoBehaviour
{
    [SerializeField] private GameObject hitbox;
    [SerializeField] private float activeTime = 0.1f;

    private bool isAttacking = false;
    private Vector2 aimDirection = Vector2.right;

    void Start()
    {
        if (hitbox != null)
            hitbox.SetActive(false);
    }

    public void Attack()
    {
        print("player Atacking");
        if (!isAttacking)
        {
            StartCoroutine(ActivateHitbox());
        }
    }

    public void SetAimDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude > 0.1f)
        {
            aimDirection = dir.normalized;

            // Rotate the weapon to face the direction
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            // Optional: Flip the hitbox visually if needed
            Vector3 scale = transform.localScale;
            scale.y = dir.x < 0 ? -Mathf.Abs(scale.y) : Mathf.Abs(scale.y);
            transform.localScale = scale;
        }
    }

    private System.Collections.IEnumerator ActivateHitbox()
    {
        isAttacking = true;
        hitbox.SetActive(true);

        yield return new WaitForSeconds(activeTime);

        hitbox.SetActive(false);
        isAttacking = false;
    }
}
