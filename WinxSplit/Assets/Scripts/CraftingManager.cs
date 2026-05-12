using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CraftingManager : MonoBehaviour
{
    public ButterflyManager butterflyManager;
    public InventoryManager inventory;
    public GameObject craftingUIPanel; // Drag your Canvas Panel here
    public Color activeColor = Color.white;
    public Color emptyColor = new Color(1, 1, 1, 0.2f);
    
    public Image[] slotImages;
    private List<int> currentCombo = new List<int>();

    void Start() => CloseUI(); // Ensure UI is hidden on start

    public void OpenUI()
    {
        craftingUIPanel.SetActive(true);
        // Unlock cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseUI()
    {
        craftingUIPanel.SetActive(false);
        // Re-lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Craft()
    {
        if (currentCombo.Count == 3)
        {
            butterflyManager.CheckRecipe(currentCombo.ToArray(), Vector3.zero);
            currentCombo.Clear();
            UpdateUI();
            
            // Hide the UI once crafting is successful
            CloseUI();
        }
    }

    // Button function: Add item to crafting queue
    public void SelectItemForCrafting(int itemID)
    {
        if (currentCombo.Count < 3 && inventory.HasItem(itemID))
        {
            currentCombo.Add(itemID);
            inventory.RemoveItem(itemID);
            UpdateUI();
        }
    }

    void UpdateUI()
    {
        for (int i = 0; i < slotImages.Length; i++)
        {
            slotImages[i].color = (i < currentCombo.Count) ? activeColor : emptyColor;
        }
    }


    public void Cancel()
    {
        foreach (int id in currentCombo) inventory.AddItem(id);
        currentCombo.Clear();
        UpdateUI();
    }
}