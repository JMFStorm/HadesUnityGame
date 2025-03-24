using Mono.Cecil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public enum LevelSoundscapeType
{
    Tunnel = 0,
    Arena,
    Ghastly
}

public enum GlobalMusic
{
    None = 0,
    TensionBooster1,
    ReverberedAudio1,
    MysticTexture1,
    MysticTexture2,
}

public enum AnnouncerVoiceGroup
{
    IntroTile = 0,
    GameOver,
    ArenaEventLevel1,
    ArenaEventLevel2,
    ArenaEventLevel3,
}

public class GlobalAudio : MonoBehaviour
{
    public AudioClip[] LevelAmbienceAudios;
    public AudioClip[] GlobalMusicAudios;

    public List<AudioClip> AnnouncerIntroTitleVoices = new();
    public List<AudioClip> AnnouncerGameOverVoices = new();

    public AudioClip UIButtonHover;
    public AudioClip UIButtonSelect;
    public AudioClip UIColorSelect;

    private AudioSource _levelAmbienceAudioSource;
    private AudioSource _hadesAnnouncerAudioSource;
    private AudioSource _globalMusicAudioSource;
    private AudioSource _globaSoundEffectAudioSource;
    private AudioSource _globaUIAudioSource;

    private Coroutine _fadeMusicCoroutine;

    void Awake()
    {
        _levelAmbienceAudioSource = gameObject.AddComponent<AudioSource>();
        _levelAmbienceAudioSource.spatialBlend = 0; // 2D global sound

        _hadesAnnouncerAudioSource = gameObject.AddComponent<AudioSource>();
        _hadesAnnouncerAudioSource.spatialBlend = 0; // 2D global sound

        _globalMusicAudioSource = gameObject.AddComponent<AudioSource>();
        _globalMusicAudioSource.spatialBlend = 0; // 2D global sound

        _globaSoundEffectAudioSource = gameObject.AddComponent<AudioSource>();
        _globaSoundEffectAudioSource.spatialBlend = 0; // 2D global sound

        _globaUIAudioSource = gameObject.AddComponent<AudioSource>();
        _globaUIAudioSource.spatialBlend = 0; // 2D global sound
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

    public void PlayUIButtonHover()
    {
        PlayUISound(UIButtonHover, 0.05f);
    }

    public void PlayUIButtonSelect()
    {
        PlayUISound(UIButtonSelect, 0.2f);
    }

    public void PlayUIColorSelect()
    {
        PlayUISound(UIColorSelect, 0.2f);
    }

    public void PlayUISound(AudioClip clip, float volume)
    {
        if (clip != null)
        {
            _globaUIAudioSource.PlayOneShot(clip, volume);
        }
    }

    public void PlayAnnouncerVoiceType(AnnouncerVoiceGroup type)
    {
        var clips = GetAnnouncerVoiceClips(type);

        AudioClip usedClip = clips[Random.Range(0, clips.Count)];

        PlayAnnouncerVoiceClip(usedClip);
    }

    public void PlaySoundEffect(AudioClip clip, float volume)
    {
        if (clip != null && (_globaSoundEffectAudioSource.clip != clip || !_globaSoundEffectAudioSource.isPlaying))
        {
            _globaSoundEffectAudioSource.loop = false;
            _globaSoundEffectAudioSource.volume = volume;
            _globaSoundEffectAudioSource.clip = clip;
            _globaSoundEffectAudioSource.Play();
        }
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
        AudioClip clip = LevelAmbienceAudios[(int)type];

        if (clip != null && (_levelAmbienceAudioSource.clip != clip || !_levelAmbienceAudioSource.isPlaying))
        {
            _levelAmbienceAudioSource.clip = clip;
            _levelAmbienceAudioSource.loop = true;
            _levelAmbienceAudioSource.volume = 0.3f;
            _levelAmbienceAudioSource.Play();
        }
    }

    public void StopAmbience()
    {
        _levelAmbienceAudioSource.Stop();
    }

    public void StopMusic(float fadeTime)
    {
        StartFade(ref _fadeMusicCoroutine, _globalMusicAudioSource, 0f, fadeTime, true, _globalMusicAudioSource.volume);
    }

    public void PlayGlobalMusic(GlobalMusic music, bool loop, float? volume = null)
    {
        if (music == GlobalMusic.None)
        {
            StartFade(ref _fadeMusicCoroutine, _globalMusicAudioSource, 0, 2f, true, _globalMusicAudioSource.volume);
            return;
        }

        AudioClip clip = GlobalMusicAudios[(int)(music - 1)];

        if (clip != null && (_globalMusicAudioSource.clip != clip || !_globalMusicAudioSource.isPlaying))
        {
            var usedVol  = volume ?? 1.0f;

            _globalMusicAudioSource.clip = clip;
            _globalMusicAudioSource.loop = loop;
            _globalMusicAudioSource.volume = usedVol;
            _globalMusicAudioSource.Play();

            StartFade(ref _fadeMusicCoroutine, _globalMusicAudioSource, usedVol, 3f, false, 0f);
        }
    }

    private void StartFade(ref Coroutine coroutine, AudioSource source, float targetVolume, float duration, bool stopAfterFade, float startVolume)
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
        }

        coroutine = StartCoroutine(FadeAudio(source, targetVolume, duration, stopAfterFade, startVolume));
    }

    private IEnumerator FadeAudio(AudioSource source, float targetVolume, float duration, bool stopAfterFade, float? startVolume = null)
    {
        float startVolume1 = startVolume ?? source.volume;
        float time = 0f;

        if (!stopAfterFade)
        {
            source.Play();
        }

        while (time < duration)
        {
            source.volume = Mathf.Lerp(startVolume1, targetVolume, time / duration);
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
