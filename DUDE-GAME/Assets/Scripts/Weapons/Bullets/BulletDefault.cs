using UnityEngine;

public class BulletDefault : MonoBehaviour
{
    [SerializeField] private int damage = 100;
    [SerializeField] private Sprite[] bloodySprites; // From level 1 to 4 (index 0 to 3)
    [SerializeField] private string spriteChildName = "Sprite"; // Child name with SpriteRenderer

    private SpriteRenderer childRenderer;
    private int currentBloodLevel = 0;

    public int Damage => damage;

    private void Awake()
    {
        Transform spriteChild = transform.Find(spriteChildName);
        if (spriteChild != null)
        {
            childRenderer = spriteChild.GetComponent<SpriteRenderer>();
        }
        else
        {
            Debug.LogWarning("Child with SpriteRenderer not found. Check name.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStats stats = other.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.TakeDamage(damage);

                // Increase blood level up to max
                if (childRenderer != null && bloodySprites != null && currentBloodLevel < bloodySprites.Length)
                {
                    childRenderer.sprite = bloodySprites[currentBloodLevel];
                    currentBloodLevel++; // Go to next blood level
                }

                // Optionally destroy after max level
                // if (currentBloodLevel >= bloodySprites.Length) Destroy(gameObject);
            }
        }
    }
}
