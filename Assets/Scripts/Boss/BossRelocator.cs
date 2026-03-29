using UnityEngine;
using System.Collections;

public class BossRelocator : MonoBehaviour
{
    public float followThreshold = 15f;
    public float hoverHeight = 5f;
    public float moveSpeed = 4f;

    private Transform player;
    private Rigidbody2D rb;
    private Collider2D col;
    private MonoBehaviour[] behaviorScripts;

    private bool isRelocating = false;
    private float distance;

    private Animator animator;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        animator = GetComponent<Animator>();

        behaviorScripts = GetComponents<MonoBehaviour>();
    }

    void Update()
    {
        if (player == null || isRelocating) return;

        distance = Vector2.Distance(transform.position, player.position);

        if (distance > followThreshold)
        {
            StartCoroutine(RelocateToPlayer());
        }
    }

    private IEnumerator RelocateToPlayer()
    {
        animator.SetBool("isFlying", true);
        isRelocating = true;
        rb.velocity = Vector2.zero;

        foreach (var script in behaviorScripts)
        {
            if (script != this)
                script.enabled = false;
        }

        if (col != null) col.isTrigger = true;
        if (rb != null) rb.gravityScale = 0f;

        Vector2 targetPos = new Vector2(player.position.x, player.position.y + hoverHeight);

        while (Vector2.Distance(rb.position, targetPos) > 0.8f)
        {
            Vector2 newPos = Vector2.MoveTowards(rb.position, targetPos, moveSpeed * Time.deltaTime);
            rb.MovePosition(newPos);
            yield return null;
        }

        foreach (var script in behaviorScripts)
        {
            if (script != this)
                script.enabled = true;
        }
        if (col != null) col.isTrigger = false;
        if (rb != null) rb.gravityScale = 1f;
        animator.SetBool("isFlying", false);
        isRelocating = false;
    }
}
