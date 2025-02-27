using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerControls : MonoBehaviour
{
    public LayerMask GroundLayer;
    public float MoveSpeed = 5f;
    public float JumpForce = 17f;

    private Rigidbody2D _rigidBody;
    private Transform _groundCheck;

    private Vector2 _groundCheckSize = new(0.7f, 0.25f);

    private bool _isGrounded = false;

    void Awake()
    {
        if (!TryGetComponent(out _rigidBody))
        {
            Debug.LogError($"{nameof(Rigidbody2D)} not found on {nameof(PlayerControls)}");
        }

        _groundCheck = transform.Find("GroundCheck");

        if (_groundCheck == null)
        {
            Debug.LogError($"GroundCheck not found as a child of {nameof(PlayerControls)} script");
        }
    }

    void Update()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        _rigidBody.linearVelocity = new Vector2(moveInput * MoveSpeed, _rigidBody.linearVelocity.y);

        if (Input.GetButtonDown("Jump") && _isGrounded)
        {
            _rigidBody.linearVelocity = new Vector2(_rigidBody.linearVelocity.x, JumpForce);
        }

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
}
