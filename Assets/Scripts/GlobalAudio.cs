using System.Collections;
using UnityEngine;

public enum LevelSoundscapeType
{
    Tunnel = 0,
    Arena,
    Ghastly
}

public class GlobalAudio : MonoBehaviour
{
    public AudioClip[] levelAmbienceAudios;

    private AudioSource _levelFXaudioSource;

    private Coroutine _fadeCoroutine;

    void Awake()
    {
        _levelFXaudioSource = gameObject.AddComponent<AudioSource>();
        _levelFXaudioSource.spatialBlend = 0; // 2D global sound
    }

    public void PlayAmbience(LevelSoundscapeType type)
    {
        AudioClip clip = levelAmbienceAudios[(int)type];

        if (clip != null && (_levelFXaudioSource.clip != clip || !_levelFXaudioSource.isPlaying))
        {
            _levelFXaudioSource.clip = clip;
            _levelFXaudioSource.loop = true;
            _levelFXaudioSource.volume = 0.1f;
            _levelFXaudioSource.Play();
        }
    }

    public void StopAmbience()
    {
        _levelFXaudioSource.Stop();
    }

    private void StartFade(AudioSource source, float targetVolume, float duration, bool stopAfterFade = false)
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        _fadeCoroutine = StartCoroutine(FadeAudio(source, targetVolume, duration, stopAfterFade));
    }

    private IEnumerator FadeAudio(AudioSource source, float targetVolume, float duration, bool stopAfterFade)
    {
        float startVolume = source.volume;
        float time = 0f;

        while (time < duration)
        {
            source.volume = Mathf.Lerp(startVolume, targetVolume, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        source.volume = targetVolume;

        if (stopAfterFade)
        {
            source.Stop();
            source.clip = null;
        }
    }
}
