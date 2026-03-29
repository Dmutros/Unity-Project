using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using Game.Types;


public class WorldGeneration : MonoBehaviour
{
    public LightManager lightManager;
    [Header("Lighting")]
    public GameObject overlay;
    public Texture2D worldTilesMap;
    public Material lightShader;
    public float lightThreshold;
    public float lightRadius = 7f;
    List<Vector2Int> unlitBlocks = new List<Vector2Int>();

    [Header("Water Generation")]
    public int waterBodyCount = 3; // кількість водойм
    public int minWaterBodySize = 5; // мінімальний розмір
    public int maxWaterBodySize = 15; // максимальний розмір
    public float waterDepth = 3f; // глибина води

    public int lavaLakeCount = 2;
    public int lavaLakeSize = 5;


    [Header("Lighting Settings")]
    public float lightFalloff = 0.7f;
    [Header("World Settings")]
    private Queue<LightNode> lightQueue = new Queue<LightNode>();
    private HashSet<Vector2Int> processedBlocks = new HashSet<Vector2Int>();


    private struct LightNode
    {
        public int x, y;
        public float intensity;
        public int iteration;

        public LightNode(int x, int y, float intensity, int iteration)
        {
            this.x = x;
            this.y = y;
            this.intensity = intensity;
            this.iteration = iteration;
        }
    }





    public float seed = 0;
    [Header("tileAtlas")]
    public TileAtlas tileAtlas;

    [Header("biomes")]
    [SerializeField] float biomeFreq = 0.04F;
    public Texture2D biomeMap;
    public Gradient biomeGradient;
    public Color[] biomeCols;
    public BiomeClass[] biomes;

    [Header("Variables")]
    [SerializeField] int chunkSize = 10;
    public int worldSizeH = 100;
    public int worldSizeW = 100;
    [SerializeField] int heightAddition = 25;
    public float caveFreq = 0.05F;
    public float worldFreq = 0.05F;

    [Header("Ores")]
    public OreClass[] ores;
    public Texture2D caveNoiseTexture;

    [HideInInspector] public List<Vector2> worldTilesIntPos = new List<Vector2>();
    [HideInInspector] public List<Vector2> worldTilesWallIntPos = new List<Vector2>();
    [HideInInspector] public List<Vector2> worldTilesDecorIntPos = new List<Vector2>();

    [HideInInspector] public List<GameObject> worldTilesObject = new List<GameObject>();
    [HideInInspector] public List<GameObject> worldTilesWallObject = new List<GameObject>();
    [HideInInspector] public List<GameObject> worldTilesDecorObject = new List<GameObject>();

    [HideInInspector] public List<TileClass> worldTilesClass = new List<TileClass>();
    [HideInInspector] public List<TileClass> worldTilesWallClass = new List<TileClass>();
    [HideInInspector] public List<TileClass> worldTilesDecorClass = new List<TileClass>();

    GameObject[] worldChunks;

    public float blockSize = 0.08f;
    BiomeClass curBiome;

    public Texture2D worldMapTexture;
    public bool generateWorld = true;
    public static Vector2 spawnPos;
    public PlayerWorldController player;
    public CamController camera;
    public Tilemap collisionTilemap;
    public TileBase solidTile;

    public LiquidManager liquidManager;
    public UIMapSystem uiMapSystem;

    public GameObject tileDrop;

    Material mat;

    /*    public enum PrefabType
        {
            Block,
            Wall,
            Decor
        }*/

    /*    private void OnValidate()
        {
            caveNoiseTexture = new Texture2D(worldSizeW, worldSizeH);//////
            DrowTextures();
            biomeCols = new Color[biomes.Length];
            for (int i = 0; i < biomes.Length; i++)
            {
                biomeCols[i] = biomes[i].biomeColor;//////

            }
            //        DrowCavesAndOres();
        }*/

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        if (PlayerPrefs.HasKey("WorldSeed"))
        {
            seed = PlayerPrefs.GetInt("WorldSeed");
            PlayerPrefs.DeleteKey("WorldSeed");
        }
        else
        {
            seed = Random.Range(-10000, 10000);
        }
        mat = overlay.GetComponent<Renderer>().material;
        //світло
        /*        worldTilesMap = new Texture2D(worldSizeW, worldSizeH);
                worldTilesMap.filterMode = FilterMode.Point;
                lightShader.SetTexture("_ShadowTexture", worldTilesMap);

                for (int x = 0; x < worldSizeW; x++)
                {
                    for (int y = 0; y < worldSizeH; y++)
                    {
                        worldTilesMap.SetPixel(x, y, Color.white);
                    }
                }
                worldTilesMap.Apply();*/

        // колайдери
        solidTile = ScriptableObject.CreateInstance<Tile>();
        ((Tile)solidTile).colliderType = Tile.ColliderType.Grid;

        // seed = Random.Range(-10000, 10000);
        for (int i = 0; i < ores.Length; i++)
        {
            ores[i].spreadTexture = new Texture2D(worldSizeW, worldSizeH);//////
            biomes[i].ores = new OreClass[4];
        }

