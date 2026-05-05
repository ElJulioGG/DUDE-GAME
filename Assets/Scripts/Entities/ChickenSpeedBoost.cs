using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ChickenSpeedBoost : MonoBehaviour
{
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private float speedMultiplier = 1.4f;
    [SerializeField] private float boostDuration = 1.5f;
    [SerializeField] private float fadeStartTime = 1.5f;

    private SpriteRenderer _sprite;
    private bool _collected;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        _sprite = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        _collected = false;

        // Fade out near end of lifetime
        if (_sprite != null)
        {
            Color c = _sprite.color;
            c.a = 1f;
            _sprite.color = c;

            _sprite.DOFade(0f, lifetime - fadeStartTime)
                .SetDelay(fadeStartTime)
                .SetEase(Ease.InQuad);
        }

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_collected) return;
        if (!other.CompareTag("Player")) return;

        var movement = other.GetComponentInParent<PlayerMovement>();
        if (movement == null) return;

        _collected = true;
        movement.ApplySpeedBoost(speedMultiplier, boostDuration);

        SoundFXManager.instance?.PlaySoundByName("PowerUp", transform, 0.6f, 1.2f);

        DOTween.Kill(_sprite);
        Destroy(gameObject);
    }
}
