using UnityEngine;

[CreateAssetMenu]
public class medkitStats : ScriptableObject
{
    [Range(1, 200)] public int healAmount;
}