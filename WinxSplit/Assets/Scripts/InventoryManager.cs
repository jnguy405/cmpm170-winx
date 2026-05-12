using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    // Dictionary to store <ItemID, Count>
    public Dictionary<int, int> inventory = new Dictionary<int, int>();

    public void AddItem(int id)
    {
        if (inventory.ContainsKey(id))
            inventory[id]++;
        else
            inventory.Add(id, 1);

        Debug.Log($"Item {id} picked up. Total: {inventory[id]}");
        // Trigger UI Update here later
    }

    public bool HasItem(int id) => inventory.ContainsKey(id) && inventory[id] > 0;

    public void RemoveItem(int id)
    {
        if (HasItem(id)) inventory[id]--;
    }
}