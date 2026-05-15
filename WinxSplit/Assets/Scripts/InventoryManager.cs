using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Tooltip("Minimum item ID slots (ID 0 .. length-1). Grows automatically if you add higher IDs at runtime.")]
    public int[] itemCounts = new int[8];

    void Awake()
    {
        EnsureCapacity(8);
    }

    void EnsureCapacity(int minLength)
    {
        if (itemCounts == null)
        {
            itemCounts = new int[minLength];
            return;
        }

        if (itemCounts.Length >= minLength)
            return;

        var next = new int[minLength];
        System.Array.Copy(itemCounts, next, itemCounts.Length);
        itemCounts = next;
    }

    void EnsureIdFits(int id)
    {
        if (id < 0) return;
        int need = id + 1;
        if (itemCounts == null || itemCounts.Length < need)
            EnsureCapacity(Mathf.NextPowerOfTwo(Mathf.Max(need, 8)));
    }

    public void AddItem(int id)
    {
        if (id < 0) return;
        EnsureIdFits(id);
        itemCounts[id]++;
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
