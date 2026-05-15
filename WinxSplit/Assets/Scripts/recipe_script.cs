using UnityEngine;

public class RecipeNote : MonoBehaviour
{
    [Header("UI")]
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
        if (playerNearby && Input.GetKeyDown(KeyCode.E))
        {
            ToggleRecipe();
        }

        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
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
        if (other.CompareTag("Player"))
        {
            playerNearby = true;

            if (interactText != null && !isOpen)
                interactText.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;
            CloseRecipe();

            if (interactText != null)
                interactText.SetActive(false);
        }
    }
}
