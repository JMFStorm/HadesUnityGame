using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Projectile : MonoBehaviour
{
    public float speed = 5f;
    public float lifetime = 3f;

    private Rigidbody2D _rb;
    private SpriteRenderer _spriteRenderer;
    private Transform _spriteTransform;
    private ParticleSystem _particleSystem;

    private Color _targetColor;
    private bool _isFading = false; // Flag to track if fading is active
    private float _fadeStartTime; // Time when fading started
    private float _launchTime = 0;

    private Coroutine _implodeCoroutine = null;

    private Transform _damageZone;

    private readonly float fadeDuration = 0.04f; // Duration for fading effect

    private void Awake()
    {
        if (!TryGetComponent(out _rb))
        {
            Debug.LogError($"{nameof(Rigidbody2D)} not found on {nameof(Rigidbody2D)}");
        }

        if (!TryGetComponent(out _particleSystem))
        {
            Debug.LogError($"{nameof(ParticleSystem)} not found on {nameof(Rigidbody2D)}");
        }

        _spriteTransform = transform.Find("Sprite");

        if (!_spriteTransform.TryGetComponent(out _spriteRenderer))
        {
            Debug.LogError($"{nameof(SpriteRenderer)} not found on child Sprite of {nameof(Projectile)}");
        }

        _damageZone = transform.Find("DamageZone");

        if (_damageZone == null)
        {
            Debug.LogError($"DamageZone not found on child of {nameof(Projectile)}");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("FlyingEnemy"))
        {
            return; // NOTE: Ignore "self"
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            StartImplode(Color.red, 2.0f);
        }
        else
        {
            StartImplode(Color.black, 1.1f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("DamageZone") && other.gameObject.CompareTag("PlayerSword"))
        {
            Debug.Log("Player sword hit!");

            StartImplode(Color.black, 1.25f);
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
                // gameObject.SetActive(false);
            }
        }
        else
        {
            _launchTime += Time.deltaTime;
        }

        if (3f < _launchTime)
        {
            StartImplode(Color.black, 1f);
        }
    }

    public void SetProjectileColor(Color color)
    {
        _spriteRenderer.material.SetColor("_BaseColour", color);
        var main = _particleSystem.main;
        main.startColor = color;
    }

    public void Launch(Vector2 direction)
    {
        _launchTime = 0f;
        _rb.linearVelocity = direction.normalized * speed;
    }

    void StartImplode(Color color, float scale)
    {
        if (_implodeCoroutine == null)
        {
            _implodeCoroutine = StartCoroutine(Implode(color, scale));
        }
    }

    IEnumerator Implode(Color color, float scale)
    {
        var collider = _damageZone.GetComponent<CircleCollider2D>();
        collider.enabled = false;

        var audio = GetComponent<AudioSource>();
        audio.enabled = false;

        _spriteRenderer.enabled = false;
        _rb.simulated = false;
        _spriteTransform.localScale *= scale;
        _targetColor = color;

        _isFading = true;
        _fadeStartTime = Time.time;

        Invoke(nameof(DeleteObject), 1f);

        yield return new WaitForSeconds(1f);

        DeleteObject();
    }

    void DeleteObject()
    {
        Destroy(gameObject);
    }
}
