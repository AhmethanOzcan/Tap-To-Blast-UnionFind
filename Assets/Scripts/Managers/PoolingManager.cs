using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolingManager : Singleton<PoolingManager>
{
    [SerializeField] int _poolCount = 100;
    [SerializeField] Transform _folder;
    public GameObject _tilePrefab;
    private Queue<GameObject> _pooledObjects;
    public bool IsReady { get; private set; }
    protected override void Awake() 
    {
        base.Awake();    
    }


    // Start is called before the first frame update
    void Start()
    {
        _pooledObjects = new Queue<GameObject>(_poolCount);
        for(int i = 0; i < _poolCount; i++)
        {
            GameObject tmp = Instantiate(_tilePrefab);
            tmp.transform.parent = _folder;
            tmp.SetActive(false);
            _pooledObjects.Enqueue(tmp);
        }
        IsReady = true;
    }

    public GameObject GetPooledObject(Vector3 position)
    {
        if(_pooledObjects.Count > 0)
        {
            GameObject obj = _pooledObjects.Dequeue();
            obj.transform.position = position;
            obj.transform.rotation = Quaternion.identity;
            obj.SetActive(true);
            return obj;
        }
        return null;
    }

    public void ReturnPooledObject(GameObject obj)
    {
        obj.SetActive(false);
        _pooledObjects.Enqueue(obj);
    }
}
