using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

public struct Size
{
    public int Width;
    public int Height;

    public Size(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public override bool Equals(object obj)
    {
        return obj is Size && Equals((Size)obj);
    }

    public override int GetHashCode()
    {
        return Width.GetHashCode() + Height.GetHashCode();
    }

    public bool Equals(Size size)
    {
        return Width == size.Width && Height == size.Height;
    }

    public static bool operator ==(Size x, Size y)
    {
        return x.Width == y.Width && x.Height == y.Height;
    }

    public static bool operator !=(Size x, Size y)
    {
        return x.Width != y.Width || x.Height != y.Height;
    }

    public static explicit operator Size(Vector2Int vec2)
    {
        return new Size(vec2.x, vec2.y);
    }
}

public struct TiledSheetData
{
    private Size _spriteSize;
    private Vector2Int _spritePadding;
    private Vector2Int _startOffset;
    private Vector2Int _spriteAreaMargin;
    private Size _fixedGridSize;
    private bool _useFixedGrid;

    public Size SpriteSize
    {
        get { return _spriteSize; }
        set
        {
            if (value != _spriteSize)
            {
                RequiresUpdate = true;
            }
            _spriteSize = value;
        }
    }
    public Vector2Int SpritePadding
    {
        get { return _spritePadding; }
        set
        {
            if (value != _spritePadding)
            {
                RequiresUpdate = true;
            }
            _spritePadding = value;
        }
    }
    public Vector2Int StartOffset
    {
        get { return _startOffset; }
        set
        {
            if (value != _startOffset)
            {
                RequiresUpdate = true;
            }
            _startOffset = value;
        }
    }
    public Vector2Int SpriteAreaMargin
    {
        get { return _spriteAreaMargin; }
        set
        {
            if (value != _spriteAreaMargin)
            {
                RequiresUpdate = true;
            }
            _spriteAreaMargin = value;
        }
    }
    public Size FixedGridSize
    {
        get { return _fixedGridSize; }
        set
        {
            if (value != _fixedGridSize)
            {
                RequiresUpdate = true;
            }
            _fixedGridSize = value;
        }
    }
    public bool UseFixedGrid
    {
        get { return _useFixedGrid; }
        set
        {
            if (value != _useFixedGrid)
            {
                RequiresUpdate = true;
            }
            _useFixedGrid = value;
        }
    }
    public Color32[] ColorData { get; private set; }
    public Texture2D Texture { get; private set; }
    public Size GridSize { get; private set; }
    public Vector2Int Advance { get; private set; }
    public bool RequiresUpdate { get; private set; }

    public int TextureWidth
    {
        get
        {
            return Texture != null ? Texture.width : 0;
        }
    }

    public int TextureHeight
    {
        get
        {
            return Texture != null ? Texture.height : 0;
        }
    }

    public int SpriteAreaWidth
    {
        get
        {
            return TextureWidth - SpriteAreaMargin.x * 2 - StartOffset.x;
        }
    }

    public int SpriteAreaHeight
    {
        get
        {
            return TextureHeight - SpriteAreaMargin.y * 2 - StartOffset.y;
        }
    }

    public Rect SpriteAreaRect
    {
        get
        {
            return new Rect(
                StartOffset.x + SpriteAreaMargin.x, 
                TextureHeight - StartOffset.y + SpriteAreaMargin.y, 
                SpriteAreaWidth, SpriteAreaHeight);
        }
    }

    public bool Ready
    {
        get
        {
            return ColorData != null && SpriteSize.Width > 0 && SpriteSize.Height > 0;
        }
    }

    public void SetSpriteSheet(Texture2D texture)
    {
        if (texture == Texture)
        {
            return;
        }
        ColorData = GetColorData(texture);
        Texture = texture;
    }

    public void UpdateData()
    {
        if (SpriteSize.Width < 1 || SpriteSize.Height < 1)
        {
            return;
        }
        if (UseFixedGrid)
        {
            UpdateFixedGridData();
        }
        else
        {
            UpdateDynamicGridData();
        }
        RequiresUpdate = false;
    }

