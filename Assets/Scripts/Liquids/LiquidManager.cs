using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Game.Types;
using UnityEngine.UIElements;
using UnityEngine.Rendering;

// Спрощена структура для зберігання даних про рідину
public struct LiquidCell
{
    public float amount;
    public LiquidType type;
    public GameObject gameObject;

    public LiquidCell(float amount, LiquidType type, GameObject gameObject = null)
    {
        this.amount = amount;
        this.type = type;
        this.gameObject = gameObject;
    }
}

public class LiquidManager : MonoBehaviour
{
    // Базові параметри
    [Header("Посилання")]
    public WorldGeneration worldGenerator;
    public LiquidClass water;
    public LiquidClass lava;
    
    [Header("Параметри фізики")]
    [Range(0.01f, 1f)] public float baseFlowRate = 1f; // Базова швидкість потоку
    [Range(0.01f, 0.1f)] public float minLiquidAmount = 0.01f;
    [Range(0.05f, 0.5f)] public float updateInterval = 0.01f;
    [Range(0.95f, 0.99f)] public float maxFillLevel = 0.98f;
    [Range(0.8f, 0.95f)] public float horizontalFlowThreshold = 0.9f;

    // Оптимізований словник для зберігання рідини
    private Dictionary<Vector2Int, LiquidCell> liquidMap = new Dictionary<Vector2Int, LiquidCell>(100);

    // Використовуємо HashSet для списку оновлень для уникнення дублікатів
    private HashSet<Vector2Int> activeCells = new HashSet<Vector2Int>();
    private Queue<Vector2Int> updateQueue = new Queue<Vector2Int>();

    // Заздалегідь створені сусідні напрямки для уникнення створення нових векторів
    private static readonly Vector2Int[] neighbors = new Vector2Int[]
    {
        new Vector2Int(0, -1),  // низ
        new Vector2Int(-1, 0),  // ліво
        new Vector2Int(1, 0),   // право
        new Vector2Int(0, 1)    // верх
    };

    // Лічильник кадрів для оптимізації оновлення
    private int frameCounter = 0;
    private int updateBatchSize = 20;

    private void Start()
    {
        StartCoroutine(UpdateLiquidSystem());
    }

    private void Update()
    {
        // Закоментуйте цей метод для продакшн
        // DebugPrintActiveLiquids();
    }

    public void DebugPrintActiveLiquids()
    {
        foreach (var position in activeCells)
        {
            if (liquidMap.TryGetValue(position, out LiquidCell cell))
            {
                Debug.Log($"Position: {position} | Amount: {cell.amount:F5} | Type: {cell.type}");
            }
        }
    }

    // Отримання LiquidClass за типом
    private LiquidClass GetLiquidClass(LiquidType type)
    {
        switch (type)
        {
            case LiquidType.Water:
                return water;
            case LiquidType.Lava:
                return lava;
            default:
                return water;
        }
    }

    // Додавання рідини в систему
    public void AddLiquid(Vector2Int position, LiquidType type, float amount = 1f)
    {
        // Перевірка на твердий блок
        if (worldGenerator.IsTileAt(position.x, position.y))
            return;

        LiquidClass liquidClass = GetLiquidClass(type);

        if (liquidMap.TryGetValue(position, out LiquidCell cell))
        {
            // Якщо рідина того ж типу - додаємо кількість
            if (cell.type == type)
            {
                float newAmount = Mathf.Min(1f, cell.amount + amount);
                cell.amount = newAmount;
                liquidMap[position] = cell;
                UpdateLiquidVisual(position);
                ScheduleForUpdate(position);
            }
        }
        else
        {
            // Створення нової рідини з використанням LiquidClass
            GameObject obj = Instantiate(liquidClass.liquidObject, 
                new Vector3(position.x * worldGenerator.blockSize, position.y * worldGenerator.blockSize, 0), Quaternion.identity, transform);

            LiquidCell newCell = new LiquidCell(amount, type, obj);
            liquidMap.Add(position, newCell);
            UpdateLiquidVisual(position);
            ScheduleForUpdate(position);
        }
    }

