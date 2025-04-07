using UnityEngine;

public class Health : MonoBehaviour
{
    public AudioClip PickupSound;

    public int AddedHealth;

    // Heartbeat scaling variables
    public float largeScaleFactor = 1.2f; // Scale factor for larger heartbeat
    public float scaleSpeed = 0.25f; // Speed of scaling effect

    private PlayerCharacter _playerCharacter;
    private Vector3 _startPos;
    private Vector3 _initialScale;

    private GlobalAudio _globalAudio;

    private void Awake()
    {
        _globalAudio = FindFirstObjectByType<GlobalAudio>();
        _playerCharacter = FindFirstObjectByType<PlayerCharacter>();
        _startPos = transform.position;
        _initialScale = transform.localScale; // Store the initial scale
    }

    void Update()
    {
        // Hover effect
        //float newY = _startPos.y + Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
        // transform.position = new Vector3(_startPos.x, newY, _startPos.z);

        // Scaling effect using PingPong
        float scale = Mathf.PingPong(Time.time * scaleSpeed, largeScaleFactor - .9f) + .9f;
        transform.localScale = new Vector3(scale, scale, scale);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (PickupSound != null)
            {
                _globalAudio.PlaySoundEffect(PickupSound, 0.25f);
            }

            _playerCharacter.GetHealthFromPickup(AddedHealth);
            Destroy(gameObject); // Destroy immediately on pickup
        }
    }
}