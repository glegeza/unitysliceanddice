using UnityEngine;
using UnityEditor;

public class SpritePreprocessor : AssetPostprocessor
{
    private string ProcessedTag = "processed";
    private string PathName = "Sprites";

	void OnPreprocessTexture()
    {
        if (assetPath.Contains(PathName))
        {
            var importer = assetImporter as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.mipmapEnabled = false;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Point;
            importer.userData = ProcessedTag;
        }
    }
}