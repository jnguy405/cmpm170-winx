using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CraftingManager : MonoBehaviour
{
    public ButterflyManager butterflyManager;
    public InventoryManager inventory;
    public GameObject craftingUIPanel;

    [Header("UI Slots")]
    public Image[] slotImages;
    public Sprite[] itemSprites;
    public Sprite emptySlotSprite;

    public Color activeColor = Color.white;
    public Color emptyColor = new Color(1, 1, 1, 0.2f);

    private List<int> currentCombo = new List<int>();

    void Start() => CloseUI();

    public void OpenUI()
    {
        if (craftingUIPanel != null)
            craftingUIPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseUI()
    {
        if (craftingUIPanel != null)
            craftingUIPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SelectItemForCrafting(int itemID)
    {
        if (currentCombo.Count >= 3) return;

        if (inventory == null)
        {
            Debug.LogError("CraftingManager: Inventory is not assigned.");
            return;
        }

        if (inventory.HasItem(itemID))
        {
            currentCombo.Add(itemID);
            inventory.RemoveItem(itemID);
            UpdateUI();
        }
    }

    void UpdateUI()
    {
        if (slotImages == null) return;

        for (int i = 0; i < slotImages.Length; i++)
        {
            if (i < currentCombo.Count)
            {
                int id = currentCombo[i];
                if (itemSprites != null && id >= 0 && id < itemSprites.Length)
                {
                    slotImages[i].sprite = itemSprites[id];
                    slotImages[i].color = activeColor;
                }
                else
                    Debug.LogWarning($"CraftingManager: No sprite for item id {id}.");
            }
            else
            {
                if (emptySlotSprite != null)
                    slotImages[i].sprite = emptySlotSprite;
                slotImages[i].color = emptyColor.a > Mathf.Epsilon
                    ? emptyColor
                    : new Color(1, 1, 1, 0);
            }
        }
    }

    public void Craft()
    {
        if (butterflyManager == null)
        {
            Debug.LogError("CraftingManager: Butterfly Manager is not assigned.");
            return;
        }

        if (inventory == null)
        {
            Debug.LogError("CraftingManager: Inventory is not assigned.");
            return;
        }

        if (currentCombo.Count != 3)
        {
            Debug.LogWarning("CraftingManager: Place 3 items in the slots before crafting.");
            return;
        }

        int[] combo = currentCombo.ToArray();
        bool crafted = butterflyManager.TryCraft(combo);

        currentCombo.Clear();
        UpdateUI();

        if (!crafted)
        {
            foreach (int id in combo)
                inventory.AddItem(id);
            Debug.LogWarning("CraftingManager: Unknown combination — ingredients returned to inventory.");
            return;
        }

        CloseUI();
    }

    public void Cancel()
    {
        if (inventory == null) return;

        foreach (int id in currentCombo) inventory.AddItem(id);
        currentCombo.Clear();
        UpdateUI();
    }

    public void AddCraftingItem(int itemID)
    {
        if (currentCombo.Count >= 3)
            return;

        if (inventory == null)
        {
            Debug.LogError("CraftingManager: Inventory is not assigned.");
            return;
        }

        if (inventory.HasItem(itemID))
        {
            currentCombo.Add(itemID);
            inventory.RemoveItem(itemID);
            UpdateUI();
        }
    }
}
