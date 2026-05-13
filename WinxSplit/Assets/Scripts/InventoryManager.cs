using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    // Index 0 = Item ID 0, Index 1 = Item ID 1, etc.
    // You can now type the numbers directly into this list in the Inspector!
    public int[] itemCounts = new int[5]; 

    public void AddItem(int id)
    {
        if (id >= 0 && id < itemCounts.Length) itemCounts[id]++;
    }

    public bool HasItem(int id)
    {
        return id >= 0 && id < itemCounts.Length && itemCounts[id] > 0;
    }

    public void RemoveItem(int id)
    {
        if (HasItem(id)) itemCounts[id]--;
    }
}