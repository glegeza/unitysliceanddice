using UnityEngine;

[CreateAssetMenu(fileName = "draggabletile", menuName = "Draggable Tile")]
public class DraggableTile : BaseTile
{
    // Sprites for nine-piece box
    public Sprite TopLeftCorner;
    public Sprite BottomLeftCorner;
    public Sprite TopRightCorner;
    public Sprite BottomRightCorner;
    public Sprite LeftBorder;
    public Sprite RightBorder;
    public Sprite TopBorder;
    public Sprite BottomBorder;
    public Sprite BlockCenter;

    // Sprites for horizontal lines
    public Sprite HorizontalCenter;
    public Sprite HorizontalLeft;
    public Sprite HorizontalRight;

    // Sprite for vertical lines
    public Sprite VerticalCenter;
    public Sprite VerticalTop;
    public Sprite VerticalBottom;

    public Sprite Island;

    private Vector3Int _above = new Vector3(0, 1, 0);
    private Vector3Int _below = new Vector3(0, -1, 0);
    private Vector3Int _left = new Vector3(-1, 0, 0);
    private Vector3Int _right = new Vector3(1, 0, 0);

    public override Sprite GetPreview()
    {
        return Island;
    }

    public override void RefreshTile(Vector3Int location, ITileMap tileMap)
    {
        for (var xOffset = -1; xOffset <= 1; xOffset++)
        {
            for (var yOffset = -1; yOffset <= 1; yOffset++)
            {
                var tileToRefresh = new Vector3Int(location.x + xOffset, location.y + yOffset, location.z);
                if (HasDraggableTile(tileMap, tileToRefresh))
                {
                    tileMap.RefreshTile(tileToRefresh);
                }
            }
        }
    }

    public override bool GetTileData(Vector3Int location, ITileMap tileMap, ref TileData tileData)
    {
        Sprite sprite = Island;

        var above = HasDraggableTile(tileMap, location + _above);
        var below = HasDraggableTile(tileMap, location + _below);
        var left = HasDraggableTile(tileMap, location + _left);
        var right = HasDraggableTile(tileMap, location + _right);
        
        if (left && right && above && below)
        {
            sprite = BlockCenter;
        }
        else if (left && right && above)
        {
            sprite = BottomBorder;
        }
        else if (left && right && below)
        {
            sprite = TopBorder;
        }
        else if (left && above && below)
        {
            sprite = RightBorder;
        }
        else if (left && right)
        {
            sprite = HorizontalCenter;
        }
        else if (left && above)
        {
            sprite = BottomRightCorner;
        }
        else if (left && below)
        {
            sprite = TopRightCorner;
        }
        else if (right && below && above)
        {
            sprite = LeftBorder;
        }
        else if (right && below)
        {
            sprite = TopLeftCorner;
        }
        else if (right & above)
        {
            sprite = BottomBorder;
        }
        else if (below && above)
        {
            sprite = VerticalCenter;
        }
        else if (right)
        {
            sprite = HorizontalLeft;
        }
        else if (left)
        {
            sprite = HorizontalRight;
        }
        else if (below)
        {
            sprite = VerticalTop;
        }
        else if (above)
        {
            sprite = VerticalBottom;
        }

        tileData.sprite = sprite;
        tileData.color = Color.white;

        return true;
    }

    private bool HasDraggableTile(ITileMap map, Vector3Int location)
    {
        return map.GetTile(location) == this;
    }
}