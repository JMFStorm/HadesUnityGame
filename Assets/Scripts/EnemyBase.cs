using System;
using System.Collections;
using UnityEngine;

public abstract class EnemyBase : MonoBehaviour
{
    public static event Action<EnemyBase> OnEnemyDied; // Shared event for all enemies

    public Color DamageColor = new(0.8f, 0.1f, 0.1f, 1f);

    public Material ShadowShaderMaterial;
    public Material TeleporterShaderMaterial;

    public bool StartFacingLeft = false;

    protected Rigidbody2D _rigidBody;

    public string Id;

    public bool IsShadowVariant = false;

    protected bool _isPassive = false;
    protected bool _isDead = false;

    protected SpriteRenderer _spriteRenderer;
    protected Material _material;

    protected LayerMask _seesTargetLayerMask;

    protected virtual void Awake()
    {
        _seesTargetLayerMask = LayerMask.GetMask("Ground", "Character");

        if (!TryGetComponent(out _rigidBody))
        {
            Debug.LogError($"{nameof(Rigidbody2D)} not found on {nameof(AeroBehaviour)}");
        }

        if (!TryGetComponent(out _spriteRenderer))
        {
            Debug.LogError($"{nameof(SpriteRenderer)} not found on {nameof(ShadowEnemyEffects)}");
        }

        if (string.IsNullOrEmpty(Id))
        {
            Id = Guid.NewGuid().ToString();
        }

        if (TryGetComponent<ShadowEnemyEffects>(out var _))
        {
            IsShadowVariant = true;
        }

        SetEnemyMaterial();
    }

    public virtual void SignalDieEvent(float? destroyTimer)
    {
        OnEnemyDied?.Invoke(this); // Notify listeners that an enemy died

        if (destroyTimer != null)
        {
            StartCoroutine(DestroyTimer(destroyTimer.Value));
        }
    }

    /// <summary>
    /// Call from animation
    /// </summary>
    protected void SetDead()
    {
        _isDead = true;
        SignalDieEvent(null);
    }

    public void SetEnemyMaterial()
    {
        _spriteRenderer.material = ShadowShaderMaterial;
        _material = _spriteRenderer.material;

        if (TryGetComponent<ShadowEnemyEffects>(out var shadowEffect))
        {
            _material.SetFloat("_OutlineThickness", shadowEffect.OutlineThickness);
            _material.SetColor("_InlineColor", shadowEffect.InlineColor);
            _material.SetColor("_OutlineColor", shadowEffect.OutlineColor);
            _material.SetColor("_DamageColor", new(0, 0, 0));
        }

        _material.SetFloat("_IsShadowVariant", IsShadowVariant ? 1f : 0f);
    }

    public void SetTeleportMaterial()
    {
        _spriteRenderer.material = TeleporterShaderMaterial;
        _material = _spriteRenderer.material;

        _material.SetFloat("_Strength", 1f);
    }

    public virtual void UpdateTeleportShaderEffect(float strength)
    {
        _spriteRenderer.flipX = !StartFacingLeft;
        _material.SetFloat("_Strength", strength);
    }

    public void SetTeleportPassive(bool isPassive)
    {
        _isPassive = isPassive;
        _rigidBody.simulated = !isPassive;
    }

    public void SetDamageColor(bool inDamage)
    {
        _material.SetColor("_DamageColor", inDamage ? DamageColor : new(0, 0, 0));
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