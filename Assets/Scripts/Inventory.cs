
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    public Vector2 offset;
    public Vector2 multiplier;
    public GameObject inventoryUI;
    public GameObject inventorySlotPrefab;
    public int inventoryWidth;
    public int inventoryHeight;
    public InventorySlot[,] inventorySlots;
    public GameObject[,] uiSlots;
    public ItemClass tool;
    public ItemClass weapon;
    public ItemClass larva;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        inventorySlots = new InventorySlot[inventoryWidth, inventoryHeight];
        uiSlots = new GameObject[inventoryWidth, inventoryHeight];
        SetupUI();
        UpdateInventoryUI();
        Add(weapon);
        Add(tool);
        Add(larva);
    }

    void SetupUI()
    {
        for (int x = 0; x < inventoryWidth; x++)
        {
            for (int y = 0; y < inventoryHeight; y++)
            {
                GameObject inventorySlot = Instantiate(inventorySlotPrefab, inventoryUI.transform.GetChild(0).transform);
                inventorySlot.GetComponent<RectTransform>().localPosition = new Vector2((x * multiplier.x) + offset.x, (y * multiplier.y) + offset.y);
                uiSlots[x, y] = inventorySlot;
                inventorySlots[x, y] = null;

            }
        }
    }
    void UpdateInventoryUI()
    {
        for (int x = 0; x < inventoryWidth; x++)
        {
            for (int y = 0; y < inventoryHeight; y++)
            {
                if (inventorySlots[x, y] == null)
                {
                    uiSlots[x, y].transform.GetChild(0).GetComponent<Image>().sprite = null;
                    uiSlots[x, y].transform.GetChild(0).GetComponent<Image>().enabled = false;

                    uiSlots[x, y].transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = "0";
                    uiSlots[x, y].transform.GetChild(1).GetComponent<TextMeshProUGUI>().enabled = false;
                }
                else
                {
                    uiSlots[x, y].transform.GetChild(0).GetComponent<Image>().enabled = true;
                    uiSlots[x, y].transform.GetChild(0).GetComponent<Image>().sprite = inventorySlots[x, y].item.icon;

                    uiSlots[x, y].transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = inventorySlots[x, y].quantity.ToString();
                    uiSlots[x, y].transform.GetChild(1).GetComponent<TextMeshProUGUI>().enabled = true;
                }

            }
        }
    }
    public bool Add(ItemClass item)
    {
        bool added = false;
        Vector2Int itemPos = Contains(item);
        if (itemPos != Vector2Int.one * -1)
        {
            if (inventorySlots[itemPos.x, itemPos.y].quantity < item.maxStackSize)
            {
                inventorySlots[itemPos.x, itemPos.y].quantity += 1;
                added = true;
            }
        }
        if (!added)
        {

            // var start = Time.realtimeSinceStartup;

            for (int y = inventoryHeight - 1; y >= 0; y--)
            {
                for (int x = 0; x < inventoryWidth; x++)
                {
                    if (added)
                    {
                        break;
                    }

                    if (inventorySlots[x, y] == null)
                    {
                        /*                    ItemClass instance = ScriptableObject.Instantiate(item);
                                            inventorySlots[x, y] = new InventorySlot(instance, new Vector2Int(x, y));
                        */
                        inventorySlots[x, y] = new InventorySlot(item, new Vector2Int(x, y));
                        added = true;
                        break;
                    }

                }
            }
        }
        UpdateInventoryUI();
        return added;
        //  Debug.Log("Add() time: " + (Time.realtimeSinceStartup - start) * 1000f + " ms");
    }

    public Vector2Int Contains(ItemClass item)
    {
        for (int y = inventoryHeight - 1; y >= 0; y--)
        {
            for (int x = 0; x < inventoryWidth; x++)
            {

                if (inventorySlots[x, y] != null)
                {
                    if (inventorySlots[x, y].item.itemName == item.itemName && inventorySlots[x, y].quantity < item.maxStackSize)
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }

        }
        return Vector2Int.one * -1;
    }


    public bool Remove(ItemClass item)
    {
        for (int y = inventoryHeight - 1; y >= 0; y--)
        {
            for (int x = 0; x < inventoryWidth; x++)
            {
                if (inventorySlots[x, y] != null && inventorySlots[x, y].item == item)
                {
                    inventorySlots[x, y].quantity -= 1 ;
                    if (inventorySlots[x, y].quantity == 0)
                    {
                        inventorySlots[x, y] = null;
                    }
                    UpdateInventoryUI();
                    return true;
                }

            }
        }
        return false;
    }

}
