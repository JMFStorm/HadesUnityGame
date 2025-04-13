using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GruntBehaviour : EnemyBase
{
    public float MovementSpeed = 1.5f;
    public float AggroSpeed = 3.0f;
    public float AttackRange = 2.0f;
    public float DamageStunTime = 0.5f;

    public int MaxHealth = 4;

    public const float MaxSoundDistance = 14f;

    private Animator _animator;
    private Transform _groundCheck;
    private Transform _attackDamageZone;
    private Transform _attackDamageZoneRight;
    private Transform _enemyDamageZone;
    private Transform _enemyCollisionZoneHitbox2;
    private PlayerCharacter _playerCharacter;
    private MainCamera _mainCamera;
    private EnemySounds _soundEmitter;

    private Transform _aggroTarget;

    private LayerMask _groundFloorLayer;
    private LayerMask _wallLayer;
    private LayerMask _playerLayer;

    private EnemyState _state;

    private int _currentHealth = 4;
    private bool _facingLeft = false;
    private bool _isAggroed = false;
    private bool _isInDamageMode = false;
    private bool _attacking = false;

    private float _previousAlert = float.MinValue;
    private float _lastAttackTime = float.MinValue;

    private readonly float DetectionDistance = 8.0f;
    private readonly float _attackCooldown = 1.5f;

    private Coroutine _currentIdleVoiceCoroutine = null;
    private Coroutine _moveCoroutine = null;
    private Coroutine _attackCoroutine = null;
    private Coroutine _maxAttackTimerCoroutine = null;

    private enum CollisionTypes
    {
        None = 0,
        GroundEdge,
        WallHit
    }

    protected override void Awake()
    {
        base.Awake();

        if (!TryGetComponent(out _animator))
        {
            Debug.LogError($"{nameof(Animator)} not found on {nameof(GruntBehaviour)}");
        }

        if (!TryGetComponent(out _soundEmitter))
        {
            Debug.LogError($"{nameof(EnemySounds)} not found on {nameof(GruntBehaviour)}");
        }

        _enemyCollisionZoneHitbox2 = transform.Find("EnemyCollisionZoneHitbox2");

        if (_enemyCollisionZoneHitbox2 == null)
        {
            Debug.LogError($"EnemyCollisionZoneHitbox2 not found on {nameof(GruntBehaviour)}");
        }

        _groundFloorLayer = LayerMask.GetMask("Ground", "Platform");
        _wallLayer = LayerMask.GetMask("Ground", "EnvDamageZone");
        _playerLayer = LayerMask.GetMask("Character");

        _groundCheck = transform.Find("GroundCheck");

        if (_groundCheck == null)
        {
            Debug.LogError($"GroundCheck not found on {nameof(GruntBehaviour)}");
        }

        _attackDamageZone = transform.Find("AttackDamageZone");

        if (_attackDamageZone == null)
        {
            Debug.LogError($"_damageZoneTransform not found on {nameof(GruntBehaviour)}");
        }

        _attackDamageZoneRight = transform.Find("AttackDamageZoneRight");

        if (_attackDamageZoneRight == null)
        {
            Debug.LogError($"_damageZoneRigth not found on {nameof(GruntBehaviour)}");
        }

        _enemyDamageZone = transform.Find("EnemyDamageZone");

        if (_enemyDamageZone == null)
        {
            Debug.LogError($"EnemyDamageZone not found on {nameof(GruntBehaviour)}");
        }

        if (!TryGetComponent(out _spriteRenderer))
        {
            Debug.LogError($"{nameof(SpriteRenderer)} not found on child of {nameof(GruntBehaviour)}");
        }

        _material = _spriteRenderer.material;

        _playerCharacter = FindAnyObjectByType<PlayerCharacter>(FindObjectsInactive.Include);

        if (_playerCharacter == null)
        {
            Debug.LogError($"{nameof(PlayerCharacter)} not found on {nameof(GruntBehaviour)}");
        }

        _mainCamera = FindFirstObjectByType<MainCamera>();

        if (_mainCamera == null)
        {
            Debug.LogError($"{nameof(MainCamera)} not found on {nameof(GruntBehaviour)}");
        }
    }

    void Start()
    {
        _currentHealth = MaxHealth;
        _attackDamageZone.gameObject.SetActive(false);
        _attackDamageZoneRight.gameObject.SetActive(false);
        _state = EnemyState.NormalMoving;

        if (StartFacingLeft)
        {
            TurnAround();
        }
    }

    void Update()
    {
        if (_isPassive)
        {
            _animator.Play("GruntIdle1");
            return;
        }

        if (_isDead)
        {
            _animator.Play("GruntDead1");
            EndAttack();
            return;
        }

        if (!_isInDamageMode || !_isDead)
        {
            if (!_isAggroed)
            {
                TryNormalMovement();
                TryDetectPlayerAndAggro();
            }
            else if (_isAggroed)
            {
                TryAggroMovement();
            }
        }

        _spriteRenderer.flipX = !_facingLeft;

        if (_state == EnemyState.Passive)
        {
            _animator.Play("GruntIdle1");
        }
        else if (_state == EnemyState.NormalMoving)
        {
            // _animator.Play("GruntMove");
            _animator.Play("GruntIdle1");
        }
        else if (_state == EnemyState.AttackMoving)
        {
            // _animator.Play("GruntMove");
            _animator.Play("GruntIdle1");
        }

        if (_state == EnemyState.Passive || _state == EnemyState.NormalMoving)
        {
            TryInitIdleVoiceLoop();
        }
        else
        {
            StopIdleVoiceLoop();
        }

        SetDamageColor(_isInDamageMode);

        if (_attackDamageZone.gameObject.activeSelf)
        {
            var boxCollider = _attackDamageZone.GetComponent<BoxCollider2D>();
            DebugUtil.DrawRectangle((Vector2)boxCollider.transform.position, boxCollider.size, Color.red);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isDead)
        {
            return;
        }

        Vector2 collisionDirection = (transform.position - other.transform.position);

        if (other.gameObject.layer == LayerMask.NameToLayer("DamageZone") && other.gameObject.CompareTag("PlayerSword"))
        {
            TryRecieveDamage(collisionDirection);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (_isDead)
        {
            return;
        }

        Vector2 collisionDirection = (transform.position - other.transform.position);

        if (other.gameObject.layer == LayerMask.NameToLayer("EnvDamageZone"))
        {
            TryRecieveDamage(collisionDirection);
        }
    }

    public override void UpdateTeleportShaderEffect(float strength)
    {
        _spriteRenderer.flipX = !StartFacingLeft;
        _material.SetFloat("_Strength", strength);
    }

    void StopIdleVoiceLoop()
    {
        if (_currentIdleVoiceCoroutine != null)
        {
            StopCoroutine(_currentIdleVoiceCoroutine);
            _currentIdleVoiceCoroutine = null;
        }
    }

    void TryInitIdleVoiceLoop()
    {
        if (_currentIdleVoiceCoroutine == null)
        {
            _currentIdleVoiceCoroutine = StartCoroutine(IdleVoiceLoop());
        }
    }

    IEnumerator IdleVoiceLoop()
    {
        while (true)
        {
            var time = Random.Range(3f, 7f);

            yield return new WaitForSeconds(time);

            if (0.50f < Random.Range(0f, 1f))
            {
                _soundEmitter.TryPlayVoiceSource(EnemyVoiceGroups.Idle);
            }
        }
    }

    void TryStartAttackCoroutine()
    {
        if (_attackCoroutine == null)
        {
            _attackCoroutine = StartCoroutine(Attack());
        }
    }

    void StopAttackCoroutine()
    {
        if (_attackCoroutine != null)
        {
            StopCoroutine(_attackCoroutine);
            _attackCoroutine = null;
        }
    }

    void TryAggroMovement()
    {
        if (_state != EnemyState.AttackMoving)
        {
            return;
        }

        var distanceFromTarget = Mathf.Abs(transform.position.x - _aggroTarget.position.x);

        DebugUtil.DrawCircle(new Vector3(_aggroTarget.position.x, transform.position.y + 0.5f, 0f), 0.25f, Color.red);

        var startLine = new Vector3(transform.position.x, transform.position.y + 0.5f, 0f);
        var endLine = new Vector3(transform.position.x + (_facingLeft ? -AttackRange : AttackRange), transform.position.y + 0.5f, 0f);

        Debug.DrawLine(startLine, endLine, Color.yellow);

        if (distanceFromTarget <= AttackRange)
        {
            var yDist = Mathf.Abs(transform.position.y - _aggroTarget.position.y);

            if (yDist < 1.75f)
            {
                DeactivateMaxAggroTimer();
                TryStartAttackCoroutine();
            }
            else
            {
                StartCoroutine(ResetAndTurnAround(1.5f, false));
                _soundEmitter.TryPlayVoiceSource(EnemyVoiceGroups.Idle);
                return;
            }
        }
        else
        {
            var collisions = GetRaycastCollisions();

            if (collisions == CollisionTypes.GroundEdge || collisions == CollisionTypes.WallHit)
            {
                // NOTE: End aggro on collisions
                StartCoroutine(ResetAndTurnAround(1.5f, collisions == CollisionTypes.WallHit));
                _soundEmitter.TryPlayVoiceSource(EnemyVoiceGroups.Idle);
                return;
            }

            var targetDir = transform.position.x - _aggroTarget.position.x;

            var targetDirRight = targetDir < 0f;
            var facingXdir = GetXDirection();

            if (targetDirRight && facingXdir < 0f || !targetDirRight && 0f < facingXdir)
            {
                TurnAround();
            }

            float newMovement = _facingLeft ? -AggroSpeed : AggroSpeed;
            _rigidBody.linearVelocity = new Vector2(newMovement, _rigidBody.linearVelocity.y);
        }
    }

    IEnumerator ResetAndTurnAround(float passiveLength, bool turnAroundFirst)
    {
        DeactivateMaxAggroTimer();

        if (turnAroundFirst)
        {
            TurnAround();
        }

        _state = EnemyState.Passive;

        yield return new WaitForSeconds(passiveLength);

        if (!turnAroundFirst)
        {
            TurnAround();
        }

        _state = EnemyState.NormalMoving;

        _isAggroed = false;
    }

    void PlaySoundAttackSwing()
    {
        _soundEmitter.TryPlaySoundSource(EnemySoundGroups.Attack);
    }

    void PlaySoundAttackHit()
    {
        _soundEmitter.TryPlaySoundSource(EnemySoundGroups.AttackMiss);
    }

    private float _lastChargeVoice = 0f;

    IEnumerator Attack()
    {
        _attacking = true;
        _state = EnemyState.Attacking;

        _animator.Play("GruntAttack1", 0, 0f);

        _soundEmitter.TryPlaySoundSource(EnemySoundGroups.AttackCharge);

        if (3.5f < Mathf.Abs(_lastChargeVoice - Time.time))
        {
            _lastChargeVoice = Time.time;
            _soundEmitter.TryPlayVoiceSource(EnemyVoiceGroups.AttackCharge);
        }

        yield return new WaitForSeconds(9f / 10f);

        if (_isDead)
        {
            EndAttack();
            yield break;
        }

        _lastAttackTime = Time.time;

        if (_facingLeft)
        {
            _enemyCollisionZoneHitbox2.localScale = new Vector3(1f, 1f, 1f);
            _attackDamageZone.gameObject.SetActive(true);
        }
        else
        {
            _enemyCollisionZoneHitbox2.localScale = new Vector3(-1f, 1f, 1f);
            _attackDamageZoneRight.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(4f / 10f);

        if (_isDead)
        {
            EndAttack();
            yield break;
        }

        _attackDamageZone.gameObject.SetActive(false);
        _attackDamageZoneRight.gameObject.SetActive(false);

        yield return new WaitForSeconds(7f / 10f);

        EndAttack();
    }

    void EndAttack()
    {
        _attacking = false;
        _attackDamageZone.gameObject.SetActive(false);
        _attackDamageZoneRight.gameObject.SetActive(false);
        _state = EnemyState.NormalMoving;
        _isAggroed = false;
        _attackCoroutine = null;
    }

    void TryRecieveDamage(Vector2 damageDir)
    {
        if (_isInDamageMode || _state == EnemyState.Dead || _isDead)
        {
            return;
        }

        if (!_attacking)
        {
            ApplyDamageKnockback(damageDir);
        }

        _currentHealth -= _playerCharacter.HasShadowPowers ? 2 : 1;

        if (_currentHealth <= 0)
        {
            StopAllCoroutines();
            ActivateDeathAndDestroy(damageDir);
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
        _isInDamageMode = true;

        _soundEmitter.TryPlayVoiceSource(EnemyVoiceGroups.Damage, true);
        _soundEmitter.TryPlaySoundSource(EnemySoundGroups.DamageTaken);

        yield return new WaitForSeconds(duration);

        _isInDamageMode = false;
    }

    void ActivateDeathAndDestroy(Vector2 damageDir)
    {
        _isDead = true;
        _spriteRenderer.enabled = false;
        _enemyCollider.enabled = false;
        // _enemyCollisionZoneHitbox2.gameObject.SetActive(false);
        // _enemyCollisionZone.gameObject.SetActive(false);
        _enemyDamageZone.gameObject.SetActive(false);
        _attackDamageZone.gameObject.SetActive(false);
        _attackDamageZoneRight.gameObject.SetActive(false);

        _soundEmitter.TryPlayVoiceSource(EnemyVoiceGroups.Death, true);
        _soundEmitter.TryPlaySoundSource(EnemySoundGroups.DamageTaken);

        _state = EnemyState.Dead;
        // _animator.Play("GruntDeath", 0, 0f);

        SetDead();
    }

    private void ApplyDamageKnockback(Vector2 knockbackDir)
    {
        var knockbackDirForce = new Vector2(knockbackDir.normalized.x, 6.5f);
        _rigidBody.linearVelocity = knockbackDirForce * 0.5f;
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

    bool SeesPlayer(Vector2 direction)
    {
        var origin = (Vector2)transform.position + new Vector2(0f, 0.5f);
        var length = DetectionDistance * 1.5f;
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, length, _seesTargetLayerMask);

        Debug.DrawRay(origin, length * direction, Color.blue);

        if (hit.collider != null)
        {
            return hit.collider.CompareTag("Player");
        }

        return false;
    }

    void TryDetectPlayerAndAggro()
    {
        if (!(_state == EnemyState.Passive || _state == EnemyState.NormalMoving) || _playerCharacter.IsDead())
        {
            return;
        }

        if (Mathf.Abs(Time.time - _lastAttackTime) < _attackCooldown)
        {
            return;
        }

        const float detectionDistance = 8.0f;
        Vector2 detectionBoxSize = new(detectionDistance, 1.5f);

        var detectionOffset = (detectionDistance * 0.22f) * Vector2.right;
        Vector2 boxPosition = (Vector2)_enemyCollider.bounds.center + (_facingLeft ? -detectionOffset : detectionOffset);

        Collider2D hit = Physics2D.OverlapBox(boxPosition, detectionBoxSize, 0f, _playerLayer);

        if (hit != null && hit.CompareTag("Player")
            && (SeesPlayer(new Vector2(1f, 0f)) || SeesPlayer(new Vector2(-1f, 0f))))
        {
            _aggroTarget = hit.gameObject.transform;
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

        if (_moveCoroutine == null)
        {
            _moveCoroutine = StartCoroutine(NormalMovementCoroutine());
        }
    }

    void TryStopNormalMovement()
    {
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }
    }

    IEnumerator NormalMovementCoroutine()
    {
        while (true)
        {
            float moveElapsed = 0f;
            float moveTime = Random.Range(4.5f, 6.5f);

            _state = EnemyState.NormalMoving;

            while (moveElapsed < moveTime)
            {
                var collisions = GetRaycastCollisions();

                if (collisions == CollisionTypes.GroundEdge || collisions == CollisionTypes.WallHit)
                {
                    StartCoroutine(ResetAndTurnAround(1.5f, collisions == CollisionTypes.WallHit));
                    TryStopNormalMovement();
                    yield break;
                }

                float newMovement = _facingLeft ? -MovementSpeed : MovementSpeed;
                Debug.DrawRay(_groundCheck.position, Vector2.right * GetXDirection(), _facingLeft ? Color.green : Color.red);
                _rigidBody.linearVelocity = new Vector2(newMovement, _rigidBody.linearVelocity.y);

                moveElapsed += Time.deltaTime;
                yield return null;
            }

            float idleElapsed = 0f;
            float idleTime = Random.Range(1.5f, 3.0f);

            _state = EnemyState.Passive;

            while (idleElapsed < idleTime)
            {
                idleElapsed += Time.deltaTime;
                yield return null;
            }
        }
    }

    IEnumerator ActivateAggro()
    {
        _isAggroed = true;
        _state = EnemyState.Passive;

        TryStopNormalMovement();

        var targetDir = transform.position.x - _aggroTarget.position.x;

        var targetDirRight = targetDir < 0f;
        var facingXdir = GetXDirection();

        if (targetDirRight && facingXdir < 0f || !targetDirRight && 0f < facingXdir)
        {
            TurnAround();
        }

        const float alertTimeBetween = 12f;
        var newTime = Time.time;

        if (alertTimeBetween < Mathf.Abs(newTime - _previousAlert))
        {
            _soundEmitter.TryPlayVoiceSource(EnemyVoiceGroups.Alert, true);
            _previousAlert = newTime;
        }

        yield return new WaitForSeconds(0.5f);

        if (_isDead)
        {
            yield return null;
        }

        _state = EnemyState.AttackMoving;

        ActivateMaxAggroTimer();
    }

    void DeactivateMaxAggroTimer()
    {
        if (_maxAttackTimerCoroutine != null)
        {
            StopCoroutine(_maxAttackTimerCoroutine);
            _maxAttackTimerCoroutine = null;
        }
    }

    void ActivateMaxAggroTimer()
    {
        DeactivateMaxAggroTimer();
        _maxAttackTimerCoroutine = StartCoroutine(AttackMoveMaxTimer());
    }

    IEnumerator AttackMoveMaxTimer()
    {
        yield return new WaitForSeconds(5.0f);

        if (_isAggroed && _state == EnemyState.AttackMoving && !_isDead)
        {
            _isAggroed = false;
            _state = EnemyState.NormalMoving;
        }
    }

    void TurnAround()
    {
        _facingLeft = !_facingLeft;

        var newCheckerX = _groundCheck.localPosition.x * -1;
        _groundCheck.localPosition = new(newCheckerX, _groundCheck.localPosition.y, _groundCheck.localPosition.z);

        // var newDamageZoneX = _attackDamageZone.localPosition.x * -1;
        // _attackDamageZone.localPosition = new(newDamageZoneX, _attackDamageZone.localPosition.y, _attackDamageZone.localPosition.z);
    }
}