using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerControls : MonoBehaviour
{
    private Rigidbody2D _rigidBody;
    private SpriteRenderer _spriteRenderer;
    private Transform _groundCheck;

    public LayerMask GroundLayer;

    public float MoveSpeed = 5f;
    public float JumpForce = 17f;
    public float DashSpeed = 15f;
    public float DashingTime = 0.2f;
    public float DashRechargeTime = 1.5f;

    public int MaxDashes = 2;

    private Vector2 _groundCheckSize = new(0.7f, 0.25f);

    private int _currentDashes = 0;

    private bool _isGrounded = false;
    private bool _isDashing = false;

    private float _dashDirX = 0f;
    private float _facingDirX = 0f;
    private float _dashTimer = 0f;
    private float _dashRegenTimer = 0f;

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

        _groundCheck = transform.Find("GroundCheck");

        if (_groundCheck == null)
        {
            Debug.LogError($"GroundCheck not found as a child of {nameof(PlayerControls)} script");
        }
    }

    private void Start()
    {
        _currentDashes = MaxDashes;
    }

    void Update()
    {
        PlayerMovement();

        DashRegen();

        Color color = _isGrounded ? Color.green : Color.red;
        DebugUtil.DrawRectangle(_groundCheck.position, _groundCheckSize, color);
    }

    void FixedUpdate()
    {
        _isGrounded = Physics2D.OverlapBox(_groundCheck.position, _groundCheckSize, 0, GroundLayer);
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

        if (Input.GetButtonDown("Jump") && _isGrounded && !_isDashing)
        {
            Debug.Log("Jump");

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
                Debug.Log("No dash available");
            }
        }

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
    }

    void StartDash()
    {
        Debug.Log("Dash start");

        _rigidBody.constraints = _dashingRigidbodyConstraints;

        _isDashing = true;
        _dashTimer = 0f;
        _dashDirX = _facingDirX;
        _currentDashes--;

        _spriteRenderer.color = Color.magenta;
    }

    void Dash()
    {
        Debug.Log("Dashing");

        _rigidBody.linearVelocity = new Vector2(_dashDirX * DashSpeed, 0f);
    }

    void StopDash()
    {
        Debug.Log("Dash stop");

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
                Debug.Log("Dash recharged");

                _currentDashes++;
                _dashRegenTimer = 0f;
            }
        }
        else
        {
            _dashRegenTimer = 0f;
        }
    }
}
