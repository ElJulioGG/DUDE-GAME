using UnityEngine;

public class MeleeWeaponBase : MonoBehaviour
{
    [SerializeField] private GameObject hitbox;
    [SerializeField] private float activeTime = 0.1f;
    [SerializeField] private float recoveryTime = 0.3f;  // New recovery time after attack

    private bool isAttacking = false;
    private Vector2 aimDirection = Vector2.right;

    void Start()
    {
        if (hitbox != null)
            hitbox.SetActive(false);
    }

    public void Attack()
    {
        if (!isAttacking)
        {
            StartCoroutine(AttackRoutine());
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

    private System.Collections.IEnumerator AttackRoutine()
    {
        isAttacking = true;

        // Activate hitbox for the active time
        if (hitbox != null)
            hitbox.SetActive(true);

        yield return new WaitForSeconds(activeTime);

        if (hitbox != null)
            hitbox.SetActive(false);

        // Recovery time before you can attack again
        yield return new WaitForSeconds(recoveryTime);

        isAttacking = false;
    }
}
