
using UnityEngine;

public class LevelManager : Singleton<LevelManager>
{
    [Header("Variables For Level Creation")]
    [Tooltip("Row count between 2 to 10")] [Range(2,10)]
    [SerializeField] int M = 6;

    [Tooltip("Column count between 2 to 10")] [Range(2,10)]
    [SerializeField] int N = 6;

    [Tooltip("Total number of colors between 1 to 6")] [Range(1,6)]
    [SerializeField] int K = 6;

    [Tooltip("First Condition")]
    [SerializeField] int A = 4;
    
    [Tooltip("Second Condition")]
    [SerializeField] int B = 6;

    [Tooltip("Third Condition")]
    [SerializeField] int C = 8;


    private int[][] _startingBoard;
    private Level _currentLevel;
    private GridController _grid;

    protected override void Awake()
    {
        base.Awake();
        _grid = FindObjectOfType<GridController>();
    }

    private void Start() {
        CreateNewLevel();
    }

    private void CreateEmptyBoard()
    {
        if(this._startingBoard != null)
            DeleteStartingBoard();

        this._startingBoard = new int[N][];
        for(int i = 0; i < this._startingBoard.Length; i++)
        {
            this._startingBoard[i] = new int[M];
        }
    }

    private void DeleteStartingBoard()
    {
        for(int i = 0; i < this._startingBoard.Length; i++)
        {
            this._startingBoard[i] = null;
        }
        this._startingBoard = null;
    }

    private void FillEmptyBoard()
    {
        for(int x = 0; x < this._startingBoard.Length; x++)
        {
            for(int y = 0; y < this._startingBoard[x].Length; y++)
            {
                this._startingBoard[x][y] = Random.Range(0, K+1);
            }
        }

        _currentLevel = new Level(
            this.M,
            this.N,
            this.K,
            this.A,
            this.B,
            this.C,
            this._startingBoard
        );
    }

    public Level GetLevel()
    {
        return _currentLevel;
    }

    public void CreateNewLevel()
    {
        CreateEmptyBoard();
        FillEmptyBoard();
        _grid.StartGridCreation();
    }
}

public struct Level
{
    public int _rowCount { get; }
    public int _columnCount { get; }
    public int _colorCount { get; }
    public int _firstCondition { get; }
    public int _secondCondition { get; }
    public int _thirdCondition { get; }
    public int[][] _startingBoard { get; }

    public Level(int rowCount, int columnCount, int colorCount, int firstCondition, int secondCondition, int thirdCondition, int[][] startingBoard)
    {
        _rowCount = rowCount;
        _columnCount = columnCount;
        _colorCount = colorCount;
        _firstCondition = firstCondition;
        _secondCondition = secondCondition;
        _thirdCondition = thirdCondition;
        _startingBoard = startingBoard;
    }
}