using UnityEngine;

[System.Serializable]
public class RiftShards : ICurrency
{
    [Header("~=~=Rift Shards Amount=~=~")]
    [SerializeField] private int amount = 0;

    public string Name => "Rift Shards";

    // Getter and setter used by the manager
    public int Amount
    {
        get => amount;
        set => amount = Mathf.Max(0, value); // Prevents negative values
    }

    public RiftShards(int initial)
    {
        Amount = initial;
    }
}
