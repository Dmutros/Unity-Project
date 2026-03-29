using UnityEngine;

public class InventorySlot 
{
    public Vector2Int position;
    public int quantity;
    public ItemClass item;

    public InventorySlot(ItemClass item, Vector2Int position, int quantity = 1)
    {
        this.item = item;
        this.position = position;
        this.quantity = 1;
    }

    public void AddQuantity(int amount = 1)
    {
        quantity += amount;
    }

}
