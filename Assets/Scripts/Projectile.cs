using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Projectile : MonoBehaviour
{
    public float speed = 5f;
    public float lifetime = 3f;

    public AudioClip SplashSound;
    private Rigidbody2D _rb;
    private SpriteRenderer _spriteRenderer;
    private Transform _spriteTransform;
    private ParticleSystem _particleSystem;
    private PlayerCharacter _player;

    private AudioSource _audio;

    private Color _targetColor;
    private bool _isFading = false; // Flag to track if fading is active
    private float _fadeStartTime; // Time when fading started
    private float _launchTime = 0;

    private Coroutine _implodeCoroutine = null;
    private GameState _gameState;

    private readonly float fadeDuration = 0.04f; // Duration for fading effect

    private void Awake()
    {
        _player = FindFirstObjectByType<PlayerCharacter>();

        _gameState = FindFirstObjectByType<GameState>();

        if (!TryGetComponent(out _audio))
        {
            Debug.LogError($"{nameof(AudioSource)} not found on {nameof(Rigidbody2D)}");
        }

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
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("FlyingEnemy"))
        {
            return; // NOTE: Ignore "self"
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            Vector2 collisionDirection = (transform.position - collision.transform.position).normalized;

            _player.TryRecieveDamage(collisionDirection);

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
        }
        else
        {
            _launchTime += Time.deltaTime;
        }

        if (3f < _launchTime)
        {
            StartImplode(Color.black, 1f);
        }

        if (_audio.isPlaying)
        {
            var gameState = _gameState.GetGameState();

            if (gameState is GameStateType.PauseMenu)
            {
                _audio.Pause();
            }
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
        _audio.clip = SplashSound;
        _audio.volume = 0.65f;
        _audio.loop = false;
        _audio.Play();

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
