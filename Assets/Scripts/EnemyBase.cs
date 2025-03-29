using System;
using System.Collections;
using UnityEngine;

public abstract class EnemyBase : MonoBehaviour
{
    public static event Action<EnemyBase> OnEnemyDied; // Shared event for all enemies

    public string Id;

    protected bool IsShadowVariant = false;

    protected virtual void Awake()
    {
        if (string.IsNullOrEmpty(Id))
        {
            Id = Guid.NewGuid().ToString();
        }

        if (TryGetComponent<ShadowEnemyEffects>(out var _))
        {
            IsShadowVariant = true;
        }
    }

    public virtual void SignalDieEvent(float destroyTimer = 2f)
    {
        OnEnemyDied?.Invoke(this); // Notify listeners that an enemy died

        StartCoroutine(DestroyTimer(destroyTimer));
    }

    IEnumerator DestroyTimer(float time)
    {
        yield return new WaitForSeconds(time);

        if (this.gameObject != null)
        {
            Destroy(this.gameObject);
        }
    }
}