    private Color32[] GetColorData(Texture2D texture)
    {
        if (texture == null)
        {
            return null;
        }

        string path = AssetDatabase.GetAssetPath(texture);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        var oldReadableState = importer.isReadable;
        var oldType = importer.textureType;
        importer.textureType = TextureImporterType.Advanced;
        importer.isReadable = true;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        Color32[] colorData = null;

        try
        {
            colorData = texture.GetPixels32();
        }
        catch (UnityException)
        {
            Debug.LogError("Failed to set texture to a readable state.");
            colorData = null;
        }
        finally
        {
            importer.textureType = oldType;
            importer.isReadable = oldReadableState;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        return colorData;
    }

    private void UpdateFixedGridData()
    {
        GridSize = new Size(FixedGridSize.Width, FixedGridSize.Height);
        Advance = new Vector2Int(SpriteAreaWidth / GridSize.Width, SpriteAreaHeight / GridSize.Height);
    }

    private void UpdateDynamicGridData()
    {
        Advance = new Vector2Int(SpriteSize.Width + SpritePadding.x, SpriteSize.Height + SpritePadding.y);
        GridSize = new Size(SpriteAreaWidth / Advance.x + 1, SpriteAreaHeight / Advance.y + 1);
    }
}

public class TileWindow : EditorWindow
{
    private static int WINDOW_WIDTH = 650;
    private static int WINDOW_HEIGHT = 500;
    private static int CONTROL_MAX_WIDTH = 350;
    private static Vector2Int MAX_VECTOR2 = new Vector2Int(Int32.MaxValue, Int32.MaxValue);
    private static string DEFAULT_SPRITE_NAME = "Sprite";

    private Texture2D _slicePreview;
    private TiledSheetData _sheetData = new TiledSheetData();
    private Vector2 _spritePivot = new Vector2(0.5f, 0.5f);
    private string _spriteName = DEFAULT_SPRITE_NAME;

    [MenuItem("Sprites/Slicer")]
	public static void ShowWindow()
    {
        var window = GetWindow(typeof(TileWindow));
        window.titleContent = new GUIContent("Tile Slicer");
        window.minSize = new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);
    }

    private void OnGUI()
    { 
        DrawTextureSection();
        
        EditorGUILayout.Space(); EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grid Layout", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        DrawFixedGridSection();
        DrawSpriteAreaSection();        

        // Sprite Details
        EditorGUILayout.Space(); EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sprite Information", EditorStyles.boldLabel);
        _spriteName = EditorGUILayout.TextField("Base Name", _spriteName, GUILayout.Width(CONTROL_MAX_WIDTH));

        DrawSpriteSettingsSection();

        // Make it go button
        EditorGUILayout.Space(); EditorGUILayout.Space();
        var oldGUIState = GUI.enabled;
        if (!_sheetData.Ready)
        {
            GUI.enabled = false;
        }
        if (GUILayout.Button("Slice", GUILayout.Width(CONTROL_MAX_WIDTH)))
        {
            SliceSprites();
        }
        GUI.enabled = oldGUIState;

        if (_sheetData.RequiresUpdate)
        {
            _sheetData.UpdateData();
            if (_sheetData.Ready)
            {
                BuildPreviewTexture();
            }
            else
            {
                _slicePreview = null;
            }
        }

        if (_sheetData.Texture)
        {
            var aspect = 1.0f / ((float)_sheetData.TextureWidth / _sheetData.TextureHeight);
            var border = 20;
            var window = GetWindow(typeof(TileWindow));
            var width = window.position.width - (CONTROL_MAX_WIDTH + border * 2);
            var height = width * aspect;
            var max_height = window.position.height - border * 2;
            if (height > max_height)
            {
                height = max_height;
                width = max_height / aspect;
            }
            if (_slicePreview)
            {
                GUI.depth = 0;
                EditorGUI.DrawTextureTransparent(new Rect(CONTROL_MAX_WIDTH + border, border, width, height), _slicePreview);
            }
            GUI.depth = 1;
            //EditorGUI.DrawTextureTransparent(new Rect(CONTROL_MAX_WIDTH + border, border, width, height), _sheetData.Texture);
        }
    }

    private void DrawTextureSection()
    {
        EditorGUILayout.LabelField("Texture Information", EditorStyles.boldLabel);
        var newSpriteFile = EditorGUILayout.ObjectField("Texture", _sheetData.Texture, typeof(Texture2D), false, GUILayout.Width(CONTROL_MAX_WIDTH)) as Texture2D;
        _sheetData.SetSpriteSheet(newSpriteFile);
    }

    private void DrawFixedGridSection()
    {
        _sheetData.UseFixedGrid = EditorGUILayout.BeginToggleGroup("Use fixed grid?", _sheetData.UseFixedGrid);
        var width = EditorGUILayout.IntField("Grid Width", _sheetData.FixedGridSize.Width, GUILayout.Width(CONTROL_MAX_WIDTH));
        var height = EditorGUILayout.IntField("Grid Height", _sheetData.FixedGridSize.Height, GUILayout.Width(CONTROL_MAX_WIDTH));
        _sheetData.FixedGridSize = new Size(width, height);
        EditorGUILayout.EndToggleGroup();
    }

    private void DrawSpriteAreaSection()
    {
        _sheetData.StartOffset = EditorGUILayout.Vector2Field("Grid Offset", _sheetData.StartOffset, GUILayout.Width(CONTROL_MAX_WIDTH));
        EditorGUILayout.Space();
        _sheetData.SpriteAreaMargin = EditorGUILayout.Vector2Field("Grid Area Margin", _sheetData.SpriteAreaMargin, GUILayout.Width(CONTROL_MAX_WIDTH));
    }

