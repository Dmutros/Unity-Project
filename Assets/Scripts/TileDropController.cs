using UnityEngine;

public class TileDropController : MonoBehaviour
{
    public ItemClass item;
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.CompareTag("Player") && col is CapsuleCollider2D)
        {
/*            ItemClass instance = ScriptableObject.Instantiate(item);
            col.GetComponent<Inventory>().Add(instance);*/
            if (col.GetComponent<Inventory>().Add(item))
            {
                Destroy(this.gameObject);
            }
            
        }
    }
}
