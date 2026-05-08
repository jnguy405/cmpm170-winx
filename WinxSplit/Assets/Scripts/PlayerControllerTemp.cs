using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControllerTemp : MonoBehaviour
{
    public float speed = 5f;
    public float sensitivity = 0.5f;
    float xRotation = 0f;

    void Start() => Cursor.lockState = CursorLockMode.Locked;

    void Update()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * sensitivity;
        
        xRotation -= mouseDelta.y * Time.deltaTime * 10f;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        
        Camera.main.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        transform.Rotate(Vector3.up * mouseDelta.x * Time.deltaTime * 10f);

        float moveX = 0;
        float moveZ = 0;

        if (Keyboard.current.wKey.isPressed) moveZ = 1;
        if (Keyboard.current.sKey.isPressed) moveZ = -1;
        if (Keyboard.current.aKey.isPressed) moveX = -1;
        if (Keyboard.current.dKey.isPressed) moveX = 1;

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        transform.Translate(move * speed * Time.deltaTime, Space.World);
    }
}