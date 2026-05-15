using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CraftSlot :
    MonoBehaviour,
    IDropHandler
{
    public CraftingManager craftingManager;

    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        DraggableCraftItem draggedItem =
            eventData.pointerDrag.GetComponent<DraggableCraftItem>();

        if (draggedItem != null)
        {
            craftingManager.AddCraftingItem(
                draggedItem.itemID
            );
        }
    }
}
