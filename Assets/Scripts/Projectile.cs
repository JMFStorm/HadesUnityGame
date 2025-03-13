using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Projectile : MonoBehaviour
{
    public float speed = 5f; // Speed of the projectile
    public float lifetime = 3f; // Lifetime of the projectile in seconds

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer; // Reference to the SpriteRenderer component

    private Color _targetColor; // The target color to fade to
    private bool _isFading = false; // Flag to track if fading is active
    private float _fadeStartTime; // Time when fading started

    private readonly float fadeDuration = 0.04f; // Duration for fading effect

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>(); // Get the Rigidbody2D component
        spriteRenderer = GetComponent<SpriteRenderer>(); // Get the SpriteRenderer component
    }

    public void Launch(Vector2 direction)
    {
        rb.linearVelocity = direction.normalized * speed; // Apply velocity in the launch direction
        Destroy(gameObject, lifetime); // Destroy the projectile after its lifetime
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
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (_isFading)
        {
            float elapsed = Time.time - _fadeStartTime;
            float t = elapsed / fadeDuration; // Calculate the proportion of the fade duration

            // Interpolate between the current color and target color
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, _targetColor, t);

            if (1f <= t)
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void Implode(Color color, float scale)
    {
        _targetColor = color;
        transform.localScale *= scale;
        _isFading = true;
        _fadeStartTime = Time.time;
    }
}
