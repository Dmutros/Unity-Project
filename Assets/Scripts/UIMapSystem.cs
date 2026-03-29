using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class UIMapSystem : MonoBehaviour
{
    [Header("Map Settings")]
    [SerializeField] private RawImage mapDisplay; // RawImage для відображення частини карти
    [SerializeField] private int viewportSize = 100; // Розмір видимої області (100x100 пікселів)
    [SerializeField] private float updateInterval = 0.3f; // Як часто оновлювати карту
    [SerializeField] WorldGeneration worldGenerator;
    [SerializeField] private Transform playerTransform; // Посилання на гравця

    [Header("Full Map")]
    private int fullMapSizeH = 200; // Повний розмір карти
    private int fullMapSizeW = 200;

    private Texture2D fullMapTexture; // Повна карта
    private Texture2D viewportTexture; // Видима частина карти
    private List<MapUpdate> pendingUpdates = new List<MapUpdate>();
    private Vector2Int lastPlayerMapPosition;
    private bool needsUpdate = false;
    private bool needsViewportUpdate = false;

    // Структура для зберігання оновлень
    [System.Serializable]
    public struct MapUpdate
    {
        public Vector2Int worldPosition;
        public Color color;
    }

    public void StartWork()
    {
        InitializeMap();
        StartCoroutine(UpdateMapRoutine());
    }

    void InitializeMap()
    {
        fullMapSizeH = worldGenerator.worldSizeH;
        fullMapSizeW = worldGenerator.worldSizeW;

        // Створюємо повну текстуру карти
        fullMapTexture = new Texture2D(fullMapSizeW, fullMapSizeH, TextureFormat.RGB24, false);
        fullMapTexture.filterMode = FilterMode.Point;

        // Створюємо текстуру для viewport
        viewportTexture = new Texture2D(viewportSize, viewportSize, TextureFormat.RGB24, false);
        viewportTexture.filterMode = FilterMode.Point;

        // Заповнюємо повну карту синім кольором (невідоме)
        Color[] fullPixels = new Color[fullMapSizeW * fullMapSizeH];
        for (int i = 0; i < fullPixels.Length; i++)
        {
            fullPixels[i] = Color.blue;
        }
        fullMapTexture.SetPixels(fullPixels);
        fullMapTexture.Apply();

        // Встановлюємо viewport текстуру на RawImage
        mapDisplay.texture = viewportTexture;

        // Оновлюємо viewport вперше
        UpdateViewport();
    }

    // Головний метод - просто додаємо в чергу оновлень
    public void UpdateMap(Vector2Int worldPosition)
    {
        Color pixelColor = GetPixelColor(worldPosition);
        pendingUpdates.Add(new MapUpdate
        {
            worldPosition = worldPosition,
            color = pixelColor
        });
        needsUpdate = true;
    }

    // Метод для встановлення позиції гравця
    public void SetPlayerPosition(Vector2Int playerPos)
    {
        if (playerPos != lastPlayerMapPosition)
        {
            lastPlayerMapPosition = playerPos;
            needsViewportUpdate = true;
        }
    }

    // Отримуємо колір блоку
    Color GetPixelColor(Vector2Int worldPosition)
    {
        Vector2 worldPos = new Vector2(worldPosition.x, worldPosition.y);
        if (worldGenerator.worldTilesIntPos.Contains(worldPos))
        {
            int indexBlock = worldGenerator.worldTilesIntPos.IndexOf(worldPos);
            return worldGenerator.worldTilesClass[indexBlock].mapColor;
        }
        if (worldGenerator.worldTilesWallIntPos.Contains(worldPos))
        {
            int indexWall = worldGenerator.worldTilesWallIntPos.IndexOf(worldPos);
            return worldGenerator.worldTilesWallClass[indexWall].mapColor;
        }
        return Color.blue; // Порожнє місце = синій
    }

    // Корутина для пакетного оновлення
    IEnumerator UpdateMapRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);

            // Оновлюємо повну карту
            if (needsUpdate && pendingUpdates.Count > 0)
            {
                ApplyPendingUpdates();
                needsUpdate = false;
                needsViewportUpdate = true; // Після оновлення карти потрібно оновити viewport
            }

            // Оновлюємо viewport
            if (needsViewportUpdate)
            {
                UpdateViewport();
                needsViewportUpdate = false;
            }
        }
    }

    // Застосовуємо всі оновлення до повної карти
    void ApplyPendingUpdates()
    {
        foreach (MapUpdate update in pendingUpdates)
        {
            Vector2Int mapPos = update.worldPosition;
            // Перевіряємо чи позиція в межах повної карти
            if (mapPos.x >= 0 && mapPos.x < fullMapSizeW && mapPos.y >= 0 && mapPos.y < fullMapSizeH)
            {
                fullMapTexture.SetPixel(mapPos.x, mapPos.y, update.color);
            }
        }

        fullMapTexture.Apply();
        pendingUpdates.Clear();
    }

    // Оновлюємо viewport навколо гравця
    void UpdateViewport()
    {
        // Отримуємо позицію гравця на карті
        Vector2Int playerMapPos = GetPlayerMapPosition();

        // Розраховуємо межі viewport
        int halfViewport = viewportSize / 2;
        int startX = playerMapPos.x - halfViewport;
        int startY = playerMapPos.y - halfViewport;

        // Створюємо масив пікселів для viewport
        Color[] viewportPixels = new Color[viewportSize * viewportSize];

        for (int y = 0; y < viewportSize; y++)
        {
            for (int x = 0; x < viewportSize; x++)
            {
                int fullMapX = startX + x;
                int fullMapY = startY + y;

                Color pixelColor;

                // Перевіряємо чи координати в межах повної карти
                if (fullMapX >= 0 && fullMapX < fullMapSizeW &&
                    fullMapY >= 0 && fullMapY < fullMapSizeH)
                {
                    pixelColor = fullMapTexture.GetPixel(fullMapX, fullMapY);
                }
                else
                {
                    // За межами карти - чорний колір
                    pixelColor = Color.black;
                }

                viewportPixels[y * viewportSize + x] = pixelColor;
            }
        }

        // Застосовуємо піксели до viewport текстури
        viewportTexture.SetPixels(viewportPixels);
        viewportTexture.Apply();

        // Додаємо маркер гравця в центр
        AddPlayerMarker();
    }

    // Додаємо маркер гравця в центр viewport
    void AddPlayerMarker()
    {
        int centerX = viewportSize / 2;
        int centerY = viewportSize / 2;

        // Малюємо червоний квадрат 3x3 для гравця
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                int pixelX = centerX + x;
                int pixelY = centerY + y;

                if (pixelX >= 0 && pixelX < viewportSize &&
                    pixelY >= 0 && pixelY < viewportSize)
                {
                    viewportTexture.SetPixel(pixelX, pixelY, Color.red);
                }
            }
        }

        viewportTexture.Apply();
    }

    // Отримуємо позицію гравця на карті
    Vector2Int GetPlayerMapPosition()
    {
        if (playerTransform != null)
        {
            // Конвертуємо світові координати в координати карти
            // Припускаємо, що одна клітинка карти = одна світова одиниця
            return new Vector2Int(
                Mathf.RoundToInt(playerTransform.position.x / 0.08f),
                Mathf.RoundToInt(playerTransform.position.y / 0.08f)
            );
        }

        return lastPlayerMapPosition;
    }

    // Публічний метод для оновлення позиції гравця ззовні
    public void UpdatePlayerPosition(Vector2Int newPosition)
    {
        SetPlayerPosition(newPosition);
    }
}