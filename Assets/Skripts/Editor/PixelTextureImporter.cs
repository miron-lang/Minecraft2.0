using UnityEditor;
using UnityEngine;

public class PixelTextureImporter : AssetPostprocessor
{
    public override uint GetVersion() => 1;// Версия настроек текстур

    void OnPreprocessTexture()// Настройки текстуры при импорте
    {
        if (!assetPath.StartsWith("Assets/", System.StringComparison.Ordinal))
            return;

        var importer = (TextureImporter)assetImporter;
        importer.filterMode = FilterMode.Point;// Пиксели без размытия
    }
}
