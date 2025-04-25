using System.Collections;
using UnityEngine;

public enum AeroSounds
{
    Hit = 0,
    Death,
    Preattack,
    ProjectileLaunch,
    Wings,
}

public class AeroBehaviour : EnemyBase
{
    public AudioClip[] AudioClips;

    public float chaseRadius = 5f; // Radius within which the enemy will chase the player
    public float attackRadius = 2f; // Radius within which the enemy will attack
    public float GiveUpRadius = 10f;
    public float speed = 3f; // Speed of the enemy movement
    public float attackOffset = 2f; // Offset for the attack position
    public float KeepXDistanceFromTarget = 2f;
    public float KeepYDistanceFromTarget = 1f;
    public float DamageInvulnerabilityTime = 0.25f;
    public float ProjectileLoadTime = 0.75f;
    public float shootingCooldown = 3f; // Cooldown time for shooting
    public float waveAmplitude = 5f; // Height of the wave
    public float waveFrequency = 2f; // Speed of the wave movement

    public int EnemyHealth = 3;

    public GameObject projectilePrefab; // Reference to the projectile prefab

    private Animator _animator;
    private Transform _attackTarget; // Reference to the player's transform
    private Transform _enemyDamageZone;
    private Transform _projectileStart;
    private AudioSource _audioSource;
    private MainCamera _mainCamera;
    private PlayerCharacter _player;
    private EnemySounds _soundEmitter;

    private Coroutine _flapWings = null;
    private Coroutine _attackMove = null;

    private bool _attackInterrupted = false;
    private bool _hasGivenUp = false;
    private bool _targetSideIsLeft = false;
    private bool isChasing = false;
    private bool _isAttacking = false;
    private bool _hasDamageInvulnerability = false;
    private float _lastShotTime; // Time of the last shot
    private float _notSeeingPlayerTime = 0;

    private Vector2 _flyTarget = new();
    private Vector2 _initialSpawnPosition = new();
    private Vector2 _targetDir = new();

    private float _directionX = 1;
    private float _waveOffset = 0.0f;
    private float _currentDistanceToTarget = 0;
    private float _targetDistance = 0;
    private float _targetDistanceX = 0;
    private float _usedSpeed = 0;

    private readonly Color _projectileColor1 = new(0.55f, 0.75f, 0f, 1f);
    private readonly Color _projectileColor2 = new(0.5f, 0.0f, 0.0f, 1f);

    private int _currentHealth = 3;
    private int _groundLayerMask;

    private readonly int _animationFPS = 14;

    protected override void Awake()
    {
        base.Awake();

        if (!TryGetComponent(out _soundEmitter))
        {
            Debug.LogError($"{nameof(EnemySounds)} not found on {nameof(AeroBehaviour)}");
        }

        if (!TryGetComponent(out _spriteRenderer))
        {
            Debug.LogError($"{nameof(SpriteRenderer)} not found on {nameof(AeroBehaviour)}");
        }

        _material = _spriteRenderer.material;

        if (!TryGetComponent(out _animator))
        {
            Debug.LogError($"{nameof(Animator)} not found on {nameof(AeroBehaviour)}");
        }

        if (!TryGetComponent(out _audioSource))
        {
            Debug.LogError($"{nameof(AudioSource)} not found on {nameof(AeroBehaviour)}");
        }

        _enemyDamageZone = transform.Find("EnemyDamageZone");

        if (_enemyDamageZone == null)
        {
            Debug.LogError($"EnemyDamageZone not found on {nameof(AeroBehaviour)}");
        }

        _projectileStart = transform.Find("ProjectileOffset");

        _groundLayerMask = LayerMask.GetMask("Ground");
        _targetDistance = Mathf.Sqrt(Mathf.Pow(KeepXDistanceFromTarget, 2) + Mathf.Pow(KeepXDistanceFromTarget, 2));
    }

    private void Start()
    {
        _player = FindAnyObjectByType<PlayerCharacter>(FindObjectsInactive.Include);
        _attackTarget = _player.transform;
        _mainCamera = FindFirstObjectByType<MainCamera>();

        _currentHealth = EnemyHealth;
        _initialSpawnPosition = transform.position;

        ResetInnerState();
    }

