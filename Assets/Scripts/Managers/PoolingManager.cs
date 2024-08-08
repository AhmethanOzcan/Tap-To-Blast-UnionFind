using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolingManager : Singleton<PoolingManager>
{
    [SerializeField] int _poolCount = 100;
    [SerializeField] Transform _folder;
    public GameObject _tilePrefab;
    private GameObject[] _pooledObjects;

    protected override void Awake() 
    {
        base.Awake();    
    }


    // Start is called before the first frame update
    void Start()
    {
        this._pooledObjects = new GameObject[_poolCount];
        GameObject tmp;
        for(int i = 0; i < _poolCount; i++)
        {
            tmp = Instantiate(_tilePrefab);
            tmp.transform.parent = _folder;
            tmp.SetActive(false);
            _pooledObjects[i] = tmp;
        }
    }

    public GameObject GetPooledObject(Vector3 position)
    {
        for(int i = 0; i < _poolCount; i++)
        {
            if(!_pooledObjects[i].activeInHierarchy)
            {
                _pooledObjects[i].transform.position = position;
                _pooledObjects[i].transform.rotation = Quaternion.identity;
                _pooledObjects[i].SetActive(true);
                return _pooledObjects[i];
            }
        }
        return null;
    }
}
