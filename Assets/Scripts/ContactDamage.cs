using UnityEngine;

public class ContactDamage : MonoBehaviour
{
    public float damageAmount = 10f;
    public float knockbackForce;

    /*private void OnCollisionEnter2D(Collision2D collision)
    {
        HealthComponent target = collision.gameObject.GetComponent<HealthComponent>();
        if (target != null && target != GetComponent<HealthComponent>())
        {
            target.TakeDamage(damageAmount);
        }
    }*/

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            HealthComponent target = collision.gameObject.GetComponent<HealthComponent>();
            if (target != null && target != GetComponent<HealthComponent>())
            {
                Vector2 knockDir = ((Vector2)collision.transform.position - (Vector2)transform.position).normalized;
                knockDir.y = 0.5f;
                knockDir.Normalize();
                target.TakeDamage(damageAmount, knockDir, knockbackForce);
            }
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
