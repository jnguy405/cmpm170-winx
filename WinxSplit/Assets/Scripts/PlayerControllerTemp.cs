using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControllerTemp : MonoBehaviour
{
    public float speed = 5f;
    public float sensitivity = 0.5f;
    float xRotation = 0f;

    void Update()
    {
        if (Cursor.lockState == CursorLockMode.Locked) 
        {
            HandleMovement();
        }
    }

    void HandleMovement()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * sensitivity;
        xRotation -= mouseDelta.y * Time.deltaTime * 10f;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        Camera.main.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        transform.Rotate(Vector3.up * mouseDelta.x * Time.deltaTime * 10f);

        float moveX = (Keyboard.current.aKey.isPressed ? -1 : 0) + (Keyboard.current.dKey.isPressed ? 1 : 0);
        float moveZ = (Keyboard.current.wKey.isPressed ? 1 : 0) + (Keyboard.current.sKey.isPressed ? -1 : 0);
        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        transform.Translate(move * speed * Time.deltaTime, Space.World);
    }
}