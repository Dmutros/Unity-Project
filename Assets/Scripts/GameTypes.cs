using UnityEngine;

namespace Game.Types
{
    public static class Constants
    {
        public const float BlockSize = 0.08f;
    }

    public enum PrefabType
    {
        Block,
        Wall,
        Decor
    }

    public enum LiquidType
    {
        Water,
        Lava
    }

    public enum ItemType
    {
        Block,
        Weapon,
        Tool,
        Consumable
    }
}
