using UnityEngine;

public class AeroBehaviour : MonoBehaviour
{
    public float chaseRadius = 5f; // Radius within which the enemy will chase the player
    public float attackRadius = 2f; // Radius within which the enemy will attack
    public float speed = 3f; // Speed of the enemy movement
    public float attackOffset = 2f; // Offset for the attack position
    public float KeepXDistanceFromTarget = 2f;
    public float KeepYDistanceFromTarget = 1f;

    public GameObject projectilePrefab; // Reference to the projectile prefab
    public float shootingCooldown = 3f; // Cooldown time for shooting

    // Wave movement parameters
    public float waveAmplitude = 5f; // Height of the wave
    public float waveFrequency = 2f; // Speed of the wave movement

    private Rigidbody2D _rigidBody;
    private SpriteRenderer _spriteRenderer;
    private Transform _attackTarget; // Reference to the player's transform

    private bool isChasing = false; // State to track if the enemy is chasing
    private float lastShotTime; // Time of the last shot

    private Vector2 _targetToFly = new();
    private float _waveOffset = 0.0f;
    private float _currentDistanceToTarget = 0;
    private float _targetDistance = 0;

    private void Start()
    {
        _attackTarget = GameObject.FindGameObjectWithTag("Player").transform;
        lastShotTime = Time.time; // Initialize last shot time
    }

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

        _targetDistance = Mathf.Sqrt(Mathf.Pow(KeepXDistanceFromTarget, 2) + Mathf.Pow(KeepXDistanceFromTarget, 2));
    }

    private void Update()
    {
        _targetToFly = new();

        _currentDistanceToTarget = Vector2.Distance(transform.position, _attackTarget.position);

        if (!isChasing && _currentDistanceToTarget <= chaseRadius)
        {
            isChasing = true;
        }

        _waveOffset = Mathf.Sin(Time.time * waveFrequency) * waveAmplitude * 0.01f;

        _targetToFly += new Vector2(0, 0 + _waveOffset);

        if (isChasing)
        {
            TryFlyToTarget();
            TryAttackPlayer();
        }
    }

    void TryFlyToTarget()
    {
        if (_attackTarget == null)
        {
            return;
        }

        var directionVector = (Vector2)_attackTarget.position - (Vector2)transform.position;

        Vector2 direction = directionVector.normalized;

        Vector2 flyTargetPlain = (Vector2)_attackTarget.position
            + new Vector2(direction.x < 0f ? KeepXDistanceFromTarget : -KeepXDistanceFromTarget, 0)
            + new Vector2(0, KeepYDistanceFromTarget);

        _targetToFly = flyTargetPlain;

        var isBackingUp = _currentDistanceToTarget < _targetDistance;
        float usedSpeed = speed * (isBackingUp ? 0.5f : 1.0f);

        Debug.Log($"isBackingUp {isBackingUp}");

        transform.position = Vector2.MoveTowards(transform.position, _targetToFly, usedSpeed * Time.deltaTime);

        _spriteRenderer.flipX = 0.0f < direction.x;

        DebugUtil.DrawCircle(_targetToFly, 0.15f, Color.magenta);
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
        Debug.Log("SHoot at player");

        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);

        // Calculate the direction towards the player
        Vector2 direction = (_attackTarget.position - transform.position).normalized;
        Projectile projectileScript = projectile.GetComponent<Projectile>();

        if (projectileScript != null)
        {
            projectileScript.Launch(direction); // Launch the projectile towards the player
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
}