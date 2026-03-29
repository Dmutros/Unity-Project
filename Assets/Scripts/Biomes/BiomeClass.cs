using UnityEngine;

[System.Serializable]
public class BiomeClass
{   
    public string biomeName;
    public Color biomeColor;

    public TileAtlas tileAtlas;
    
    [Header("Noise")]
/*    public float caveFreq = 0.05F;
    public float worldFreq = 0.05F;*/
    public Texture2D caveNoiseTexture;

    [Header("Generations")]
    public bool generateCaves = true;
    public float surfaceValue = 0.5f;
    public float heightMultiplier = 25f;
    public int dirtLayerHeight = 5;

    [Header("Trees+")]
    public int treeChance = 10;
    public int grassChance = 3;
    public int minTreeHeight = 3;
    public int maxTreeHeight = 7;

    [Header("Ores")]
    public OreClass[] ores;
}
