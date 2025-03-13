using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Projectile : MonoBehaviour
{
    public float speed = 5f; // Speed of the projectile
    public float lifetime = 3f; // Lifetime of the projectile in seconds

    private Rigidbody2D _rb;
    private SpriteRenderer _spriteRenderer; // Reference to the SpriteRenderer component
    private Transform _spriteTransform; // Reference to the SpriteRenderer component

    private Color _targetColor; // The target color to fade to
    private bool _isFading = false; // Flag to track if fading is active
    private float _fadeStartTime; // Time when fading started

    private readonly float fadeDuration = 0.04f; // Duration for fading effect

    private void Awake()
    {
        if (!TryGetComponent(out _rb))
        {
            Debug.LogError($"{nameof(Rigidbody2D)} not found on {nameof(Rigidbody2D)}");
        }

        _spriteTransform = transform.Find("Sprite");

        if (!_spriteTransform.TryGetComponent(out _spriteRenderer))
        {
            Debug.LogError($"{nameof(SpriteRenderer)} not found on child Sprite of {nameof(Projectile)}");
        }
    }

    public void Launch(Vector2 direction)
    {
        _rb.linearVelocity = direction.normalized * speed;
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("FlyingEnemy"))
        {
            return; // NOTE: Ignore "self"
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            Implode(Color.red, 2.0f);
        }
        else
        {
            Implode(Color.black, 1.1f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("DamageZone") && other.gameObject.CompareTag("PlayerSword"))
        {
            Debug.Log("Player sword hit!");

            Implode(Color.black, 1.25f);
            Destroy(gameObject, 2.0f);
        }
    }

    private void Update()
    {
        if (_isFading)
        {
            float elapsed = Time.time - _fadeStartTime;
            float t = elapsed / fadeDuration; // Calculate the proportion of the fade duration

            // Interpolate between the current color and target color
            _spriteRenderer.color = Color.Lerp(_spriteRenderer.color, _targetColor, t);

            if (1f <= t)
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void Implode(Color color, float scale)
    {
        _rb.simulated = false;
        _targetColor = color;
        _spriteTransform.localScale *= scale;
        _isFading = true;
        _fadeStartTime = Time.time;
    }
}
