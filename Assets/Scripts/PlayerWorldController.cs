using UnityEngine;
using Game.Types;
public class PlayerWorldController : MonoBehaviour
{
    public int selectedSlotIndex = 0;
    public Transform hotBarSelector;

    public Inventory inventory;
    public TileClass selectedTile;
    public ItemClass selectedItem;
    public Vector2Int mousePos;
    bool rm;
    public bool lm;
    public WorldGeneration worldGeneration;
    public Vector2 spawnPos;

    public MeleeWeapon weapon;

    public int playerRange = 5;
    float blockSize = 0.08f;
    bool a = true;
    bool lava;
    bool water;

    public float spawnTimer = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Spawn()
    {
        GetComponent<Transform>().position = spawnPos;
        inventory = GetComponent<Inventory>();
    }
    private void FixedUpdate()
    {
        // ќновлюЇмо позиц≥ю гравц€ на м≥н≥карт≥
        Vector2Int playerMapPos = new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y)
        );
        worldGeneration.uiMapSystem.UpdatePlayerPosition(playerMapPos);

        if (a == true)
        {
            SelectItem(selectedSlotIndex);
            a = false;
        }


        SelectItem(selectedSlotIndex);
        if (selectedItem != null)
        {
            if (Vector2.Distance(transform.position / blockSize, mousePos) <= playerRange)
            {
                if (selectedItem.ItemType == ItemType.Block)
                {
                    if (lm && Vector2.Distance(transform.position / blockSize, mousePos) >= 2f)
                    {
                        if (selectedItem is TileClass tile)
                        {
                            if (worldGeneration.placePrefab(tile, mousePos.x, mousePos.y))
                            {
                                inventory.Remove(selectedItem);
                            }

                        }
                    }

                    if (rm)
                    {
                        if (selectedItem is TileClass tile)
                        {
                            if (tile.variant != null)
                            {
                                if (worldGeneration.placePrefab(tile.variant, mousePos.x, mousePos.y))
                                {
                                    inventory.Remove(selectedItem);
                                }

                            }

                        }
                    }
                }

                if (selectedItem.ItemType == ItemType.Tool)
                {
                    if (lm)
                    {
                        worldGeneration.removePrefab(mousePos.x, mousePos.y, PrefabType.Block);
                        worldGeneration.removePrefab(mousePos.x, mousePos.y, PrefabType.Decor);
                    }
                    if (rm)
                    {
                        worldGeneration.removePrefab(mousePos.x, mousePos.y, PrefabType.Wall);
                    }
                }
            }
            if (selectedItem.ItemType == ItemType.Weapon)
            {
                if (lm)
                {
                    weapon.Attack();
                }
            }

            if (selectedItem.ItemType == ItemType.Consumable)
            {
                if (lm)
                {
                    if (spawnTimer >= 2)
                    {
                        FindObjectOfType<MobSpawner>()?.SpawnBoss();
                        spawnTimer = 0;
                    }
                }
            }

            if (lava)
            {
                worldGeneration.liquidManager.AddLiquid(new Vector2Int(mousePos.x, mousePos.y), LiquidType.Lava);
            }
            else if (water)
            {
                worldGeneration.liquidManager.AddLiquid(new Vector2Int(mousePos.x, mousePos.y), LiquidType.Water);
            }
        }


    }
    // Update is called once per frame
    void Update()
    {
        spawnTimer += Time.deltaTime;

        lm = Input.GetMouseButton(0);
        rm = Input.GetMouseButton(1);
        lava = Input.GetKey(KeyCode.H);
        water = Input.GetKey(KeyCode.J);
        mousePos.x = Mathf.RoundToInt((Camera.main.ScreenToWorldPoint(Input.mousePosition).x - blockSize / 2) / blockSize);
        mousePos.y = Mathf.RoundToInt((Camera.main.ScreenToWorldPoint(Input.mousePosition).y - blockSize / 2) / blockSize);

        if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            if (selectedSlotIndex < inventory.inventoryWidth - 1)
            {
                selectedSlotIndex++;
            }
            SelectItem(selectedSlotIndex);
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            if (selectedSlotIndex > 0)
            {
                selectedSlotIndex--;
            }
            SelectItem(selectedSlotIndex);
        }
        hotBarSelector.transform.position = inventory.uiSlots[selectedSlotIndex, 0].transform.position;
    }

    public void SelectItem(int selectedSlotIndex)
    {

        if (inventory.inventorySlots[selectedSlotIndex, 0] != null)
        {
            selectedItem = inventory.inventorySlots[selectedSlotIndex, 0].item;
        }
        else
        {
            selectedItem = null;
        }
    }
}