    private void FixedUpdate()
    {
        if (_isDead)
        {
            return;
        }

        if (_isPassive)
        {
            _animator.SetBool("_IsAttacking", false);
            return;
        }

        _flyTarget = transform.position;
        _targetDir = GetTargetDirection();

        _currentDistanceToTarget = Vector2.Distance(transform.position, _attackTarget.position);
        _targetDistanceX = Mathf.Abs(transform.position.x - _attackTarget.position.x);

        if (!_hasDamageInvulnerability && !_isDead)
        {
            MovementBehaviour();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isDead)
        {
            return;
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("DamageZone"))
        {
            Vector2 collisionDirection = (transform.position - other.transform.position);

            if (other.gameObject.CompareTag("PlayerSword"))
            {
                RecieveDamage(collisionDirection);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, GiveUpRadius);
    }

    void TeleportToSpwanPosition()
    {
        transform.position = _initialSpawnPosition;
    }

    void ResetInnerState()
    {
        _directionX = 1;
        _hasGivenUp = false;
        _targetSideIsLeft = false;
        isChasing = false;
        _isAttacking = false;
        _hasDamageInvulnerability = false;
        _lastShotTime = Time.time;
        _isDead = false;

        _rigidBody.linearVelocity = new();

        _animator.SetBool("_IsAttacking", false);
        SetDamageColor(false);

        StopWingsFlap();
        StopAttackMove();

        StartWingsFlap();
    }

    private IEnumerator CheckRespawnLocation()
    {
        while (_hasGivenUp)
        {
            if (isChasing)
            {
                yield break;
            }

            var isEnemyVisible = _mainCamera.IsWorldPositionVisible(transform.position);
            var isSpawnVisible = _mainCamera.IsWorldPositionVisible(_initialSpawnPosition);

            Debug.Log($"isSpawnVisible: {isSpawnVisible}, isEnemyVisible: {isEnemyVisible}");

            if (!isEnemyVisible && !isSpawnVisible)
            {
                TeleportToSpwanPosition();
                ResetInnerState();

                yield break;
            }

            yield return new WaitForSeconds(2f);
        }
    }

    Vector2 GetTargetDirection()
    {
        return (_attackTarget.position - _projectileStart.position).normalized;
    }

    void MovementBehaviour()
    {
        if ((_hasGivenUp || !isChasing) && _currentDistanceToTarget <= chaseRadius && SeesPlayer())
        {
            isChasing = true;
            _soundEmitter.TryPlayVoiceSource(EnemyVoiceGroups.Alert, true);
        }

        _usedSpeed = speed;

        if (isChasing && !_player.IsDead())
        {
            _directionX = _targetDir.normalized.x < 0 ? -1f : 1f;

            if (SeesPlayer())
            {
                _notSeeingPlayerTime = 0f;
            }
            else
            {
                _notSeeingPlayerTime += Time.deltaTime;
            }

            const float giveupTime = 3.0f;

            if (GiveUpRadius < _currentDistanceToTarget || giveupTime < _notSeeingPlayerTime)
            {
                isChasing = false;
                _hasGivenUp = true;
                ResetInnerState();
                StartCoroutine(CheckRespawnLocation());
            }
            else
            {
                Vector3 rayDirectionX = new Vector3(_targetDir.x, 0, 0).normalized;
                Vector3 rayDirectionY = new Vector3(0, _targetDir.y, 0).normalized;

                var rayStartX = _enemyCollider.transform.position + (Vector3)_enemyCollider.offset;
                var rayStartY = _enemyCollider.transform.position + (Vector3)_enemyCollider.offset;

                bool hitX = Physics2D.Raycast(rayStartX, rayDirectionX, _enemyCollider.size.x + 0.25f, _groundLayerMask);
                bool hitY = Physics2D.Raycast(rayStartY, rayDirectionY, _enemyCollider.size.y + 0.25f, _groundLayerMask);

                if (hitX && !hitY)
                {
                    _flyTarget = new Vector2(transform.position.x, _attackTarget.position.y);
                }
                else if (hitY && !hitX)
                {
                    _flyTarget = new Vector2(_attackTarget.position.x, transform.position.y);
                }
                else
                {
                    TryFlyToTarget();
                    TryAttackPlayer();
                }
            }
        }

        FlapWings();

        // Movement is the last thing
        transform.position = Vector2.MoveTowards(transform.position, _flyTarget, _usedSpeed * Time.fixedDeltaTime);
        DebugUtil.DrawCircle(_flyTarget, 0.40f, Color.red);
        Debug.DrawLine(transform.position, _flyTarget, Color.red);
    }

    void RecieveDamage(Vector2 damageDir)
    {
        if (_hasDamageInvulnerability)
        {
            return;
        }

        if (!IsShadowVariant)
        {
            // _attackInterrupted = true;
        }

        ResetShotLoadTime(66.6f);
        ApplyDamageKnockback(damageDir);

        _currentHealth -= _player.HasShadowPowers ? 2 : 1;

        if (_currentHealth <= 0)
        {
            _isDead = true;
            StopAllCoroutines();
            ActivateDeathAndDestroy();
        }
        else
        {
            StartCoroutine(ActivateDamageTakenTime(DamageInvulnerabilityTime));
        }
    }

    private IEnumerator ActivateDamageTakenTime(float duration)
    {
        _soundEmitter.TryPlayVoiceSource(EnemyVoiceGroups.Damage);
        _soundEmitter.TryPlaySoundSource(EnemySoundGroups.DamageTaken);

        _hasDamageInvulnerability = true;
        SetDamageColor(true);

        yield return new WaitForSeconds(duration);

        _hasDamageInvulnerability = false;
        SetDamageColor(false);
    }

    bool SeesPlayer()
    {
        var dir = GetTargetDirection();
        RaycastHit2D hit = Physics2D.Raycast(_projectileStart.position, dir, chaseRadius * 2, _seesTargetLayerMask);

        Debug.DrawRay(_projectileStart.position, 2 * chaseRadius * dir, Color.blue);

        if (hit.collider != null)
        {
            return hit.collider.CompareTag("Player");
        }

        return false;
    }

    void ActivateDeathAndDestroy()
    {
        _isDead = true;
        _enemyDamageZone.gameObject.SetActive(false);
        _enemyCollider.enabled = false;

        _animator.Play("AeroDeath", 0, 0f);

        StopWingsFlap();
        _soundEmitter.TryPlaySoundSource(EnemySoundGroups.DamageTaken);
        _soundEmitter.TryPlaySoundSource(EnemySoundGroups.GoreDeath);
        _soundEmitter.TryPlayVoiceSource(EnemyVoiceGroups.Death, true);
    }

    public void OnDeathAnimationEnd()
    {
        if (IsShadowVariant)
        {
            _outlineSpriteRenderer.enabled = false;
        }

        _spriteRenderer.enabled = false;
        SignalDieEvent(3f);
    }

    private void ApplyDamageKnockback(Vector2 knockbackDir)
    {
        var knockbackDirForce = knockbackDir.normalized * _rigidBody.mass * 2.5f;
        _rigidBody.AddForce(knockbackDirForce, ForceMode2D.Force);
    }

    void FlapWings()
    {
        _waveOffset = Mathf.Sin(Time.time * waveFrequency) * waveAmplitude * 0.01f;

        if (isChasing && !_player.IsDead())
        {
            _waveOffset *= 50.0f; // NOTE: isChasing = more multiplication needed, but WHY???
        }

        _flyTarget += new Vector2(0, _waveOffset);
    }

    void TryFlyToTarget()
    {
        if (_attackTarget == null)
        {
            return;
        }

        if (_targetDir.x < -0.3f)
        {
            FaceTargetDirection();
        }
        else if (0.3f < _targetDir.x)
        {
            FaceTargetDirection();
        }

        Vector2 flyTargetPlain = (Vector2)_attackTarget.position
            + new Vector2(_targetSideIsLeft ? -KeepXDistanceFromTarget : KeepXDistanceFromTarget, 0)
            + new Vector2(0, KeepYDistanceFromTarget);

        var isBackingUp = _currentDistanceToTarget < _targetDistance;
        _usedSpeed *= (isBackingUp ? 0.5f : 1.0f);
        _flyTarget = flyTargetPlain;
    }

    private void FaceTargetDirection()
    {
        var right = 0.0f < _directionX;
        _targetSideIsLeft = !right;

        if (0.25f < _targetDistanceX)
        {
            _spriteRenderer.flipX = right;
        }

        _projectileStart.localPosition =  new(right ? Mathf.Abs(_projectileStart.localPosition.x) : -Mathf.Abs(_projectileStart.localPosition.x), _projectileStart.localPosition.y);
    }

    private void TryAttackPlayer()
    {
        if (attackRadius < _currentDistanceToTarget)
        {
            return;
        }

        var canShoot = _lastShotTime + shootingCooldown <= Time.time;

        if (canShoot && !_isAttacking)
        {
            _isAttacking = true;
        }
    }

    private IEnumerator AttackMove()
    {
        _animator.SetBool("_IsAttacking", true);
        _soundEmitter.TryPlayVoiceSource(EnemyVoiceGroups.AttackCharge, true);

        _attackInterrupted = false;

        yield return new WaitForSeconds(4.0f / _animationFPS); // NOTE: Padding time?

        yield return new WaitForSeconds(10f / _animationFPS);

        if (_isDead)
        {
            Debug.Log("Attack interrupted from death!");

            _isAttacking = false;
            _animator.SetBool("_IsAttacking", false);
            yield break;
        }

        if (_attackInterrupted)
        {
            Debug.Log("Attack interrupted from damage!");

            _isAttacking = false;
            _animator.SetBool("_IsAttacking", false);
            StartWingsFlap();
            yield break;
        }

        GameObject projectile = Instantiate(projectilePrefab, _projectileStart.transform.position, Quaternion.identity);

        var target = (Vector2)_attackTarget.position + new Vector2(0f, 0.5f);
        Vector2 direction = (target - (Vector2)_projectileStart.position).normalized;

        if (!projectile.TryGetComponent<Projectile>(out var projectileScript))
        {
            Debug.LogError($"Did not find {nameof(Projectile)} in {nameof(Projectile)}");
        }

        projectile.transform.SetParent(this.transform);

        ResetShotLoadTime();

        _soundEmitter.TryPlaySoundSource(EnemySoundGroups.Attack);

        projectileScript.speed = IsShadowVariant ? 5.5f : 4.5f;
        projectileScript.SetProjectileColor(!IsShadowVariant ? _projectileColor1 : _projectileColor2);
        projectileScript.Launch(direction);

        // NOTE: Set animation state beck a bit "prematurely" to avoid extra loop
        _animator.SetBool("_IsAttacking", false);

        yield return new WaitForSeconds(6.0f / _animationFPS);

        _isAttacking = false;
        StartWingsFlap();
    }

    void ResetShotLoadTime(float percentage = 100.0f)
    {
        var timeOffset = shootingCooldown - (shootingCooldown * (percentage / 100));
        _lastShotTime = Time.time - timeOffset;
    }

    void StopAttackMove()
    {
        if (_attackMove != null)
        {
            StopCoroutine(_attackMove);
            _attackMove = null;
        }
    }

    void StartAttackMove()
    {
        if (_attackMove == null)
        {
            StopWingsFlap();
            _attackMove = StartCoroutine(AttackMove());
        }
    }

    void StopWingsFlap()
    {
        if (_flapWings != null)
        {
            StopCoroutine(_flapWings);
            _flapWings = null;
        }
    }

    void StartWingsFlap()
    {
        if (_flapWings == null)
        {
            StopAttackMove();

            _flapWings = StartCoroutine(FlapWingsLoop());
        }
    }

    IEnumerator FlapWingsLoop()
    {
        yield return new WaitForSeconds(4.0f / _animationFPS);

        while (true)
        {
            if (_isAttacking)
            {
                // NOTE: Attack move gets initiated in main animation loop

                StartAttackMove();

                yield break;
            }

            yield return new WaitForSeconds(8.0f / _animationFPS);

            if (_isDead)
            {
                yield break;
            }
        }
    }

    void PlayWingsFlap()
    {
        _soundEmitter.TryPlaySoundSource(EnemySoundGroups.Fly);
    }
}