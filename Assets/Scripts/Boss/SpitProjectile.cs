using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SpitProjectile : MonoBehaviour
{
    public float lifetime = 3f;
    public float damageAmount = 10f;
    public float knockbackForce;
    public float speed = 5f;

    private Vector2 moveDirection = Vector2.zero;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetDirection(Vector2 direction)
    {
        moveDirection = direction.normalized;

        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    void Start()
    {
        rb.velocity = moveDirection * speed;
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            HealthComponent target = collision.GetComponent<HealthComponent>();
            if (target != null && target != GetComponent<HealthComponent>())
            {
                Vector2 knockDir = ((Vector2)collision.transform.position - (Vector2)transform.position).normalized;
                knockDir.y = 0.5f;
                knockDir.Normalize();
                target.TakeDamage(damageAmount, knockDir, knockbackForce);
            }

            Destroy(gameObject);
        }
    }
}
