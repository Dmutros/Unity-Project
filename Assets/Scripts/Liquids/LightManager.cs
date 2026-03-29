using System.Collections.Generic;
using UnityEngine;

public class LightManager : MonoBehaviour
{
    public int worldWidth, worldHeight;
    public float ambientLight = 0.1f;
    public int lightRadius = 10;

    private float[,] lightMap;
    public Texture2D lightTexture;
    public float blockSize = 0.08f;
    private SpriteRenderer overlayRenderer;

    public List<Vector2Int> lightSources = new List<Vector2Int>();
    public WorldGeneration worldGeneration;

    public void Start()
    {
        
        lightMap = new float[worldWidth, worldHeight];
        lightTexture = new Texture2D(worldWidth, worldHeight);
        lightTexture.filterMode = FilterMode.Point;
        lightTexture.wrapMode = TextureWrapMode.Clamp;

        GameObject overlay = new GameObject("LightOverlay");
        overlayRenderer = overlay.AddComponent<SpriteRenderer>();
        overlayRenderer.sprite = Sprite.Create(lightTexture, new Rect(0, 0, worldWidth, worldHeight), new Vector2(0, 0), 1);
        overlayRenderer.sortingOrder = 100;
        overlay.transform.position = Vector3.zero;
        overlay.transform.localScale = Vector3.one * blockSize;
        for (int i = 50; i < 60; i++)
        {
            for (int j = 50; j < 60; i++)
            {
                AddLightSource(new Vector2Int(i, j));
            }
        }

        RecalculateLighting();
    }

    public void AddLightSource(Vector2Int pos)
    {
        if (!lightSources.Contains(pos))
            lightSources.Add(pos);
        RecalculateLighting();
    }

    public void RemoveLightSource(Vector2Int pos)
    {
        lightSources.Remove(pos);
        RecalculateLighting();
    }

    public void RecalculateLighting()
    {
        // Очистити карту
        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                lightMap[x, y] = ambientLight;
            }
        }

        // Додати кожне джерело світла
        foreach (Vector2Int source in lightSources)
        {
            FloodLight(source.x, source.y, 1f, lightRadius);
        }

        // Оновити текстуру
        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                float light = lightMap[x, y];
                float alpha = 1f - light;
                lightTexture.SetPixel(x, y, new Color(0, 0, 0, alpha));
            }
        }

        lightTexture.Apply();
    }

    void FloodLight(int x, int y, float intensity, int radius)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, float> visited = new Dictionary<Vector2Int, float>();

        queue.Enqueue(new Vector2Int(x, y));
        visited[new Vector2Int(x, y)] = intensity;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            float currentIntensity = visited[current];

            if (current.x < 0 || current.y < 0 || current.x >= worldWidth || current.y >= worldHeight)
                continue;

            if (currentIntensity < 0.05f)
                continue;

            // Зупинити, якщо блок світло непроникний:
            if (IsBlocked(current.x, current.y))
                continue;

            if (lightMap[current.x, current.y] < currentIntensity)
                lightMap[current.x, current.y] = currentIntensity;

            Vector2Int[] directions = new Vector2Int[]
            {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
            };

            foreach (Vector2Int dir in directions)
            {
                Vector2Int next = current + dir;
                float nextIntensity = currentIntensity - 0.1f;

                if (!visited.ContainsKey(next) || visited[next] < nextIntensity)
                {
                    visited[next] = nextIntensity;
                    queue.Enqueue(next);
                }
            }
        }
    }

    bool IsBlocked(int x, int y)
    {
        return false;//worldGeneration.worldTilesIntPos.Contains(new Vector2(x, y)); // якщо там блок — він не пропускає світло
    }
}
