using UnityEngine;
using UnityEngine.InputSystem;

public class CraftingTable : MonoBehaviour
{
    public CraftingManager craftingManager;

    public GameObject interactText;

    private bool playerNearby = false;

    void OnEnable()
    {
        if (craftingManager != null)
            craftingManager.CraftingUiClosed += OnCraftingUiClosed;
    }

    void OnDisable()
    {
        if (craftingManager != null)
            craftingManager.CraftingUiClosed -= OnCraftingUiClosed;
    }

    void Start()
    {
        if (interactText != null)
            interactText.SetActive(false);
    }

    void HideInteractPrompt()
    {
        if (interactText != null)
            interactText.SetActive(false);
    }

    void RestoreInteractPrompt()
    {
        if (playerNearby && interactText != null)
            interactText.SetActive(true);
    }

    void OnCraftingUiClosed()
    {
        RestoreInteractPrompt();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || craftingManager == null)
            return;

        if (playerNearby && keyboard.eKey.wasPressedThisFrame)
        {
            craftingManager.OpenUI();
            HideInteractPrompt();
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
            craftingManager.CloseUI();
    }

    // Fallback when no keyboard / testing in editor — same as legacy ButterflySpawner.
    void OnMouseDown()
    {
        if (craftingManager != null)
        {
            craftingManager.OpenUI();
            HideInteractPrompt();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerNearby = true;

        if (interactText != null)
            interactText.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerNearby = false;

        if (craftingManager != null)
            craftingManager.CloseUI();

        HideInteractPrompt();
    }
}