    // Оптимізований метод для запису клітинки в чергу оновлень
    private void ScheduleForUpdate(Vector2Int position)
    {
        if (activeCells.Add(position))
        {
            updateQueue.Enqueue(position);
        }

        // Плануємо оновлення сусідніх клітинок
        for (int i = 0; i < neighbors.Length; i++)
        {
            Vector2Int neighborPos = new Vector2Int(
                position.x + neighbors[i].x,
                position.y + neighbors[i].y
            );

            if (liquidMap.ContainsKey(neighborPos) && activeCells.Add(neighborPos))
            {
                updateQueue.Enqueue(neighborPos);
            }
        }
    }

    // Видалення рідини
    public void RemoveLiquid(Vector2Int position)
    {
        if (liquidMap.TryGetValue(position, out LiquidCell cell))
        {
            if (cell.gameObject != null)
            {
                Destroy(cell.gameObject);
            }

            liquidMap.Remove(position);
            activeCells.Remove(position);

            // Оновлюємо сусідів
            for (int i = 0; i < neighbors.Length; i++)
            {
                Vector2Int neighborPos = new Vector2Int(
                    position.x + neighbors[i].x,
                    position.y + neighbors[i].y
                );

                if (liquidMap.ContainsKey(neighborPos))
                {
                    ScheduleForUpdate(neighborPos);
                }
            }
        }
    }

    // Оновлення візуального представлення рідини з використанням LiquidClass
    private void UpdateLiquidVisual(Vector2Int position)
    {
        if (!liquidMap.TryGetValue(position, out LiquidCell cell))
            return;

        if (cell.amount <= minLiquidAmount)
        {
            RemoveLiquid(position);
            return;
        }

        if (cell.gameObject == null)
            return;

        LiquidClass liquidClass = GetLiquidClass(cell.type);

        // Оновлення спрайту з використанням спрайтів з LiquidClass
        SpriteRenderer renderer = cell.gameObject.GetComponent<SpriteRenderer>();
        if (renderer != null && liquidClass.liquidSprites.Length > 0)
        {
            int spriteIndex = Mathf.RoundToInt(cell.amount * (liquidClass.liquidSprites.Length - 1));
            spriteIndex = Mathf.Clamp(spriteIndex, 0, liquidClass.liquidSprites.Length - 1);
            renderer.sprite = liquidClass.liquidSprites[spriteIndex];

            // Встановлення кольору з LiquidClass
            Color color = liquidClass.liquidColor;

            // Для води додаємо прозорість
            if (cell.type == LiquidType.Water)
            {
                color.a = 0.7f + 0.3f * cell.amount;
            }

            renderer.color = color;
        }

        // Додавання світла для рідин що світяться (наприклад, лава)
        if (liquidClass.causesLight)
        {
            Light lightComponent = cell.gameObject.GetComponent<Light>();
            if (lightComponent == null)
            {
                lightComponent = cell.gameObject.AddComponent<Light>();
                lightComponent.type = LightType.Point;
                lightComponent.range = 5f;
            }

            lightComponent.color = liquidClass.lightColor;
            lightComponent.intensity = liquidClass.lightIntensity * cell.amount;
        }
    }

