using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileController : MonoBehaviour
{
    [SerializeField] private float _fallSpeed = 0.2f;
    public Tile _tile;
    public bool _extraLife;
    public SpriteRenderer _spriteRenderer;
    private bool _falling;
    private Vector3 _targetPosition;
    private bool _fallAllowed;
    private int _spriteType;

    private void Awake() {
        this._spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(Tile _tile)
    {
        this._spriteType            = -1;
        this._fallAllowed           = false;
        this._tile                  = _tile;
        this._falling               = false;
        this._extraLife             = _tile._tileType == TileType.box;
    }

    public void StartFalling(int targetHeight)
    {
        this._tile._coordinates.y           = targetHeight;
        this._targetPosition                = TileManager.Instance._gridPositions[this._tile._coordinates.x][targetHeight];
        this._spriteRenderer.sortingOrder   = targetHeight+1;
        SetSprite(1);
        this._falling                       = true;
    }

    void Update()
    {
        if(this._falling && this._fallAllowed)
        {
            float step = _fallSpeed * Time.deltaTime;
            transform.position -= transform.up * step;
        
            if (transform.position.y - _targetPosition.y < 0.25f )
            {
                transform.position = _targetPosition;
                _falling = false;
                TileManager.Instance.PerformUnionFind();
            }
        }
    }

    public bool FallInitiated()
    {
        return this._fallAllowed;
    }

    public void InitiateFall()
    {
        this._fallAllowed = true;
    }

    public bool IsFalling()
    {
        return this._falling;
    }

    public void QuitFalling()
    {
        this._falling = false;
    }

    public void SetSprite(int count)
    {
        if(this._falling)
            return;

        Sprite newSprite = GetSprite(count);

        if (newSprite != null)
        {
            this._spriteRenderer.sprite = newSprite;
        }
        
    }

    private Sprite GetSprite(int count)
    {
        int number = (int)_tile._tileType;
        if(number == 0)
        {
            if(_spriteType != 0 && this._extraLife)
            {
                _spriteType = 0;
                return TileManager.Instance._tileSprites[number];
            }
            else if(_spriteType != 1 && !this._extraLife)
            {
                _spriteType = 1;
                return TileManager.Instance._tileSprites[number + 7];
            }
            else
            {
                return null;
            }
        }
        else
        {
            int _condA = TileManager.Instance._level._firstCondition;
            int _condB = TileManager.Instance._level._secondCondition;
            int _condC = TileManager.Instance._level._thirdCondition;


            if(_spriteType != 0 && count <= _condA)
            {
                _spriteType = 0;
                return TileManager.Instance._tileSprites[number];
            }
            if(_spriteType != 1 && count > _condA && count <= _condB)
            {
                _spriteType = 1;
                return TileManager.Instance._tileSprites[number + 7];
            }
            else if(_spriteType != 2 && count > _condB && count <= _condC)
            {
                _spriteType = 2;
                return TileManager.Instance._tileSprites[number + 14];
            }
            else if(_spriteType != 3 && count > _condC)
            {
                _spriteType = 3;
                return TileManager.Instance._tileSprites[number + 21];
            }
            else
            {
                return null;
            }   
        }
    }
}
