using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Projectile : MonoBehaviour
{
    public float speed = 5f; // Speed of the projectile
    public float lifetime = 3f; // Lifetime of the projectile in seconds

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer; // Reference to the SpriteRenderer component

    private Color targetColor; // The target color to fade to
    private float fadeDuration = 0.04f; // Duration for fading effect
    private bool isFading = false; // Flag to track if fading is active
    private float fadeStartTime; // Time when fading started

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
        GameObject otherObject = collision.gameObject; // Get the colliding GameObject
        int layerID = otherObject.layer; // Get the layer index (ID)
        string layerName = LayerMask.LayerToName(layerID); // Convert ID to name

        Debug.Log($"Collided with: {otherObject.name}, Layer: {layerName} (ID: {layerID})");

        // Check if the projectile collides with the player
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Projectile hit the player!"); // Log the collision
            ChangeColor(Color.red); // Change color to black
            ScaleProjectile(4f); // Scale the projectile by a factor of four
            isFading = true; // Start fading
            fadeStartTime = Time.time; // Record the time fading started
        }
        else
        {
            // Handle other collisions here (like hitting walls, etc.)
            Debug.Log("Projectile hit something else!");
            ChangeColor(Color.black); // Change color to black
            isFading = true; // Start fading
            fadeStartTime = Time.time; // Record the time fading started
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