using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
public class TileManager : Singleton<TileManager>
{
    [HideInInspector] public List<Transform> _activeSpawns = new List<Transform>();
    public Sprite[] _tileSprites;
    public TileController[][] _tileControllers;
    public TileType?[] _flattenedGrid;
    public Vector3[][] _gridPositions;
    private int _totalTiles;
    private UnionFind _unionFind;
    private bool _clicked;
    private float _tileSize;
    public Level _level;
    private object _unionInProgress;
    private object _fallLock;
    private object _creationLock;
    private Queue<int[]> _creationQueue;

    protected override void Awake() {
        base.Awake();
    }

    private void Update()
    {
        ClickDetection();
    }

    private void ClickDetection()
    {
        if (!_clicked && Input.GetMouseButton(0))
        {
            if(!Monitor.TryEnter(_unionInProgress))
                return;
            try
            {
                this._clicked = true;
                Vector2 tapPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                int x = Mathf.FloorToInt((tapPosition.x - _gridPositions[0][0].x + _tileSize / 2) / _tileSize);
                int y = Mathf.FloorToInt((tapPosition.y - _gridPositions[0][0].y + _tileSize / 2) / _tileSize);

                if (x < 0 || y < 0 || x >= _level._columnCount || y >= _level._rowCount)
                    return;
                else
                    TileBurst(x, y);
            }
            finally
            {
                Monitor.Exit(_unionInProgress);
            }
        }
        else if (_clicked && Input.GetMouseButtonUp(0))
        {
            this._clicked = false;
        }
    }

    private void TileBurst(int x, int y)
    {
        TileController clickedTile = _tileControllers[x][y];
        int flatIndex = to_index(x, y);
        if (clickedTile == null || clickedTile.IsFalling() || _unionFind.GetSize(flatIndex) == 1 || _flattenedGrid[flatIndex] == TileType.box)
            return;

        PopRoutine(x, y);
    }

    private void PopRoutine(int corX, int corY)
    {
        lock(_fallLock)
        {

            // Pop the tiles
            int flatIndex = to_index(corX, corY);
            int leaderIndex = _unionFind.Find(flatIndex);
            for (int i = 0; i < _totalTiles; i++)
                _unionFind.Find(i);
            for (int i = 0; i < _totalTiles; i++)
            {
                if (_unionFind.Find(i) == leaderIndex)
                {
                    Vector2Int coordinates = from_index(i);
                    GameObject gameObject = _tileControllers[coordinates.x][coordinates.y].gameObject;
                    PoolingManager.Instance.ReturnPooledObject(gameObject);
                    _tileControllers[coordinates.x][coordinates.y] = null;
                    _flattenedGrid[flatIndex] = null;
                }
            }


            // Make Tiles Above Fall
            int currentFall;
            int[] newTileAmounts = new int[_level._columnCount];
            for(int x = 0; x < _level._columnCount; x++)
            {
                currentFall = 0;
                for(int y = 0; y < _level._rowCount; y++)
                {
                    if(_tileControllers[x][y] == null)
                    {
                        currentFall += 1;
                        newTileAmounts[x]++;
                    }else if(_tileControllers[x][y]._tile._tileType == TileType.box)
                    {
                        currentFall = 0;
                        newTileAmounts[x] = 0;
                    }
                    else if(currentFall != 0)
                    {
                        int oldFlatIndex = to_index(x,y);
                        int newFlatIndex = to_index(x,y-currentFall);
                        _tileControllers[x][y-currentFall] = _tileControllers[x][y];
                        _flattenedGrid[newFlatIndex] = _flattenedGrid[oldFlatIndex];
                        _tileControllers[x][y] = null;
                        _flattenedGrid[oldFlatIndex] = null;
                        _tileControllers[x][y-currentFall].StartFalling(y-currentFall);
                    }
                }
            }
            PerformUnionFind();

            // Let New Tiles Come
            for (int amount = _level._rowCount; amount > 0; amount--)
            {
                for (int x = 0; x < _level._columnCount; x++)
                {
                    if (newTileAmounts[x] == amount)
                    {
                        int y = _level._rowCount - amount;
                        GameObject tile = PoolingManager.Instance.GetPooledObject(_activeSpawns[x].position);
                        Tile tileInfo = new Tile(new Vector2Int(x, y), (TileType)Random.Range(1, _level._colorCount + 1));
                        _tileControllers[x][y] = tile.GetComponent<TileController>();
                        _flattenedGrid[to_index(x, y)] = tileInfo._tileType;
                        _tileControllers[x][y].Initialize(tileInfo);
                        newTileAmounts[x]--;
                        _creationQueue.Enqueue(new int[] {x, y});
                    }
                }
            }
        }
        StartCoroutine(ProcessCreationQueue());
    }

