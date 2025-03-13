using System.Collections;
using UnityEngine;

public enum AeroSounds
{
    Hit = 0,
    Death
}

public class AeroBehaviour : MonoBehaviour
{
    public AudioClip[] AudioClips;

    public float chaseRadius = 5f; // Radius within which the enemy will chase the player
    public float attackRadius = 2f; // Radius within which the enemy will attack
    public float speed = 3f; // Speed of the enemy movement
    public float attackOffset = 2f; // Offset for the attack position
    public float KeepXDistanceFromTarget = 2f;
    public float KeepYDistanceFromTarget = 1f;
    public float DamageInvulnerabilityTime = 0.25f;

    public int EnemyHealth = 3;

    public GameObject projectilePrefab; // Reference to the projectile prefab
    public float shootingCooldown = 3f; // Cooldown time for shooting

    // Wave movement parameters
    public float waveAmplitude = 5f; // Height of the wave
    public float waveFrequency = 2f; // Speed of the wave movement

    private Rigidbody2D _rigidBody;
    private SpriteRenderer _spriteRenderer;
    private Transform _attackTarget; // Reference to the player's transform
    private BoxCollider2D _boxCollider;
    private AudioSource _audioSource;

    private bool _targetSideIsLeft = false;
    private bool isChasing = false;
    private bool _hasDamageInvulnerability = false;
    private bool _isDead = false;
    private float lastShotTime; // Time of the last shot

    private Vector2 _flyTarget = new();

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

        if (!TryGetComponent(out _boxCollider))
        {
            Debug.LogError($"{nameof(BoxCollider2D)} not found on {nameof(AeroBehaviour)}");
        }

        if (!TryGetComponent(out _audioSource))
        {
            Debug.LogError($"{nameof(AudioSource)} not found on {nameof(AeroBehaviour)}");
        }

        _groundLayerMask = LayerMask.GetMask("Ground");

        _targetDistance = Mathf.Sqrt(Mathf.Pow(KeepXDistanceFromTarget, 2) + Mathf.Pow(KeepXDistanceFromTarget, 2));
    }

    private void Start()
    {
        _attackTarget = GameObject.FindGameObjectWithTag("Player").transform;
        lastShotTime = Time.time; // Initialize last shot time

        _currentHealth = EnemyHealth;
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
        Debug.Log($"Collision: {other.gameObject.name}");

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
        // Draw chase and attack radii in the editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }

    void MovementBehaviour()
    {
        _flyTarget = transform.position;

        _currentDistanceToTarget = Vector2.Distance(transform.position, _attackTarget.position);

        if (!isChasing && _currentDistanceToTarget <= chaseRadius)
        {
            isChasing = true;
        }

        _usedSpeed = speed;

        if (isChasing)
        {
            var targetDir = transform.position - _attackTarget.position;

            Vector3 rayDirectionX = new Vector3(-targetDir.x, 0, 0).normalized;
            Vector3 rayDirectionY = new Vector3(0, -targetDir.y, 0).normalized;

            var rayStartX = _boxCollider.transform.position + (Vector3)_boxCollider.offset;
            var rayStartY = _boxCollider.transform.position + (Vector3)_boxCollider.offset;

            bool hitX = Physics2D.Raycast(rayStartX, rayDirectionX, _boxCollider.size.x + 0.25f, _groundLayerMask);
            bool hitY = Physics2D.Raycast(rayStartY, rayDirectionY, _boxCollider.size.y + 0.25f, _groundLayerMask);

            if (hitX && !hitY)
            {
                _spriteRenderer.color = Color.red;
                _flyTarget = new Vector2(transform.position.x, _attackTarget.position.y);
            }
            else if (hitY && !hitX)
            {
                _spriteRenderer.color = Color.green;
                _flyTarget = new Vector2(_attackTarget.position.x, transform.position.y);
            }
            else
            {
                _spriteRenderer.color = Color.white;
                TryFlyToTarget();
                TryAttackPlayer();
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

        ApplyDamageKnockback(damageDir);

        _currentHealth -= 1;

        if (_currentHealth <= 0)
        {
            Debug.Log($"AERO DIED {_currentHealth}");

            _isDead = true;
            StartCoroutine(ActivateDeathAndDestroy());
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

    private IEnumerator ActivateDeathAndDestroy()
    {
        PlaySound(AeroSounds.Death);
        _spriteRenderer.enabled = false;

        yield return new WaitForSeconds(1.0f);

        Destroy(gameObject);
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
        if (attackRadius < _currentDistanceToTarget)
        {
            return;
        }

        var canShoot = lastShotTime + shootingCooldown <= Time.time;

        if (canShoot)
        {
            ShootAtPlayer();
            lastShotTime = Time.time; // Reset the shot time
        }
    }

    private void ShootAtPlayer()
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);

        Vector2 direction = (_attackTarget.position - transform.position).normalized;
        
        if (!projectile.TryGetComponent<Projectile>(out var projectileScript))
        {
            Debug.LogError($"Did not find {nameof(Projectile)} in {nameof(Projectile)}");
        }

        projectileScript.Launch(direction);
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
}