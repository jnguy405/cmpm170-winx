using UnityEngine;
using UnityEngine.InputSystem;

public class ShopTrigger : MonoBehaviour
{
    public GameObject shopUI;
    public GameObject interactText;

    private bool playerNearby = false;
    private bool shopOpen = false;

    void Start()
    {
        if (shopUI != null)
            shopUI.SetActive(false);

        if (interactText != null)
            interactText.SetActive(false);
    }

    void Update()
    {
        // Open shop with Q
        if (playerNearby &&
            Keyboard.current.qKey.wasPressedThisFrame)
        {
            ToggleShop();
        }

        // Close shop with Escape
        if (shopOpen &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseShop();
        }
    }

    void ToggleShop()
    {
        shopOpen = !shopOpen;

        if (shopUI != null)
            shopUI.SetActive(shopOpen);

        Cursor.lockState = shopOpen
            ? CursorLockMode.None
            : CursorLockMode.Locked;

        Cursor.visible = shopOpen;

        if (interactText != null)
            interactText.SetActive(!shopOpen && playerNearby);
    }

    void CloseShop()
    {
        shopOpen = false;

        if (shopUI != null)
            shopUI.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (interactText != null && playerNearby)
            interactText.SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = true;

            if (interactText != null)
                interactText.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;

            CloseShop();

            if (interactText != null)
                interactText.SetActive(false);
        }
    }
}