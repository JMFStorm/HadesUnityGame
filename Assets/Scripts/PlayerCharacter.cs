using System.Collections;
using TMPro;
using UnityEngine;

public enum PlayerSounds
{
    Attack = 0,
    Dash,
    Jump,
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerCharacter : MonoBehaviour
{
    public AudioClip[] _audioClips;
    public Transform GroundCheck;

    public LayerMask GroundCollisionLayer;
    public LayerMask PlatformLayer;

    public Sprite AttackSprite;
    public Sprite IdleSprite;
    public Sprite AirSprite;

    public float MoveSpeed = 5f;
    public float JumpForce = 17f;
    public float DashSpeed = 15f;
    public float DashingTime = 0.175f;
    public float DashRechargeTime = 1.5f;
    public float AttackSpeed = 0.3f;

    public bool DebugLogging = false;

    public int MaxDashes = 2;

    private AudioSource _audioSource;
    private Rigidbody2D _rigidBody;
    private SpriteRenderer _spriteRenderer;
    private BoxCollider2D _boxCollider;
    private BoxCollider2D _attackSwordBoxCollider;
    private Transform _attackSwordTransform;
    private Collider2D _platformFallthrough;
    private Material _material;
    private TextMeshPro _floatingText;

    private Vector2 _groundCheckSize = new(0.5f, 0.25f);
    private Vector2 _originalSize;
    private Vector2 _originalOffset;

    private int _currentDashes = 0;

    private bool _isAtDoorwayExit = false;
    private bool _isGrounded = false;
    private bool _isDashing = false;
    private bool _isCrouching = false;
    private bool _isAttacking = false;
    private bool _attackCharged = true;

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
            Debug.LogError($"{nameof(Rigidbody2D)} not found on {nameof(PlayerCharacter)}");
        }

        var spriteRenderer = transform.Find("SpriteRenderer");

        if (spriteRenderer == null)
        {
            Debug.LogError($"SpriteRenderer child not found on {nameof(PlayerCharacter)} game object");
        }

        if (!spriteRenderer.TryGetComponent(out _spriteRenderer))
        {
            Debug.LogError($"{nameof(SpriteRenderer)} not found on {nameof(PlayerCharacter)}");
        }

        if (!TryGetComponent(out _boxCollider))
        {
            Debug.LogError($"{nameof(BoxCollider2D)} not found on {nameof(PlayerCharacter)}");
        }

        if (!TryGetComponent(out _audioSource))
        {
            Debug.LogError($"{nameof(AudioSource)} not found on {nameof(PlayerCharacter)}");
        }

        if (GroundCheck == null)
        {
            Debug.LogError($"GroundCheck has not been set in {nameof(PlayerCharacter)}");
        }

        _attackSwordTransform = transform.Find("AttackSword");

        if (_attackSwordTransform == null)
        {
            Debug.LogError($"AttackSword not found as a child of {nameof(PlayerCharacter)} script");
        }
        
        if (!_attackSwordTransform.TryGetComponent(out _attackSwordBoxCollider))
        {
            Debug.LogError($"{nameof(BoxCollider2D)} not found on {nameof(PlayerCharacter)} attack sword child");
        }

        _material = _spriteRenderer.material;

        var floatingText = transform.Find("FloatingText");

        if (floatingText == null)
        {
            Debug.LogError($"FloatingText not found as a child of {nameof(PlayerCharacter)} script");
        }

