using UnityEditor;
using UnityEngine;

public class AlienKoSpritesheetPostprocessor : AssetPostprocessor
{
    const string SpritesheetAssetPath = "Assets/Graphics/Alien_Ko_Spritesheet.png";
    const int FrameSize = 256;
    const int FrameCount = 61;
    const int Cols = 8;
    const int Rows = 8;

    void OnPreprocessTexture()
    {
        if (assetPath != SpritesheetAssetPath) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 2048;

        var sprites = new SpriteMetaData[FrameCount];
        int textureHeight = Rows * FrameSize;

        for (int i = 0; i < FrameCount; i++)
        {
            int col = i % Cols;
            int row = i / Cols;
            // Unity rects are Y-up from bottom-left; PNG rows are top-down
            float x = col * FrameSize;
            float y = textureHeight - (row + 1) * FrameSize;

            sprites[i] = new SpriteMetaData
            {
                name = $"Alien_Ko_{i:D2}",
                rect = new Rect(x, y, FrameSize, FrameSize),
                pivot = new Vector2(0.5f, 0.5f),
                alignment = (int)SpriteAlignment.Center
            };
        }

        importer.spritesheet = sprites;
    }
}
