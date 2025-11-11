using UnityEngine;
using System.Collections.Generic;

public class EffectsManager : MonoBehaviour
{
    public static EffectsManager Instance;
    public class EffectSetup
    {
        [SerializeField] public string effectName;
        [SerializeField] public GameObject prefab;
        [SerializeField] public int poolSize = 5;
    }
    public class ElementEffectSetup
    {
        [SerializeField] public string elementType;
        [SerializeField] public GameObject prefab;
        [SerializeField] public int poolSize = 10;
    }

    //general game effects
    public EffectSetup[] effects;

   // element effects
    public ElementEffectSetup[] elementEffects;

    Dictionary<string, ObjectPool> effectPools = new Dictionary<string, ObjectPool>();
    Dictionary<string, ObjectPool> elementPools = new Dictionary<string, ObjectPool>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SetupPools();
    }

    void SetupPools()
    {
        // Setup general effect pools
        foreach (EffectSetup setup in effects)
        {
            GameObject poolObj = new GameObject(setup.effectName + "_Pool");
            poolObj.transform.SetParent(transform);

            ObjectPool pool = poolObj.AddComponent<ObjectPool>();
            pool.prefab = setup.prefab;
            pool.poolSize = setup.poolSize;
            pool.Initialize();

            effectPools.Add(setup.effectName, pool);
        }

        // Setup element effect pools
        foreach (ElementEffectSetup setup in elementEffects)
        {
            GameObject poolObj = new GameObject(setup.elementType + "_Pool");
            poolObj.transform.SetParent(transform);

            ObjectPool pool = poolObj.AddComponent<ObjectPool>();
            pool.prefab = setup.prefab;
            pool.poolSize = setup.poolSize;
            pool.Initialize();

            elementPools.Add(setup.elementType, pool);
        }
    }

    public GameObject CreateEffect(string effectName, Vector3 position)
    {
        if (effectPools.ContainsKey(effectName))
        {
            GameObject effect = effectPools[effectName].GetFromPool();
            effect.transform.position = position;
            effect.SetActive(true);
            return effect;
        }

        Debug.LogWarning("Effect not found: " + effectName);
        return null;
    }

    public GameObject CreateElementEffect(string elementType, Vector3 position)
    {
        if (elementPools.ContainsKey(elementType))
        {
            GameObject effect = elementPools[elementType].GetFromPool();
            effect.transform.position = position;
            effect.SetActive(true);
            return effect;
        }

        Debug.LogWarning("Element effect not found: " + elementType);
        return null;
    }

    public void ReturnEffect(GameObject effect)
    {
        // Check all pools
        foreach (ObjectPool pool in effectPools.Values)
        {
            if (pool.BelongsToPool(effect))
            {
                pool.ReturnToPool(effect);
                return;
            }
        }

        foreach (ObjectPool pool in elementPools.Values)
        {
            if (pool.BelongsToPool(effect))
            {
                pool.ReturnToPool(effect);
                return;
            }
        }
    }
}