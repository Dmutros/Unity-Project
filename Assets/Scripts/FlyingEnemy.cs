using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FlyingEnemy : Enemy
{
    // moving
    private float patrolSpeed;
    private float chaseSpeed;
    private float searchSpeed;
    private float acceleration = 2f;
    private Vector2 currentVelocity;
    private float verticalSpeed = 0f;

    // patrol behavior
    private float nextDirectionChangeTime;
    private Vector2 lastGridCenter;

    // stuck
    private bool stuckCheckActive = false;
    private float stuckTimer = 0f;
    private float stuckCheckTime = 1f;
    private Vector2 stuckCheckStartPos;

    // bounce
    private float bounceCooldown = 0.2f;
    private float lastBounceTime = -1f;
    private float rayDistanceHorizontal = 0.2f;
    private float rayDistanceVertical = 0.2f;

    // search behavior
    private Vector3 searchCenterPoint;
    private Vector3 currentSearchTarget;
    private float searchTimer;
    private float searchDuration = 10f;
    private float searchPointReachedThreshold = 0.5f;
    private float minSearchPointStayTime = 1f;
    private float maxSearchPointStayTime = 3f;
    private float searchRadius = 3f;
    private bool isSearchingRandomPoints;

    // search points
    private class SearchPoint
    {
        public Vector3 position;
        public float stayTime;
        public float timeRemaining;

        public SearchPoint(Vector3 pos, float time)
        {
            position = pos;
            stayTime = time;
            timeRemaining = time;
        }
    }

    private SearchPoint currentSearchPoint;
    private Queue<SearchPoint> searchPointQueue = new Queue<SearchPoint>();
    private int maxSearchPoints = 5;

    // pathfinding
    private class Node
    {
        public Vector2Int gridPos;
        public Vector3 worldPos;
        public bool walkable;
        public int gCost;
        public int hCost;
        public Node parent;
        public int fCost => gCost + hCost;

        public Node(Vector2Int gridPos, Vector3 worldPos, bool walkable)
        {
            this.gridPos = gridPos;
            this.worldPos = worldPos;
            this.walkable = walkable;
        }
    }

    private Node[,] nodeGrid;
    public int nodeGridSizeX = 50;
    public int nodeGridSizeY = 50;
    private Vector2 nodeGridWorldSize;
    private List<Vector3> currentPath = new List<Vector3>();
    private int pathIndex = 0;
    private float nodeThreshold = 0.1f;
    public float pathUpdateInterval = 0.5f;
    private float nextPathUpdateTime = 0f;

    // height
    private enum AltitudeResult { TooLow, TooHigh, GoodHeight, NoGroundBelow }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        base.Start();

        patrolSpeed = moveSpeed;
        chaseSpeed = patrolSpeed + patrolSpeed / 2;
        searchSpeed = patrolSpeed + patrolSpeed / 4;

        nodeGridWorldSize = new Vector2(nodeGridSizeX * nodeDiameter, nodeGridSizeY * nodeDiameter);
        CreateNodeGrid();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        if (Vector2.Distance(transform.position, lastGridCenter) > nodeDiameter)
        {
            CreateNodeGrid();
            lastGridCenter = transform.position;
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (horizontal != 0)
        {
            spriteRend.flipX = horizontal < 0;
        }

        ApplyTiltBasedOnVelocity();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0 || collision.collider.CompareTag("Player"))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                Vector2 normal = contact.normal;
                Vector2 reflectedVelocity = Vector2.Reflect(currentVelocity, normal);

                rb.velocity = reflectedVelocity;
                currentVelocity = reflectedVelocity;

                lastBounceTime = Time.time;
                break;
            }
        }
    }

    protected override void Vision()
    {
        if (targetPlayer == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                targetPlayer = player.transform;
            else
                return;
        }

        Vector2 baseDirection = (targetPlayer.position - transform.position).normalized;
        int half = rayCount / 2;
        bool playerSpotted = false;

        for (int i = -half; i <= half; i++)
        {
            float angleStep = visionAngle / rayCount;
            float angle = angleStep * i;

            Vector2 rayDirection = Quaternion.Euler(0, 0, angle) * baseDirection;
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, rayDirection, visionDist);

            Debug.DrawRay(transform.position, rayDirection * visionDist, Color.yellow);

            foreach (var hit in hits)
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground")) break;

                if (hit.collider.CompareTag("Player"))
                {
                    currentState = EnemyState.Chasing;
                    targetPlayer = hit.collider.transform;

                    Vector3 potentialLastSeenPos = targetPlayer.position;
                    var path = FindPath(transform.position, potentialLastSeenPos);
                    if (path.Count > 0)
                    {
                        lastSeenPos = potentialLastSeenPos;
                    }

                    timeSinceSeen = 0f;
                    isLookingAround = false;
                    playerSpotted = true;
                    break;
                }
            }

            if (playerSpotted) break;
        }
    }

    protected override void PatrollingState()
    {
        if (Time.time >= nextDirectionChangeTime)
        {
            ChooseDirection();
            nextDirectionChangeTime = Time.time + Random.Range(3f, 30f);
        }

        if (!IsInCave())
        {
            var altitude = EvaluateAltitude();

            switch (altitude)
            {
                case AltitudeResult.TooLow:
                    verticalSpeed = 1f;
                    break;
                case AltitudeResult.TooHigh:
                    verticalSpeed = -1f;
                    break;
                case AltitudeResult.GoodHeight:
                    verticalSpeed = 0f;
                    break;
                case AltitudeResult.NoGroundBelow:
                    verticalSpeed = -0.5f;
                    break;
            }
        }

        CaveRaycastCheck();

        Vector2 targetVelocity = new Vector2(horizontal * patrolSpeed, verticalSpeed * patrolSpeed);
        currentVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
        rb.velocity = currentVelocity;

        if (!stuckCheckActive)
        {
            stuckCheckStartPos = transform.position;
            stuckTimer = stuckCheckTime;
            stuckCheckActive = true;
        }
        else
        {
            stuckTimer -= Time.deltaTime;
            if (stuckTimer <= 0)
            {
                float deltaX = Mathf.Abs(transform.position.x - stuckCheckStartPos.x);

                if (deltaX < 0.1f)
                {
                    horizontal *= -1f;
                }

                stuckCheckActive = false;
            }
        }
    }

    protected override void ChasingState()
    {
        if (targetPlayer == null)
        {
            SwitchToSearchingState();
            return;
        }

        timeSinceSeen += Time.deltaTime;
        if (timeSinceSeen > lostSightCooldown)
        {
            SwitchToSearchingState();
            return;
        }

        if (Time.time >= nextPathUpdateTime)
        {
            nextPathUpdateTime = Time.time + pathUpdateInterval;
            currentPath = FindPath(transform.position, targetPlayer.position);
            pathIndex = 0;
        }

        if (currentPath != null && pathIndex < currentPath.Count)
        {
            int lookAhead = 3;
            int idx = Mathf.Min(pathIndex + lookAhead, currentPath.Count - 1);
            Vector2 dir = (currentPath[idx] - transform.position).normalized;

            currentVelocity = Vector2.MoveTowards(currentVelocity, dir * chaseSpeed, acceleration * Time.fixedDeltaTime);
            rb.velocity = currentVelocity;

            float threshold = Mathf.Max(nodeThreshold, chaseSpeed * Time.fixedDeltaTime * 1.5f);
            if (Vector2.Distance(transform.position, currentPath[pathIndex]) < threshold)
                pathIndex++;

            horizontal = dir.x;
        }
        else
        {
            Vector2 dir = (targetPlayer.position - transform.position).normalized;
            currentVelocity = Vector2.MoveTowards(currentVelocity, dir * chaseSpeed, acceleration * Time.fixedDeltaTime);
            rb.velocity = currentVelocity;
        }
    }

    protected override void SearchingState()
    {
        searchTimer -= Time.deltaTime;

        if (!isSearchingRandomPoints)
        {
            if (currentPath != null && currentPath.Count > 0 && pathIndex < currentPath.Count)
            {
                int lookAhead = 1;
                int idx = Mathf.Min(pathIndex + lookAhead, currentPath.Count - 1);
                Vector2 dir = (currentPath[idx] - transform.position).normalized;

                horizontal = dir.x;

                currentVelocity = Vector2.MoveTowards(currentVelocity, dir * searchSpeed, acceleration * Time.fixedDeltaTime);
                rb.velocity = currentVelocity;

                float threshold = Mathf.Max(nodeThreshold, searchSpeed * Time.fixedDeltaTime * 1.5f);
                if (Vector2.Distance(transform.position, currentPath[pathIndex]) < threshold)
                {
                    pathIndex++;
                }

                if (pathIndex >= currentPath.Count || Vector2.Distance(transform.position, searchCenterPoint) < searchPointReachedThreshold)
                {
                    isSearchingRandomPoints = true;
                    GenerateRandomSearchPoint();
                }
            }
            else
            {
                isSearchingRandomPoints = true;
                GenerateRandomSearchPoint();
            }
        }
        else
        {
            if (currentSearchPoint == null)
            {
                GenerateRandomSearchPoint();
            }

            if (Vector2.Distance(transform.position, currentSearchTarget) < searchPointReachedThreshold)
            {
                currentSearchPoint.timeRemaining -= Time.deltaTime;

                Vector2 randomSmallMovement = Random.insideUnitCircle * 0.3f;
                Vector2 hoverDir = randomSmallMovement.normalized;

                currentVelocity = Vector2.MoveTowards(currentVelocity, hoverDir * searchSpeed * 0.3f, acceleration * 0.5f * Time.fixedDeltaTime);
                rb.velocity = currentVelocity;

                if (currentSearchPoint.timeRemaining <= 0)
                {
                    GenerateRandomSearchPoint();
                }
            }
            else
            {
                Vector2 dir = (currentSearchTarget - transform.position).normalized;

                horizontal = dir.x;

                currentVelocity = Vector2.MoveTowards(currentVelocity, dir * searchSpeed * 0.8f, acceleration * Time.fixedDeltaTime);
                rb.velocity = currentVelocity;
            }
        }

        if (searchTimer <= 0)
        {
            currentState = EnemyState.Patrolling;
            currentPath.Clear();
            isLookingAround = false;
            searchPointQueue.Clear();
            currentSearchPoint = null;
        }
    }

    private void SwitchToSearchingState()
    {
        currentState = EnemyState.Searching;
        searchCenterPoint = lastSeenPos;
        isSearchingRandomPoints = false;
        searchTimer = searchDuration;
        searchPointQueue.Clear();
        currentSearchPoint = null;

        currentPath = FindPath(transform.position, searchCenterPoint);
        pathIndex = 0;
    }

    protected override void ChooseDirection()
    {
        horizontal = Random.Range(0, 2) == 0 ? -1f : 1f;
    }

    protected override void MoveTowards(Vector2 target)
    {
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        currentVelocity = Vector2.MoveTowards(currentVelocity, dir * searchSpeed, acceleration * Time.fixedDeltaTime);
        rb.velocity = currentVelocity;
    }

    private void ApplyTiltBasedOnVelocity()
    {
        float maxTiltAngle = 40f;
        float tiltSpeed = 5f;

        float maxSpeed = Mathf.Max(patrolSpeed, chaseSpeed, searchSpeed);

        float tilt = -rb.velocity.x / maxSpeed * maxTiltAngle;
        tilt = Mathf.Clamp(tilt, -maxTiltAngle, maxTiltAngle);

        Quaternion targetRotation = Quaternion.Euler(0f, 0f, tilt);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * tiltSpeed);
    }


    private void CaveRaycastCheck()
    {
        Vector2 origin = transform.position;
        Vector2 horizontalDir = new Vector2(horizontal, 0);
        Vector2 verticalDir = new Vector2(0, verticalSpeed);

        RaycastHit2D wallHit = Physics2D.Raycast(origin, horizontalDir, rayDistanceHorizontal, groundLayer);
        Debug.DrawRay(origin, horizontalDir * rayDistanceHorizontal, Color.red);

        RaycastHit2D ceilingHit = Physics2D.Raycast(origin, verticalDir, rayDistanceVertical, groundLayer);
        Debug.DrawRay(origin, verticalDir * rayDistanceVertical, Color.blue);

        if (wallHit.collider != null && Time.time - lastBounceTime > bounceCooldown)
        {
            if (ceilingHit.collider == null)
            {
                verticalSpeed = 1f;
            }
            else
            {
                horizontal *= -1f;
                verticalSpeed *= -1f;
                lastBounceTime = Time.time;
            }
        }
    }

    private bool IsInCave()
    {
        float caveCheckHeight = 1f;
        Vector2 origin = transform.position;

        var hitAbove = Physics2D.Raycast(origin, Vector2.up, caveCheckHeight, groundLayer);
        var hitBelow = Physics2D.Raycast(origin, Vector2.down, caveCheckHeight, groundLayer);

        return hitAbove.collider != null && hitBelow.collider != null;
    }

    private AltitudeResult EvaluateAltitude()
    {
        Vector2 currentPos = transform.position;
        Vector2Int gridPos = GetGridFromWorld(currentPos);

        int x = gridPos.x;

        for (int y = gridPos.y; y >= 0; y--)
        {
            if (!IsInBounds(x, y)) continue;

            Vector3 checkPos = grid[x, y];
            bool isGround = Physics2D.OverlapCircle(checkPos, nodeRadius * 0.1f, groundLayer);

            if (isGround)
            {
                float groundY = checkPos.y;
                float heightAboveGround = currentPos.y - groundY;

                float preferredMinHeight = 0.8f;
                float preferredMaxHeight = 1.6f;

                if (heightAboveGround < preferredMinHeight)
                    return AltitudeResult.TooLow;
                else if (heightAboveGround > preferredMaxHeight)
                    return AltitudeResult.TooHigh;
                else
                    return AltitudeResult.GoodHeight;
            }
        }

        return AltitudeResult.NoGroundBelow;
    }

    private void GenerateRandomSearchPoint()
    {
        if (searchPointQueue.Count == 0)
        {
            for (int i = 0; i < maxSearchPoints; i++)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Random.Range(0.5f, searchRadius);

                Vector2 offset = new Vector2(
                    Mathf.Cos(angle) * distance,
                    Mathf.Sin(angle) * distance
                );

                Vector3 potentialTarget = searchCenterPoint + new Vector3(offset.x, offset.y, 0);

                Vector2Int targetNodeIdx = GetNodeIndexFromWorld(potentialTarget);
                if (IsValidNodeIndex(targetNodeIdx) && nodeGrid[targetNodeIdx.x, targetNodeIdx.y].walkable)
                {
                    float stayTime = Random.Range(minSearchPointStayTime, maxSearchPointStayTime);
                    searchPointQueue.Enqueue(new SearchPoint(potentialTarget, stayTime));
                }
            }

            if (searchPointQueue.Count == 0)
            {
                searchRadius = Mathf.Max(1f, searchRadius * 0.8f);
                GenerateRandomSearchPoint();
                return;
            }
        }

        currentSearchPoint = searchPointQueue.Dequeue();
        currentSearchTarget = currentSearchPoint.position;
    }

    private void CreateNodeGrid()
    {
        nodeGrid = new Node[nodeGridSizeX, nodeGridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * nodeGridWorldSize.x / 2 - Vector3.up * nodeGridWorldSize.y / 2;

        for (int x = 0; x < nodeGridSizeX; x++)
        {
            for (int y = 0; y < nodeGridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeDiameter / 2) + Vector3.up * (y * nodeDiameter + nodeDiameter / 2);
                bool walkable = !Physics2D.OverlapCircle(worldPoint, nodeDiameter * 0.45f, groundLayer);
                nodeGrid[x, y] = new Node(new Vector2Int(x, y), worldPoint, walkable);
            }
        }
    }

    private List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Vector2Int startIdx = GetNodeIndexFromWorld(startPos);
        Vector2Int targetIdx = GetNodeIndexFromWorld(targetPos);

        if (!IsValidNodeIndex(startIdx) || !IsValidNodeIndex(targetIdx)) return new List<Vector3>();

        Node startNode = nodeGrid[startIdx.x, startIdx.y];
        Node targetNode = nodeGrid[targetIdx.x, targetIdx.y];

        var openSet = new List<Node>();
        var closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        foreach (Node n in nodeGrid)
        {
            n.gCost = int.MaxValue;
            n.hCost = 0;
            n.parent = null;
        }

        startNode.gCost = 0;
        startNode.hCost = GetDistance(startNode, targetNode);

        while (openSet.Count > 0)
        {
            Node current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < current.fCost || openSet[i].fCost == current.fCost && openSet[i].hCost < current.hCost)
                {
                    current = openSet[i];
                }
            }

            openSet.Remove(current);
            closedSet.Add(current);

            if (current == targetNode)
                return RetracePath(startNode, targetNode);

            foreach (Node neighbour in GetNeighbours(current))
            {
                if (closedSet.Contains(neighbour)) continue;

                int newCost = current.gCost + GetDistance(current, neighbour);
                if (newCost < neighbour.gCost)
                {
                    neighbour.gCost = newCost;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = current;

                    if (!openSet.Contains(neighbour))
                        openSet.Add(neighbour);
                }
            }
        }
        return new List<Vector3>();
    }

    private List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int checkX = node.gridPos.x + dx;
                int checkY = node.gridPos.y + dy;
                if (checkX >= 0 && checkX < nodeGridSizeX && checkY >= 0 && checkY < nodeGridSizeY)
                {
                    Node neighbour = nodeGrid[checkX, checkY];
                    if (neighbour.walkable)
                        neighbours.Add(neighbour);
                }
            }
        }
        return neighbours;
    }

    private int GetDistance(Node a, Node b)
    {
        int dstX = Mathf.Abs(a.gridPos.x - b.gridPos.x);
        int dstY = Mathf.Abs(a.gridPos.y - b.gridPos.y);
        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }

    private List<Vector3> RetracePath(Node startNode, Node endNode)
    {
        List<Vector3> path = new List<Vector3>();
        Node current = endNode;
        while (current != startNode)
        {
            path.Add(current.worldPos);
            current = current.parent;
        }
        path.Reverse();
        return path;
    }

    private Vector2Int GetNodeIndexFromWorld(Vector3 worldPos)
    {
        float percentX = Mathf.Clamp01((worldPos.x - (transform.position.x - nodeGridWorldSize.x / 2)) / nodeGridWorldSize.x);
        float percentY = Mathf.Clamp01((worldPos.y - (transform.position.y - nodeGridWorldSize.y / 2)) / nodeGridWorldSize.y);
        int x = Mathf.Clamp(Mathf.RoundToInt((nodeGridSizeX - 1) * percentX), 0, nodeGridSizeX - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt((nodeGridSizeY - 1) * percentY), 0, nodeGridSizeY - 1);
        return new Vector2Int(x, y);
    }

    private bool IsValidNodeIndex(Vector2Int index)
    {
        return index.x >= 0 && index.x < nodeGridSizeX && index.y >= 0 && index.y < nodeGridSizeY;
    }

    private void OnDrawGizmos()
    {
        if (currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
            }

            for (int i = 0; i < currentPath.Count; i++)
            {
                Gizmos.DrawWireSphere(currentPath[i], nodeDiameter * 0.25f);
            }
        }

        if (currentState == EnemyState.Searching)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(searchCenterPoint, searchRadius);

            if (isSearchingRandomPoints && currentSearchPoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(currentSearchTarget, 0.2f);
                Gizmos.DrawLine(transform.position, currentSearchTarget);

                float timerPercentage = currentSearchPoint.timeRemaining / currentSearchPoint.stayTime;
                Color timerColor = Color.Lerp(Color.red, Color.green, timerPercentage);
                Gizmos.color = timerColor;
                Gizmos.DrawWireSphere(currentSearchTarget, 0.1f);
            }

            Gizmos.color = new Color(1f, 1f, 0.5f, 0.5f);
            foreach (var point in searchPointQueue)
            {
                Gizmos.DrawWireSphere(point.position, 0.15f);
            }
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, nodeDiameter * 0.3f);
        if (targetPlayer != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPlayer.position, nodeDiameter * 0.3f);
        }
    }
}