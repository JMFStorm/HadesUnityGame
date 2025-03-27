using UnityEngine;

public class HadesAnnouncement : MonoBehaviour
{
    public AudioClip HadesVoiceline;

    private GlobalAudio _globalAudio;

    void Awake()
    {
        _globalAudio = FindFirstObjectByType<GlobalAudio>();

        if (_globalAudio == null)
        {
            Debug.LogError($"{nameof(GlobalAudio)} not found on {nameof(GameState)}");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Hades announcement trigger");

            if (_globalAudio != null)
            {
                _globalAudio.PlayAnnouncerVoiceClip(HadesVoiceline);
            }

            gameObject.SetActive(false);
        }
    }
}
