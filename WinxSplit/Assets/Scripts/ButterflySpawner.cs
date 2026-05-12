using UnityEngine;

public class ButterflySpawner : MonoBehaviour
{
    // Drag your CraftingManager object into this slot in the Inspector
    public CraftingManager craftingManager;

    void OnMouseDown()
    {
        if (craftingManager != null)
        {
            craftingManager.OpenUI();
        }
    }
}