using UnityEngine;

public class AeroBehaviour : MonoBehaviour
{
    public float ChaseRadius = 10f; // Radius within which the enemy will chase the player
    public float AttackRadius = 5f; // Radius within which the enemy will attack
    public float Speed = 3f; // Speed of the enemy movement
    public float AttackOffset = 2f; // Offset for the attack position
    public float ShootingCooldown = 3f; // Cooldown time for shooting

    public GameObject ProjectilePrefab; // Reference to the projectile prefab

    // Wave movement parameters
    public float waveAmplitude = 0.5f; // Height of the wave
    public float waveFrequency = 2f; // Speed of the wave movement

    private SpriteRenderer _spriteRenderer;
    private Transform _attackTarget;
    private Projectile _projectileScript;

    private bool _isChasing = false; // State to track if the enemy is chasing
    private float _facingEnemyDirection = 1f; // 1 for right, -1 for left
    private float _lastShotTime; // Time of the last shot

    void Awake()
    {
        if (!TryGetComponent(out _spriteRenderer))
        {
            Debug.LogError($"{nameof(SpriteRenderer)} not found on {nameof(AeroBehaviour)}");
        }

        if (!TryGetComponent(out _projectileScript))
        {
            Debug.LogError($"{nameof(Projectile)} not found on {nameof(AeroBehaviour)}");
        }
    }

    private void Start()
    {
        // Find the player object with the tag "Player"
        _attackTarget = GameObject.FindGameObjectWithTag("Player").transform;
        _lastShotTime = Time.time; // Initialize last shot time
    }

    private void Update()
    {
        if (_attackTarget == null)
        {
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, _attackTarget.position);

        // Update chasing state based on distance to player
        if (distanceToPlayer <= ChaseRadius)
        {
            _isChasing = true;

            if (distanceToPlayer > AttackRadius)
            {
                ChasePlayer();
            }
            else
            {
                AttackPlayer();
            }
        }
        else
        {
            // If the enemy was chasing but the player is out of chase radius
            if (_isChasing)
            {
                ChasePlayer();
            }
        }
    }

    private void ChasePlayer()
    {
        // Move towards the player within the chase radius
        Vector2 direction = (_attackTarget.position - transform.position).normalized;

        // Update facing direction based on player's position
        if (direction.x < 0)
        {
            _facingEnemyDirection = -1f;
        }
        else
        {
            _facingEnemyDirection = 1f;
        }

        _spriteRenderer.flipX = 0f < _facingEnemyDirection;

        // Calculate wave movement using a sine function
        float waveOffset = Mathf.Sin(Time.time * waveFrequency) * waveAmplitude;

        // Set the new position with wave effect
        Vector2 targetPosition = (Vector2)_attackTarget.position + new Vector2(0, waveOffset);
        transform.position = Vector2.MoveTowards(transform.position, targetPosition, Speed * Time.deltaTime);
    }

    private void AttackPlayer()
    {
        // Calculate the offset based on the player's position
        float offsetDirection;

        if (_attackTarget.position.x > transform.position.x)
        {
            offsetDirection = -AttackOffset;
        }
        else
        {
            offsetDirection = AttackOffset;
        }

        Vector2 targetPosition = new(_attackTarget.position.x + offsetDirection, _attackTarget.position.y + AttackOffset);

        // Move to the target position above the player
        transform.position = Vector2.MoveTowards(transform.position, targetPosition, Speed * Time.deltaTime);

        var canShoot = _lastShotTime + ShootingCooldown <= Time.time;

        if (canShoot)
        {
            ShootAtPlayer();
            _lastShotTime = Time.time; // Reset the shot time
        }
    }

    private void ShootAtPlayer()
    {
        Instantiate(ProjectilePrefab, transform.position, Quaternion.identity);

        Vector2 direction = (_attackTarget.position - transform.position).normalized;
        _projectileScript?.Launch(direction);
    }

    private void OnDrawGizmos()
    {
        // Draw chase and attack radii in the editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, ChaseRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AttackRadius);
    }
}