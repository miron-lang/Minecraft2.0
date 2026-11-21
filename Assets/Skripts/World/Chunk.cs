using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public enum BlockType
{
    Air, Grass, Dirt, Stone
}


public class Chunk : MonoBehaviour
{
    public const int size = 16;

    public const int minY = -10;
    public const int maxY = 31;

    public const int hight = (maxY - minY) + 1;

    private BlockType[,,] blocks = new BlockType[size, hight, size];

    public void SetBlock(int x, int y, int z, BlockType type)
    {
        if (x < 0 || x >= size || y < minY || y > maxY || z < 0 || z >= size)
        {
            Debug.LogError($"Ѕлок за гроницами ч€нка: {x}, {y}, {z}", this);
            return;
        }

        int arrayY = y - minY;

        blocks[x, arrayY, z] = type;
    }

    public BlockType GetBlock(int x, int y, int z)
    {
        if (x < 0 || x >= size || y < minY || y > maxY || z < 0 || z >= size) return BlockType.Air;

        int arrayY = y - minY;

        return blocks[x, arrayY, z];
    }

}