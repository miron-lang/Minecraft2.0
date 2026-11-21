using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class WorldGenerator : MonoBehaviour
{
    [Header("Desert")]
    public GameObject sandStone;

    [Header("Plains")]
    public GameObject dirt;

    [Header("Evry")]
    public GameObject stone;
    public GameObject deepslate;
    public GameObject bedrock;
    public GameObject andesite;
    public GameObject tuff;
    public GameObject grassBlock;
    public GameObject sand;

    [Header("Trees")]
    public GameObject oakLeave;
    public GameObject oakLog;
    private int[,] blockHight;
    private bool[,] isBlockDesert;

    [Header("World size")]
    public int xSize;// Рамземер мира по x
    public int zSize;// Рамземер мира по z

    [Header("Lakes, rivers and oceans")]
    public int waterLevelY = 2;
    public GameObject water;

    [Header("Chunks")]
    public int chunkSize = 16;
    public int chunksX = 4;
    public int chunksZ = 4;

    private float biomeOffsetX;
    private float biomeOffsetZ;
    private float hightOffsetX;
    private float hightOffsetZ;

    private void Awake()
    {
        xSize = chunksX * chunkSize;
        zSize = chunksZ * chunkSize;

        transform.position = new Vector3(xSize/2, 0, zSize/2);
    }

    void Start()
    {
        GenerateWorld();
    }

    void GenerateChunks(int chunkX, int chunkZ)
    {
        GameObject chunk = new GameObject($"Chunk_{chunkX}_{chunkZ}");
        chunk.transform.SetParent(transform, false);

        for (int localX = 0; localX < chunkSize; localX++)
        {
            for (int localZ = 0; localZ < chunkSize; localZ++)
            {
                int x = chunkX * chunkSize + localX;
                int z = chunkZ * chunkSize + localZ;

                float noise = Mathf.PerlinNoise(x * 0.12f, z * 0.12f);// Шум для высоты
                float biomeNoise = Mathf.PerlinNoise(x * 0.025f + biomeOffsetX, z * 0.025f + biomeOffsetZ);// Шум для биомов

                float plainsNoice = Mathf.PerlinNoise(x * 0.05f + hightOffsetX, z * 0.05f + hightOffsetZ);// Шум для высоту полей
                float duneNoice = Mathf.PerlinNoise(x * 0.035f + 300f + hightOffsetX, z * 0.09f + 500f + hightOffsetZ);// Щум для высоты пустыни

                float plainsHeight = plainsNoice * 6f;// Просчитывоние высоты в полянах
                float desetHeight = duneNoice * 8f;// Просчитывоние высоты в пустыни

                float desertBlend = Mathf.InverseLerp(0.5f, 0.65f, biomeNoise);// Высота в биоме пустыни

                int hight = Mathf.RoundToInt(Mathf.Lerp(plainsHeight, desetHeight, desertBlend));// Прасчитывоние высоты блоков

                bool isDesert = biomeNoise > 0.5f;// Проверка биома

                bool isUnderWater = hight < waterLevelY;
                GameObject surface = (isDesert || isUnderWater) ? sand : grassBlock;// Настройки зависимые от биома
                GameObject underGround = isDesert ? sandStone : dirt;// Настройки зависимые от биома

                blockHight[x, z] = hight;
                isBlockDesert[x, z] = isDesert;

                SpawnBlok(surface, x, hight, z, chunk.transform);// Начяло ставление блоков
                SpawnBlok(underGround, x, hight - 1, z, chunk.transform);
                SpawnBlok(underGround, x, hight - 2, z, chunk.transform);
                SpawnBlok(stone, x, hight - 3, z, chunk.transform);
                SpawnBlok(stone, x, hight - 4, z, chunk.transform);
                SpawnBlok(Random.Range(0, 4) == 3 ? andesite : stone, x, hight - 5, z, chunk.transform);
                SpawnBlok(Random.Range(0, 3) == 2 ? tuff : deepslate, x, hight - 6, z, chunk.transform);
                SpawnBlok(deepslate, x, hight - 7, z, chunk.transform);
                SpawnBlok(deepslate, x, hight - 8, z, chunk.transform);
                SpawnBlok(deepslate, x, hight - 9, z, chunk.transform);
                SpawnBlok(bedrock, x, hight - 10, z, chunk.transform);// Канец ставление блоков

                for (int y = hight + 1; y <= waterLevelY; y++)
                {
                    SpawnBlok(water, x, y, z, chunk.transform);
                }
            }

        }

        GenerateTrees(chunkX, chunkZ, chunk.transform);
    }

    public void GenerateWorld()
    {
        biomeOffsetX = Random.Range(1000f, 10001f);
        biomeOffsetZ = Random.Range(1000f, 10001f);
        hightOffsetX = Random.Range(1000f, 10001f);
        hightOffsetZ = Random.Range(1000f, 10001f);

        blockHight = new int[xSize, zSize];
        isBlockDesert = new bool[xSize, zSize];

        for (int x = 0; x < chunksX; x++)// Генераыя по x
        {
            for (int z = 0; z < chunksZ; z++)// На каждый x по 16 блоков по оси z
            {
                GenerateChunks(x, z);
            }
        }
    }

    void SpawnBlok(GameObject block, int x, int y, int z, Transform parent = null)// Ставить блок(как /setBlock в маине)
    {
        Vector3 position = new Vector3(x, y, z);

        Instantiate(block, position, Quaternion.identity, parent == null ? transform : parent);// Ставица блок
    }

    void SpawnNormalTree(int x, int groundY, int z, Transform parent)
    {
        int treeHight = Random.Range(2, 4);

        for (int y = 1; y <= treeHight; y++)
        {
            SpawnBlok(oakLog, x, groundY + y, z, parent);// Ставица брёвна
        }

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                SpawnBlok(oakLeave, x + dx, groundY + treeHight+1, z + dz, parent);// Ставица листва
            }
        }

        SpawnBlok(oakLeave, x, groundY + treeHight+2, z, parent);// Верхнея Листва начяло
        SpawnBlok(oakLeave, x+1, groundY + treeHight+2, z, parent);
        SpawnBlok(oakLeave, x, groundY + treeHight+2, z+1, parent);
        SpawnBlok(oakLeave, x-1, groundY + treeHight+2, z, parent);
        SpawnBlok(oakLeave, x, groundY + treeHight+2, z-1, parent);// Верхнея Листва канец
    }
    void GenerateTrees(int chunkX, int chunkZ, Transform parent)
    {
        int startX = chunkX*chunkSize;
        int startZ = chunkZ*chunkSize;

        for (int areaX = 0; areaX + 5 <= chunkSize; areaX += 5)
        {
            for (int areaZ = 0; areaZ + 5 <= chunkSize; areaZ += 5)
            {
                int x = startX + areaX + Random.Range(1, 4);// X для дерва
                int z = startZ + areaZ + Random.Range(1, 4);// Z для дерева

                int groundY = blockHight[x, z];

                if (!isBlockDesert[x, z] && groundY >= waterLevelY)
                {
                    bool hasSpace = true;

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (blockHight[x + dx, z + dz] >= groundY + 5)
                            {
                                hasSpace = false;
                            }
    ;
                        }
                    }

                    if (hasSpace) SpawnNormalTree(x, groundY, z, parent);// Дерева спвница
                }
            }
        }
    }
}