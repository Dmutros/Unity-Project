using UnityEngine;

[CreateAssetMenu(fileName = "LiquidClass", menuName = "Scriptable Objects/LiquidClass")]
public class LiquidClass : ScriptableObject
{
    public string liquidName;
    public GameObject liquidObject;
    public Sprite[] liquidSprites; // Спрайти для різних рівнів наповнення (0 - пусто, макс - повне)
    public Color liquidColor = Color.white;
    public float viscosity = 1f; // Впливає на швидкість течії (вода = 1, лава = 3)
    public bool damagesPlayer = false;
    public float damageAmount = 0f;
    public float buoyancy = 1f; // Вплив на плавучість гравця (вода = 1, лава = 0.5)
    public bool causesLight = false; // Чи створює світло (для лави)
    public Color lightColor = Color.white;
    public float lightIntensity = 1f;
}
