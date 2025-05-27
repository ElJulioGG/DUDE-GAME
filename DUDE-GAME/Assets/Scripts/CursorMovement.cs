using UnityEngine;
using UnityEngine.InputSystem;

public class CursorMovement : MonoBehaviour
{
    public float speed = 5f;

    private Vector2 moveInput;
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    void Update()
    {
        transform.position += (Vector3)(moveInput * speed * Time.deltaTime);

        Vector3 worldSize = mainCamera.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));

        transform.position = new Vector3(
            Mathf.Clamp(transform.position.x, -worldSize.x, worldSize.x),
            Mathf.Clamp(transform.position.y, -worldSize.y, worldSize.y),
            transform.position.z
        );
    }
}
