using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private PlayerStats playerStats;

    public Vector2 moveInput;
    private Rigidbody2D rb;
    private int playerIndex;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        var input = GetComponent<PlayerInput>();
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
        Vector2 desiredVelocity = moveInput * maxSpeed;
        Vector2 velocityDiff = desiredVelocity - rb.linearVelocity;

        // Apply force to achieve the target velocity over time (responsive but allows physics)
        rb.AddForce(velocityDiff * 10f, ForceMode2D.Force); // Adjust multiplier for snappiness
    }

    public void SetInputVector(Vector2 direction)
    {
        moveInput = direction;
    }
}
