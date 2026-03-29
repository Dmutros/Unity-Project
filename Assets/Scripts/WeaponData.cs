using Game.Types;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapons/Melee Weapon")]
public class WeaponData : ItemClass
{
    public GameObject weaponPrefab;
    public Sprite iconSprite;
    public int damage;

    public float swingStartAngle;
    public float swingEndAngle;

    public float swingDuration;

    public float knockbackForce;
    public override ItemType ItemType => ItemType.Weapon;
}
