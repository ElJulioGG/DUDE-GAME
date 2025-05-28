using UnityEngine;
using UnityEngine.InputSystem;

public class CursorMovement : MonoBehaviour
{
    private Vector2 move;

    public float speed = 5f;

    // Este método se llamará automáticamente si usas Send Messages
    public void OnMove(InputValue value)
    {
        move = value.Get<Vector2>();
    }

    void Update()
    {
        transform.position += (Vector3)(move * speed * Time.deltaTime);
    }
}
