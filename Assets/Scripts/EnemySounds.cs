using UnityEngine;

public enum EnemySoundGroups
{
    Attack,
    DamageTaken,
    AttackMiss,
    AttackCharge,
    Walk,
    Fly,
    Drag
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

    private AudioSource[] _enemySoundSources = new AudioSource[2];
    private AudioSource _enemyVoiceSource;

    private int _lastSoundSourceIndex = 0;

    public AudioClip[] AlertVoiceClips;
    public AudioClip[] DamageTakenVoiceClips;
    public AudioClip[] DeathVoiceClips;
    public AudioClip[] IdleVoiceClips;
    public AudioClip[] AttackVoiceClips;
    public AudioClip[] AttackChargeVoiceClips;

    public AudioClip[] AttackSoundClips;
    public AudioClip[] DamageTakenSoundClips;
    public AudioClip[] WalkSoundClips;
    public AudioClip[] DragSoundClips;
    public AudioClip[] FlySoundClips;
    public AudioClip[] AttackMissSoundClips;
    public AudioClip[] AttackChargeSoundClips;

    private MainCamera _mainCamera;

    private void Awake()
    {
        var soundSource1 = gameObject.AddComponent<AudioSource>();
        var soundSource2 = gameObject.AddComponent<AudioSource>();

        _enemySoundSources[0] = soundSource1;
        _enemySoundSources[1] = soundSource2;

        _enemyVoiceSource = gameObject.AddComponent<AudioSource>();

        _mainCamera = FindFirstObjectByType<MainCamera>();

        if (_mainCamera == null)
        {
            Debug.LogError($"{nameof(MainCamera)} not found on {nameof(EnemySounds)}");
        }
    }

    void Start()
    {
        _enemySoundSources[0].playOnAwake = false;
        _enemySoundSources[1].playOnAwake = false;
        _enemyVoiceSource.playOnAwake = false;

        _enemySoundSources[0].spatialBlend = 1.0f;
        _enemySoundSources[1].spatialBlend = 1.0f;
        _enemyVoiceSource.spatialBlend = 1.0f;

        _enemySoundSources[0].minDistance = 1.0f;
        _enemySoundSources[1].minDistance = 1.0f;
        _enemyVoiceSource.minDistance = 1.0f;

        _enemySoundSources[0].maxDistance = 10.0f;
        _enemySoundSources[1].maxDistance = 10.0f;
        _enemyVoiceSource.maxDistance = 10.0f;

        _enemySoundSources[0].rolloffMode = AudioRolloffMode.Linear;
        _enemySoundSources[1].rolloffMode = AudioRolloffMode.Linear;
        _enemyVoiceSource.rolloffMode = AudioRolloffMode.Linear;

        _enemySoundSources[0].dopplerLevel = 0.0f;
        _enemySoundSources[1].dopplerLevel = 0.0f;
        _enemyVoiceSource.dopplerLevel = 0.0f;
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
            EnemySoundGroups.Fly => FlySoundClips,
            EnemySoundGroups.Drag => DragSoundClips,
            _ => new AudioClip[] { },
        };

        if (!_mainCamera.IsWorldPositionVisible(_enemySoundSources[0].transform.position))
        {
            return;
        }

        if (0 < clips.Length)
        {
            AudioClip usedClip = clips[Random.Range(0, clips.Length)];

            var usedIndex = _lastSoundSourceIndex++ % _enemySoundSources.Length;

            var lowerVolume = soundType is EnemySoundGroups.Walk or EnemySoundGroups.Drag;

            _enemySoundSources[usedIndex].pitch = Random.Range(0.95f, 1.05f);
            _enemySoundSources[usedIndex].loop = false;
            _enemySoundSources[usedIndex].volume = lowerVolume ? 0.35f : 1.0f;
            _enemySoundSources[usedIndex].clip = usedClip;
            _enemySoundSources[usedIndex].Play();
        }
    }

    public void TryPlayVoiceSource(EnemyVoiceGroups soundType, bool forceSound = false)
    {
        var newLastVoiceTime = Time.time;

        if (!forceSound && Mathf.Abs(_lastVoiceTime - newLastVoiceTime) < 1.0f)
        {
            return;
        }

        if (!forceSound && !_mainCamera.IsWorldPositionVisible(_enemyVoiceSource.transform.position))
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

            _enemyVoiceSource.pitch = Random.Range(0.95f, 1.05f);
            _enemyVoiceSource.clip = usedClip;
            _enemyVoiceSource.Play();

            _lastVoiceTime = newLastVoiceTime;
        }
    }
}
