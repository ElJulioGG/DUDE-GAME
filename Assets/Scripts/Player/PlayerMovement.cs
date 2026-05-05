using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private PlayerStats playerStats;

    public Vector2 moveInput;
    private Rigidbody2D rb;
    private int playerIndex;

    private float _speedMultiplier = 1f;
    private float _boostEndTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerIndex = playerStats.GetPlayerIndex();
    }

    public void SetPlayerIndex(int index)
    {
        playerIndex = index;
    }

    public int GetPlayerIndex()
    {
        return playerIndex;
    }

    void FixedUpdate()
    {
        if (!GameManager.instance.playersCanMove)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (_speedMultiplier > 1f && Time.time >= _boostEndTime)
            _speedMultiplier = 1f;

        Vector2 desiredVelocity = moveInput * maxSpeed * _speedMultiplier;
        Vector2 velocityDiff = desiredVelocity - rb.linearVelocity;

        // Apply force to achieve the target velocity over time (responsive but allows physics)
        rb.AddForce(velocityDiff * 10f, ForceMode2D.Force); // Adjust multiplier for snappiness
    }

    public void SetInputVector(Vector2 direction)
    {
        moveInput = direction;
    }

    public void ApplySpeedBoost(float multiplier, float duration)
    {
        _speedMultiplier = multiplier;
        _boostEndTime = Time.time + duration;
    }
}
