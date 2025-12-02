using UnityEngine;

[CreateAssetMenu(menuName = "Items/Medkit")]
public class medkitStats : ScriptableObject
{
    [Range(10, 200)] public int healAmount = 50;

    public GameObject useEffect; // Optional particle effect
}