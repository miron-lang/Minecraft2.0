using Unity.VisualScripting;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    [Header("Desert")]
    public GameObject sand;
    public GameObject sandStone;

    [Header("Plains")]
    public GameObject grassBlock;
    public GameObject dirt;

    [Header("Evry")]
    public GameObject stone;
    public GameObject bedrock;

    [Header("World size")]
    public int xSize = 258;// Рамземер мира по x
    public int zSize = 258;// Рамземер мира по z

    private void Awake()
    {
        transform.position = new Vector3(xSize/2, 0, zSize/2);
    }

    void Start()
    {
        GenerateWorld();
    }

    public void GenerateWorld()// Вуенцыя создания мира
    {
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

                float desertBlend = Mathf.InverseLerp(0.5f, 0.65f, biomeNoise);

                int hight = Mathf.RoundToInt(Mathf.Lerp(plainsHeight, desetHeight, desertBlend));// Прасчитывоние высоты блоков

                bool isDesert = biomeNoise > 0.5f;// Проверка биома

                GameObject surface = isDesert ? sand : grassBlock;// Настройки зависимые от биома
                GameObject underGround = isDesert ? sandStone : dirt;// Настройки зависимые от биома

                SpawnBloks(surface, x, hight, z);
                SpawnBloks(underGround, x, hight - 1, z);
                SpawnBloks(underGround, x, hight - 2, z);
                SpawnBloks(stone, x, hight - 3, z);
                SpawnBloks(bedrock, x, hight - 4, z);
            }
        }
    }

    void SpawnBloks(GameObject block, int x, int y, int z)// Ставить блок(как /setBlock в маине)
    {
        Vector3 position = new Vector3(x, y, z);

        Instantiate(block, position, Quaternion.identity, transform);
    }
}