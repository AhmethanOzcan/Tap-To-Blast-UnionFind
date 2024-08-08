using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class TileManager : Singleton<TileManager>
{
    [HideInInspector] public List<Transform> _activeSpawns = new List<Transform>();
    public Sprite[] _tileSprites;
    public GameObject _tilePrefab;
    public TileController[][] _tileControllers;
    public TileType[] _flattenedGrid;
    public Vector3[][] _gridPositions;
    private int _totalTiles;
    private UnionFind _unionFind;
    Level _level;

    protected override void Awake() {
        base.Awake();
    }

    public void StartNewLevel(Transform gridTransform)
    {
        this._level             = LevelManager.Instance.GetLevel();
        _totalTiles             = this._level._rowCount * this._level._columnCount;
        this._flattenedGrid     = new TileType[_totalTiles];
        this._unionFind         = new UnionFind(_totalTiles);
        FillGridTransforms(gridTransform);
        CreateTileMatrices();
        FillTiles();
        PerformUnionFind(0, 0, _level._columnCount);
    }

    

    private void CreateTileMatrices()
    {
        if(_tileControllers != null && _tileControllers.Length > 0)
            CleanMatrix<TileController>(_tileControllers);
        _tileControllers = new TileController[_level._columnCount][];
        for (int i = 0; i < _level._columnCount; i++)
        {
            _tileControllers[i] = new TileController[_level._rowCount];
        }
    }

    private void FillGridTransforms(Transform gridTransform)
    {
        if(_gridPositions != null && _gridPositions.Length > 0)
            CleanMatrix<Vector3>(_gridPositions);

        _gridPositions = new Vector3[_level._columnCount][];
        for (int i = 0; i < _level._columnCount; i++)
        {
            _gridPositions[i] = new Vector3[_level._rowCount];
        }


        float _tileSize = this._tilePrefab.GetComponent<SpriteRenderer>().bounds.size.x - .075f;
        float _yPoint   = gridTransform.position.y - _tileSize * _level._rowCount/2 + (_level._rowCount%2)*_tileSize/2 + 0.225f;
        
        for (int y = 0; y < _level._rowCount; y++)
        {
           for (int x = 0; x < _level._columnCount; x++)
           {
                float _xPoint           = _activeSpawns[x].position.x;
                _gridPositions[x][y]    = new Vector3(_xPoint, _yPoint);
           } 

           _yPoint += _tileSize;
        }
    }

    private void CleanMatrix<T>(T[][] matrix)
    {
        for (int i = 0; i < matrix.Length; i++)
        {
            for (int j = 0; j < matrix[i].Length; j++)
            {
                matrix[i][j] = default(T);
            }
            matrix[i] = default(T[]);
        }
    }
    
    private void FillTiles()
    {
        for(int y = 0; y < _level._rowCount; y++)
        {
            for(int x = 0; x < _level._columnCount; x++)
            {
                GenerateTile((TileType)_level._startingBoard[x][y], x, y);
            }
        }
    }

    private void GenerateTile(TileType type, int x, int y)
    {
        GameObject tile                        = Instantiate(_tilePrefab, _activeSpawns[x]);
        Tile tileInfo                          = new Tile(new Vector2Int(x, y), type);
        _tileControllers[x][y]                 = tile.GetComponent<TileController>();
        int flatIndex                          = to_index(x,y);
        _flattenedGrid[flatIndex]              = tileInfo._tileType;
        _tileControllers[x][y].Initialize(tileInfo);
        
    }


    void PerformUnionFind(int xStart, int yStart, int xEnd)
    {
        for (int y = yStart; y < _level._rowCount; y++)
        {
            for (int x = xStart; x < xEnd; x++)
            {
                int index = to_index(x, y);
                if(_flattenedGrid[index] == TileType.box)
                    continue;
                if (x > 0 && _flattenedGrid[index] == _flattenedGrid[to_index(x - 1, y)])
                {
                    _unionFind.Union(index, to_index(x - 1, y));
                }
                if (y > 0 && _flattenedGrid[index] == _flattenedGrid[to_index(x, y - 1)])
                {
                    _unionFind.Union(index, to_index(x, y - 1));
                }
            }
        }
    }

    private int to_index(int x, int y)
    {
        return y * _level._columnCount + x;
    }

    private Vector2Int from_index(int index)
    {
        int x = index%_level._columnCount;
        int y = (index-x)/_level._columnCount;
        return new Vector2Int(x, y);
    }
}

public class UnionFind
{
    private int[] parent;
    private int[] size;

    public UnionFind(int n)
    {
        parent = new int[n];
        size = new int[n];

        for (int i = 0; i < n; i++)
        {
            parent[i] = i;
            size[i] = 1;
        }
    }

    public int Find(int index)
    {
        if (parent[index] != index)
        {
            parent[index] = Find(parent[index]);
        }
        return parent[index];
    }

    public void Union(int index1, int index2)
    {
        int root1 = Find(index1);
        int root2 = Find(index2);

        if (root1 != root2)
        {
            if (size[root1] > size[root2])
            {
                parent[root2] = root1;
                size[root1] += size[root2];
            }
            else
            {
                parent[root1] = root2;
                size[root2] += size[root1];
            }
        }
    }

    public int GetSize(int index)
    {
        int root = Find(index);
        return size[root];
    }
}