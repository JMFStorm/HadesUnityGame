using UnityEngine;

public class Health : MonoBehaviour
{
    public AudioClip PickupSound;

    public float hoverHeight = 0.5f;
    public float hoverSpeed = 2f;
    public int AddedHealth;

    private PlayerCharacter _playerCharacter;
    private Vector3 _startPos;

    private void Awake()
    {
        _playerCharacter = FindFirstObjectByType<PlayerCharacter>();
        _startPos = transform.position;
    }

    void Update()
    {
        float newY = _startPos.y + Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
        transform.position = new Vector3(_startPos.x, newY, _startPos.z);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (PickupSound != null)
            {
                AudioSource.PlayClipAtPoint(PickupSound, transform.position);
            }

            _playerCharacter.GetHealthFromPickup(AddedHealth);
            gameObject.SetActive(false);
            Destroy(gameObject, 3f);
        }
    }
}