using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerControls : MonoBehaviour
{
    public LayerMask GroundCollisionLayer;
    public LayerMask PlatformLayer;

    public float MoveSpeed = 5f;
    public float JumpForce = 17f;
    public float DashSpeed = 15f;
    public float DashingTime = 0.2f;
    public float DashRechargeTime = 1.5f;

    public bool DebugLogging = false;

    public int MaxDashes = 2;

    private Rigidbody2D _rigidBody;
    private SpriteRenderer _spriteRenderer;
    private Transform _groundCheck;
    private BoxCollider2D _boxCollider;

    private Collider2D _platformFallthrough;

    private Vector2 _groundCheckSize = new(0.7f, 0.25f);
    
    private Vector2 _originalSize;
    private Vector2 _originalOffset;

    private int _currentDashes = 0;

    private bool _isGrounded = false;
    private bool _isDashing = false;
    private bool _isCrouching = false;

    private float _facingDirX = 0f;
    private float _dashDirX = 0f;
    private float _dashTimer = 0f;
    private float _dashRegenTimer = 0f;

    private readonly float _platformFallthroughRaycastDistance = 1.0f;

    private readonly RigidbodyConstraints2D _defaultRigidbodyConstraints = RigidbodyConstraints2D.None | RigidbodyConstraints2D.FreezeRotation;
    private readonly RigidbodyConstraints2D _dashingRigidbodyConstraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;

    void Awake()
    {
        if (!TryGetComponent(out _rigidBody))
        {
            Debug.LogError($"{nameof(Rigidbody2D)} not found on {nameof(PlayerControls)}");
        }

        if (!TryGetComponent(out _spriteRenderer))
        {
            Debug.LogError($"{nameof(SpriteRenderer)} not found on {nameof(PlayerControls)}");
        }

        if (!TryGetComponent(out _boxCollider))
        {
            Debug.LogError($"{nameof(BoxCollider2D)} not found on {nameof(PlayerControls)}");
        }

        _groundCheck = transform.Find("GroundCheck");

        if (_groundCheck == null)
        {
            Debug.LogError($"GroundCheck not found as a child of {nameof(PlayerControls)} script");
        }
    }

    private void Start()
    {
        _currentDashes = MaxDashes;

        _originalSize = _boxCollider.size;
        _originalOffset = _boxCollider.offset;
    }

    void Update()
    {
        PlayerMovement();

        if (_isDashing)
        {
            _dashTimer += Time.deltaTime;

            if (DashingTime <= _dashTimer)
            {
                StopDash();
            }
            else
            {
                Dash();
            }
        }

        DashRegen();

        if (_isCrouching)
        {
            _boxCollider.size = new Vector2(_originalSize.x, _originalSize.y * 0.5f);
            _boxCollider.offset = new Vector2(_originalOffset.x, _originalOffset.y - 0.25f);

            Debug.DrawRay(transform.position, Vector2.down * _platformFallthroughRaycastDistance, Color.red);
        }
        else
        {
            _boxCollider.size = _originalSize;
            _boxCollider.offset = _originalOffset;
        }

        Color color = _isGrounded ? Color.green : Color.red;
        DebugUtil.DrawRectangle(_groundCheck.position, _groundCheckSize, color);
    }

    void FixedUpdate()
    {
        _isGrounded = Physics2D.OverlapBox(_groundCheck.position, _groundCheckSize, 0, GroundCollisionLayer);
    }

    private void OnDrawGizmosSelected()
    {
        if (_isGrounded)
        {
            Gizmos.color = Color.green;
        }
        else
        {
            Gizmos.color = Color.red;
        }

        Gizmos.DrawWireCube(_groundCheck.position, _groundCheckSize);
    }

    void PlayerMovement()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");

        _rigidBody.linearVelocity = new Vector2(moveInput * MoveSpeed, _rigidBody.linearVelocity.y);

        if (0f < Mathf.Abs(moveInput))
        {
            _facingDirX = Mathf.Ceil(moveInput);
        }

        _isCrouching = false;

        if (Input.GetButton("Crouch") && Input.GetButtonDown("Jump") && _isGrounded && !_isDashing)
        {
            DebugLog("Jump down");

            _isCrouching = true;

            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                Vector2.down,
                _platformFallthroughRaycastDistance,
                PlatformLayer
            );

            if (hit.collider != null)
            {
                DebugLog("Fallthrough collider hit");

                _platformFallthrough = hit.collider;

                const float fallthroughCollisionDisableTime = 0.5f;
                StartCoroutine(DisablePlatformCollisionForTime(_platformFallthrough, fallthroughCollisionDisableTime));
            }
        }
        else if (Input.GetButtonDown("Jump") && _isGrounded && !_isDashing)
        {
            DebugLog("Jump");

            _rigidBody.linearVelocity = new Vector2(_rigidBody.linearVelocity.x, JumpForce);
        }
        else if (Input.GetButtonDown("Dash") && !_isDashing)
        {
            if (0 < _currentDashes)
            {
                StartDash();
            }
            else
            {
                DebugLog("No dash available");
            }
        }
        else if (Input.GetButton("Crouch") && _isGrounded && !_isDashing)
        {
            _isCrouching = true;
        }
    }

    private IEnumerator DisablePlatformCollisionForTime(Collider2D platformCollider, float time)
    {
        DebugLog($"DisableCollisionForTime start {platformCollider.name}");

        Physics2D.IgnoreCollision(_boxCollider, platformCollider, true);

        yield return new WaitForSeconds(time);

        Physics2D.IgnoreCollision(_boxCollider, platformCollider, false);

        DebugLog($"DisableCollisionForTime end {platformCollider.name}");
    }

    void StartDash()
    {
        DebugLog("Dash start");

        _rigidBody.constraints = _dashingRigidbodyConstraints;

        _isDashing = true;
        _dashTimer = 0f;
        _dashDirX = _facingDirX;
        _currentDashes--;

        _spriteRenderer.color = Color.magenta;
    }

    void Dash()
    {
        _rigidBody.linearVelocity = new Vector2(_dashDirX * DashSpeed, 0f);
    }

    void StopDash()
    {
        DebugLog("Dash stop");

        _rigidBody.constraints = _defaultRigidbodyConstraints;

        _isDashing = false;
        _rigidBody.linearVelocity = Vector2.zero;

        _spriteRenderer.color = Color.white;
    }

    void DashRegen()
    {
        if (_currentDashes < MaxDashes)
        {
            _dashRegenTimer += Time.deltaTime;

            if (DashRechargeTime <= _dashRegenTimer)
            {
                DebugLog("Dash recharged");

                _currentDashes++;
                _dashRegenTimer = 0f;
            }
        }
        else
        {
            _dashRegenTimer = 0f;
        }
    }

    void DebugLog(string message)
    {
        if (DebugLogging)
        {
            Debug.Log($"{nameof(PlayerControls)}: {message}");
        }
    }
}
