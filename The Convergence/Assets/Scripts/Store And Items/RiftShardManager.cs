using UnityEngine;
using System;

public class RiftShardManager : MonoBehaviour, ISaveable
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

        amount = Mathf.Max(0, amount);
        OnShardAmountChanged?.Invoke(amount);

    }

    public void Add(int value)
    {
        if (value < 0) return;

        amount += value;
        OnShardAmountChanged?.Invoke(amount);
    }

    public bool CanAfford(int cost)
    {
        if (cost <= 0) return true;
        return amount >= cost;
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

    public void SetAmount(int newAmount)
    {
        amount = Mathf.Max(0, newAmount);
        OnShardAmountChanged?.Invoke(amount);
    }

    public void ResetAmount()
    {
        amount = 0;
        OnShardAmountChanged?.Invoke(amount);
    }

    [Serializable]
    private struct RiftShardSaveData
    {
        public int amount;
    }
    object ISaveable.CaptureState() => CaptureState();
    void ISaveable.RestoreState(object state) => RestoreState(state);

    public object CaptureState()
    {
        return new RiftShardSaveData
        {
            amount = this.amount
        };
    }

    public void RestoreState(object state)
    {
        if (state is not RiftShardSaveData s)
        {
           // Debug.LogError($"RiftShardManager.RestoreState: expected RiftShardSaveData, got {state?.GetType()} on {name}");
            return;
        }

        amount = Mathf.Max(0, s.amount);
        OnShardAmountChanged?.Invoke(amount);
    }
}
