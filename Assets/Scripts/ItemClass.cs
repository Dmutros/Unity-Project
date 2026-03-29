using Game.Types;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemClass", menuName = "Scriptable Objects/ItemClass")]
public abstract class ItemClass : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public int maxStackSize = 10;

    public abstract ItemType ItemType { get; }
}
