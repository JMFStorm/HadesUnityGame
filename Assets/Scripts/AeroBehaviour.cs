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

public class AeroBehaviour : MonoBehaviour
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

    // For shadow variant
    public bool IsShadowVariant = false;
    public float ShadowOutlineThreshold = 0.1f;

    public int EnemyHealth = 3;

    public GameObject projectilePrefab; // Reference to the projectile prefab

    private Rigidbody2D _rigidBody;
    private SpriteRenderer _spriteRenderer;
    private Transform _attackTarget; // Reference to the player's transform
    private Transform _enemyDamageZone;
    private CapsuleCollider2D _physicsCollider;
    private AudioSource _audioSource;
    private MainCamera _mainCamera;

    // For shadow variant
    private Material _material;

    private Coroutine _flapWings = null;

    private bool _isAttacking = false;
    private bool _hasGivenUp = false;
    private bool _targetSideIsLeft = false;
    private bool isChasing = false;
    private bool _hasDamageInvulnerability = false;
    private bool _isDead = false;
    private float _lastShotTime; // Time of the last shot

    private Vector2 _flyTarget = new();
    private Vector2 _initialSpawnPosition = new();

    private float _waveOffset = 0.0f;
    private float _currentDistanceToTarget = 0;
    private float _targetDistance = 0;
    private float _usedSpeed = 0;

    private int _currentHealth = 3;
    private int _groundLayerMask;

    private void Awake()
    {
        if (!TryGetComponent(out _spriteRenderer))
        {
            Debug.LogError($"{nameof(SpriteRenderer)} not found on {nameof(AeroBehaviour)}");
        }

        if (!TryGetComponent(out _rigidBody))
        {
            Debug.LogError($"{nameof(Rigidbody2D)} not found on {nameof(AeroBehaviour)}");
        }

        if (!TryGetComponent(out _physicsCollider))
        {
            Debug.LogError($"{nameof(BoxCollider2D)} not found on {nameof(AeroBehaviour)}");
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

        _material = _spriteRenderer.material;

        _groundLayerMask = LayerMask.GetMask("Ground");
        _targetDistance = Mathf.Sqrt(Mathf.Pow(KeepXDistanceFromTarget, 2) + Mathf.Pow(KeepXDistanceFromTarget, 2));
    }

    private void Start()
    {
        _material.SetFloat("_IsShadowVariant", IsShadowVariant ? 1f : 0f);
        _material.SetFloat("_ShadowOutlineThreshold", ShadowOutlineThreshold);

        _attackTarget = GameObject.FindGameObjectWithTag("Player").transform;
        _mainCamera = FindFirstObjectByType<MainCamera>();

        _currentHealth = EnemyHealth;

        _initialSpawnPosition = transform.position;

        ResetInnerState();
    }

    private void FixedUpdate()
    {
        if (!_hasDamageInvulnerability && !_isDead)
        {
            MovementBehaviour();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
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
        _isAttacking = false;
        _hasGivenUp = false;
        _targetSideIsLeft = false;
        isChasing = false;
        _hasDamageInvulnerability = false;
        _isDead = false;
        _lastShotTime = Time.time;

        StopWingsFlap();
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

    void MovementBehaviour()
    {
        _flyTarget = transform.position;

        _currentDistanceToTarget = Vector2.Distance(transform.position, _attackTarget.position);

        if ((_hasGivenUp || !isChasing) && _currentDistanceToTarget <= chaseRadius)
        {
            isChasing = true;
        }

        _usedSpeed = speed;

        if (isChasing)
        {
            if (GiveUpRadius < _currentDistanceToTarget)
            {
                isChasing = false;
                _hasGivenUp = true;
                StartCoroutine(CheckRespawnLocation());
            }
            else
            {
                var targetDir = transform.position - _attackTarget.position;

                Vector3 rayDirectionX = new Vector3(-targetDir.x, 0, 0).normalized;
                Vector3 rayDirectionY = new Vector3(0, -targetDir.y, 0).normalized;

                var rayStartX = _physicsCollider.transform.position + (Vector3)_physicsCollider.offset;
                var rayStartY = _physicsCollider.transform.position + (Vector3)_physicsCollider.offset;

                bool hitX = Physics2D.Raycast(rayStartX, rayDirectionX, _physicsCollider.size.x + 0.25f, _groundLayerMask);
                bool hitY = Physics2D.Raycast(rayStartY, rayDirectionY, _physicsCollider.size.y + 0.25f, _groundLayerMask);

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

        _isAttacking = false; // NOTE: Reset attack
        ResetShotLoadTime(66.6f);

        ApplyDamageKnockback(damageDir);

        _currentHealth -= 1;

        if (_currentHealth <= 0)
        {
            _isDead = true;
            ActivateDeathAndDestroy();
        }
        else
        {
            StartCoroutine(ActivateDamageTakenTime(DamageInvulnerabilityTime));
        }
    }

    private IEnumerator ActivateDamageTakenTime(float duration)
    {
        PlaySound(AeroSounds.Hit);
        _hasDamageInvulnerability = true;
        _spriteRenderer.color = Color.red;

        yield return new WaitForSeconds(duration);

        _hasDamageInvulnerability = false;
        _spriteRenderer.color = Color.white;
    }

    void ActivateDeathAndDestroy()
    {
        StopWingsFlap();
        PlaySound(AeroSounds.Death);

        _spriteRenderer.enabled = false;
        _enemyDamageZone.gameObject.SetActive(false);

        Destroy(gameObject, 1.5f);
    }

    private void ApplyDamageKnockback(Vector2 knockbackDir)
    {
        var knockbackDirForce = knockbackDir.normalized * _rigidBody.mass * 2.5f;
        _rigidBody.AddForce(knockbackDirForce, ForceMode2D.Force);
    }

    void FlapWings()
    {
        _waveOffset = Mathf.Sin(Time.time * waveFrequency) * waveAmplitude * 0.01f;

        if (isChasing)
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

        var directionVector = (Vector2)_attackTarget.position - (Vector2)transform.position;

        if (directionVector.x < -0.5f)
        {
            _targetSideIsLeft = true;
        }
        else if (0.5f < directionVector.x)
        {
            _targetSideIsLeft = false;
        }

        Vector2 direction = directionVector.normalized;

        Vector2 flyTargetPlain = (Vector2)_attackTarget.position
            + new Vector2(_targetSideIsLeft ? -KeepXDistanceFromTarget : KeepXDistanceFromTarget, 0)
            + new Vector2(0, KeepYDistanceFromTarget);

        var isBackingUp = _currentDistanceToTarget < _targetDistance;
        _usedSpeed *= (isBackingUp ? 0.5f : 1.0f);
        
        _spriteRenderer.flipX = 0.0f < direction.x;

        _flyTarget = flyTargetPlain;
    }

    private void TryAttackPlayer()
    {
        if (attackRadius < _currentDistanceToTarget || _isAttacking)
        {
            return;
        }

        var canShoot = _lastShotTime + shootingCooldown <= Time.time;

        if (canShoot)
        {
            StartCoroutine(ActivateShootAtPlayer());
        }
    }

    private IEnumerator ActivateShootAtPlayer()
    {
        PlaySound(AeroSounds.Preattack);

        _isAttacking = true;
        _spriteRenderer.color = Color.yellow;

        yield return new WaitForSeconds(ProjectileLoadTime);

        if (_isDead || !_isAttacking) // NOTE: _isAttacking might get disabled during wait time
        {
            Debug.Log("Attack interrupted");
            yield break;
        }

        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        Vector2 direction = (_attackTarget.position - transform.position).normalized;

        if (!projectile.TryGetComponent<Projectile>(out var projectileScript))
        {
            Debug.LogError($"Did not find {nameof(Projectile)} in {nameof(Projectile)}");
        }

        PlaySound(AeroSounds.ProjectileLaunch);
        projectileScript.Launch(direction);

        ResetShotLoadTime();
        _isAttacking = false;
        _spriteRenderer.color = Color.white;
    }

    void ResetShotLoadTime(float percentage = 100.0f)
    {
        var timeOffset = shootingCooldown - (shootingCooldown * (percentage / 100));
        _lastShotTime = Time.time - timeOffset;
    }

    void PlaySound(AeroSounds soundIndex)
    {
        var index = (int)soundIndex;

        if (_audioSource != null && index < AudioClips.Length && AudioClips[index] != null)
        {
            _audioSource.clip = AudioClips[index];
            _audioSource.Play();
        }
        else
        {
            Debug.LogWarning($"Error playing Player sound index {index}. {nameof(AudioSource)}, {nameof(AudioClip)}, or the specified sound is not assigned.");
        }
    }

    void StopWingsFlap()
    {
        if (_flapWings != null)
        {
            StopCoroutine(_flapWings);
        }
    }

    void StartWingsFlap()
    {
        if (_flapWings == null)
        {
            StartCoroutine(FlapWingsLoop());
        }
    }

    IEnumerator FlapWingsLoop()
    {
        yield return new WaitForSeconds(4.0f / 10.0f);

        while (true)
        {
            yield return new WaitForSeconds(8.0f / 10.0f);

            if (_isDead)
            {
                yield return null;
            }

            PlaySound(AeroSounds.Wings);
        }
    }
}