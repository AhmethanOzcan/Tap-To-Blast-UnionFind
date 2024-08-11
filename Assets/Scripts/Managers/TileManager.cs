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
    private int _totalBlast;

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
            HashSet<Vector2Int> boxCoordinates = new HashSet<Vector2Int>();
            for (int i = 0; i < _totalTiles; i++)
                _unionFind.Find(i);
            for (int i = 0; i < _totalTiles; i++)
            {
                if(_flattenedGrid[i] == null)
                    continue;
                if (_unionFind.Find(i) == leaderIndex)
                {
                    Vector2Int coordinates = from_index(i);
                    GameObject gameObject = _tileControllers[coordinates.x][coordinates.y].gameObject;
                    PoolingManager.Instance.ReturnPooledObject(gameObject);
                    _tileControllers[coordinates.x][coordinates.y] = null;
                    _flattenedGrid[i] = null;
                    
                    // Check for boxes
                    if(coordinates.y > 0 && _flattenedGrid[i - _level._columnCount] != null && _flattenedGrid[i - _level._columnCount] == TileType.box)
                        boxCoordinates.Add(new Vector2Int(coordinates.x, coordinates.y-1));
                    if(coordinates.x > 0 && _flattenedGrid[i - 1] != null && _flattenedGrid[i - 1] == TileType.box)
                        boxCoordinates.Add(new Vector2Int(coordinates.x-1, coordinates.y));
                    if(coordinates.x < _level._columnCount-1 && _flattenedGrid[i + 1] != null && _flattenedGrid[i + 1] == TileType.box)
                        boxCoordinates.Add(new Vector2Int(coordinates.x+1, coordinates.y));
                    if(coordinates.y < _level._rowCount-1 && _flattenedGrid[i + _level._columnCount] != null && _flattenedGrid[i + _level._columnCount] == TileType.box)
                        boxCoordinates.Add(new Vector2Int(coordinates.x, coordinates.y+1));

                }
            }

            // Damage the boxes
            foreach(Vector2Int boxPos in boxCoordinates)
            {
                TileController boxController = _tileControllers[boxPos.x][boxPos.y];
                if(boxController._extraLife)
                    boxController._extraLife = false;
                else
                {
                    GameObject boxGameObject = boxController.gameObject;
                    PoolingManager.Instance.ReturnPooledObject(boxGameObject);
                    _tileControllers[boxPos.x][boxPos.y] = null;
                    _flattenedGrid[to_index(boxPos.x, boxPos.y)] = null;
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
            _totalBlast++;
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
                        _creationQueue.Enqueue(new int[] {x, y, _totalBlast});
                    }
                }
            }
            PerformUnionFind();
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
            int _lastOrder = 0;
            while (_creationQueue.Count > 0)
            {

                int[] coordinates = _creationQueue.Dequeue();
                int x       = coordinates[0];
                int y       = coordinates[1];
                int order   = coordinates[2];

                if(y != _lastY || order != _lastOrder)
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
        this._totalBlast        = 0;
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
                GameObject tile                                     = PoolingManager.Instance.GetPooledObject(_activeSpawns[x].position);
                Tile tileInfo                                       = new Tile(new Vector2Int(x, y), (TileType)_level._startingBoard[x][y]);
                _tileControllers[x][y]                              = tile.GetComponent<TileController>();
                int flatIndex                                       = to_index(x,y);
                _flattenedGrid[flatIndex]                           = tileInfo._tileType;
                _tileControllers[x][y].Initialize(tileInfo);
                _tileControllers[x][y]._newlyCreated                = false;
                _tileControllers[x][y].QuitFalling();
                _tileControllers[x][y]._spriteRenderer.sortingOrder = y + 1;
                _tileControllers[x][y].transform.position           = _gridPositions[x][y];
            }
        }
        PerformUnionFind();
        this._clicked           = false;
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

                    if(_flattenedGrid[index] == null || _tileControllers[x][y] == null)
                        continue;
                    if(_flattenedGrid[index] == TileType.box || _tileControllers[x][y].IsFalling() || _tileControllers[x][y]._newlyCreated)
                        continue;

                    if (x > 0 && _tileControllers[x-1][y] != null && !_tileControllers[x-1][y].IsFalling() && !_tileControllers[x-1][y]._newlyCreated && _flattenedGrid[index] == _flattenedGrid[to_index(x - 1, y)])
                        _unionFind.Union(index, to_index(x - 1, y));
                    if (y > 0 && _tileControllers[x][y-1] != null && !_tileControllers[x][y-1].IsFalling() && !_tileControllers[x][y-1]._newlyCreated && _flattenedGrid[index] == _flattenedGrid[to_index(x, y - 1)])
                        _unionFind.Union(index, to_index(x, y - 1));
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

            bool _isDeadlock = true;
            for(int index = 0; index < _totalTiles; index++)
            {
                if(_unionFind.GetSize(index) != 1)
                {
                    _isDeadlock = false;
                    break;
                }
            }
                
            
            if(_isDeadlock)
            {
                ResolveDeadlock();
            } 
        }
    }

    private void ResolveDeadlock()
    {
        List<PotentialMove> potentialMoves = new List<PotentialMove>();
        int halfPoint = _totalTiles / 2;
        for(int from = 0; from < _totalTiles; from++)
        {
            for(int to = from < halfPoint? 0 : halfPoint - 1; to < _totalTiles; to++)
            {
                if(from == to || _flattenedGrid[to] == _flattenedGrid[from])
                    continue;

                int points = CheckPointsEarned(from, to);
                if(points > 0)
                    potentialMoves.Add(new PotentialMove{fromIndex = from, toIndex = to, score = points});
            }
        }
        potentialMoves.Sort((a,b) => b.score.CompareTo(a.score));
        // 10 comes from each change locking the area of near tiles
        int maxChange = _totalTiles / 10;
        bool[] lockCheck = new bool[_totalTiles]; 
        foreach(PotentialMove move in potentialMoves)
        {
            if(lockCheck[move.fromIndex] || lockCheck[move.toIndex])
                continue;
            SwapPosition(move.fromIndex, move.toIndex, lockCheck);
            maxChange--;
            if(maxChange == 0)
                break;
        }
        
    }

    private void SwapPosition(int from, int to, bool[] lockCheck)
    {
        Vector2Int fromPos = from_index(from);
        Vector2Int toPos = from_index(to);

        TileController fromTile = _tileControllers[fromPos.x][fromPos.y];
        TileController toTile = _tileControllers[toPos.x][toPos.y];

        // Swap the positions in the grid
        _tileControllers[fromPos.x][fromPos.y] = toTile;
        _tileControllers[toPos.x][toPos.y] = fromTile;

        // Swap their coordinates
        fromTile._tile._coordinates = toPos;
        toTile._tile._coordinates = fromPos;

        // Swap the flattened grid
        TileType? tempType = _flattenedGrid[from];
        _flattenedGrid[from] = _flattenedGrid[to];
        _flattenedGrid[to] = tempType;

        // Smoothly swap positions using coroutines
        StartCoroutine(SmoothSwap(fromTile, fromPos, toPos));
        StartCoroutine(SmoothSwap(toTile, toPos, fromPos));

        LockRegion(lockCheck, from);
        LockRegion(lockCheck, to);
    }

    private IEnumerator SmoothSwap(TileController tile, Vector2Int startPos, Vector2Int endPos, float duration = 0.3f)
    {
        tile._spriteRenderer.sortingOrder = 100;
        Vector3 start = _gridPositions[startPos.x][startPos.y];
        Vector3 end = _gridPositions[endPos.x][endPos.y];
        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            tile.transform.position = Vector3.Lerp(start, end, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        tile._spriteRenderer.sortingOrder = endPos.y + 1;
        tile.transform.position = end;
        PerformUnionFind();
    }

    private void LockRegion(bool[] lockCheck, int index)
    {
        lockCheck[index] = true;
        if (index > 0) lockCheck[index - 1] = true;
        if (index < _totalTiles - 1) lockCheck[index + 1] = true;
        if (index >= _level._columnCount) lockCheck[index - _level._columnCount] = true;
        if (index < _totalTiles - _level._columnCount) lockCheck[index + _level._columnCount] = true;
    }

    private int CheckPointsEarned(int a, int b)
    {
        int points = 0;
        // Check if around b contains a's type and around a contains b's type
        points += CalculatePointsAroundTile(a, _flattenedGrid[b]);
        points += CalculatePointsAroundTile(b, _flattenedGrid[a]);

        return points;
    }

    private int CalculatePointsAroundTile(int index, TileType? targetType)
    {
        int points = 0;

        if (targetType != TileType.box)
        {
            if (index > 0 && _flattenedGrid[index - 1] == targetType)
                points++;
            if (index < _totalTiles - 1 && _flattenedGrid[index + 1] == targetType)
                points++;
            if (index >= _level._columnCount && _flattenedGrid[index - _level._columnCount] == targetType)
                points++;
            if (index < _totalTiles - _level._columnCount && _flattenedGrid[index + _level._columnCount] == targetType)
                points++;
        }

        return points;
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

public class PotentialMove
{
    public int fromIndex;
    public int toIndex;
    public int score;
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