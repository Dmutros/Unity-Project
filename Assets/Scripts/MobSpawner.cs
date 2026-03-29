using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MobSpawner : MonoBehaviour
{
    public GameObject[] mobPrefabs;
    public float spawnInterval = 5f;
    public int maxActiveMobs = 10;
    public float spawnOffset = 1f;

    public GameObject bossPrefab;
    public GameObject player;

    private Camera mainCam;
    public WorldGeneration worldGeneration;

    private float blockSize = 0.08f;
    private float worldHeightLimit = 1000f;

    public bool allowMobSpawning = true;
    private void Start()
    {
        mainCam = GetComponent<Camera>();

        if (worldGeneration == null)
        {
            worldGeneration = FindObjectOfType<WorldGeneration>();
            Debug.Log(worldGeneration != null ? "WorldGeneration found automatically." : "WorldGeneration NOT FOUND!");
        }

        if (mainCam == null)
        {
            return;
        }

        if (player == null)
        {
            return;
        }

        StartCoroutine(SpawnRoutine());
    }

    public void RespawnPlayer()
    {
        StartCoroutine(RespawnCoroutine());
    }
    
    private IEnumerator RespawnCoroutine()
    {
        var health = player.GetComponent<HealthComponent>().Health;
        health.Reset();

        var spawnPoint = player.GetComponent<PlayerWorldController>().spawnPos;
        player.transform.position = spawnPoint;

        MobDespawn mobDespawn = FindObjectOfType<MobDespawn>();

        yield return new WaitForSeconds(1);

        if (mobDespawn != null)
        {
            mobDespawn.playerDead = false;
        }
    }

    private IEnumerator SpawnRoutine()
    {
        int enemyLayerMask = 1 << LayerMask.NameToLayer("Enemy");

        while (allowMobSpawning == true)
        {
            yield return new WaitForSeconds(spawnInterval);

            Collider2D[] enemies = Physics2D.OverlapCircleAll(player.transform.position, 100f, enemyLayerMask);
            if (enemies.Length >= maxActiveMobs) continue;

            Vector2 spawnPoint;
            if (TryGetValidSpawnPoint(out spawnPoint))
            {
                GameObject prefab = mobPrefabs[Random.Range(0, mobPrefabs.Length)];
                Instantiate(prefab, spawnPoint, Quaternion.identity);
            }
        }
    }

    public void SpawnBoss()
    {
        if (bossPrefab == null)
        {
            return;
        }

        Vector2 spawnPoint;
        if (TryGetValidSpawnPoint(out spawnPoint))
        {
            Instantiate(bossPrefab, spawnPoint, Quaternion.identity);
            allowMobSpawning = false;
        }
    }

    private bool TryGetValidSpawnPoint(out Vector2 spawnPosition)
    {
        float camHeight = mainCam.orthographicSize * 2f;
        float camWidth = camHeight * mainCam.aspect;
        Vector3 camPos = mainCam.transform.position;
        float playerY = player.transform.position.y;

        for (int attempts = 0; attempts < 10; attempts++)
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            float spawnX = camPos.x + side * (camWidth / 2f + spawnOffset);
            int gridX = Mathf.RoundToInt(spawnX / blockSize);
            int playerGridY = Mathf.RoundToInt(playerY / blockSize);

            for (int dy = -10; dy <= 10; dy++)
            {
                int checkY = playerGridY + dy;
                if (checkY <= 0 || checkY >= worldHeightLimit) continue;

                Vector2Int tilePos = new Vector2Int(gridX, checkY);
                if (worldGeneration.worldTilesIntPos.Contains(tilePos))
                {
                    bool spaceFree = true;
                    for (int dx = -1; dx <= 1 && spaceFree; dx++)
                    {
                        for (int h = 1; h <= 4 && spaceFree; h++)
                        {
                            Vector2Int check = new Vector2Int(gridX + dx, checkY + h);
                            if (worldGeneration.worldTilesIntPos.Contains(check))
                            {
                                spaceFree = false;
                            }
                        }
                    }

                    if (spaceFree)
                    {
                        spawnPosition = new Vector2(gridX * blockSize, (checkY + 1) * blockSize + 0.08f);
                        return true;
                    }
                }
            }
        }

        spawnPosition = Vector2.zero;
        return false;
    }
}
