using Game.Types;
using UnityEngine;

[CreateAssetMenu(fileName = "ToolClass", menuName = "Scriptable Objects/ToolClass")]
public class ToolClass : ItemClass
{
    public GameObject toolPrefab;
    
    
    public int damage;
    public float swingStartAngle;
    public float swingEndAngle;
    public float swingDuration;
    public float knockbackForce;
    public override ItemType ItemType => ItemType.Tool;
}
