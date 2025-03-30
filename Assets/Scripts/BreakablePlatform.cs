using UnityEngine;

public class BreakablePlatform : MonoBehaviour
{
    public AudioClip[] InitBreakAudios;
    public AudioClip[] EndBreakAudios;

    public Color BrokenColor = Color.red; 
    public Color DefaultColor = Color.yellow; 

    public float DestroyTimeMin = 0.75f;
    public float DestroyTimeMax = 0.95f;

    public float RespawnTime = 3f;

    private BoxCollider2D _boxCollider;
    private SpriteRenderer _spriteLeft;
    private SpriteRenderer _spriteRight;
    private AudioSource _audioSource;

    private Vector3 _collisionPosition;

    private void Awake()
    {
        if (!TryGetComponent(out _boxCollider))
        {
            Debug.LogError($"{nameof(BoxCollider2D)} not found on {nameof(BreakablePlatform)}.");
        }

        if (!TryGetComponent(out _audioSource))
        {
            Debug.LogError($"{nameof(AudioSource)} not found on {nameof(BreakablePlatform)}.");
        }

        var left = transform.Find("SpriteLeft");

        if (left != null)
        {
            _spriteLeft = left.GetComponent<SpriteRenderer>();
        }
        else
        {
            Debug.LogError($"Child object 'SpriteLeft' not found on {nameof(BreakablePlatform)}.");
        }

        var right = transform.Find("SpriteRight");

        if (right != null)
        {
            _spriteRight = right.GetComponent<SpriteRenderer>();
        }
        else
        {
            Debug.LogError($"Child object 'SpriteRight' not found on {nameof(BreakablePlatform)}.");
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

            if (collisionNormal.y < 0f && Mathf.Abs(collisionNormal.x) < Mathf.Abs(collisionNormal.y))
            {
                _audioSource.clip = InitBreakAudios[Random.Range(0, InitBreakAudios.Length)];
                _audioSource.pitch = Random.Range(0.8f, 1.15f);
                _audioSource.loop = false;
                _audioSource.volume = 0.15f;
                _audioSource.Play();

                SetColor(BrokenColor);

                Invoke(nameof(DestroyPlatform), Random.Range(DestroyTimeMin, DestroyTimeMax));
            }
        }
    }

    private void DestroyPlatform()
    {
        _spriteLeft.enabled = false;
        _spriteRight.enabled = false;
        _boxCollider.enabled = false;

        _audioSource.clip = EndBreakAudios[Random.Range(0, InitBreakAudios.Length)];
        _audioSource.pitch = Random.Range(0.8f, 1.15f);
        _audioSource.loop = false;
        _audioSource.volume = 0.15f;
        _audioSource.Play();

        Invoke(nameof(RespawnPlatform), RespawnTime);
    }

    private void RespawnPlatform()
    {
        _spriteLeft.enabled = true;
        _spriteRight.enabled = true;
        _boxCollider.enabled = true;

        SetColor(DefaultColor);
    }

    private void SetColor(Color color)
    {
        _spriteLeft.material.color = color;
        _spriteRight.material.color = color;
    }
}

