using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LevelSoundscapeType
{
    Tunnel = 0,
    Arena,
    Ghastly
}

public enum AnnouncerVoiceGroup
{
    IntroTile = 0,
    GameOver,
}

public class GlobalAudio : MonoBehaviour
{
    public AudioClip[] levelAmbienceAudios;

    public List<AudioClip> AnnouncerIntroTitleVoices = new();
    public List<AudioClip> AnnouncerGameOverVoices = new();

    private AudioSource _levelFXaudioSource;
    private AudioSource _hadesAnnouncerAudioSource;

    private Coroutine _fadeCoroutine;

    void Awake()
    {
        _levelFXaudioSource = gameObject.AddComponent<AudioSource>();
        _levelFXaudioSource.spatialBlend = 0; // 2D global sound

        _hadesAnnouncerAudioSource = gameObject.AddComponent<AudioSource>();
        _hadesAnnouncerAudioSource.spatialBlend = 0; // 2D global sound
    }

    List<AudioClip> GetAnnouncerVoiceClips(AnnouncerVoiceGroup group)
    {
        return group switch
        {
            AnnouncerVoiceGroup.IntroTile => AnnouncerIntroTitleVoices,
            AnnouncerVoiceGroup.GameOver => AnnouncerGameOverVoices,
            _ => new()
        };
    }

    public void PlayAnnouncerVoiceType(AnnouncerVoiceGroup type)
    {
        var clips = GetAnnouncerVoiceClips(type);

        AudioClip usedClip = clips[Random.Range(0, clips.Count)];

        PlayAnnouncerVoiceClip(usedClip);
    }

    public void PlayAnnouncerVoiceClip(AudioClip clip)
    {
        if (clip != null && (_hadesAnnouncerAudioSource.clip != clip || !_hadesAnnouncerAudioSource.isPlaying))
        {
            _hadesAnnouncerAudioSource.loop = false;
            _hadesAnnouncerAudioSource.clip = clip;
            _hadesAnnouncerAudioSource.Play();
        }
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
