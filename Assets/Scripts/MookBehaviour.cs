using System.Collections;
using UnityEngine;

public enum EnemyState
{
    Passive = 0,
    Alert,
    AttackMoving,
    Attacking,
    NormalMoving,
}

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class MookBehaviour : MonoBehaviour
{
    public float MovementSpeed = 1.5f;
    public float AggroSpeed = 3.0f;
    public float AttackRange = 1.0f;

    private Transform _groundCheck;
    private BoxCollider2D _boxCollider;
    private Rigidbody2D _rigidBody;
    private SpriteRenderer _spriteRenderer;

    private Vector2 _aggroTarget;

    private LayerMask _groundFloorLayer;
    private LayerMask _wallLayer;
    private LayerMask _playerLayer;

    private EnemyState _state;

    private bool _facingLeft = false;

    private enum CollisionTypes
    {
        None = 0,
        GroundEdge,
        WallHit
    }

    void Awake()
    {
        if (!TryGetComponent(out _boxCollider))
        {
            Debug.LogError($"{nameof(BoxCollider2D)} not found on {nameof(MookBehaviour)}");
        }

        if (!TryGetComponent(out _rigidBody))
        {
            Debug.LogError($"{nameof(BoxCollider2D)} not found on {nameof(MookBehaviour)}");
        }

        _groundFloorLayer = LayerMask.GetMask("Ground", "Platform");
        _wallLayer = LayerMask.GetMask("Ground", "DamageZone");
        _playerLayer = LayerMask.GetMask("Character");

        _groundCheck = transform.Find("GroundCheck");

        if (_groundCheck == null)
        {
            Debug.LogError($"GroundCheck not found on {nameof(MookBehaviour)}");
        }

        var sprite = transform.Find("Sprite");

        if (!sprite.TryGetComponent(out _spriteRenderer))
        {
            Debug.LogError($"{nameof(SpriteRenderer)} not found on child of {nameof(MookBehaviour)}");
        }
    }

    void Start()
    {
        _state = EnemyState.NormalMoving;
        StartCoroutine(MovementLoop());
    }

    void Update()
    {
        TryNormalMovement();
        TryAggroMovement();

        DetectPlayerAndAggro();

        _spriteRenderer.color = GetEnemyStateColor(_state);
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
                Debug.Log("Aggro ended on collision " + Time.time);

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
        _state = EnemyState.Passive;

        yield return new WaitForSeconds(passiveLength);

        TurnAround();

        _state = EnemyState.NormalMoving;
    }

    IEnumerator Attack()
    {
        _state = EnemyState.Attacking;

        yield return new WaitForSeconds(1.0f);

        _spriteRenderer.color = Color.white;
        _state = EnemyState.NormalMoving;

        StartCoroutine(MovementLoop());
    }

    CollisionTypes GetRaycastCollisions()
    {
        var direction = GetXDirection();

        const float groundRayLength = 0.25f;
        RaycastHit2D groundHit = Physics2D.Raycast(_groundCheck.position, Vector2.down, groundRayLength, _groundFloorLayer);

        Debug.DrawRay(_groundCheck.position, Vector2.down * groundRayLength, Color.green);

        float wallRayLength = (_boxCollider.size.x / 2) + 0.1f;
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

        yield return new WaitForSeconds(0.5f);

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

            if (0.75f < Random.value)
            {
                TurnAround();
            }
        }
    }

    void TurnAround()
    {
        _facingLeft = !_facingLeft;

        Debug.Log("Enemy turn around" + Time.time);

        var newCheckerX = _groundCheck.localPosition.x * -1;
        _groundCheck.localPosition = new(newCheckerX, _groundCheck.localPosition.y, _groundCheck.localPosition.z);
    }
}
