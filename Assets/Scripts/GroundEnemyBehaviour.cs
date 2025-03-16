using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public enum EnemyState
{
    Passive = 0,
    Alert,
    AttackMoving,
    Attacking,
    NormalMoving,
    HitTaken
}

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


[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(AudioSource))]
public class GroundEnemyBehaviour : MonoBehaviour
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

    public float MovementSpeed = 1.5f;
    public float AggroSpeed = 3.0f;
    public float AttackRange = 2.0f;
    public float DamageStunTime = 0.5f;
    public float AttackChargeTime = 0.8f;
    public float ShadowOutlineThreshold = 0.1f;
    public float NormalWalkFrequency = 0.65f;
    public float AggroWalkFrequency = 0.45f;

    public bool IsShadowVariant = false;
    public int MaxHealth = 4;

    public const float MaxSoundDistance = 14f;

    private Transform _groundCheck;
    private Transform _attackDamageZone;
    private Transform _enemyDamageZone;
    private Transform _particleFx;
    private BoxCollider2D _enemyCollider;
    private Rigidbody2D _rigidBody;
    private SpriteRenderer _spriteRenderer;
    private Material _material;
    private ParticleSystem _smokeEffect;
    private PlayerCharacter _playerCharacter;
    private MainCamera _mainCamera;

    private Vector2 _aggroTarget;

    private LayerMask _groundFloorLayer;
    private LayerMask _wallLayer;
    private LayerMask _playerLayer;

    private EnemyState _state;

    private int _currentHealth = 4;
    private bool _facingLeft = false;
    private bool _isDead = false;
    private bool _attackHitPlayer = false;

    private Coroutine _currentWalkCycleCoroutine = null;

    private enum CollisionTypes
    {
        None = 0,
        GroundEdge,
        WallHit
    }

    void Awake()
    {
        if (!TryGetComponent(out _enemyCollider))
        {
            Debug.LogError($"{nameof(BoxCollider2D)} not found on {nameof(GroundEnemyBehaviour)}");
        }

        if (!TryGetComponent(out _rigidBody))
        {
            Debug.LogError($"{nameof(Rigidbody2D)} not found on {nameof(GroundEnemyBehaviour)}");
        }

        _groundFloorLayer = LayerMask.GetMask("Ground", "Platform");
        _wallLayer = LayerMask.GetMask("Ground", "EnvDamageZone");
        _playerLayer = LayerMask.GetMask("Character");

        _groundCheck = transform.Find("GroundCheck");

        if (_groundCheck == null)
        {
            Debug.LogError($"GroundCheck not found on {nameof(GroundEnemyBehaviour)}");
        }

        _attackDamageZone = transform.Find("AttackDamageZone");

        if (_attackDamageZone == null)
        {
            Debug.LogError($"_damageZoneTransform not found on {nameof(GroundEnemyBehaviour)}");
        }

        _enemyDamageZone = transform.Find("EnemyDamageZone");

        if (_enemyDamageZone == null)
        {
            Debug.LogError($"EnemyDamageZone not found on {nameof(AeroBehaviour)}");
        }

        var sprite = transform.Find("Sprite");

        if (!sprite.TryGetComponent(out _spriteRenderer))
        {
            Debug.LogError($"{nameof(SpriteRenderer)} not found on child of {nameof(GroundEnemyBehaviour)}");
        }

        _material = _spriteRenderer.material;

        _particleFx = transform.Find("ParticleFX");

        if (!_particleFx.TryGetComponent(out _smokeEffect))
        {
            Debug.LogError($"{nameof(ParticleSystem)} not found on child of {nameof(GroundEnemyBehaviour)}");
        }

        var audioSources = GetComponents<AudioSource>();

        if (audioSources.Length != 2)
        {
            Debug.LogError($"Expected 2 {nameof(AudioSource)} components on {nameof(GroundEnemyBehaviour)}, but found {audioSources.Length}");
        }
        else
        {
            _enemySoundSource = audioSources[0];
            _enemyVoiceSource = audioSources[1];
        }

        _playerCharacter = FindFirstObjectByType<PlayerCharacter>();

        if (_playerCharacter == null)
        {
            Debug.LogError($"{nameof(PlayerCharacter)} not found on {nameof(GroundEnemyBehaviour)}");
        }

        _mainCamera = FindFirstObjectByType<MainCamera>();

        if (_mainCamera == null)
        {
            Debug.LogError($"{nameof(MainCamera)} not found on {nameof(PlayerCharacter)}");
        }
    }

    void Start()
    {
         _material.SetFloat("_IsShadowVariant", IsShadowVariant ? 1f : 0f);
         _material.SetFloat("_ShadowOutlineThreshold", ShadowOutlineThreshold);

        if (IsShadowVariant)
        {
            _particleFx.gameObject.SetActive(true);
            _smokeEffect.Play();
        }
        else
        {
            _smokeEffect.Stop();
            _particleFx.gameObject.SetActive(false);
        }

        _currentHealth = MaxHealth;
        _attackDamageZone.gameObject.SetActive(false);
        _state = EnemyState.NormalMoving;
    }

    void Update()
    {
        if (!_isDead)
        {
            TryNormalMovement();
            TryAggroMovement();
            DetectPlayerAndAggro();

            if (_state == EnemyState.NormalMoving)
            {
                TryInitWalkCycleAudio(NormalWalkFrequency);
            }
            else if (_state == EnemyState.AttackMoving)
            {
                TryInitWalkCycleAudio(AggroWalkFrequency);
            }
            else
            {
                StopWalkCycleAudio();
            }
        }

        _spriteRenderer.flipX = !_facingLeft;
        _spriteRenderer.color = GetEnemyStateColor(_state);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Vector2 collisionDirection = (transform.position - other.transform.position);

        if (other.gameObject.layer == LayerMask.NameToLayer("DamageZone") && other.gameObject.CompareTag("PlayerSword")
            || other.gameObject.layer == LayerMask.NameToLayer("EnvDamageZone"))
        {
            RecieveDamage(collisionDirection);
        }

        if (_attackDamageZone.gameObject.activeSelf && other.gameObject.CompareTag("Player") && !_attackHitPlayer)
        {
            Debug.Log("Hit player!");
            _attackHitPlayer = true;
        }
    }

    Color GetEnemyStateColor(EnemyState state)
    {
        return state switch
        {
            EnemyState.Alert => Color.yellow,
            EnemyState.AttackMoving => Color.yellow,
            EnemyState.HitTaken => Color.red,
            _ => Color.white,
        };
    }

    void StopWalkCycleAudio()
    {
        if (_currentWalkCycleCoroutine != null)
        {
            StopCoroutine(_currentWalkCycleCoroutine);
            _currentWalkCycleCoroutine = null;
        }
    }

    void TryInitWalkCycleAudio(float walkFrequency)
    {
        if (_currentWalkCycleCoroutine == null)
        {
            Debug.Log("StartCoroutine");
            _currentWalkCycleCoroutine = StartCoroutine(WalkCycleAudio(walkFrequency));
        }
    }

    void TryAggroMovement()
    {
        if (_state != EnemyState.AttackMoving)
        {
            return;
        }

        var distanceFromTarget = Mathf.Abs(transform.position.x - _aggroTarget.x);

        if (distanceFromTarget <= AttackRange)
        {
            StartCoroutine(Attack());
        }
        else
        {
            var collisions = GetRaycastCollisions();

            if (collisions == CollisionTypes.GroundEdge || collisions == CollisionTypes.WallHit)
            {
                // NOTE: End aggro on collisions
                StartCoroutine(ResetAndTurnAround(1.5f));
                return;
            }

            float newMovement = _facingLeft ? -AggroSpeed : AggroSpeed;
            _rigidBody.linearVelocity = new Vector2(newMovement, _rigidBody.linearVelocity.y);
        }
    }

    IEnumerator ResetAndTurnAround(float passiveLength)
    {
        Debug.Log("Set to passive, ResetAndTurnAround()");
        _state = EnemyState.Passive;

        yield return new WaitForSeconds(passiveLength);

        if (_state != EnemyState.Passive)
        {
            yield return null;
        }

        TurnAround();

        if (0.50f < Random.Range(0f, 1f))
        {
            TryPlayVoiceSource(EnemyVoiceGroups.Idle);
        }

        _state = EnemyState.NormalMoving;
    }

    IEnumerator Attack()
    {
        _state = EnemyState.Attacking;
        _attackHitPlayer = false;

        TryPlayVoiceSource(EnemyVoiceGroups.AttackCharge);
        TryPlaySoundSource(EnemySoundGroups.AttackCharge);

        yield return new WaitForSeconds(AttackChargeTime);

        if (_isDead)
        {
            yield return null;
        }

        _attackDamageZone.gameObject.SetActive(true);

        TryPlayVoiceSource(EnemyVoiceGroups.Attack);
        TryPlaySoundSource(EnemySoundGroups.Attack);

        yield return new WaitForSeconds(0.20f);

        if (_state != EnemyState.Attacking || _isDead)
        {
            yield return null;
        }

        if (!_attackHitPlayer)
        {
            TryPlaySoundSource(EnemySoundGroups.AttackMiss);
        }

        _spriteRenderer.color = Color.white;
        _state = EnemyState.NormalMoving;
        _attackDamageZone.gameObject.SetActive(false);
    }

    void RecieveDamage(Vector2 damageDir)
    {
        if (_state == EnemyState.HitTaken)
        {
            return;
        }

        ApplyDamageKnockback(damageDir);

        _currentHealth -= 1;

        if (_currentHealth <= 0)
        {
            ActivateDeathAndDestroy();
        }
        else
        {
            if (!_facingLeft && 0f < damageDir.x || _facingLeft && damageDir.x < 0f)
            {
                TurnAround();
            }

            StartCoroutine(ActivateDamageTakenTime(DamageStunTime));
        }
    }

    private IEnumerator ActivateDamageTakenTime(float duration)
    {
        TryPlayVoiceSource(EnemyVoiceGroups.Damage, true);
        TryPlaySoundSource(EnemySoundGroups.DamageTaken);
        _state = EnemyState.HitTaken;

        yield return new WaitForSeconds(duration);

        _state = EnemyState.NormalMoving;
    }

    void ActivateDeathAndDestroy()
    {
        _isDead = true;
        _smokeEffect.Stop();
        TryPlayVoiceSource(EnemyVoiceGroups.Death, true);
        _enemyDamageZone.gameObject.SetActive(false);
        _spriteRenderer.enabled = false;
        Destroy(gameObject, 2.5f);
    }

    private void ApplyDamageKnockback(Vector2 knockbackDir)
    {
        var knockbackDirForce = new Vector2(knockbackDir.normalized.x, 6.5f);
        _rigidBody.linearVelocity = knockbackDirForce;
    }

    CollisionTypes GetRaycastCollisions()
    {
        var direction = GetXDirection();

        const float groundRayLength = 0.25f;
        RaycastHit2D groundHit = Physics2D.Raycast(_groundCheck.position, Vector2.down, groundRayLength, _groundFloorLayer);

        float wallRayLength = (_enemyCollider.size.x / 2) + 0.5f;
        RaycastHit2D wallHit = Physics2D.Raycast(_enemyCollider.bounds.center, Vector2.right * direction, wallRayLength, _wallLayer);

        Debug.DrawRay(_groundCheck.position, Vector2.down * groundRayLength, Color.green);
        Debug.DrawRay(_enemyCollider.bounds.center, direction * wallRayLength * Vector2.right, wallHit.collider ? Color.magenta : Color.cyan);

        if (wallHit.collider)
        {
            return CollisionTypes.WallHit;
        }

        if (!groundHit.collider)
        {
            return CollisionTypes.GroundEdge;
        }

        return CollisionTypes.None;
    }

    float GetXDirection()
    {
        return _facingLeft ? -1f : 1f;
    }

    void DetectPlayerAndAggro()
    {
        if (_state != EnemyState.NormalMoving || _playerCharacter.IsDead())
        {
            return;
        }

        const float detectionDistance = 6.0f;
        Vector2 detectionBoxSize = new(detectionDistance, 1.5f);

        var detectionOffset = (detectionDistance * 0.25f) * Vector2.right;
        Vector2 boxPosition = (Vector2)_enemyCollider.bounds.center + (_facingLeft ? -detectionOffset : detectionOffset);

        Collider2D hit = Physics2D.OverlapBox(boxPosition, detectionBoxSize, 0f, _playerLayer);

        if (hit != null && hit.CompareTag("Player"))
        {
            _aggroTarget = hit.gameObject.transform.position;
            StartCoroutine(ActivateAggro());
            DebugUtil.DrawRectangle(boxPosition, detectionBoxSize, Color.red);
        }
        else
        {
            DebugUtil.DrawRectangle(boxPosition, detectionBoxSize, Color.green);
        }
    }

    void TryNormalMovement()
    {
        if (_state != EnemyState.NormalMoving)
        {
            return;
        }

        var collisions = GetRaycastCollisions();

        if (collisions == CollisionTypes.GroundEdge || collisions == CollisionTypes.WallHit)
        {
            StartCoroutine(ResetAndTurnAround(1.0f));
        }

        float newMovement = _facingLeft ? -MovementSpeed : MovementSpeed;
        Debug.DrawRay(_groundCheck.position, Vector2.right * GetXDirection(), _facingLeft ? Color.green : Color.red);
        _rigidBody.linearVelocity = new Vector2(newMovement, _rigidBody.linearVelocity.y);
    }

    IEnumerator WalkCycleAudio(float frequency)
    {
        while (true)
        {
            yield return new WaitForSeconds(frequency);

            TryPlaySoundSource(EnemySoundGroups.Walk);
        }
    }

    IEnumerator ActivateAggro()
    {
        var targetDir = transform.position.x - _aggroTarget.x;

        var targetDirRight = targetDir < 0f;
        var facingXdir = GetXDirection();

        if (targetDirRight && facingXdir < 0f || !targetDirRight && 0f < facingXdir)
        {
            TurnAround();
        }

        _spriteRenderer.color = Color.yellow;
        _state = EnemyState.Alert;

        TryPlayVoiceSource(EnemyVoiceGroups.Alert);

        yield return new WaitForSeconds(0.5f);

        if (_state != EnemyState.Alert || _isDead)
        {
            yield return null;
        }

        _spriteRenderer.color = Color.red;
        _state = EnemyState.AttackMoving;
        StartCoroutine(AttackMoveMaxTimer());
    }

    IEnumerator AttackMoveMaxTimer()
    {
        yield return new WaitForSeconds(3.5f);

        if (_state == EnemyState.AttackMoving && !_isDead)
        {
            _state = EnemyState.NormalMoving;
        }
    }

    void TurnAround()
    {
        _facingLeft = !_facingLeft;

        var newCheckerX = _groundCheck.localPosition.x * -1;
        _groundCheck.localPosition = new(newCheckerX, _groundCheck.localPosition.y, _groundCheck.localPosition.z);

        var newDamageZoneX = _attackDamageZone.localPosition.x * -1;
        _attackDamageZone.localPosition = new(newDamageZoneX, _attackDamageZone.localPosition.y, _attackDamageZone.localPosition.z);
    }

    void TryPlaySoundSource(EnemySoundGroups soundType)
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

    void TryPlayVoiceSource(EnemyVoiceGroups soundType, bool forceSound = false)
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