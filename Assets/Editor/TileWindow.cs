using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

public class TileWindow : EditorWindow
{
    static int WINDOW_WIDTH = 350;
    static int WINDOW_HEIGHT = 500;
    static Vector2Int MAX_VECTOR2 = new Vector2Int(Int32.MaxValue, Int32.MaxValue);

    Texture2D _spriteFile;
    int _spriteWidth;
    int _spriteHeight;
    int _gridWidth;
    int _gridHeight;
    Vector2 _spritePivot = new Vector2(0.5f, 0.5f);
    Vector2 _startOffset = new Vector2(0.0f, 0.0f);
    Vector2 _spriteAreaMargin = new Vector2(0.0f, 0.0f);
    Vector2 _spriteBorders = new Vector2(0.0f, 0.0f);
    bool _useFixedGrid;
    bool _forceReadable = true;
    string _defaultSpriteName = "Sprite";

    [MenuItem("Sprites/Slicer")]
	public static void ShowWindow()
    {
        var window = GetWindow(typeof(TileWindow));
        window.titleContent = new GUIContent("Tile Slicer");
        window.maxSize = new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);
        window.minSize = new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);
    }

    private void OnGUI()
    {
        // Texture Settings
        EditorGUILayout.LabelField("Texture Information", EditorStyles.boldLabel);
        var newSpriteFile = EditorGUILayout.ObjectField("Texture", _spriteFile, typeof(Texture2D), false) as Texture2D;
        if (_spriteFile != newSpriteFile && newSpriteFile != null)
        {
            _defaultSpriteName = newSpriteFile.name;
        }
        _spriteFile = newSpriteFile;

        // Grid layout
        EditorGUILayout.Space(); EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grid Layout", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        _useFixedGrid = EditorGUILayout.BeginToggleGroup("Use fixed grid?", _useFixedGrid);
        _gridWidth = EditorGUILayout.IntField("Grid Width", _gridWidth);
        _gridWidth = _gridWidth < 1 ? 1 : _gridWidth;
        _gridHeight = EditorGUILayout.IntField("Grid Height", _gridHeight);
        _gridHeight = _gridHeight < 1 ? 1 : _gridHeight;
        EditorGUILayout.EndToggleGroup();
        
        _startOffset = EditorGUILayout.Vector2Field("Grid Offset", _startOffset);
        EditorGUILayout.Space();

        _spriteAreaMargin = EditorGUILayout.Vector2IntField("Grid Area Margin", _spriteAreaMargin);

        // Sprite Details
        EditorGUILayout.Space(); EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sprite Information", EditorStyles.boldLabel);
        _defaultSpriteName = EditorGUILayout.TextField("Base Name", _defaultSpriteName);

        // Sprite size settings
        _spriteWidth = EditorGUILayout.IntField("Slice Width", _spriteWidth);
        _spriteWidth = _spriteWidth < 1 ? 1 : _spriteWidth;
        _spriteHeight = EditorGUILayout.IntField("Slice Height", _spriteHeight);
        _spriteHeight = _spriteHeight < 1 ? 1 : _spriteHeight;
        EditorGUILayout.Space();

        _spriteBorders = EditorGUILayout.Vector2IntField("Sprite Borders", _spriteBorders);
        EditorGUILayout.Space();

        // Sprite pivot settings
        _spritePivot = EditorGUILayout.Vector2Field("Sprite Pivot", _spritePivot);
        _spritePivot.x = Mathf.Clamp(_spritePivot.x, 0.0f, 1.0f);
        _spritePivot.y = Mathf.Clamp(_spritePivot.y, 0.0f, 1.0f);

        // Make it go button
        EditorGUILayout.Space(); EditorGUILayout.Space();
        var oldGUIState = GUI.enabled;
        if (!CheckTexture())
        {
            GUI.enabled = false;
        }
        if (GUILayout.Button("Slice"))
        {
            SliceSprites();
        }
        GUI.enabled = oldGUIState;
    }

    private bool CheckTexture()
    {
        return _spriteFile != null;
    }

    private void ForceReadable()
    {
        string path = AssetDatabase.GetAssetPath(_spriteFile);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        importer.isReadable = true;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var ready = false;
        Debug.Log("Attempting to force texture to be readable.");
        while (!ready)
        {
            try
            {
                _spriteFile.GetPixel(0, 0);
                ready = true;
            }
            catch (UnityException)
            {
                ready = false;
            }
        }
        Debug.Log("Texture now readable.");
    }

    private bool SpriteIsEmpty(Rect spriteRect)
    {
        for (int px = (int)spriteRect.xMin; px < spriteRect.xMax; px++)
        {
            for (int py = (int)spriteRect.yMin; py < spriteRect.yMax; py++)
            {
                var color = _spriteFile.GetPixel(px, py);
                if (color.a > 0.001f)
                {
                    return false;
                }
            }
        }
        return true;
    }

    private void SliceSprites()
    {
        var offsetX = (int)_startOffset.x;
        var offsetY = (int)_startOffset.y;
        var spriteBorderX = (int)_spriteBorders.x;
        var spriteBorderY = (int)_spriteBorders.y;
        var gridWidth = !_useFixedGrid ? (_spriteFile.width - offsetX) / (_spriteWidth + spriteBorderX) : _gridWidth;
        var gridHeight = !_useFixedGrid ? (_spriteFile.height - offsetY) / (_spriteHeight + spriteBorderY) : _gridHeight;
        var metaDataList = new List<SpriteMetaData>();
        var idx = 0;
        var checkForEmpty = true;
        try
        {
            _spriteFile.GetPixel(0,0);
        }
        catch (UnityException)
        {
            if (_forceReadable)
            {
                ForceReadable();
                checkForEmpty = true;
            }
            else
            {
                Debug.LogError("Texture is not readable. Cannot check for empty sprites.");
                checkForEmpty = false;
            }
        }

        var xAdvance = _spriteWidth + spriteBorderX;
        var yAdvance = _spriteHeight + spriteBorderY;
        for (var y = gridHeight - 1; y >= 0; y--)
        {
            for (var x = 0; x < gridWidth; x++)
            {
                var metaData = new SpriteMetaData();
                metaData.pivot = _spritePivot;
                metaData.alignment = 9;
                metaData.name = String.Format("{0}_{1}", _defaultSpriteName, idx++);
                metaData.rect = new Rect(x * xAdvance + offsetX, y * yAdvance + offsetY, _spriteWidth, _spriteHeight);

                if (!checkForEmpty || !SpriteIsEmpty(metaData.rect))
                {
                    metaDataList.Add(metaData);
                }
            }
        }
        string path = AssetDatabase.GetAssetPath(_spriteFile);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.textureType = TextureImporterType.Sprite;
        importer.spritesheet = metaDataList.ToArray();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.Default);
    }
}
