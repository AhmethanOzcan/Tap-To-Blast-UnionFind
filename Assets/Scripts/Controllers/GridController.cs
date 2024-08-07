using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridController : MonoBehaviour
{
    SpriteRenderer _spriteRenderer;
    private int _width;
    private int _height;
    private float _tileSize;
    private float _borderSize = 0.3f;
    Vector2 _gridSize;
    [SerializeField] Transform[] _spawnPoints;

    private void Awake() {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }
    

    private void SetSizes()
    {
        this._width     = LevelManager.Instance.GetLevel()._columnCount;
        this._height    = LevelManager.Instance.GetLevel()._rowCount;
        this._tileSize  = 0.5f; 
    }

    private void SetGridSize()
    {
        _gridSize   = new Vector2(_borderSize + _tileSize * _width, _borderSize + _tileSize * _height);
        _spriteRenderer.size = _gridSize;
    }

    private void ReplaceSpawnPoints()
    {
        Vector3 _screenPosition = new Vector3(Screen.width / 2, Screen.height, Camera.main.nearClipPlane);
        Vector3 _worldPosition = Camera.main.ScreenToWorldPoint(_screenPosition);
        _worldPosition.x -= (this._width /2) * this._tileSize;
        if(this._width % 2 == 0) 
            _worldPosition.x += this._tileSize/2f;
        TileManager.Instance._activeSpawns.Clear();
        for(int i = 0; i < this._width; i++)
        {
            _spawnPoints[i].transform.position = _worldPosition;
            _worldPosition.x += this._tileSize;
            TileManager.Instance._activeSpawns.Add(_spawnPoints[i].transform);
        }
    }



    public void StartGridCreation()
    {
        SetSizes();
        SetGridSize();
        transform.localScale = Vector2.one;
        ReplaceSpawnPoints();
        TileManager.Instance.StartNewLevel(this.transform);
    }
}
