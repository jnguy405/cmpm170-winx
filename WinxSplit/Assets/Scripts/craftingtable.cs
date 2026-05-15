using UnityEngine;
using UnityEngine.InputSystem;

public class CraftingTable : MonoBehaviour
{
    public CraftingManager craftingManager;

    public GameObject interactText;

    private bool playerNearby = false;

    void Start()
    {
        if (interactText != null)
            interactText.SetActive(false);
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || craftingManager == null)
            return;

        if (playerNearby && keyboard.eKey.wasPressedThisFrame)
            craftingManager.OpenUI();

        if (keyboard.escapeKey.wasPressedThisFrame)
            craftingManager.CloseUI();
    }

    /// <summary>
    /// Fallback when no keyboard / testing in editor — same as legacy ButterflySpawner.
    /// </summary>
    void OnMouseDown()
    {
        if (craftingManager != null)
            craftingManager.OpenUI();
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

        if (interactText != null)
            interactText.SetActive(false);
    }
}
