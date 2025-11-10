using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.VFX;


public class EffectsManager : MonoBehaviour
{
    public ObjectPool jumpPrepPool;
    public ObjectPool jumpImpactPool;
    public ObjectPool pulsePool;
    public ObjectPool surgePool;

    [Header("Elemental Impact Pools")]
    public ObjectPool electricPool;
    public ObjectPool firePool;
    public ObjectPool crystalPool;
    public ObjectPool plasmaPool;
    public ObjectPool icePool;

    public static EffectsManager Instance;

    void Awake() => Instance = this;

    public GameObject PlayEffect(EffectType type, Vector3 position, ElementType element = ElementType.Neutral)
    {
        GameObject effect = null;

        switch (type)
        {
            case EffectType.JumpPrep:
                effect = jumpPrepPool.GetObject();
                break;
            case EffectType.JumpImpact:
                effect = jumpImpactPool.GetObject();
                break;
            case EffectType.PulseCast:
                effect = pulsePool.GetObject();
                break;
            case EffectType.SurgeCast:
                effect = surgePool.GetObject();
                break;
            case EffectType.ElementalImpact:
                effect = GetElementalEffect(element);
                break;
        }

        effect.transform.position = position;
        effect.SetActive(true);
        return effect;
    }

    ObjectPool GetElementalEffect(ElementType element)
    {
        return element switch
        {
            ElementType.Electric => electricPool,
            ElementType.Fire => firePool,
            ElementType.Crystal => crystalPool,
            ElementType.Plasma => plasmaPool,
            ElementType.Ice => icePool,
            _ => firePool
        };
    }

    public enum EffectType { JumpPrep, JumpImpact, PulseCast, SurgeCast, ElementalImpact }
    public enum ElementType { Neutral, Electric, Fire, Crystal, Plasma, Ice }
}