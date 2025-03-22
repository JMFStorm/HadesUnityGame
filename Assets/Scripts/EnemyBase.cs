using System;
using UnityEngine;

public abstract class EnemyBase : MonoBehaviour
{
    public static event Action<EnemyBase> OnEnemyDied; // Shared event for all enemies

    public virtual void SignalDieEvent()
    {
        OnEnemyDied?.Invoke(this); // Notify listeners that an enemy died
        Destroy(gameObject);
    }
}