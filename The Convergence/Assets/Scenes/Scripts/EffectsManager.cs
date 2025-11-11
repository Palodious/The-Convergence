using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.VFX;


public class EffectsManager : MonoBehaviour
{
    [SerializeField] ObjectPool jumpPrepPool;
    [SerializeField] ObjectPool jumpImpactPool;
    [SerializeField] ObjectPool pulsePool;
    [SerializeField] ObjectPool surgePool;

    [SerializeField] ObjectPool electricPool;
    [SerializeField] ObjectPool firePool;
    [SerializeField] ObjectPool crystalPool;
    [SerializeField] ObjectPool laserPool;
    [SerializeField] ObjectPool icePool;

    public static EffectsManager Instance;

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
                effect = GetElementalEffect(element).GetObject();
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
            ElementType.Laser => laserPool,
            ElementType.Ice => icePool,
            _ => electricPool
        };
    }

    public enum EffectType { JumpPrep, JumpImpact, PulseCast, SurgeCast, ElementalImpact }
    public enum ElementType { Neutral, Electric, Fire, Crystal, Laser, Ice }
}