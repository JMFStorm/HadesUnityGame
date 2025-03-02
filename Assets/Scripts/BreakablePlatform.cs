using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BreakablePlatform : MonoBehaviour
{
    public Color BrokenColor = Color.red; 
    public Color DefaultColor = Color.yellow; 

    public float DestroyTime = 0.5f;
    public float RespawnTime = 3f;

    private SpriteRenderer _platformRenderer;

    private Vector3 _collisionPosition;

    private void Awake()
    {
        _platformRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        _platformRenderer.material.color = DefaultColor;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Vector2 collisionNormal = collision.contacts[0].normal;

            if (collisionNormal.y < 0f)
            {
                Debug.Log("DestroyPlatform");

                _platformRenderer.material.color = BrokenColor;

                Invoke(nameof(DestroyPlatform), DestroyTime);
            }
        }
    }

    private void DestroyPlatform()
    {
        transform.gameObject.SetActive(false);
        Invoke(nameof(RespawnPlatform), RespawnTime);
    }

    private void RespawnPlatform()
    {
        transform.gameObject.SetActive(true);
        _platformRenderer.material.color = DefaultColor;
    }
}

