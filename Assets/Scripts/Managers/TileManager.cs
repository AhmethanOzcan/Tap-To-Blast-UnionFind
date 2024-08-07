using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class TileManager : Singleton<TileManager>
{
    [HideInInspector] public List<Transform> _activeSpawns = new List<Transform>();
    public GameObject _tilePrefab;
    public TileController[][] _tileControllers;
    public Vector3[][] _gridPositions;
    Level _level;

    protected override void Awake() {
        base.Awake();
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartNewLevel(Transform gridTransform)
    {
        this._level     = LevelManager.Instance.GetLevel();
        FillGridTransforms(gridTransform);
        CreateTileMatrices();
    }

    private void CreateTileMatrices()
    {
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
        float _tileSize = this._tilePrefab.GetComponent<BoxCollider2D>().bounds.size.x;
        float _yPoint   =  gridTransform.position.y - _activeSpawns[0].position.y - _tileSize * _level._rowCount/2 + (_level._rowCount%2)*_tileSize/2 + Math.Abs((_level._rowCount%2)-1)*0.2f;
        
        _gridPositions = new Vector3[_level._columnCount][];
        for (int i = 0; i < _level._columnCount; i++)
        {
            _gridPositions[i] = new Vector3[_level._rowCount];
        }
        
        for (int i = 0; i < _level._rowCount; i++)
        {
           for (int j = 0; j < _level._columnCount; j++)
           {
                float _xPoint           = _activeSpawns[j].position.x;
                _gridPositions[j][i]    = new Vector3(_xPoint, _yPoint);
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
}
