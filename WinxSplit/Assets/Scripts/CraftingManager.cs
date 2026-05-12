using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CraftingManager : MonoBehaviour
{
    public ButterflyManager butterflyManager;
    public InventoryManager inventory;
    public GameObject craftingUIPanel;
    
    public Image[] slotImages; // Drag your 3 UI Box images here
    public Color activeColor = Color.white;
    public Color emptyColor = new Color(1, 1, 1, 0.2f);
    
    private List<int> currentCombo = new List<int>();

    void Start() => CloseUI();

    public void OpenUI()
    {
        craftingUIPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseUI()
    {
        craftingUIPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SelectItemForCrafting(int itemID)
    {
        if (currentCombo.Count >= 3) {
            Debug.Log("Crafting slots are full!");
            return;
        }

        if (inventory.HasItem(itemID))
        {
            currentCombo.Add(itemID);
            inventory.RemoveItem(itemID);
            UpdateUI();
        }
        else {
            Debug.Log($"You don't have any of Item ID: {itemID} in your inventory!");
        }
    }

    public void Craft()
    {
        if (currentCombo.Count == 3)
        {
            // Spawns the butterfly at the center of the world
            butterflyManager.CheckRecipe(currentCombo.ToArray(), Vector3.zero);
            currentCombo.Clear();
            UpdateUI();
            CloseUI();
        }
        else {
            Debug.Log("You need 3 items to craft!");
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