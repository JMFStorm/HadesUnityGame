using UnityEngine;
using UnityEngine.Audio;

public enum EnemySoundGroups
{
    Attack,
    DamageTaken,
    AttackMiss,
    AttackCharge,
    Walk
}

public enum EnemyVoiceGroups
{
    Alert = 0,
    Damage,
    Death,
    Idle,
    Attack,
    AttackCharge,
}

public class EnemySounds : MonoBehaviour
{
    private static float _lastVoiceTime = 0;

    AudioSource _enemySoundSource;
    AudioSource _enemyVoiceSource;

    public AudioClip[] AlertVoiceClips;
    public AudioClip[] DamageTakenVoiceClips;
    public AudioClip[] DeathVoiceClips;
    public AudioClip[] IdleVoiceClips;
    public AudioClip[] AttackVoiceClips;
    public AudioClip[] AttackChargeVoiceClips;

    public AudioClip[] AttackSoundClips;
    public AudioClip[] DamageTakenSoundClips;
    public AudioClip[] WalkSoundClips;
    public AudioClip[] AttackMissSoundClips;
    public AudioClip[] AttackChargeSoundClips;

    private MainCamera _mainCamera;

    private void Awake()
    {
        _enemySoundSource = gameObject.AddComponent<AudioSource>();
        _enemyVoiceSource = gameObject.AddComponent<AudioSource>();

        _mainCamera = FindFirstObjectByType<MainCamera>();

        if (_mainCamera == null)
        {
            Debug.LogError($"{nameof(MainCamera)} not found on {nameof(EnemySounds)}");
        }
    }

    void Start()
    {
        _enemySoundSource.playOnAwake = false;
        _enemyVoiceSource.playOnAwake = false;

        _enemySoundSource.spatialBlend = 1.0f;
        _enemyVoiceSource.spatialBlend = 1.0f;

        _enemySoundSource.minDistance = 1.0f;
        _enemyVoiceSource.minDistance = 1.0f;

        _enemySoundSource.maxDistance = 10.0f;
        _enemyVoiceSource.maxDistance = 10.0f;

        _enemySoundSource.rolloffMode = AudioRolloffMode.Linear;
        _enemyVoiceSource.rolloffMode = AudioRolloffMode.Linear;
    }

    public void TryPlaySoundSource(EnemySoundGroups soundType)
    {
        AudioClip[] clips = soundType switch
        {
            EnemySoundGroups.Attack => AttackSoundClips,
            EnemySoundGroups.DamageTaken => DamageTakenSoundClips,
            EnemySoundGroups.Walk => WalkSoundClips,
            EnemySoundGroups.AttackCharge => AttackChargeSoundClips,
            EnemySoundGroups.AttackMiss => AttackMissSoundClips,
            _ => new AudioClip[] { },
        };

        if (!_mainCamera.IsWorldPositionVisible(_enemySoundSource.transform.position))
        {
            return;
        }

        if (0 < clips.Length)
        {
            AudioClip usedClip = clips[Random.Range(0, clips.Length)];

            _enemySoundSource.clip = usedClip;
            _enemySoundSource.volume = soundType == EnemySoundGroups.Walk ? 0.35f : 1.0f;
            _enemySoundSource.Play();
        }
    }

    public void TryPlayVoiceSource(EnemyVoiceGroups soundType, bool forceSound = false)
    {
        var newLastVoiceTime = Time.time;

        if (!forceSound && Mathf.Abs(_lastVoiceTime - newLastVoiceTime) < 1.0f)
        {
            return;
        }

        if (!_mainCamera.IsWorldPositionVisible(_enemySoundSource.transform.position))
        {
            return;
        }

        AudioClip[] clips = soundType switch
        {
            EnemyVoiceGroups.Alert => AlertVoiceClips,
            EnemyVoiceGroups.Damage => DamageTakenVoiceClips,
            EnemyVoiceGroups.Death => DeathVoiceClips,
            EnemyVoiceGroups.Idle => IdleVoiceClips,
            EnemyVoiceGroups.Attack => AttackVoiceClips,
            EnemyVoiceGroups.AttackCharge => AttackChargeVoiceClips,
            _ => new AudioClip[] { },
        };

        if (0 < clips.Length)
        {
            AudioClip usedClip = clips[Random.Range(0, clips.Length)];

            _enemyVoiceSource.clip = usedClip;
            _enemyVoiceSource.Play();

            _lastVoiceTime = newLastVoiceTime;
        }
    }
}
