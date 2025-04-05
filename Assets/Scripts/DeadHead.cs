using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class DeadHead : MonoBehaviour
{
    private Rigidbody2D _rb;
    private AudioSource _as;

    private float _kickedTime = 0f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _as = GetComponent<AudioSource>();

        var spriteRenderer = GetComponent<SpriteRenderer>();

        var material = spriteRenderer.material;

        if (transform.parent.TryGetComponent<ShadowEnemyEffects>(out var shadowEffect))
        {
            material.SetFloat("_OutlineThickness", shadowEffect.OutlineThickness);
            material.SetColor("_InlineColor", shadowEffect.InlineColor);
            material.SetColor("_OutlineColor", shadowEffect.OutlineColor);
            material.SetColor("_DamageColor", new(0, 0, 0));
        }

        material.SetFloat("_IsShadowVariant", 1f);

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;

        float minPitch = 0.8f;
        float maxPitch = 1.3f;
        float forceThreshold = 8f;

        _as.pitch = Mathf.Lerp(minPitch, maxPitch, impactForce / forceThreshold);
        _as.volume = Mathf.Lerp(0.25f, 0.75f, impactForce / forceThreshold);
        _as.Play();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && (1f < Mathf.Abs(Time.time - _kickedTime)))
        {
            Vector2 collisionDirection = (transform.position - collision.transform.position).normalized;

            var x = collisionDirection.x;
            var impulse = new Vector2(x, 0.5f) * 15f;
            _rb.AddForce(impulse, ForceMode2D.Impulse);

            var spin = collisionDirection.x < 0f ? -1f : 1f;
            _rb.AddTorque(spin, ForceMode2D.Impulse);

            _as.pitch = 1f;
            _as.volume = 1f;
            _as.Play();

            _kickedTime = Time.time;
        }
    }
}
