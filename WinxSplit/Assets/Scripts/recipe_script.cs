using UnityEngine;
using UnityEngine.InputSystem;

public class RecipeNote : MonoBehaviour
{
    public GameObject recipePanel;
    public GameObject interactText;

    private bool playerNearby = false;
    private bool isOpen = false;

    void Start()
    {
        if (recipePanel != null)
            recipePanel.SetActive(false);

        if (interactText != null)
            interactText.SetActive(false);
    }

    void Update()
    {
        if (playerNearby && Keyboard.current.eKey.wasPressedThisFrame)
        {
            ToggleRecipe();
        }

        if (isOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseRecipe();
        }
    }

    private void ToggleRecipe()
    {
        isOpen = !isOpen;

        if (recipePanel != null)
            recipePanel.SetActive(isOpen);

        if (interactText != null)
            interactText.SetActive(!isOpen);
    }

    private void CloseRecipe()
    {
        isOpen = false;

        if (recipePanel != null)
            recipePanel.SetActive(false);

        if (interactText != null && playerNearby)
            interactText.SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Entered trigger: " + other.name);

        if (other.CompareTag("Player") || other.name.ToLower().Contains("player"))
        {
            playerNearby = true;

            if (interactText != null)
                interactText.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.name.ToLower().Contains("player"))
        {
            playerNearby = false;
            CloseRecipe();

            if (interactText != null)
                interactText.SetActive(false);
        }
    }
}
