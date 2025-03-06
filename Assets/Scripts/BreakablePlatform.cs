using UnityEngine;

public class BreakablePlatform : MonoBehaviour
{
    public Color BrokenColor = Color.red; 
    public Color DefaultColor = Color.yellow; 

    public float DestroyTime = 0.5f;
    public float RespawnTime = 3f;

    private SpriteRenderer _spriteLeft;
    private SpriteRenderer _spriteRight;

    private Vector3 _collisionPosition;

    private void Awake()
    {
        var left = transform.Find("SpriteLeft");

        if (left != null)
        {
            _spriteLeft = left.GetComponent<SpriteRenderer>();
        }
        else
        {
            Debug.LogError($"Child object 'SpriteLeft' not found on {nameof(SpriteRenderer)}.");
        }

        var right = transform.Find("SpriteRight");

        if (right != null)
        {
            _spriteRight = right.GetComponent<SpriteRenderer>();
        }
        else
        {
            Debug.LogError($"Child object 'SpriteRight' not found on {nameof(SpriteRenderer)}.");
        }
    }

    void Start()
    {
        SetColor(DefaultColor);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Vector2 collisionNormal = collision.contacts[0].normal;

            if (collisionNormal.y < 0f)
            {
                Debug.Log("DestroyPlatform");

                SetColor(BrokenColor);

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
        SetColor(DefaultColor);
    }

    private void SetColor(Color color)
    {
        _spriteLeft.material.color = color;
        _spriteRight.material.color = color;
    }
}

