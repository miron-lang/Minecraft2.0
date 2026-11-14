using UnityEditor;
using UnityEngine;

public class PixelTextureImporter : AssetPostprocessor
{
    public override uint GetVersion() => 1;// Unity переимпортирует текстуры при смене версии правила

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/", System.StringComparison.Ordinal))
            return;

        var importer = (TextureImporter)assetImporter;
        importer.filterMode = FilterMode.Point;// Всегда оставляем чёткие пиксели
    }
}
