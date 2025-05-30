using System.Collections;
using UnityEngine;

public class BreakablePlatform : MonoBehaviour
{
    public AudioClip[] InitBreakAudios;
    public AudioClip[] EndBreakAudios;

    public float DestroyTimeMin = 0.75f;
    public float DestroyTimeMax = 0.95f;

    public float RespawnTime = 3f;

    private BoxCollider2D _boxCollider;
    private SpriteRenderer _spriteLeft;
    private AudioSource _audioSource;
    private Animator _animator;

    private Vector3 _collisionPosition;

    private bool _isDestroying = false;

    private void Awake()
    {
        if (!TryGetComponent(out _animator))
        {
            Debug.LogError($"{nameof(Animator)} not found on {nameof(BreakablePlatform)}.");
        }

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
    }

    private void Start()
    {
        _animator.Play("BreakablePlatform_Idle");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Vector2 collisionNormal = collision.contacts[0].normal;

            if (!_isDestroying && collisionNormal.y < 0f && Mathf.Abs(collisionNormal.x) < Mathf.Abs(collisionNormal.y))
            {
                _audioSource.clip = InitBreakAudios[Random.Range(0, InitBreakAudios.Length)];
                _audioSource.pitch = Random.Range(0.8f, 1.15f);
                _audioSource.loop = false;
                _audioSource.volume = 0.35f;
                _audioSource.Play();

                _animator.Play("BreakablePlatform_Breaks");

                StartCoroutine(DestroyPlatform());
            }
        }
    }

    IEnumerator DestroyPlatform()
    {
        _isDestroying = true;

        yield return new WaitForSeconds(Random.Range(DestroyTimeMin, DestroyTimeMax));

        _boxCollider.enabled = false;

        _audioSource.clip = EndBreakAudios[Random.Range(0, InitBreakAudios.Length)];
        _audioSource.pitch = Random.Range(0.8f, 1.15f);
        _audioSource.loop = false;
        _audioSource.volume = 0.35f;
        _audioSource.Play();

        _animator.Play("BreakablePlatform_Shatters", 0, 0f);

        yield return new WaitForSeconds(4f / 10f);

        _spriteLeft.enabled = false;

        yield return new WaitForSeconds(RespawnTime);

        RespawnPlatform();
    }

    private void RespawnPlatform()
    {
        _spriteLeft.enabled = true;
        _boxCollider.enabled = true;
        _isDestroying = false;

        _animator.Play("BreakablePlatform_Idle");
    }

    private void SetColor(Color color)
    {
        _spriteLeft.material.color = color;
    }
}