        biomeCols = new Color[biomes.Length];
        for (int i = 0; i < biomes.Length; i++)
        {
            biomeCols[i] = biomes[i].biomeColor;//////

        }

        // Сніг (biomes[0])
        biomes[0].ores[0] = new OreClass { name = "Coal", frequency = 0.06f, size = 0.85f, maxSpawnHeight = 60 };
        biomes[0].ores[1] = new OreClass { name = "Copper", frequency = 0.07f, size = 0.78f, maxSpawnHeight = 58 };
        biomes[0].ores[2] = new OreClass { name = "Iron", frequency = 0.10f, size = 0.68f, maxSpawnHeight = 48 };
        biomes[0].ores[3] = new OreClass { name = "Gold", frequency = 0.12f, size = 0.65f, maxSpawnHeight = 38 };

        // Ліс (biomes[1])
        biomes[1].ores[0] = new OreClass { name = "Coal", frequency = 0.11f, size = 0.70f, maxSpawnHeight = 68 };
        biomes[1].ores[1] = new OreClass { name = "Copper", frequency = 0.08f, size = 0.75f, maxSpawnHeight = 60 };
        biomes[1].ores[2] = new OreClass { name = "Iron", frequency = 0.09f, size = 0.70f, maxSpawnHeight = 50 };
        biomes[1].ores[3] = new OreClass { name = "Gold", frequency = 0.05f, size = 0.83f, maxSpawnHeight = 40 };

        // Левади (biomes[2])
        biomes[2].ores[0] = new OreClass { name = "Coal", frequency = 0.12f, size = 0.65f, maxSpawnHeight = 70 };
        biomes[2].ores[1] = new OreClass { name = "Copper", frequency = 0.10f, size = 0.70f, maxSpawnHeight = 65 };
        biomes[2].ores[2] = new OreClass { name = "Iron", frequency = 0.06f, size = 0.82f, maxSpawnHeight = 50 };
        biomes[2].ores[3] = new OreClass { name = "Gold", frequency = 0.03f, size = 0.87f, maxSpawnHeight = 35 };

        // Пустеля (biomes[3])
        biomes[3].ores[0] = new OreClass { name = "Coal", frequency = 0.052f, size = 0.80f, maxSpawnHeight = 65 };
        biomes[3].ores[1] = new OreClass { name = "Copper", frequency = 0.05f, size = 0.83f, maxSpawnHeight = 60 };
        biomes[3].ores[2] = new OreClass { name = "Iron", frequency = 0.09f, size = 0.75f, maxSpawnHeight = 50 };
        biomes[3].ores[3] = new OreClass { name = "Gold", frequency = 0.11f, size = 0.65f, maxSpawnHeight = 40 };

        DrawBiomeTexture();
        //DrowTextures();
        worldMapTexture = new Texture2D(worldSizeW, worldSizeH);//////
        DrowCavesAndOres();
        CreateChunks();
        uiMapSystem.StartWork();

        /*        for (int x = 0; x < worldSizeW; x++)
                {
                    for (int y = 0; y < worldSizeH; y++)
                    {
                        if (worldTilesMap.GetPixel(x, y) == Color.white)
                        {
                            LightBlock(x, y, 1f, 0);
                        }
                    }
                }
                worldTilesMap.Apply();*/
        InitializeLighting();
        GenerateWorld();

        GenerateWaterBodies();
        GenerateLavaLakes();

        player.Spawn();
        camera.Spawn(new Vector3(player.spawnPos.x, player.spawnPos.y, camera.transform.position.z));
        camera.worldSizeH = worldSizeH;
        camera.worldSizeW = worldSizeW;

