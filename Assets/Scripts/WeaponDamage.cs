using UnityEngine;

public class WeaponDamage : MonoBehaviour
{
    MeleeWeapon meleeWeapon;
    private void Start()
    {
        meleeWeapon = GetComponentInParent<MeleeWeapon>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        meleeWeapon.ColDamage(collision);
    }
}
