using Unity.VisualScripting;
using UnityEngine;

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
    private bool treeSpawned;
    private int[,] blockHight;
    private bool[,] isBlockDesert;

    [Header("World size")]
    public int xSize;// Рамземер мира по x
    public int zSize;// Рамземер мира по z

    private void Awake()
    {
        transform.position = new Vector3(xSize/2, 0, zSize/2);
    }

    void Start()
    {
        GenerateWorld();
        GenerateTrees();
    }

    public void GenerateWorld()
    {
        blockHight = new int[xSize, zSize];
        isBlockDesert = new bool[xSize, zSize];

        for (int x = 0; x < xSize; x++)// Генераыя по x
        {
            for (int z = 0; z < zSize; z++)// На каждый x по zSize блоков по оси z
            {
                float noise = Mathf.PerlinNoise(x * 0.12f, z * 0.12f);// Шум для высоты
                float biomeNoise = Mathf.PerlinNoise(x * 0.015f + 10f, z * 0.015f + 20f);// Шум для биомов

                float plainsNoice = Mathf.PerlinNoise(x * 0.05f, z * 0.05f);// Шум для высоту полей
                float duneNoice = Mathf.PerlinNoise(x * 0.035f + 30f, z * 0.09f + 40f);// Щум для высоты пустыни

                float plainsHeight = plainsNoice * 3f;// Просчитывоние высоты в полянах
                float desetHeight = duneNoice * 7f;// Просчитывоние высоты в пустыни

                float desertBlend = Mathf.InverseLerp(0.5f, 0.65f, biomeNoise);// Высота в биоме пустыни

                int hight = Mathf.RoundToInt(Mathf.Lerp(plainsHeight, desetHeight, desertBlend));// Прасчитывоние высоты блоков

                bool isDesert = biomeNoise > 0.5f;// Проверка биома

                GameObject surface = isDesert ? sand : grassBlock;// Настройки зависимые от биома
                GameObject underGround = isDesert ? sandStone : dirt;// Настройки зависимые от биома

                blockHight[x, z] = hight;
                isBlockDesert[x, z] = isDesert;

                SpawnBlok(surface, x, hight, z);// Начяло ставление блоков
                SpawnBlok(underGround, x, hight - 1, z);
                SpawnBlok(underGround, x, hight - 2, z);
                SpawnBlok(stone, x, hight - 3, z);
                SpawnBlok(stone, x, hight - 4, z);
                SpawnBlok(Random.Range(0, 4) == 3 ? andesite : stone, x, hight - 5, z);
                SpawnBlok(Random.Range(0, 3) == 2 ? tuff : deepslate, x, hight - 6, z);
                SpawnBlok(deepslate, x, hight - 7, z);
                SpawnBlok(deepslate, x, hight - 8, z);
                SpawnBlok(deepslate, x, hight - 9, z);
                SpawnBlok(bedrock, x, hight - 10, z);// Канец ставление блоков
            }
        }    
    }

    void SpawnBlok(GameObject block, int x, int y, int z)// Ставить блок(как /setBlock в маине)
    {
        Vector3 position = new Vector3(x, y, z);

        Instantiate(block, position, Quaternion.identity, transform);// Ставица блок
    }

    void SpawnNormalTree(int x, int groundY, int z)
    {
        for (int y = 1; y <= 3; y++)
        {
            SpawnBlok(oakLog, x, groundY + y, z);// Ставица брёвна
        }

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                SpawnBlok(oakLeave, x + dx, groundY + 4, z + dz);// Ставица листва
            }
        }

        SpawnBlok(oakLeave, x, groundY + 5, z);// Листва сверху
    }
    void GenerateTrees()
    {
        for (int x = 3; x < xSize / 6; x += 6)
        {
            for (int z = 3; z < zSize / 6; z += 6)
            {
                int currentX = Mathf.RoundToInt(Random.Range((x * 6) - 6/2, (x * 6) + 6/2));// X для дерва
                int currentZ = Mathf.RoundToInt(Random.Range((z * 6) - 6/2, (z * 6) + 6/2));// Z для дерева

                if (!isBlockDesert[currentX, currentZ]) SpawnNormalTree(currentX, blockHight[currentX, currentZ], currentZ);// Дерева спвница если не пустыня
            }
        }
    }
}