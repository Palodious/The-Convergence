using UnityEngine;

[CreateAssetMenu(menuName = "Items/Key")]
public class KeyItem : ScriptableObject
{
    [Header("~=~= Key Settings =~=~")]
    public string keyID = "MainKey";      // optional: supports multiple keys
}