        if (!floatingText.TryGetComponent(out _floatingText))
        {
            Debug.LogError($"{nameof(TextMeshPro)} not found on {nameof(PlayerCharacter)} FloatingText child");
        }
    }

    private void Start()
    {
        _originalSize = _boxCollider.size;
        _originalOffset = _boxCollider.offset;

        _material.SetColor("_NewColor", new(0.21f, 0.25f, 0.3f));

        ResetPlayerInnerState();
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
        DebugUtil.DrawRectangle(GroundCheck.position, _groundCheckSize, color);

        _spriteRenderer.flipX = _facingDirX < 0.0f;
        _spriteRenderer.sprite = _isAttacking ? AttackSprite : (!_isGrounded ? AirSprite : IdleSprite);

    }

    void FixedUpdate()
    {
        _isGrounded = Physics2D.OverlapBox(GroundCheck.position, _groundCheckSize, 0, GroundCollisionLayer);
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

        Gizmos.DrawWireCube(GroundCheck.position, _groundCheckSize);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Doorway"))
        {
            Debug.Log("Player entered doorway zone");

            _isAtDoorwayExit = true;

            ShowText("Enter", Color.white);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Doorway"))
        {
            _isAtDoorwayExit = false;

            Debug.Log("Player exited doorway zone");

            HideText();
        }
    }

    public void ResetPlayerInnerState()
    {
        _isAtDoorwayExit = false;
        _isGrounded = false;
        _isDashing = false;
        _isCrouching = false;
        _isAttacking = false;
        _attackCharged = true;

        _facingDirX = 0f;
        _dashDirX = 0f;
        _dashTimer = 0f;
        _dashRegenTimer = 0f;

        _attackSwordBoxCollider.enabled = false;

        _currentDashes = MaxDashes;

        HideText();
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
            _isCrouching = true;

            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                Vector2.down,
                _platformFallthroughRaycastDistance,
                PlatformLayer
            );

            if (hit.collider != null)
            {
                _platformFallthrough = hit.collider;

                const float fallthroughCollisionDisableTime = 0.5f;
                StartCoroutine(DisablePlatformCollisionForTime(_platformFallthrough, fallthroughCollisionDisableTime));
            }
        }
        else if (Input.GetButtonDown("Jump") && _isGrounded && !_isDashing)
        {
            PlaySound(PlayerSounds.Jump);

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

        if (Input.GetButtonDown("Attack") && !_isAttacking && _attackCharged)
        {
            StartCoroutine(PlayerAttack(0f < _facingDirX));
        }

        if (_isAtDoorwayExit && _isGrounded && Input.GetButtonDown("Up"))
        {
            DebugLog("Player doorway exit");

            GameState.Instance.LoadNextLevel();
        }
    }

    private IEnumerator PlayerAttack(bool rightSideAttack)
    {
        const float attackPreSwingTime = 0.05f;

        PlaySound(PlayerSounds.Attack);

        _attackCharged = false;
        _isAttacking = true;

        yield return new WaitForSeconds(attackPreSwingTime);

        var attackSwordXOffset = rightSideAttack ? 1f : -1f;
        _attackSwordTransform.position = new Vector3(_rigidBody.position.x + attackSwordXOffset, _rigidBody.position.y, _attackSwordTransform.position.z);

        const float attackVisibleTime = 0.125f;
        float elapsedTime1 = 0f;

        _attackSwordBoxCollider.enabled = true;

        while (elapsedTime1 < attackVisibleTime)
        {
            elapsedTime1 += Time.deltaTime;
            yield return null;
        }

        _attackSwordBoxCollider.enabled = false;
        _isAttacking = false;

        var attackWaitTime = Mathf.Max(AttackSpeed - (attackPreSwingTime + attackVisibleTime), 0f);

        yield return new WaitForSeconds(attackWaitTime);

        _attackCharged = true;
    }

    private IEnumerator DisablePlatformCollisionForTime(Collider2D platformCollider, float time)
    {
        DebugLog($"DisableCollisionForTime start {platformCollider.name}");

        Physics2D.IgnoreCollision(_boxCollider, platformCollider, true);

        yield return new WaitForSeconds(time);

        Physics2D.IgnoreCollision(_boxCollider, platformCollider, false);
    }

    void StartDash()
    {
        PlaySound(PlayerSounds.Dash);

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

    void PlaySound(PlayerSounds soundIndex)
    {
        var index = (int)soundIndex;

        if (_audioSource != null && index < _audioClips.Length && _audioClips[index] != null)
        {
            _audioSource.clip = _audioClips[index];
            _audioSource.Play();
        }
        else
        {
            Debug.LogWarning($"Error playing Player sound index {index}. {nameof(AudioSource)}, {nameof(AudioClip)}, or the specified sound is not assigned.");
        }
    }

    void DebugLog(string message)
    {
        if (DebugLogging)
        {
            Debug.Log($"{nameof(PlayerCharacter)}: {message}");
        }
    }

    void HideText()
    {
        _floatingText.text = "";
    }

    void ShowText(string text, Color color)
    {
        _floatingText.text = text;
        _floatingText.color = color;
    }
}