    // Основний цикл оновлення рідин - тепер з сортуванням знизу вгору
    private IEnumerator UpdateLiquidSystem()
    {
        WaitForSeconds wait = new WaitForSeconds(updateInterval);

        while (true)
        {
            // Створюємо список позицій для оновлення і сортуємо їх знизу вгору
            List<Vector2Int> positionsToUpdate = new List<Vector2Int>();

            int processedCount = 0;
            int maxPerFrame = Mathf.Min(updateBatchSize, updateQueue.Count);

            while (processedCount < maxPerFrame && updateQueue.Count > 0)
            {
                Vector2Int position = updateQueue.Dequeue();
                activeCells.Remove(position);

                if (liquidMap.ContainsKey(position))
                {
                    positionsToUpdate.Add(position);
                }
                processedCount++;
            }

            // КЛЮЧОВЕ: сортуємо позиції знизу вгору (найнижчі Y спочатку)
            positionsToUpdate.Sort((a, b) => a.y.CompareTo(b.y));

            // Обробляємо всі позиції знизу вгору
            foreach (Vector2Int position in positionsToUpdate)
            {
                if (liquidMap.ContainsKey(position))
                {
                    UpdateLiquidPhysics(position);
                }
            }

            yield return wait;
        }
    }

    // Оновлена логіка фізики рідин з урахуванням в'язкості з LiquidClass
    private void UpdateLiquidPhysics(Vector2Int position)
    {
        if (!liquidMap.TryGetValue(position, out LiquidCell cell) || cell.amount <= minLiquidAmount)
            return;

        bool liquidMoved = false;
        LiquidType type = cell.type;
        LiquidClass liquidClass = GetLiquidClass(type);

        // ПРІОРИТЕТ 1: Спочатку намагаємось повністю заповнити клітинку знизу
        Vector2Int belowPos = new Vector2Int(position.x, position.y - 1);

        if (CanFlowTo(belowPos, type))
        {
            float flowAmount = CalculateVerticalFlowComplete(position, belowPos, liquidClass);

            if (flowAmount > 0)
            {
                MoveFluid(position, belowPos, flowAmount);
                liquidMoved = true;
            }
        }

        // ПРІОРИТЕТ 2: Горизонтальний рух тільки якщо не можемо текти вниз
        // АБО якщо поточна клітинка майже заповнена
        if (!liquidMoved || GetLiquidAmount(position) >= horizontalFlowThreshold)
        {
            // Перевіряємо чи справді не можемо текти вниз
            bool canStillFlowDown = CanFlowTo(belowPos, type) && GetLiquidAmount(belowPos) < maxFillLevel;

            // Горизонтальний потік тільки якщо не можемо текти вниз
            if (!canStillFlowDown)
            {
                TryHorizontalFlow(position, type, liquidClass, ref liquidMoved);
            }
        }

        // Оновлюємо клітинку, якщо рідина рухалась
        if (liquidMoved)
        {
            UpdateLiquidVisual(position);
            ScheduleForUpdate(position);
        }
        CheckLiquidInteractions(position);/////
    }

    // Оновлений розрахунок вертикального потоку з урахуванням в'язкості
    private float CalculateVerticalFlowComplete(Vector2Int from, Vector2Int to, LiquidClass liquidClass)
    {
        if (!liquidMap.TryGetValue(from, out LiquidCell sourceCell))
            return 0;

        // Урахування в'язкості - менша в'язкість = швидший потік
        float viscosityMultiplier = baseFlowRate / liquidClass.viscosity;

        if (liquidMap.TryGetValue(to, out LiquidCell targetCell))
        {
            float availableSpace = maxFillLevel - targetCell.amount;
            float maxAvailable = sourceCell.amount * viscosityMultiplier;

            // КЛЮЧОВЕ: якщо після переливання залишиться мало - переливаємо ВСЕ
            float wouldRemain = sourceCell.amount - Mathf.Min(maxAvailable, availableSpace);
            if (wouldRemain <= minLiquidAmount * 2f)
            {
                return Mathf.Min(sourceCell.amount, availableSpace);
            }

            return Mathf.Min(maxAvailable - minLiquidAmount, availableSpace);
        }

        // Для порожньої клітинки - також перевіряємо
        if (sourceCell.amount <= minLiquidAmount * 2f)
        {
            return sourceCell.amount;
        }

        return (sourceCell.amount - minLiquidAmount) * viscosityMultiplier;
    }

