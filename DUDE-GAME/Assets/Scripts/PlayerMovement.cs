using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private int playerIndex = 0;

    private Vector2 moveInput;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        var input = GetComponent<PlayerInput>();
       // Debug.Log("Current action map: " + input.currentActionMap);
    }

    public int getPlayerIndex()
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
