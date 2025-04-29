using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 5f;

    private Vector2 moveInput;
    private Rigidbody2D rb;
    private int playerIndex;
    [SerializeField] private PlayerStats playerStats;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        var input = GetComponent<PlayerInput>();
        playerIndex = playerStats.GetPlayerIndex();
        // Debug.Log("Current action map: " + input.currentActionMap);
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
        rb.linearVelocity = moveInput * maxSpeed;
    }

    public void SetInputVector(Vector2 direction)
    {
        moveInput = direction;
    }
}
