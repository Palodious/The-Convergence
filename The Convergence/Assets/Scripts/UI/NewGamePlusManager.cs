using System;
using UnityEngine;

public class NewGamePlusManager : MonoBehaviour, ISaveable
{
    public static NewGamePlusManager Instance { get; private set; }

    [Header("~=~= New Game+ Progression =~=~")]
    [SerializeField] private int cycle = 0; // 0 = Base, 1 = NG+, 2 = NG++, etc.

    [Header("~=~= Difficulty Scaling Per + Cycle =~=~")]
    [SerializeField] private float enemyHpPerCycle = 0.25f;       // +25% HP per cycle
    [SerializeField] private float enemyDamagePerCycle = 0.15f;   // +15% damage per cycle
    [SerializeField] private float enemySpeedPerCycle = 0.05f;    // +5% speed per cycle

    public int Cycle => cycle;

    public float GetEnemyHealthMultiplier() => EnemyHpMultiplier();
    public float GetEnemyDamageMultiplier() => EnemyDamageMultiplier();
    public float GetEnemySpeedMultiplier() => EnemySpeedMultiplier();

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

    public void AdvanceCycle()
    {
        cycle++;
        Debug.Log($"NewGamePlusManager: Cycle advanced. Now at {GetCycleLabel()}");
    }

    public void SetCycle(int value)
    {
        cycle = Mathf.Max(0, value);
    }

    public string GetCycleLabel()
    {
        if (cycle <= 0) return "Base";

        // Keep it readable if someone goes wild
        if (cycle <= 10)
            return new string('+', cycle);

        return $"+{cycle}";
    }

    public float EnemyHpMultiplier()
    {
        return 1f + (enemyHpPerCycle * cycle);
    }

    public float EnemyDamageMultiplier()
    {
        return 1f + (enemyDamagePerCycle * cycle);
    }

    public float EnemySpeedMultiplier()
    {
        return 1f + (enemySpeedPerCycle * cycle);
    }

    [Serializable]
    private struct NGPSaveData
    {
        public int cycle;
    }

    object ISaveable.CaptureState() => CaptureState();
    void ISaveable.RestoreState(object state) => RestoreState(state);

    public object CaptureState()
    {
        return new NGPSaveData { cycle = this.cycle };
    }

    public void RestoreState(object state)
    {
        if (state is not NGPSaveData data)
            return;

        cycle = Mathf.Max(0, data.cycle);
    }
}