        // RefreshChunks();
    }




    void InitializeLighting()
    {
        // Ініціалізація текстури освітлення
        worldTilesMap = new Texture2D(worldSizeW, worldSizeH);
        worldTilesMap.filterMode = FilterMode.Point;
        worldTilesMap.SetPixel(0, 0, Color.black);
        worldTilesMap.Apply();
        //Material mat = overlay.GetComponent<Renderer>().material;

        mat.SetTexture("_ShadowTex", worldTilesMap);
        mat.SetFloat("WorldSize", worldSizeW);
        // Заповнення початковим білим кольором (повністю освітлено)
        for (int x = 0; x < worldSizeW; x++)
        {
            for (int y = 0; y < worldSizeH; y++)
            {
                worldTilesMap.SetPixel(x, y, Color.white);
            }
        }
        worldTilesMap.Apply();

        RecalculateAllLighting();
    }

    void RecalculateAllLighting()
    {
        for (int x = 0; x < worldSizeW; x++)
        {
            for (int y = 0; y < worldSizeH; y++)
            {
                if (worldTilesMap.GetPixel(x, y) == Color.white)
                {
                    LightBlockNonRecursive(x, y, 1f);
                }
            }
        }
        worldTilesMap.Apply();
    }


    private void Update()
    {
        RefreshChunks();
    }
    void RefreshChunks()
    {
        /*        for (int i = 0; i < worldChunks.Length; i++)
                {
                    if (Vector2.Distance(new Vector2((i * chunkSize), 0), new Vector2(player.transform.position.x, 0)) > Camera.main.orthographicSize * 2.5f)
                    {
                        worldChunks[i].SetActive(false);
                    }
                    else
                    {
                        worldChunks[i].SetActive(true);
                    }

                }*/
        float camHalfHeight = Camera.main.orthographicSize;
        float camHalfWidth = camHalfHeight * Camera.main.aspect;

        float camLeft = Camera.main.transform.position.x - camHalfWidth;
        float camRight = Camera.main.transform.position.x + camHalfWidth;

        float chunkWidth = chunkSize * blockSize;

        camLeft -= 5 * chunkWidth;
        camRight += 5 * chunkWidth;

        for (int i = 0; i < worldChunks.Length; i++)
        {
            float chunkX = i * chunkWidth;

            if (chunkX + chunkWidth > camLeft && chunkX < camRight)
            {
                worldChunks[i].SetActive(true);
            }
            else
            {
                worldChunks[i].SetActive(false);
            }
        }

    }

    public void DrowTextures()
    {
        for (int i = 0; i < biomes.Length; i++)
        {
            biomes[i].caveNoiseTexture = new Texture2D(worldSizeW, worldSizeH);
            for (int j = 0; j < biomes[i].ores.Length; j++)
            {
                biomes[i].ores[j].spreadTexture = new Texture2D(worldSizeW, worldSizeH);
                GenerateNoiseTextures(biomes[i].ores[j].frequency, biomes[i].ores[j].size, biomes[i].ores[j].spreadTexture);
            }
            //      GenerateNoiseTextures(biomes[i].caveFreq, biomes[i].surfaceValue, biomes[i].caveNoiseTexture);
        }
    }

    public void DrowCavesAndOres()
    {
        caveNoiseTexture = new Texture2D(worldSizeW, worldSizeH);//////
        float v;
        float o;
        for (int x = 0; x < worldSizeW; x++)
        {
            for (int y = 0; y < worldSizeH; y++)
            {
                curBiome = GetCurrentBiome(x, y);
                v = Mathf.PerlinNoise((x + seed) * caveFreq, (y + seed) * caveFreq);//+
                if (v > curBiome.surfaceValue)
                {
                    caveNoiseTexture.SetPixel(x, y, Color.white);
                }
                else
                {
                    caveNoiseTexture.SetPixel(x, y, Color.black);
                }

                for (int i = 0; i < ores.Length; i++)
                {
                    ores[i].spreadTexture.SetPixel(x, y, Color.black);
                    if (curBiome.ores.Length >= i + 1)
                    {
                        o = Mathf.PerlinNoise((x + seed) * curBiome.ores[i].frequency, (y + seed) * curBiome.ores[i].frequency);
                        if (o > curBiome.ores[i].size)
                        {
                            ores[i].spreadTexture.SetPixel(x, y, Color.white);
                        }
                    }
                    ores[i].spreadTexture.Apply();

                }

            }
        }
        caveNoiseTexture.Apply();


        /*        for (int x = 0; x < worldSizeW; x++)
                {
                    for (int y = 0; y < worldSizeH; y++)
                    {
                        curBiome = GetCurrentBiome(x, y);
                        for (int i = 0; i < ores.Length; i++)
                        {
                            ores[i].spreadTexture.SetPixel(x, y, Color.black);
                            if (curBiome.ores.Length >= i + 1)
                            {
                                float v = Mathf.PerlinNoise((x + seed) * curBiome.ores[i].frequency, (y + seed) * curBiome.ores[i].frequency);
                                if (v > curBiome.ores[i].size)
                                {
                                    ores[i].spreadTexture.SetPixel(x, y, Color.white);
                                }
                            }
                            ores[i].spreadTexture.Apply();

                        }
                    }

                }*/
    }

    public void DrawBiomeTexture()
    {
        float v;
        Color color;
        biomeMap = new Texture2D(worldSizeW, worldSizeH);

        for (int x = 0; x < biomeMap.width; x++)
        {
            for (int y = 0; y < biomeMap.height; y++)
            {
                v = Mathf.PerlinNoise((x + seed) * biomeFreq, (y + seed) * biomeFreq);
                color = biomeGradient.Evaluate(v);
                biomeMap.SetPixel(x, y, color);
            }
        }
        biomeMap.Apply();
    }

    public BiomeClass GetCurrentBiome(int x, int y)
    {
        //    Debug.Log(System.Array.IndexOf(biomeCols, biomeMap.GetPixel(x, y)));
        if (System.Array.IndexOf(biomeCols, biomeMap.GetPixel(x, y)) >= 0)
        {
            return biomes[System.Array.IndexOf(biomeCols, biomeMap.GetPixel(x, y))];
        }
        return curBiome;
    }

    // створення чанків
    public void CreateChunks()
    {
        int numChunks = worldSizeW / chunkSize;
        worldChunks = new GameObject[numChunks];
        for (int i = 0; i < numChunks; i++)
        {
            GameObject newChunk = new GameObject();
            newChunk.name = i.ToString();
            newChunk.transform.parent = this.transform;
            worldChunks[i] = newChunk;
        }
    }

    // генерація світу
    public void GenerateWorld()
    {
        TileClass Prefab;
        TileClass WallPrefab;
        /*        GameObject Prefab;
                GameObject WallPrefab;
                Sprite[] tileSprites;
                Sprite[] tileWallSprites;*/

        for (int x = 0; x < worldSizeW; x++)
        {
            // грань поверхні та печер
            float height;

            for (int y = 0; y < worldSizeH; y++)  // генерація 
            {
                curBiome = GetCurrentBiome(x, y);
                //       Debug.Log(curBiome);
                height = Mathf.PerlinNoise((x + seed) * worldFreq, seed * worldFreq) * curBiome.heightMultiplier + heightAddition; //+

                if (x == worldSizeW / 2)
                {
                    player.spawnPos = new Vector2(x * blockSize, (height + 2) * blockSize);
                }

                if (y >= height)
                {
                    break;
                }
                if (y < height - curBiome.dirtLayerHeight) // камінь
                {

                    if (ores[0].spreadTexture.GetPixel(x, y).r > 0.5f && height - y > ores[0].maxSpawnHeight)///////
                    {
                        Prefab = curBiome.tileAtlas.coal;
                    }
                    else if (ores[1].spreadTexture.GetPixel(x, y).r > 0.5f && height - y > ores[1].maxSpawnHeight)///////
                    {
                        Prefab = curBiome.tileAtlas.copper;
                    }
                    else if (ores[2].spreadTexture.GetPixel(x, y).r > 0.5f && height - y > ores[2].maxSpawnHeight)///////
                    {
                        Prefab = curBiome.tileAtlas.iron;
                    }
                    else if (ores[3].spreadTexture.GetPixel(x, y).r > 0.5f && height - y > ores[3].maxSpawnHeight)///////
                    {
                        Prefab = curBiome.tileAtlas.gold;
                    }
                    else
                    {
                        Prefab = curBiome.tileAtlas.stone;
                    }

                    WallPrefab = curBiome.tileAtlas.stoneWall;

                }
                else if (y < height - 1)  // земля
                {
                    Prefab = curBiome.tileAtlas.dirt;
                    WallPrefab = curBiome.tileAtlas.dirtWall;
                }
                else // верхній шар // трава 
                {
                    Prefab = curBiome.tileAtlas.grass;
                    WallPrefab = curBiome.tileAtlas.dirtWall;
                }

                // стіни
                placePrefab(WallPrefab, x, y);

                if (curBiome.generateCaves)  // генерація печер шум перліна
                {
                    if (caveNoiseTexture.GetPixel(x, y).r < 0.5f)///////
                    {
                        placePrefab(Prefab, x, y);
                    }
                }
                else
                {
                    placePrefab(Prefab, x, y);
                }

                if (y >= height - 1) // верхній шар // дерева
                {
                    int tree = Random.Range(0, curBiome.treeChance);
                    if (tree == 1)
                    {
                        if (worldTilesIntPos.Contains(new Vector2(x, y))) // перевірка блоку для дерева
                        {
                            // генерація дерева
                            // GenerteTree(x, y + 1);
                            if (curBiome.biomeName == "Desert")
                            {
                                GenerteCactus(curBiome.tileAtlas, Random.Range(curBiome.minTreeHeight, curBiome.maxTreeHeight), x, y + 1);
                            }
                            else
                            {
                                int maxTreeTop = y + 1 + curBiome.maxTreeHeight + 2;

                                bool inBoundsX = x > 1 && x < worldSizeW - 1;
                                bool inBoundsY = maxTreeTop < worldSizeH;

                                if (inBoundsX && inBoundsY)
                                {
                                    GenerteTree(Random.Range(curBiome.minTreeHeight, curBiome.maxTreeHeight), x, y + 1);
                                }
                                else
                                {
                                    Debug.Log($"Пропущена генерація дерева на ({x * blockSize} , {y * blockSize})");
                                }
                            }
                        }

                    }
                    else
                    {
                        int i = Random.Range(0, curBiome.grassChance);
                        if (i == 1)
                        {
                            if (worldTilesIntPos.Contains(new Vector2(x, y))) // перевірка блоку 
                            {
                                if (curBiome.tileAtlas.grassTall != null)
                                {
                                    placePrefab(curBiome.tileAtlas.grassTall, x, y + 1);
                                }

                            }
                        }
                    }
                }
            }
        }
        worldTilesMap.Apply();

    }

    // генерація дерев
    void GenerteCactus(TileAtlas atlas, int treeHeight, int x, int y)
    {
        //treeHeight = ;

        for (int i = 0; i < treeHeight; i++)
        {
            placePrefab(atlas.log, x, y + i);
        }

    }

    // генерація дерев
    void GenerteTree(int treeHeight, int x, int y)
    {
        //treeHeight = ;

        for (int i = 0; i < treeHeight; i++)
        {
            placePrefab(tileAtlas.log, x, y + i);
        }

        placePrefab(tileAtlas.lief, x, y + treeHeight);
        placePrefab(tileAtlas.lief, x, y + treeHeight + 1);
        placePrefab(tileAtlas.lief, x, y + treeHeight + 2);

        placePrefab(tileAtlas.lief, x - 1, y + treeHeight);
        placePrefab(tileAtlas.lief, x + 1, y + treeHeight + 1);

        placePrefab(tileAtlas.lief, x - 1, y + treeHeight + 1);
        placePrefab(tileAtlas.lief, x + 1, y + treeHeight);
    }

    // генерація текстури шуму перліна
    private void GenerateNoiseTextures(float frequency, float limit, Texture2D noiseTexture)
    {
        float v;
        for (int x = 0; x < noiseTexture.width; x++)
        {
            for (int y = 0; y < noiseTexture.height; y++)
            {
                v = Mathf.PerlinNoise((x + seed) * frequency, (y + seed) * frequency);

                if (v > limit)
                {
                    noiseTexture.SetPixel(x, y, Color.white);
                }
                else
                {
                    noiseTexture.SetPixel(x, y, Color.black);
                }

            }
        }
        noiseTexture.Apply();
    }

    public void removePrefab(int x, int y, PrefabType type, bool drop = true)
    {
        TileClass tile;
        switch (type)
        {
            case PrefabType.Block:
                if (!worldTilesIntPos.Contains(new Vector2(x, y)))
                    return; // 
                if (worldTilesDecorIntPos.Contains(new Vector2(x, y + 1)))
                {
                    removePrefab(x, y + 1, PrefabType.Decor);
                }

                collisionTilemap.SetTile(new Vector3Int(x, y, 0), null);
                int indexBlock = worldTilesIntPos.IndexOf(new Vector2(x, y));
                Destroy(worldTilesObject[indexBlock]);
                tile = worldTilesClass[indexBlock];
                if (drop)
                {
                    DropTile(tile, x, y, type);
                }
                worldTilesObject.RemoveAt(indexBlock);
                worldTilesIntPos.RemoveAt(indexBlock);
                worldTilesClass.RemoveAt(indexBlock);
                liquidManager.OnBlockDestroyed(new Vector2Int(x, y));
                // Оновлюємо освітлення

                break;
            case PrefabType.Wall:
                if (!worldTilesWallIntPos.Contains(new Vector2(x, y)))
                    return; // 
                int indexWall = worldTilesWallIntPos.IndexOf(new Vector2(x, y));
                Destroy(worldTilesWallObject[indexWall]);
                tile = worldTilesWallClass[indexWall];
                if (drop)
                {
                    DropTile(tile, x, y, type);
                }
                worldTilesWallObject.RemoveAt(indexWall);
                worldTilesWallIntPos.RemoveAt(indexWall);
                worldTilesWallClass.RemoveAt(indexWall);

                break;
            case PrefabType.Decor:
                if (!worldTilesDecorIntPos.Contains(new Vector2(x, y)))
                    return; // 
                int indexDecor = worldTilesDecorIntPos.IndexOf(new Vector2(x, y));
                Destroy(worldTilesDecorObject[indexDecor]);
                worldTilesDecorObject.RemoveAt(indexDecor);
                worldTilesDecorIntPos.RemoveAt(indexDecor);
                worldTilesDecorClass.RemoveAt(indexDecor);
                break;
        }

        uiMapSystem.UpdateMap(new Vector2Int(x, y));
    }


    public void DropTile(TileClass tile, int x, int y, PrefabType type)
    {
        GameObject newTileDrop = Instantiate(tileDrop, new Vector2(x * blockSize + blockSize / 4, y * blockSize + 0.05f), Quaternion.identity);
        newTileDrop.GetComponent<SpriteRenderer>().sprite = tile.tileDrop.icon;//.GetComponent<SpriteRenderer>().sprite;
        ItemClass tileDropItem = tile.tileDrop;
        newTileDrop.GetComponent<TileDropController>().item = tileDropItem;
    }


    // розміщення префабів
    public bool placePrefab(TileClass tileData, int x, int y)
    {
        if (x >= 0 && x <= worldSizeW && y >= 0 && y <= worldSizeH)
        {
            GameObject Prefab = tileData.tileObject;
            Sprite[] tileSprites = tileData.tileSprites;
            PrefabType type = tileData.prefabType;

            
            //  if (!worldPrefabsIntPos.Contains(new Vector2Int(x, y)) && x >= 0 && x <= worldSizeW && y >= 0 && y <= worldSizeH) 
            switch (type)
            {
                case PrefabType.Block:
                    if (worldTilesIntPos.Contains(new Vector2(x, y)))
                        return false; // 
                    if (worldTilesDecorIntPos.Contains(new Vector2(x, y)))
                    {
                        removePrefab(x, y, PrefabType.Decor);
                    }
                    liquidManager.RemoveLiquid(new Vector2Int(x, y));
                    break;
                case PrefabType.Wall:
                    if (worldTilesWallIntPos.Contains(new Vector2(x, y)))
                        return false; // 
                    break;
                case PrefabType.Decor:
                    if (worldTilesDecorIntPos.Contains(new Vector2(x, y)) || worldTilesIntPos.Contains(new Vector2(x, y)))
                        return false; // 
                    break;
            }
            float chunkCoord = Mathf.Round(x / chunkSize) * chunkSize;
            chunkCoord /= chunkSize;


            RemoveLightSource(x, y);///////////
            worldTilesMap.SetPixel(x, y, Color.black);

            if (generateWorld)
            {
                GameObject newPrefab = Instantiate(Prefab, new Vector2(x * blockSize, y * blockSize), Quaternion.identity);

                if (tileSprites.Length != 0)
                {
                    int i = Random.Range(0, tileSprites.Length);
                    newPrefab.GetComponent<SpriteRenderer>().sprite = tileSprites[i];
                }

                newPrefab.transform.parent = worldChunks[(int)chunkCoord].transform;

                //     worldPrefabsPos.Add(newPrefab.transform.position);
                // liquidManager.AddTile(new Vector2(x, y), tileData);
                switch (type)
                {
                    case PrefabType.Block:
                        worldTilesIntPos.Add(new Vector2(x, y));
                        worldTilesObject.Add(newPrefab);
                        worldTilesClass.Add(tileData);
                        collisionTilemap.SetTile(new Vector3Int(x, y, 0), solidTile);
                        break;
                    case PrefabType.Wall:
                        worldTilesWallIntPos.Add(new Vector2(x, y));
                        worldTilesWallObject.Add(newPrefab);
                        worldTilesWallClass.Add(tileData);
                        break;
                    case PrefabType.Decor:
                        worldTilesDecorIntPos.Add(new Vector2(x, y));
                        worldTilesDecorObject.Add(newPrefab);
                        worldTilesDecorClass.Add(tileData);
                        break;
                }
            }
        }

        uiMapSystem.UpdateMap(new Vector2Int(x, y));
        return true;
        // RegenerateColliders();
    }

    /*
        void LightBlock(int x, int y, float intensity, int iteration)
        {
            if (iteration < lightRadius)
            {
                worldTilesMap.SetPixel(x, y, Color.white * intensity);
                for (int nx = x - 1; nx < x + 2; nx++)
                {
                    for (int ny = y - 1; ny < y + 2; ny++)
                    {
                        if (nx != x || ny != y)
                        {
                            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(nx, ny));
                            float targetIntensity = Mathf.Pow(0.7f, dist) * intensity;
                            if (worldTilesMap.GetPixel(nx, ny) != null)
                            {
                                if (worldTilesMap.GetPixel(nx, ny).r < targetIntensity)
                                {
                                    LightBlock(nx, ny, targetIntensity, iteration);
                                }
                            }
                        }
                    }
                }
                worldTilesMap.Apply();
            }
        }

        void RemoveLightSource(int x, int y)
        {
            unlitBlocks.Clear();
            UnlightBlock(x, y, x, y);

            List<Vector2Int> toRelight = new List<Vector2Int>();

            foreach (Vector2Int block in unlitBlocks)
            {
                for (int nx = block.x - 1; nx < block.x + 2; nx++)
                {
                    for (int ny = block.y - 1; ny < block.y + 2; ny++)
                    {

                        if (worldTilesMap.GetPixel(nx, ny) != null)
                        {
                            if (worldTilesMap.GetPixel(nx, ny).r > worldTilesMap.GetPixel(block.x, block.y).r)
                            {
                                if (!toRelight.Contains(new Vector2Int(nx, ny)))
                                {
                                    toRelight.Add(new Vector2Int(nx, ny));
                                }

                            }
                        }

                    }
                }
            }

            foreach (Vector2Int source in toRelight)
            {
                LightBlock(source.x, source.y, worldTilesMap.GetPixel(source.x, source.y).r, 0);
            }
            worldTilesMap.Apply();
        }

        void UnlightBlock(int x, int y, int ix, int iy)
        {
            if (Mathf.Abs(x - ix) >= lightRadius || Mathf.Abs(y - iy) >= lightRadius || unlitBlocks.Contains(new Vector2Int(x, y)))
                return;

            for (int nx = x - 1; nx < x + 2; nx++)
            {
                for (int ny = y - 1; ny < y + 2; ny++)
                {
                    if (nx != x || ny != y)
                    {
                        if (worldTilesMap.GetPixel(nx, ny) != null)
                        {
                            if (worldTilesMap.GetPixel(nx, ny).r < worldTilesMap.GetPixel(x, y).r)
                            {
                                UnlightBlock(nx, ny, ix, iy);
                            }
                        }
                    }
                }
            }
            worldTilesMap.SetPixel(x, y, Color.black);
            unlitBlocks.Add(new Vector2Int(x, y));  
            worldTilesMap.Apply();
        }*/



    void LightBlockNonRecursive(int startX, int startY, float startIntensity)
    {
        lightQueue.Clear();
        processedBlocks.Clear();

        lightQueue.Enqueue(new LightNode(startX, startY, startIntensity, 0));

        while (lightQueue.Count > 0)
        {
            LightNode current = lightQueue.Dequeue();

            // Перевірка меж та ітерацій
            if (current.iteration >= lightRadius ||
                current.x < 0 || current.x >= worldSizeW ||
                current.y < 0 || current.y >= worldSizeH)
                continue;

            Vector2Int pos = new Vector2Int(current.x, current.y);

            // Перевірка, чи вже обробляли цей блок з такою ж або кращою інтенсивністю
            Color currentPixel = worldTilesMap.GetPixel(current.x, current.y);
            if (currentPixel.r >= current.intensity)
                continue;

            // Встановлюємо нову інтенсивність
            worldTilesMap.SetPixel(current.x, current.y, Color.white * current.intensity);

            // Додаємо сусідні блоки до черги
            for (int nx = current.x - 1; nx <= current.x + 1; nx++)
            {
                for (int ny = current.y - 1; ny <= current.y + 1; ny++)
                {
                    if (nx == current.x && ny == current.y) continue;
                    if (nx < 0 || nx >= worldSizeW || ny < 0 || ny >= worldSizeH) continue;

                    float dist = Vector2.Distance(new Vector2(current.x, current.y), new Vector2(nx, ny));
                    float targetIntensity = Mathf.Pow(lightFalloff, dist) * current.intensity;

                    if (targetIntensity > 0.01f) // Мінімальний поріг освітлення
                    {
                        lightQueue.Enqueue(new LightNode(nx, ny, targetIntensity, current.iteration + 1));
                    }
                }
            }
        }
    }

    // Видалення джерела світла (нерекурсивна версія)
    public void RemoveLightSource(int x, int y)
    {
        unlitBlocks.Clear();
        UnlightBlockNonRecursive(x, y);

        // Знаходимо блоки для перерахунку освітлення
        List<Vector2Int> toRelight = new List<Vector2Int>();
        foreach (Vector2Int block in unlitBlocks)
        {
            for (int nx = block.x - 1; nx <= block.x + 1; nx++)
            {
                for (int ny = block.y - 1; ny <= block.y + 1; ny++)
                {
                    if (nx < 0 || nx >= worldSizeW || ny < 0 || ny >= worldSizeH) continue;

                    Color neighborPixel = worldTilesMap.GetPixel(nx, ny);
                    Color blockPixel = worldTilesMap.GetPixel(block.x, block.y);

                    if (neighborPixel.r > blockPixel.r)
                    {
                        Vector2Int relightPos = new Vector2Int(nx, ny);
                        if (!toRelight.Contains(relightPos))
                        {
                            toRelight.Add(relightPos);
                        }
                    }
                }
            }
        }

        // Перерахунок освітлення для знайдених блоків
        foreach (Vector2Int source in toRelight)
        {
            float currentIntensity = worldTilesMap.GetPixel(source.x, source.y).r;
            LightBlockNonRecursive(source.x, source.y, currentIntensity);
        }

        worldTilesMap.Apply();
    }

    void UnlightBlockNonRecursive(int startX, int startY)
    {
        Queue<Vector2Int> unlightQueue = new Queue<Vector2Int>();
        HashSet<Vector2Int> processed = new HashSet<Vector2Int>();

        unlightQueue.Enqueue(new Vector2Int(startX, startY));

        while (unlightQueue.Count > 0)
        {
            Vector2Int current = unlightQueue.Dequeue();

            if (processed.Contains(current)) continue;
            if (Mathf.Abs(current.x - startX) >= lightRadius ||
                Mathf.Abs(current.y - startY) >= lightRadius) continue;
            if (current.x < 0 || current.x >= worldSizeW ||
                current.y < 0 || current.y >= worldSizeH) continue;

            processed.Add(current);

            Color currentPixel = worldTilesMap.GetPixel(current.x, current.y);

            // Перевіряємо сусідів
            for (int nx = current.x - 1; nx <= current.x + 1; nx++)
            {
                for (int ny = current.y - 1; ny <= current.y + 1; ny++)
                {
                    if (nx == current.x && ny == current.y) continue;
                    if (nx < 0 || nx >= worldSizeW || ny < 0 || ny >= worldSizeH) continue;

                    Vector2Int neighborPos = new Vector2Int(nx, ny);
                    if (processed.Contains(neighborPos)) continue;

                    Color neighborPixel = worldTilesMap.GetPixel(nx, ny);

                    if (neighborPixel.r < currentPixel.r)
                    {
                        unlightQueue.Enqueue(neighborPos);
                    }
                }
            }

            // Затемнюємо поточний блок
            worldTilesMap.SetPixel(current.x, current.y, Color.black);
            unlitBlocks.Add(current);
        }
    }




    public bool IsTileAt(int x, int y)
    {
        return worldTilesIntPos.Contains(new Vector2(x, y));
    }

    /*    public void UpdateMap(Vector2Int position)
        {
            Color color = Color.magenta;

            if (worldTilesIntPos.Contains(new Vector2(position.x, position.y)))
            {
                int indexBlock = worldTilesIntPos.IndexOf(new Vector2(position.x, position.y));
                color = worldTilesClass[indexBlock].mapColor;
            }
            else if (worldTilesWallIntPos.Contains(new Vector2(position.x, position.y)))
            {
                int indexWall = worldTilesWallIntPos.IndexOf(new Vector2(position.x, position.y));
                color = worldTilesWallClass[indexWall].mapColor;
            }
            worldMapTexture.SetPixel(position.x, position.y, color);
            worldMapTexture.Apply();
        }*/

    void GenerateWaterBodies()
    {
        for (int i = 0; i < waterBodyCount; i++)
        {
            Vector2Int lakeCenter = FindDepression();
            if (lakeCenter.x != -1)
                CreateLake(lakeCenter);
        }
    }

    Vector2Int FindDepression()
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            int x = Random.Range(30, worldSizeW - 30);

            // Перевіряємо відстань від спавну
            if (Mathf.Abs(x - worldSizeW / 2) < 25) continue;

            float centerHeight = GetHeightAt(x);
            int higherCount = 0;

            // Перевіряємо навколишні точки
            for (int checkX = x - 15; checkX <= x + 15; checkX += 5)
            {
                if (checkX >= 0 && checkX < worldSizeW && GetHeightAt(checkX) > centerHeight + 2)
                    higherCount++;
            }

            // Якщо 60%+ точок вище - це заглиблення
            if (higherCount >= 4) // 7 точок перевіряємо, 4+ = 60%+
            {
                return new Vector2Int(x, (int)centerHeight);
            }
        }
        return new Vector2Int(-1, -1);
    }

    void CreateLake(Vector2Int center)
    {
        int radius = Random.Range(minWaterBodySize, maxWaterBodySize);
        int waterLevel = center.y + (int)waterDepth;

        for (int x = center.x - radius; x <= center.x + radius; x++)
        {
            if (x < 0 || x >= worldSizeW) continue;

            int groundY = (int)GetHeightAt(x);

            if (groundY <= waterLevel && Mathf.Abs(x - center.x) <= radius)
            {
                for (int y = groundY; y <= waterLevel; y++)
                {
                    if (y >= 0 && y < worldSizeH)
                    {
                        // Видаляємо перешкоди
                        Vector2 pos = new Vector2(x, y);
                        if (worldTilesIntPos.Contains(pos))
                            removePrefab(x, y, PrefabType.Block, false);
                        if (worldTilesDecorIntPos.Contains(pos))
                            removePrefab(x, y, PrefabType.Decor, false);

                        // Додаємо воду
                        liquidManager.AddLiquid(new Vector2Int(x, y), LiquidType.Water, 1f);
                    }
                }
            }
        }
    }

    void GenerateLavaLakes()
    {
        for (int i = 0; i < lavaLakeCount; i++)
        {
            // Випадкова позиція глибоко під землею
            int x = Random.Range(20, worldSizeW - 20);
            int y = Random.Range(5, worldSizeH / 3);

            // Перевіряємо, що біом не зимний
            BiomeClass biome = GetCurrentBiome(x, y);
            if (biome.biomeName == "Snow") continue;

            // Перевіряємо, що вся область буде в закритій породі
            bool isEnclosed = true;
            for (int checkX = x - lavaLakeSize - 1; checkX <= x + lavaLakeSize + 1; checkX++)
            {
                for (int checkY = y - lavaLakeSize / 2 - 1; checkY <= y + lavaLakeSize / 2 + 1; checkY++)
                {
                    if (checkX >= 0 && checkX < worldSizeW && checkY >= 0 && checkY < worldSizeH)
                    {
                        // Якщо це порожнина за картою печер - пропускаємо
                        if (caveNoiseTexture.GetPixel(checkX, checkY).r > 0.5f)
                        {
                            isEnclosed = false;
                            break;
                        }
                    }
                }
                if (!isEnclosed) break;
            }

            if (!isEnclosed) continue;

            // Створюємо овальну дірку
            for (int dx = -lavaLakeSize; dx <= lavaLakeSize; dx++)
            {
                for (int dy = -lavaLakeSize / 2; dy <= lavaLakeSize / 2; dy++)
                {
                    int posX = x + dx;
                    int posY = y + dy;



                    // Овальна форма (нормалізовані координати)
                    float normalizedX = (float)dx / lavaLakeSize;
                    float normalizedY = (float)dy / (lavaLakeSize / 2f);
                    if (normalizedX * normalizedX + normalizedY * normalizedY <= 1f)
                    {
                        Vector2 pos = new Vector2(posX, posY);

                        // Видаляємо блоки
                        if (worldTilesIntPos.Contains(pos))
                            removePrefab(posX, posY, PrefabType.Block, false);

                        // Додаємо лаву тільки в нижню частину
                        if (dy >= -1) // залишаємо верхні 2-3 шари порожніми
                            liquidManager.AddLiquid(new Vector2Int(posX, posY), LiquidType.Lava, 1f);
                    }
                }
            }
        }
    }

    float GetHeightAt(int x)
    {
        BiomeClass biome = GetCurrentBiome(x, 0);
        return Mathf.PerlinNoise((x + seed) * worldFreq, seed * worldFreq) * biome.heightMultiplier + heightAddition;
    }

}
