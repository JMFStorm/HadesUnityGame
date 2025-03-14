using System.Collections;
using UnityEngine;

public enum EnemyState
{
    Passive = 0,
    Alert,
    AttackMoving,
    Attacking,
    NormalMoving,
    HitTaken
}

public enum MookSoundGroups
{
    Attack,
    Hit,
    Walk
}

public enum MookVoiceGroups
{
    // Voices
    Alert = 0,
    Damage,
    Death,
    Idle,
    Attack
}


[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(AudioSource))]
public class GroundEnemyBehaviour : MonoBehaviour
{
    AudioSource _enemySoundSource;
    AudioSource _enemyVoiceSource;

    public AudioClip[] AlertVoiceClips;
    public AudioClip[] DamageVoiceClips;
    public AudioClip[] DeathVoiceClips;
    public AudioClip[] IdleVoiceClips;
    public AudioClip[] AttackVoiceClips;

    public AudioClip[] AttackSoundClips;
    public AudioClip[] HitSoundClips;
    public AudioClip[] WalkSoundClips;

    public float MovementSpeed = 1.5f;
    public float AggroSpeed = 3.0f;
    public float AttackRange = 2.0f;
    public float DamageStunTime = 0.5f;

    public int MaxHealth = 4;

    private Transform _groundCheck;
    private Transform _attackDamageZone;
    private Transform _enemyDamageZone;
    private BoxCollider2D _enemyCollider;
    private Rigidbody2D _rigidBody;
    private SpriteRenderer _spriteRenderer;

    private Vector2 _aggroTarget;

    private LayerMask _groundFloorLayer;
    private LayerMask _wallLayer;
    private LayerMask _playerLayer;

    private EnemyState _state;

