using UnityEngine;
using Game.Types;

[CreateAssetMenu(fileName = "TileClass", menuName = "Scriptable Objects/TileClass")]
public class TileClass : ItemClass
{
    public string tileName;
    public GameObject tileObject;
    public Sprite[] tileSprites;
    public Color mapColor;
    public PrefabType prefabType;
    public ItemClass tileDrop;
    public TileClass variant;

    public override ItemType ItemType => ItemType.Block;

}
