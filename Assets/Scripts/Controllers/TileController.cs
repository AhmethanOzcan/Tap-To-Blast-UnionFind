using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileController : MonoBehaviour
{
    [SerializeField] private float _fallSpeed = 0.2f;
    public Tile _tile;
    public int _groupIndex;
    private bool _extraLife;
    private SpriteRenderer _spriteRenderer;
    private bool _falling;
    private Vector3 _targetPosition;

    private void Awake() {
        this._spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(Tile _tile)
    {
        this._tile                  = _tile;
        this._extraLife             = _tile._tileType == 0;
        this._groupIndex            = -1;
        this._spriteRenderer.sprite = TileManager.Instance._tileSprites[(int)_tile._tileType];
        StartFalling(_tile._coordinates.y);
    }

    private void StartFalling(int targetHeight)
    {
        this._targetPosition                = TileManager.Instance._gridPositions[this._tile._coordinates.x][targetHeight];
        this._spriteRenderer.sortingOrder   = targetHeight+1;
        this._falling                       = true;
    }

    void FixedUpdate()
    {
        if(this._falling)
        {
            float step = _fallSpeed * Time.fixedDeltaTime;
            transform.position -= transform.up * step;
        
            if (Mathf.Abs(transform.position.y - _targetPosition.y) < step)
            {
                transform.position = _targetPosition;
                _falling = false;
            }
        }
    }
}
