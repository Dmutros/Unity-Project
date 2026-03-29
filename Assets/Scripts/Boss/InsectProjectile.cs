using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class InsectProjectile : MonoBehaviour
{
    public float speed = 10f;
    public float lifetime = 3f;
    public float damageAmount = 10f;
    public float knockbackForce = 5f;

    private Vector2 moveDirection;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void Initialize(Vector2 direction)
    {
        moveDirection = direction.normalized;
        GetComponent<Rigidbody2D>().velocity = moveDirection * speed;

        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;

        Debug.Log("Angle " + angle);

        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (moveDirection.x < 0)
        {
            Vector3 scale = transform.localScale;
            scale.y = -Mathf.Abs(scale.y);
            transform.localScale = scale;
        }
        else
        {
            Vector3 scale = transform.localScale;
            scale.y = Mathf.Abs(scale.y);
            transform.localScale = scale;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            HealthComponent target = collision.GetComponent<HealthComponent>();
            if (target != null)
            {
                Vector2 knockDir = ((Vector2)collision.transform.position - (Vector2)transform.position).normalized;
                knockDir.y = 0.5f;
                knockDir.Normalize();
                target.TakeDamage(damageAmount, knockDir, knockbackForce);
            }
        }

    }
}
