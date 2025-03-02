using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BreakablePlatform : MonoBehaviour
{
    public Color BrokenColor = Color.red; 
    public Color DefaultColor = Color.yellow; 

    public float DestroyTime = 0.4f;
    public float RespawnTime = 3f;

    private SpriteRenderer _platformRenderer;

    private float _platformStaticElapsedTime = 0f;
    private bool _staticCollisionMode = false;

    private readonly float _staticCollisionTime = 0.1f;

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
        Debug.Log("Collision");

        if (collision.gameObject.CompareTag("Player"))
        {
            _staticCollisionMode = true;
            _collisionPosition = collision.gameObject.transform.position;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (_staticCollisionMode && collision.gameObject.CompareTag("Player"))
        {
            _platformStaticElapsedTime += Time.deltaTime;

            float yDiff = Mathf.Abs(_collisionPosition.y - collision.gameObject.transform.position.y);

            if (yDiff <= 0.001f)
            {
                if (_platformStaticElapsedTime < _staticCollisionTime)
                {
                    _platformRenderer.material.color = BrokenColor;
                    Invoke(nameof(DestroyPlatform), DestroyTime);
                }
            }
            else
            {
                _platformStaticElapsedTime = 0f;
                _collisionPosition = collision.gameObject.transform.position;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (_staticCollisionMode && collision.gameObject.CompareTag("Player"))
        {
            _staticCollisionMode = false;
            Debug.Log("Collision end");
        }
    }

    private void DestroyPlatform()
    {
        transform.gameObject.SetActive(false);
        Invoke(nameof(RespawnPlatform), RespawnTime);
    }

    private void RespawnPlatform()
    {
        Debug.Log("RespawnPlatform");

        transform.gameObject.SetActive(true);
       
        _platformRenderer.material.color = DefaultColor;
        _platformStaticElapsedTime = 0f;
    }
}

