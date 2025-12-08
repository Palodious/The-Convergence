using UnityEngine;
using System;

public class RiftShardManager : MonoBehaviour
{
    public static RiftShardManager Instance { get; private set; }

    [Header("~=~=Rift Shard Settings=~=~")]
    [SerializeField] private int amount = 0;

    public event Action<int> OnShardAmountChanged;

    public int Amount => amount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Add(int value)
    {
        if (value < 0) return;

        amount += value;
        OnShardAmountChanged?.Invoke(amount);
    }

    public bool TrySpend(int value)
    {
        if (value < 0) return false;

        if (amount >= value)
        {
            amount -= value;
            OnShardAmountChanged?.Invoke(amount);
            return true;
        }

        return false;
    }
}
