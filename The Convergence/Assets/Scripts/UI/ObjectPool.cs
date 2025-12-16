using UnityEngine;
using System.Collections.Generic;

public class ObjectPool : MonoBehaviour
{
    [SerializeField] public GameObject prefab;
    [SerializeField] public int poolSize = 10;

    Queue<GameObject> available = new Queue<GameObject>();
    List<GameObject> allObjects = new List<GameObject>();

    public void Initialize()
    {
        for (int i = 0; i < poolSize; i++)
        {
            CreateNewObject();
        }
    }

    void CreateNewObject()
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        available.Enqueue(obj);
        allObjects.Add(obj);
    }

    public GameObject GetFromPool()
    {
        if (available.Count == 0)
        {
            CreateNewObject();
        }

        return available.Dequeue();
    }

    public void ReturnToPool(GameObject obj)
    {
        if (obj == null)
        {
          //  Debug.LogWarning($"ObjectPool: Attempted to return null object.");
            return;
        }

        if (!allObjects.Contains(obj))
        {
          //  Debug.LogWarning($"ObjectPool: Attempted to return {obj.name} which doesn't belong to this pool.");
            return;
        }

        if (available.Contains(obj))
        {
          //  Debug.LogWarning($"ObjectPool: {obj.name} already in pool. Double-return?");
            return;
        }

        obj.SetActive(false);
        obj.transform.SetParent(transform);
        available.Enqueue(obj);
    }

    public bool BelongsToPool(GameObject obj)
    {
        return allObjects.Contains(obj);
    }
}