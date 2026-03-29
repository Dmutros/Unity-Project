using UnityEngine;

public class GroundMovementBehavior : MonoBehaviour
{
    public float speed = 2f;
    private Transform player;
    public float stopDistance = 0.5f;
    private bool facingRight = false;

    public Animator animator;

    private float resumeMoveDelay = 0.5f;
    private float moveCooldownTimer = 0f;
    public bool IsCloseToPlayer { get; private set; }

    private bool isActive = false;
    private Rigidbody2D rb;
    private Collider2D col;

    private float stuckTimer = 0f;
    private float stuckCheckInterval = 0.5f;
    private float stuckThreshold = 1.5f;
    private Vector2 lastPosition;
    private bool ghostMode = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    public void StartBehavior()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        isActive = true;
    }

    public void UpdateBehavior()
    {
        if (!isActive || player == null) return;

        Vector2 direction = player.position - transform.position;
        float distance = direction.magnitude;

        stuckTimer += Time.deltaTime;
        if (stuckTimer >= stuckCheckInterval)
        {
            float moved = Vector2.Distance(transform.position, lastPosition);
            if (moved < 0.05f)
            {
                if (!ghostMode)
                {
                    EnterGhostMode();
                }
            }
            else
            {
                if (ghostMode)
                {
                    if (!IsInsideSolid())
                    {
                        ExitGhostMode();
                    }
                    else
                    {
                        HandleGhostMovement();
                    }
                }
            }

            stuckTimer = 0f;
            lastPosition = transform.position;
        }

        if (distance < stopDistance)
        {
            animator.SetBool("isMoving", false);
            return;
        }

        Vector2 dirNormalized = direction.normalized;
        transform.position += (Vector3)dirNormalized * speed * Time.deltaTime;

        if (dirNormalized.x > 0.1f && transform.localScale.x > 0)
            Flip();
        else if (dirNormalized.x < -0.1f && transform.localScale.x < 0)
            Flip();

        animator.SetBool("isMoving", true);
    }

    private void EnterGhostMode()
    {
        ghostMode = true;
        if (col != null) col.isTrigger = true;
        if (rb != null) rb.gravityScale = 0f;
    }

    private void ExitGhostMode()
    {
        ghostMode = false;
        if (col != null) col.isTrigger = false;
        if (rb != null) rb.gravityScale = 1f;
    }

    private void Flip()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    public void StopBehavior()
    {
        isActive = false;
        animator.SetBool("isMoving", false);
    }

    private bool IsInsideSolid()
    {
        var hits = Physics2D.OverlapBoxAll(transform.position, new Vector2(0.1f, 0.1f), 0f);
        foreach (var hit in hits)
        {
            if (hit.gameObject != gameObject && hit.isTrigger == false)
                return true;
        }
        return false;
    }
    private void HandleGhostMovement()
    {
        if (player == null) return;

        Vector2 dir = (player.position - transform.position).normalized;

        if (player.position.y > transform.position.y)
        {
            dir.y = 1f;
        }
        else
        {
            dir.y = Mathf.Max(0.1f, dir.y);
        }

        transform.position += (Vector3)(dir * speed * Time.deltaTime);
    }

}
