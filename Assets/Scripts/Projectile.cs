using UnityEngine;
using static Codice.Client.Common.EventTracking.TrackFeatureUseEvent.Features.DesktopGUI.Filters;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Projectile : MonoBehaviour
{
    public float speed = 5f; // Speed of the projectile
    public float lifetime = 3f; // Lifetime of the projectile in seconds

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer; // Reference to the SpriteRenderer component

    private Color targetColor; // The target color to fade to
    private bool isFading = false; // Flag to track if fading is active
    private float fadeStartTime; // Time when fading started

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
            // NOTE: Ignore "self"
            return;
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            ChangeColor(Color.red);
            ScaleProjectile(4f);
            isFading = true;
            fadeStartTime = Time.time; // Record the time fading started
        }
        else
        {
            ChangeColor(Color.black);
            isFading = true;
            fadeStartTime = Time.time;
        }
    }

    private void Update()
    {
        // If fading is active, update the color
        if (isFading)
        {
            float elapsed = Time.time - fadeStartTime; // Time since fading started
            float t = elapsed / fadeDuration; // Calculate the proportion of the fade duration

            // Interpolate between the current color and target color
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, targetColor, t);

            // Check if the fade is complete
            if (t >= 1f)
            {
                gameObject.SetActive(false); // Deactivate the projectile
            }
        }
    }

    private void ChangeColor(Color newColor)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = newColor; // Change the color of the projectile
        }
    }

    private void ScaleProjectile(float scaleFactor)
    {
        transform.localScale *= scaleFactor; // Scale the projectile by the given factor
    }
}