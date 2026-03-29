using System;
using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    // movement
    public float moveSpeed;
    public float jumpForce;
    public bool onGround;

    [SerializeField] protected float horizontal;

    // references
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRend;
    [SerializeField] protected Animator animator;

    [SerializeField] protected LayerMask groundLayer;

    // player tracking
    protected Transform targetPlayer;
    protected Vector2 lastSeenPos;
    protected float timeSinceSeen = 0;
    public float lostSightCooldown = 3f;

    // search behavior
    protected bool isLookingAround = false;
    protected float lookAroundTimer = 2f;
    protected float currentLookTimer = 0;
    protected int lookDirection = 1;

    // behavior
    protected enum EnemyState { Patrolling, Chasing, Searching }
    protected EnemyState currentState = EnemyState.Patrolling;

    // grid
    protected float blockSize = 0.08f;
    protected Vector2 gridWorldSize;
    protected float nodeRadius;
    protected float nodeDiameter;
    public int gridSizeX = 20;
    public int gridSizeY = 20;
    protected Vector3[,] grid;

    // vision
    protected int rayCount = 5;
    protected float visionDist = 5f;
    protected float visionAngle = 45f;

    //knockback
    private bool isKnockedBack = false;
    public float knockbackDuration = 0.2f;
    private float knockbackTimer = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRend = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        nodeDiameter = blockSize;
        nodeRadius = nodeDiameter / 2;

        gridWorldSize = new Vector2(gridSizeX * nodeDiameter, gridSizeY * nodeDiameter);
        CreateGrid();

        ChooseDirection();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        Vision();
        animator.SetFloat("Horizontal", MathF.Abs(horizontal));
        animator.SetBool("onGround", onGround);
    }
    protected virtual void FixedUpdate()
    {
        CreateGrid();

        if (isKnockedBack)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            if (knockbackTimer <= 0f)
            {
                isKnockedBack = false;
            }
            return;
        }

        switch (currentState)
        {
            case EnemyState.Patrolling:
                PatrollingState();
                break;
            case EnemyState.Chasing:
                ChasingState();
                break;
            case EnemyState.Searching:
                SearchingState();
                break;
        }
    }
    protected virtual void OnTriggerStay2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
            onGround = true;
    }

    protected virtual void OnTriggerExit2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
            onGround = false;
    }

    protected abstract void ChooseDirection();

    protected abstract void MoveTowards(Vector2 target);

    protected void CreateGrid()
    {
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        grid = new Vector3[gridSizeX, gridSizeY];

        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.up * gridWorldSize.y / 2;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                float worldX = x * nodeDiameter + nodeRadius;
                float worldY = y * nodeDiameter;
                Vector3 worldPoint = worldBottomLeft + new Vector3(worldX, worldY, 0);
                grid[x, y] = worldPoint;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (grid != null)
        {
            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    Vector3 point = grid[x, y];
                    bool isGround = Physics2D.OverlapCircle(point, nodeRadius * 0.1f, groundLayer);
                    Gizmos.color = isGround ? Color.red : Color.white;
                    Gizmos.DrawCube(point, Vector3.one * (nodeDiameter * 0.5f));
                }
            }
        }
    }

    protected Vector2Int GetGridFromWorld(Vector3 worldPos)
    {
        float percentX = Mathf.Clamp01((worldPos.x - (transform.position.x - gridWorldSize.x / 2)) / gridWorldSize.x);
        float percentY = Mathf.Clamp01((worldPos.y - (transform.position.y - gridWorldSize.y / 2)) / gridWorldSize.y);

        int x = Mathf.Clamp(Mathf.RoundToInt((gridSizeX - 1) * percentX), 0, gridSizeX - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt((gridSizeY - 1) * percentY), 0, gridSizeY - 1);

        return new Vector2Int(x, y);
    }

    protected Vector3 GetWorldFromGrid(Vector2Int gridPos)
    {
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.up * gridWorldSize.y / 2;

        float worldX = gridPos.x * nodeDiameter + nodeRadius;
        float worldY = gridPos.y * nodeDiameter;

        return worldBottomLeft + new Vector3(worldX, worldY, 0);
    }

    protected bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < gridSizeX && y >= 0 && y < gridSizeY;
    }

    protected abstract void Vision();
    protected abstract void PatrollingState();
    protected abstract void ChasingState();
    protected abstract void SearchingState();

    public void ApplyKnockback(object data)
    {
        KnockbackData kb = (KnockbackData)data;

        rb.velocity = Vector2.zero;
        rb.AddForce(kb.direction.normalized * kb.force, ForceMode2D.Impulse);

        isKnockedBack = true;
        knockbackTimer = knockbackDuration;
    }

    public void LookTowardsDirection(Vector2 direction)
    {
        if (currentState != EnemyState.Chasing)
        {
            if (Mathf.Abs(direction.x) > 0.1f)
            {
                horizontal = direction.x > 0 ? -1 : 1;
            }
        }
    }
}
