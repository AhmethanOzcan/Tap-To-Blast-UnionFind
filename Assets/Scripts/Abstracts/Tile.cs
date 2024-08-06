using UnityEngine;

public class Tile
{
    public Vector2Int _coordinates;
    public TileType _tileType;

    public Tile(Vector2Int coordinates, TileType tileType)
    {
        this._coordinates   = coordinates;
        this._tileType      = tileType;
    }
}