    private IEnumerator ProcessCreationQueue()
    {
        if(!Monitor.TryEnter(_creationLock))
            yield return null;

        try
        {
            int _lastY = 15;
            while (_creationQueue.Count > 0)
            {

                int[] coordinates = _creationQueue.Dequeue();
                int x = coordinates[0];
                int y = coordinates[1];

                if(y != _lastY)
                {
                    yield return new WaitForSeconds(0.1f);
                    _lastY = y;
                }
                
                _tileControllers[x][y].StartFalling(y);
            }
        }
        finally
        {
            Monitor.Exit(_creationLock);
        }
        
    }

    public void StartNewLevel(Vector3 gridPos)
    {
        DisableEveryTile();
        this._clicked           = true;
        this._fallLock          = new object();
        this._unionInProgress   = new object();
        this._creationLock      = new object();
        this._tileSize          = _activeSpawns[1].position.x - _activeSpawns[0].position.x;
        this._level             = LevelManager.Instance.GetLevel();
        _totalTiles             = this._level._rowCount * this._level._columnCount;
        this._flattenedGrid     = new TileType?[_totalTiles];
        this._unionFind         = new UnionFind(_totalTiles);
        this._creationQueue     = new Queue<int[]>();
        FillGridTransforms(gridPos);
        CreateTileMatrices();
        StartCoroutine(FillTiles());
    }

    private void DisableEveryTile()
    {
        if(_tileControllers != null && _tileControllers.Length > 0)
        {
            for (int x = 0; x < _tileControllers.Length; x++)
            {
                for (int y = 0; y < _tileControllers[x].Length; y++)
                {
                    if(_tileControllers[x][y] != null)
                        PoolingManager.Instance.ReturnPooledObject(_tileControllers[x][y].gameObject);
                }
            }
        }
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

    private void FillGridTransforms(Vector3 gridPos)
    {
        if(_gridPositions != null && _gridPositions.Length > 0)
            CleanMatrix<Vector3>(_gridPositions);

        _gridPositions = new Vector3[_level._columnCount][];
        for (int i = 0; i < _level._columnCount; i++)
        {
            _gridPositions[i] = new Vector3[_level._rowCount];
        }


        
        float _yPoint   = gridPos.y - _tileSize * _level._rowCount/2 + (_level._rowCount%2)*_tileSize/2 + 0.225f;
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
    
    public IEnumerator FillTiles()
    {
        while(!PoolingManager.Instance.IsReady)
        {
            yield return null;
        }

        for(int y = 0; y < _level._rowCount; y++)
        {
            for(int x = 0; x < _level._columnCount; x++)
            {
                GenerateTile((TileType)_level._startingBoard[x][y], x, y);
            }
            yield return new WaitForSeconds(0.1f);
        }
        this._clicked           = false;
    }

    private void GenerateTile(TileType type, int x, int y)
    {
        GameObject tile                        = PoolingManager.Instance.GetPooledObject(_activeSpawns[x].position);
        Tile tileInfo                          = new Tile(new Vector2Int(x, y), type);
        _tileControllers[x][y]                 = tile.GetComponent<TileController>();
        int flatIndex                          = to_index(x,y);
        _flattenedGrid[flatIndex]              = tileInfo._tileType;
        _tileControllers[x][y].Initialize(tileInfo);
        _tileControllers[x][y].StartFalling(y);
        
    }

    public void PerformUnionFind()
    {
        lock (_unionInProgress)
        {
            _unionFind.Start();
            for (int y = 0; y < _level._rowCount; y++)
            {
                for (int x = 0; x < _level._columnCount; x++)
                {
                    int index = to_index(x, y);
                    if(_flattenedGrid[index] == null || _flattenedGrid[index] == TileType.box || _tileControllers[x][y] == null || _tileControllers[x][y].IsFalling() || _tileControllers[x][y]._newlyCreated)
                        continue;
                    if (x > 0 && _tileControllers[x-1][y] != null && !_tileControllers[x-1][y].IsFalling() && !_tileControllers[x-1][y]._newlyCreated && _flattenedGrid[index] == _flattenedGrid[to_index(x - 1, y)])
                    {
                        _unionFind.Union(index, to_index(x - 1, y));
                    }
                    if (y > 0 && _tileControllers[x][y-1] != null && !_tileControllers[x][y-1].IsFalling() && !_tileControllers[x][y-1]._newlyCreated && _flattenedGrid[index] == _flattenedGrid[to_index(x, y - 1)])
                    {
                        _unionFind.Union(index, to_index(x, y - 1));
                    }
                }
            }

            for (int y = 0; y < _level._rowCount; y++)
            {
                for (int x = 0; x < _level._columnCount; x++)
                {
                    if(this._tileControllers[x][y] == null)
                        continue;
                    this._tileControllers[x][y].SetSprite(this._unionFind.GetSize(to_index(x,y)));
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
        this.Start();
    }

    public void Start()
    {
        for (int i = 0; i < parent.Length; i++)
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