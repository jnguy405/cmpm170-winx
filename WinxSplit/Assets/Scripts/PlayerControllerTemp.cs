using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControllerTemp : MonoBehaviour
{
    public float speed = 5f;
    public float sensitivity = 0.5f;
    public InventoryManager inventory; // Link this in inspector
    float xRotation = 0f;
    bool isCursorLocked = true;

    void Start() => UpdateCursorState();

    void Update()
    {
        // Toggle Cursor with Tab for UI interaction
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            isCursorLocked = !isCursorLocked;
            UpdateCursorState();
        }

        if (isCursorLocked) HandleMovement();
        
        // Pickup Logic: Click to collect item
        if (Mouse.current.leftButton.wasPressedThisFrame && !isCursorLocked)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform.TryGetComponent(out CollectibleItem item))
                {
                    inventory.AddItem(item.itemID);
                    Destroy(hit.transform.gameObject);
                }
            }
        }
    }

    void UpdateCursorState()
    {
        Cursor.lockState = isCursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isCursorLocked;
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