    private float _lastVoiceTime = 0;
    private int _currentHealth = 4;
    private bool _facingLeft = false;
    private bool _isDead = false;

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
    }

    void Start()
    {
        _currentHealth = MaxHealth;
        _attackDamageZone.gameObject.SetActive(false);
        _state = EnemyState.NormalMoving;

        StartCoroutine(MovementLoop());
    }

    void Update()
    {
        if (!_isDead)
        {
            TryNormalMovement();
            TryAggroMovement();
            DetectPlayerAndAggro();
        }

        _spriteRenderer.color = GetEnemyStateColor(_state);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("DamageZone"))
        {
            Vector2 collisionDirection = (transform.position - other.transform.position);

            if (other.gameObject.CompareTag("PlayerSword"))
            {
                RecieveDamage(collisionDirection);
            }
        }
    }

    Color GetEnemyStateColor(EnemyState state)
    {
        return state switch
        {
            EnemyState.NormalMoving => Color.green,
            EnemyState.Alert => Color.yellow,
            EnemyState.AttackMoving => Color.red,
            EnemyState.Attacking => Color.blue,
            EnemyState.Passive => Color.gray,
            EnemyState.HitTaken => Color.cyan,
            _ => throw new System.NotImplementedException(),
        };
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

        TryPlayVoiceSource(MookVoiceGroups.Idle);

        _state = EnemyState.NormalMoving;
    }

    IEnumerator Attack()
    {
        _state = EnemyState.Attacking;

        _attackDamageZone.gameObject.SetActive(true);

        TryPlayVoiceSource(MookVoiceGroups.Attack);
        PlaySoundSource(MookSoundGroups.Attack);

        yield return new WaitForSeconds(1.0f);

        if (_state != EnemyState.Attacking)
        {
            yield return null;
        }

        _spriteRenderer.color = Color.white;
        _state = EnemyState.NormalMoving;
        _attackDamageZone.gameObject.SetActive(false);

        StartCoroutine(MovementLoop());
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
            _isDead = true;
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
        TryPlayVoiceSource(MookVoiceGroups.Damage);
        _state = EnemyState.HitTaken;

        yield return new WaitForSeconds(duration);

        _state = EnemyState.NormalMoving;
    }

    void ActivateDeathAndDestroy()
    {
        TryPlayVoiceSource(MookVoiceGroups.Death, true);
        _enemyDamageZone.gameObject.SetActive(false);
        _spriteRenderer.enabled = false;
        Destroy(gameObject, 2.5f);
    }

    private void ApplyDamageKnockback(Vector2 knockbackDir)
    {
        var knockbackDirForce = _rigidBody.mass * 4.5f * knockbackDir.normalized;
        _rigidBody.AddForce(knockbackDirForce, ForceMode2D.Force);
    }

    CollisionTypes GetRaycastCollisions()
    {
        var direction = GetXDirection();

        const float groundRayLength = 0.25f;
        RaycastHit2D groundHit = Physics2D.Raycast(_groundCheck.position, Vector2.down, groundRayLength, _groundFloorLayer);

        Debug.DrawRay(_groundCheck.position, Vector2.down * groundRayLength, Color.green);

        float wallRayLength = (_enemyCollider.size.x / 2) + 0.1f;
        RaycastHit2D wallHit = Physics2D.Raycast(transform.position, Vector2.right * direction, wallRayLength, _wallLayer);
        Debug.DrawRay(transform.position, direction * wallRayLength * Vector2.right, wallHit.collider ? Color.magenta : Color.cyan);

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
        if (_state != EnemyState.NormalMoving)
        {
            return;
        }

        const float detectionDistance = 6.0f;
        Vector2 detectionBoxSize = new(detectionDistance, 1.5f);

        var detectionOffset = (detectionDistance * 0.25f) * Vector2.right;
        Vector2 boxPosition = (Vector2)transform.position + (_facingLeft ? -detectionOffset : detectionOffset);

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

        TryPlayVoiceSource(MookVoiceGroups.Alert);

        yield return new WaitForSeconds(0.5f);

        if (_state != EnemyState.Alert)
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

        if (_state == EnemyState.AttackMoving)
        {
            _state = EnemyState.NormalMoving;
        }
    }

    IEnumerator MovementLoop()
    {
        while (true)
        {
            float moveTime = Random.Range(2f, 8f);
            yield return new WaitForSeconds(moveTime);

            if (_state != EnemyState.NormalMoving)
            {
                yield return null;
            }

            float waitTime = Random.Range(2f, 4f);
            yield return new WaitForSeconds(waitTime);

            if (_state != EnemyState.NormalMoving)
            {
                yield return null;
            }

            if (0.75f < Random.Range(0f, 1f))
            {
                TurnAround();
            }
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

    void PlaySoundSource(MookSoundGroups soundType)
    {
        AudioClip[] clips = soundType switch
        {
            MookSoundGroups.Attack => AttackSoundClips,
            MookSoundGroups.Hit => HitSoundClips,
            MookSoundGroups.Walk => WalkSoundClips,
            _ => new AudioClip[] { },
        };

        if (0 < clips.Length)
        {
            AudioClip usedClip = clips[Random.Range(0, clips.Length)];

            _enemySoundSource.clip = usedClip;
            _enemySoundSource.Play();
        }
        else
        {
            Debug.LogWarning($"Error playing PlaySoundSource in {nameof(GroundEnemyBehaviour)}");
        }
    }

    void TryPlayVoiceSource(MookVoiceGroups soundType, bool forceSound = false)
    {
        var newLastVoiceTime = Time.time;

        if (!forceSound && Mathf.Abs(_lastVoiceTime - newLastVoiceTime) < 1.0f)
        {
            return;
        }

        AudioClip[] clips = soundType switch
        {
            MookVoiceGroups.Alert => AlertVoiceClips,
            MookVoiceGroups.Damage => DamageVoiceClips,
            MookVoiceGroups.Death => DeathVoiceClips,
            MookVoiceGroups.Idle => IdleVoiceClips,
            MookVoiceGroups.Attack => AttackVoiceClips,
            _ => new AudioClip[] { },
        };

        if (0 < clips.Length)
        {
            AudioClip usedClip = clips[Random.Range(0, clips.Length)];

            _enemySoundSource.clip = usedClip;
            _enemySoundSource.Play();

            _lastVoiceTime = newLastVoiceTime;
        }
        else
        {
            Debug.LogWarning($"Error playing PlayVoiceSource in {nameof(GroundEnemyBehaviour)}");
        }
    }
}