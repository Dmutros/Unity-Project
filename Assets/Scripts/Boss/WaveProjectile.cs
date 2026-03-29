using UnityEngine;

public class WaveProjectile : MonoBehaviour
{
    public float speed = 5f;
    public float lifetime = 1f;
    private Vector2 direction = Vector2.right;
    public float damageAmount = 10f;
    public float knockbackForce;

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        transform.localScale = new Vector3(Mathf.Sign(direction.x), 1, 1);
    }
    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, speed * Time.deltaTime + 0.1f, LayerMask.GetMask("Ground"));
        if (hit.collider != null)
        {
            Destroy(gameObject);
            return;
        }
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        StickToGround();
    }

    private void StickToGround()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position + Vector3.up * 0.1f, Vector2.down, 0.5f, LayerMask.GetMask("Ground"));
        if (hit.collider != null)
        {
            Vector3 pos = transform.position;
            pos.y = hit.point.y + 0.05f;
            transform.position = pos;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            HealthComponent target = collision.gameObject.GetComponent<HealthComponent>();
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
