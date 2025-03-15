using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class MookBehaviour : MonoBehaviour
{
    public float MovementSpeed = 1.5f;
    public Transform GroundCheck;

    private BoxCollider2D _boxCollider;
    private Rigidbody2D _rigidBody;
    
    private LayerMask _groundFloorLayer;
    private LayerMask _wallLayer;

    private bool _isMoving = false;

    private bool _facingLeft = false;

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

        GroundCheck = transform.Find("GroundCheck");

        if (GroundCheck == null)
        {
            Debug.LogError($"GroundCheck not found on {nameof(MookBehaviour)}");
        }

        Debug.Log($"GroundCheck {GroundCheck.localPosition}");
    }

    void Start()
    {
        StartCoroutine(MovementLoop());
    }

    void Update()
    {
        TryMove();
    }

    void TryMove()
    {
        if (!_isMoving)
        {
            return;
        }

        var direction = _facingLeft ? -1f : 1f;

        const float groundRayLength = 0.25f;
        RaycastHit2D groundHit = Physics2D.Raycast(GroundCheck.position, Vector2.down, groundRayLength, _groundFloorLayer);

        Debug.DrawRay(GroundCheck.position, Vector2.down * groundRayLength, Color.green);

        float wallRayLength = (_boxCollider.size.x / 2) + 0.1f;
        RaycastHit2D wallHit = Physics2D.Raycast(transform.position, Vector2.right * direction, wallRayLength, _wallLayer);
        Debug.DrawRay(transform.position, direction * wallRayLength * Vector2.right, wallHit.collider ? Color.magenta : Color.cyan);

        if (!groundHit.collider || wallHit.collider)
        {
            TurnAround();
        }

        float newMovement = _facingLeft ? -MovementSpeed : MovementSpeed;
        Debug.DrawRay(GroundCheck.position, Vector2.right * direction, _facingLeft ? Color.green : Color.red);
        _rigidBody.linearVelocity = new Vector2(newMovement, _rigidBody.linearVelocity.y);
    }

    IEnumerator MovementLoop()
    {
        while (true) 
        {
            float moveTime = Random.Range(2f, 8f);
            _isMoving = true;
            yield return new WaitForSeconds(moveTime);

            float waitTime = Random.Range(2f, 4f);
            _isMoving = false;
            yield return new WaitForSeconds(waitTime);

            if (0.75f < Random.value)
            {
                TurnAround();
            }
        }
    }

    void TurnAround()
    {
        _facingLeft = !_facingLeft;

        var newCheckerX = GroundCheck.localPosition.x * -1;
        GroundCheck.localPosition = new(newCheckerX, GroundCheck.localPosition.y, GroundCheck.localPosition.z);

        Debug.Log($"Turn around {gameObject.name} to left: {_facingLeft}");
    }
}