    // Оновлений горизонтальний потік з урахуванням в'язкості
    private void TryHorizontalFlow(Vector2Int position, LiquidType type, LiquidClass liquidClass, ref bool liquidMoved)
    {
        Vector2Int leftPos = new Vector2Int(position.x - 1, position.y);
        Vector2Int rightPos = new Vector2Int(position.x + 1, position.y);

        bool canFlowLeft = CanFlowTo(leftPos, type);
        bool canFlowRight = CanFlowTo(rightPos, type);

        if (canFlowLeft || canFlowRight)
        {
            float currentAmount = GetLiquidAmount(position);
            float viscosityMultiplier = baseFlowRate / liquidClass.viscosity;

            if (canFlowLeft && canFlowRight)
            {
                // МИТТЄВЕ розподілення між трьома клітинками з урахуванням в'язкості
                float leftAmount = GetLiquidAmount(leftPos);
                float rightAmount = GetLiquidAmount(rightPos);
                float totalLiquid = currentAmount + leftAmount + rightAmount;

                // Ідеальний рівень для кожної клітинки
                float idealLevel = totalLiquid / 3f;
                float targetLevel = Mathf.Min(idealLevel, maxFillLevel);

                // Переливаємо ліворуч якщо потрібно
                if (leftAmount < targetLevel && currentAmount > targetLevel)
                {
                    float flowLeft = Mathf.Min(targetLevel - leftAmount, currentAmount - minLiquidAmount) * viscosityMultiplier;
                    if (flowLeft > 0.01f)
                    {
                        MoveFluid(position, leftPos, flowLeft);
                        liquidMoved = true;
                        currentAmount -= flowLeft;
                    }
                }

                // Переливаємо праворуч якщо потрібно
                if (rightAmount < targetLevel && currentAmount > targetLevel)
                {
                    float flowRight = Mathf.Min(targetLevel - rightAmount, currentAmount - minLiquidAmount) * viscosityMultiplier;
                    if (flowRight > 0.01f)
                    {
                        MoveFluid(position, rightPos, flowRight);
                        liquidMoved = true;
                    }
                }
            }
            else if (canFlowLeft)
            {
                float flowAmount = CalculateHorizontalFlow(position, leftPos, liquidClass);
                if (flowAmount > 0.01f)
                {
                    MoveFluid(position, leftPos, flowAmount);
                    liquidMoved = true;
                }
            }
            else if (canFlowRight)
            {
                float flowAmount = CalculateHorizontalFlow(position, rightPos, liquidClass);
                if (flowAmount > 0.01f)
                {
                    MoveFluid(position, rightPos, flowAmount);
                    liquidMoved = true;
                }
            }
        }
    }

    // Перевірка можливості перетікання рідини
    private bool CanFlowTo(Vector2Int position, LiquidType type)
    {
        // Не можна протікати крізь тверді блоки
        if (worldGenerator.IsTileAt(position.x, position.y))
            return false;

        // Якщо клітинка порожня - можна перетікати
        if (!liquidMap.ContainsKey(position))
            return true;

        // Якщо клітинка містить той самий тип рідини і не повністю заповнена
        if (liquidMap.TryGetValue(position, out LiquidCell cell) &&
            cell.type == type &&
            cell.amount < maxFillLevel)
            return true;

        return false;
    }

