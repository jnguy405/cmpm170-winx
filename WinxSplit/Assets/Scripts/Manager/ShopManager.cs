using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public InventoryManager inventory;

    public int[] itemPrices;

    public void BuyItem(int itemID)
    {
        int price = itemPrices[itemID];

        bool bought =
            CurrencyManager.Instance.SpendDust(price);

        if (bought)
        {
            inventory.AddItem(itemID);

            Debug.Log("Bought item: " + itemID);
        }
        else
        {
            Debug.Log("Not enough Fairy Dust");
        }
    }
}
