using Game.Types;
using UnityEngine;

[CreateAssetMenu(fileName = "ConsumableClass", menuName = "Scriptable Objects/ConsumableClass")]
public class ConsumableClass : ItemClass
{
    public override ItemType ItemType => ItemType.Consumable;
}