    // Оновлений розрахунок горизонтального потоку з урахуванням в'язкості
    private float CalculateHorizontalFlow(Vector2Int from, Vector2Int to, LiquidClass liquidClass)
    {
        if (!liquidMap.TryGetValue(from, out LiquidCell sourceCell))
            return 0;

        float viscosityMultiplier = baseFlowRate / liquidClass.viscosity;

        // Якщо клітинка призначення вже має рідину
        if (liquidMap.TryGetValue(to, out LiquidCell targetCell))
        {
            // Перевіряємо чи є сенс переливати
            if (sourceCell.amount <= targetCell.amount + 0.02f)
                return 0;

            // МИТТЄВЕ вирівнювання рівнів з урахуванням в'язкості
            float totalLiquid = sourceCell.amount + targetCell.amount;
            float targetLevel = totalLiquid / 2f;

            float maxTargetCanHold = maxFillLevel;
            float actualTargetLevel = Mathf.Min(targetLevel, maxTargetCanHold);

            float flowAmount = (actualTargetLevel - targetCell.amount) * viscosityMultiplier;
            float maxFromSource = sourceCell.amount - minLiquidAmount;

            return Mathf.Max(0, Mathf.Min(flowAmount, maxFromSource));
        }

        // Якщо клітинка порожня - передаємо половину рідини з урахуванням в'язкості
        float maxAvailable = sourceCell.amount - minLiquidAmount;
        float halfTransfer = sourceCell.amount * 0.5f * viscosityMultiplier;

        return Mathf.Min(halfTransfer, maxAvailable, maxFillLevel);
    }

    // Переміщення рідини між клітинками
    private void MoveFluid(Vector2Int from, Vector2Int to, float amount)
    {
        if (amount <= 0)
            return;

        // Зменшуємо рідину з джерела
        if (liquidMap.TryGetValue(from, out LiquidCell sourceCell))
        {
            sourceCell.amount -= amount;
            liquidMap[from] = sourceCell;

            // Додаємо рідину до призначення
            if (liquidMap.TryGetValue(to, out LiquidCell targetCell))
            {
                targetCell.amount += amount;
                liquidMap[to] = targetCell;
                UpdateLiquidVisual(to);
            }
            else
            {
                // Створюємо нову рідину
                AddLiquid(to, sourceCell.type, amount);
            }

            // Плануємо оновлення
            ScheduleForUpdate(to);
        }
       CheckLiquidInteractions(to);///////////
       CheckLiquidInteractions(from);///////////
    }
    // Додай цей метод для перевірки взаємодій
    private void CheckLiquidInteractions(Vector2Int position) ///////////
    {
        if (!liquidMap.TryGetValue(position, out LiquidCell cell))
            return;

        // Перевіряємо сусідні клітинки
        foreach (Vector2Int neighbor in neighbors)
        {
            Vector2Int neighborPos = new Vector2Int(position.x + neighbor.x, position.y + neighbor.y);
            if (liquidMap.TryGetValue(neighborPos, out LiquidCell neighborCell))
            {
                // Якщо поточна клітинка - лава, а сусідня - вода (або навпаки)
                if ((cell.type == LiquidType.Lava && neighborCell.type == LiquidType.Water) ||
                    (cell.type == LiquidType.Water && neighborCell.type == LiquidType.Lava))
                {
                    // Визначаємо позицію води та лави
                    Vector2Int waterPos = (cell.type == LiquidType.Water) ? position : neighborPos;
                    Vector2Int lavaPos = (cell.type == LiquidType.Lava) ? position : neighborPos;

                    // Створюємо блок обсидіана на місці нижньої рідини
                    Vector2Int lowerPos = (waterPos.y <= lavaPos.y) ? waterPos : lavaPos;
                    CreateObsidian(lowerPos);

                    // Зменшуємо кількість у верхньої рідини
                    Vector2Int upperPos = (waterPos.y > lavaPos.y) ? waterPos : lavaPos;
                    if (liquidMap.TryGetValue(upperPos, out LiquidCell upperCell))
                    {
                        if (upperCell.type == LiquidType.Water)
                            ReduceWater(upperPos);
                        else if (upperCell.type == LiquidType.Lava)
                            ReduceWater(upperPos); // Припускаю, що у вас є такий метод
                    }

                    return; // Виходимо після першої взаємодії
                }
            }
        }
    }

    // Створення обсидіану на місці лави
    private void CreateObsidian(Vector2Int lavaPosition)
    {
        // Видаляємо лаву
        RemoveLiquid(lavaPosition);
        TileClass obsidianTile = worldGenerator.tileAtlas.stone;
        // Створюємо блок обсидіану
        if (obsidianTile != null)
        {
            worldGenerator.placePrefab(obsidianTile, lavaPosition.x, lavaPosition.y);
        }
    }

