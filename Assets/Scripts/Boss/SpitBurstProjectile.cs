using System.Collections;
using UnityEngine;

public class SpitBurstProjectile : MonoBehaviour
{
    public GameObject smallProjectilePrefab;
    public int minCount = 6;
    public int maxCount = 10;
    public float speed = 5f;
    public float radius = 1f;

    private GameObject boss;
    private Vector2 moveDirection;
    private Rigidbody2D rb;
    private float damageAmount = 20;
    private float knockbackForce = 3;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetOwner(GameObject bossObj)
    {
        boss = bossObj;
    }
    public void SetDirection(Vector2 dir)
    {
        moveDirection = dir.normalized;
    }
    private void Start()
    {
        rb.velocity = moveDirection * speed;
        rb.gravityScale = 0f;

        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        if (moveDirection.x < 0f)
        {
            Vector3 scale = transform.localScale;
            scale.y *= -1;
            transform.localScale = scale;
        }
    }

    public void StartFuse(float delay)
    {
        StartCoroutine(FuseRoutine(delay));
    }

    private IEnumerator FuseRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        Explode();
    }

    private void Explode()
    {
        int count = Random.Range(minCount, maxCount + 1);

        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            GameObject proj = Instantiate(smallProjectilePrefab, transform.position, Quaternion.identity);
            proj.GetComponent<SpitProjectile>()?.SetDirection(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
        }

        Destroy(gameObject);
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
            Explode();
        }

    }
}
