using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CraftingManager : MonoBehaviour
{
    public ButterflyManager butterflyManager;
    public InventoryManager inventory;
    public GameObject craftingUIPanel;
    
    [Header("UI Slots")]
    public Image[] slotImages; // Drag your 3 UI Box images here
    public Sprite[] itemSprites; // Drag your item icons here in ID order
    public Sprite emptySlotSprite; // Optional
    
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
        if (currentCombo.Count >= 3) return;

        if (inventory.HasItem(itemID))
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
            if (i < currentCombo.Count)
            {
                // Set the sprite to the one matching the item ID
                int id = currentCombo[i];
                slotImages[i].sprite = itemSprites[id];
                slotImages[i].color = activeColor;
            }
            else
            {
                // Show empty slot sprite and lower the alpha
                slotImages[i].sprite = emptySlotSprite;
                slotImages[i].color = emptyColor;
            }
        }
    }

    public void Craft()
    {
        if (currentCombo.Count == 3)
        {
            butterflyManager.CheckRecipe(currentCombo.ToArray(), Vector3.zero);
            currentCombo.Clear();
            UpdateUI();
            CloseUI();
        }
    }

    public void Cancel()
    {
        foreach (int id in currentCombo) inventory.AddItem(id);
        currentCombo.Clear();
        UpdateUI();
    }

    public void AddCraftingItem(int itemID)
    {
        if (currentCombo.Count >= 3)
            return;

        if (inventory.HasItem(itemID))
        {
            currentCombo.Add(itemID);

            inventory.RemoveItem(itemID);

            UpdateUI();
        }
    }
}