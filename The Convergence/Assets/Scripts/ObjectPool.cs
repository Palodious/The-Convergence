using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.VFX;

public class ObjectPool : MonoBehaviour
{

    public GameObject prefab;
    public int initialSize = 10;
    private Queue<GameObject> pool = new Queue<GameObject>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < initialSize; i++)
        {
            CreateNewObject();
        }
    }

    public GameObject GetObject()
    {
        if (pool.Count == 0)
        {
            CreateNewObject();
        }

        GameObject obj = pool.Dequeue();
        obj.SetActive(true);
        return obj;
    }

    public void ReturnObject(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }

    private void CreateNewObject()
    {
        GameObject obj = Instantiate(prefab);
        obj.SetActive(false);
        pool.Enqueue(obj);

        var returnToPool = obj.AddComponent<ReturnToPool>();
        returnToPool.pool = this;

    }
}

public class ReturnToPool : MonoBehaviour
{
    public ObjectPool pool;
    private float timer;

   void OnDisable()
    {
        pool?.ReturnObject(gameObject);
    }

    void Update()
    {
        var ps = GetComponent<ParticleSystem>();
        if (ps && !ps.IsAlive()) {
                pool.ReturnObject(gameObject);
        }
    }
}