    // Зменшення кількості води
    private void ReduceWater(Vector2Int waterPosition)
    {
        if (liquidMap.TryGetValue(waterPosition, out LiquidCell waterCell))
        {
            // Зменшуємо кількість води наполовину
            waterCell.amount *= 0.3f;

            if (waterCell.amount <= minLiquidAmount)
            {
                RemoveLiquid(waterPosition);
            }
            else
            {
                liquidMap[waterPosition] = waterCell;
                UpdateLiquidVisual(waterPosition);
            }
        }
    }

    // Додай цей публічний метод в LiquidManager
    public void OnBlockDestroyed(Vector2Int blockPosition)
    {
        // Перевіряємо всі сусідні позиції навколо зламаного блоку
        for (int i = 0; i < neighbors.Length; i++)
        {
            Vector2Int neighborPos = new Vector2Int(
                blockPosition.x + neighbors[i].x,
                blockPosition.y + neighbors[i].y
            );

            // Якщо в сусідній клітинці є рідина - активуємо її для перевірки потоку
            if (liquidMap.ContainsKey(neighborPos))
            {
                ScheduleForUpdate(neighborPos);
            }
        }

        // Також перевіряємо діагональні сусіди для більш реалістичного потоку
        Vector2Int[] diagonalNeighbors = new Vector2Int[]
        {
        new Vector2Int(-1, -1), // ліво-низ
        new Vector2Int(1, -1),  // право-низ
        new Vector2Int(-1, 1),  // ліво-верх
        new Vector2Int(1, 1)    // право-верх
        };

        for (int i = 0; i < diagonalNeighbors.Length; i++)
        {
            Vector2Int diagonalPos = new Vector2Int(
                blockPosition.x + diagonalNeighbors[i].x,
                blockPosition.y + diagonalNeighbors[i].y
            );

            if (liquidMap.ContainsKey(diagonalPos))
            {
                ScheduleForUpdate(diagonalPos);
            }
        }

        // Перевіряємо чи на місці зламаного блоку може з'явитися рідина з верхніх клітинок
        Vector2Int abovePos = new Vector2Int(blockPosition.x, blockPosition.y + 1);
        if (liquidMap.ContainsKey(abovePos))
        {
            ScheduleForUpdate(abovePos);
        }
    }

    // Додаткові корисні методи
    public bool HasLiquid(Vector2Int position)
    {
        return liquidMap.ContainsKey(position);
    }

    public float GetLiquidAmount(Vector2Int position)
    {
        if (liquidMap.TryGetValue(position, out LiquidCell cell))
            return cell.amount;
        return 0f;
    }

    public LiquidType? GetLiquidType(Vector2Int position)
    {
        if (liquidMap.TryGetValue(position, out LiquidCell cell))
            return cell.type;
        return null;
    }

    public LiquidClass GetLiquidClassAt(Vector2Int position)
    {
        if (liquidMap.TryGetValue(position, out LiquidCell cell))
            return GetLiquidClass(cell.type);
        return null;
    }

    // Перевірка чи рідина завдає шкоди
    public bool DamagesPlayer(Vector2Int position)
    {
        LiquidClass liquidClass = GetLiquidClassAt(position);
        return liquidClass != null && liquidClass.damagesPlayer;
    }

    // Отримання кількості шкоди від рідини
    public float GetDamageAmount(Vector2Int position)
    {
        LiquidClass liquidClass = GetLiquidClassAt(position);
        return liquidClass != null ? liquidClass.damageAmount : 0f;
    }

    // Отримання плавучості рідини
    public float GetBuoyancy(Vector2Int position)
    {
        LiquidClass liquidClass = GetLiquidClassAt(position);
        return liquidClass != null ? liquidClass.buoyancy : 1f;
    }
}