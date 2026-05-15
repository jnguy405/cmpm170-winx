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
        {
            interactText.SetActive(false);
        }
    }

    void Update()
    {
        if (playerNearby &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            craftingManager.OpenUI();
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            craftingManager.CloseUI();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = true;

            if (interactText != null)
            {
                interactText.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;

            craftingManager.CloseUI();

            if (interactText != null)
            {
                interactText.SetActive(false);
            }
        }
    }
}