    private void DrawSpriteSettingsSection()
    {
        var width = EditorGUILayout.IntField("Slice Width", _sheetData.SpriteSize.Width, GUILayout.Width(CONTROL_MAX_WIDTH));
        var height = EditorGUILayout.IntField("Slice Height", _sheetData.SpriteSize.Height, GUILayout.Width(CONTROL_MAX_WIDTH));
        _sheetData.SpriteSize = new Size(width, height);
        EditorGUILayout.Space();

        _sheetData.SpritePadding = EditorGUILayout.Vector2Field("Sprite Borders", _sheetData.SpritePadding, GUILayout.Width(CONTROL_MAX_WIDTH));
        EditorGUILayout.Space();

        _spritePivot = EditorGUILayout.Vector2Field("Sprite Pivot", _spritePivot, GUILayout.Width(CONTROL_MAX_WIDTH));
        _spritePivot.x = Mathf.Clamp(_spritePivot.x, 0.0f, 1.0f);
        _spritePivot.y = Mathf.Clamp(_spritePivot.y, 0.0f, 1.0f);
    }

    private bool SpriteIsEmpty(Rect spriteRect)
    {
        for (int px = (int)spriteRect.xMin; px < spriteRect.xMax; px++)
        {
            for (int py = (int)spriteRect.yMin; py < spriteRect.yMax; py++)
            {
                var color = _sheetData.ColorData[py * _sheetData.TextureWidth + px];
                if (color.a > 0)
                {
                    return false;
                }
            }
        }
        return true;
    }

    private void SliceSprites()
    {
        var metaDataList = new List<SpriteMetaData>();
        var idx = 0;
        for (var y = _sheetData.GridSize.Height - 1; y >= 0; y--)
        {
            for (var x = 0; x < _sheetData.GridSize.Width; x++)
            {
                var spriteRect = new Rect(x * _sheetData.Advance.x + _sheetData.StartOffset.x, 
                    y * _sheetData.Advance.y + _sheetData.StartOffset.y, 
                    _sheetData.SpriteSize.Width, _sheetData.SpriteSize.Height);
                if (SpriteIsEmpty(spriteRect))
                {
                    continue;
                }

                var metaData = new SpriteMetaData();
                metaData.pivot = _spritePivot;
                metaData.alignment = 9;
                metaData.name = String.Format("{0}_{1}", _spriteName, idx++);
                metaData.rect = spriteRect;
                metaDataList.Add(metaData);
            }
        }

        // Make sure the texture import settings are correct for a sprite sheet with multiple sprites
        string path = AssetDatabase.GetAssetPath(_sheetData.Texture);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.textureType = TextureImporterType.Sprite;
        importer.spritesheet = metaDataList.ToArray();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.Default);
    }

    private void BuildPreviewTexture()
    {
        if (_slicePreview != null)
        {
            DestroyImmediate(_slicePreview);
        }
        var fill = new Color32[_sheetData.TextureWidth * _sheetData.TextureHeight];
        for (int px = 0; px < _sheetData.TextureWidth; px++)
        {
            for (int py = 0; py < _sheetData.TextureHeight; py++)
            {
                fill[py * _sheetData.TextureWidth + px] = new Color32(255, 0, 0, 255);
            }
        }

        // Create color blocks
        var blockA = new Color32[_sheetData.SpriteSize.Width * _sheetData.SpriteSize.Height];
        var blockB = new Color32[_sheetData.SpriteSize.Width * _sheetData.SpriteSize.Height];
        for (int p = 0; p < _sheetData.SpriteSize.Width * _sheetData.SpriteSize.Height; p++)
        {
            blockA[p] = new Color32(0, 255, 0, 255);
            blockB[p] = new Color32(0, 0, 255, 255);
        }

        _slicePreview = new Texture2D(_sheetData.TextureWidth, _sheetData.TextureHeight);
        _slicePreview.filterMode = FilterMode.Point;
        _slicePreview.SetPixels32(fill);
        var startY = _sheetData.SpriteAreaMargin.y;
        var startX = _sheetData.SpriteAreaMargin.x;
        var color = Color.black;
        var x = startX;
        var y = startY;
        var useBlockA = true;
        var prevStart = true;
        for (var gridX = 0; gridX < _sheetData.GridSize.Width - 1; gridX++)
        {
            for (var gridY = 0; gridY < _sheetData.GridSize.Height - 1; gridY++)
            {
                _slicePreview.SetPixels32(x, y, _sheetData.SpriteSize.Width, _sheetData.SpriteSize.Height, useBlockA ? blockA : blockB);
                useBlockA = !useBlockA;
                y += _sheetData.Advance.y;
            }
            y = startY;
            x += _sheetData.Advance.x;
            useBlockA = !prevStart;
            prevStart = useBlockA;
        }
        _slicePreview.Apply();
    }
}