using System.Collections;
using TMPro;
using UnityEngine;

public enum PlayerSounds
{
    Attack = 0,
    Dash,
    Jump,
    Hit,
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
    public Sprite DashSprite;
    public Sprite CrouchSprite;
    public Sprite HitSprite;
    public Sprite MoveSprite;

    public float MoveSpeed = 5f;
    public float JumpForce = 17f;
    public float DashSpeed = 15f;
    public float DashingTime = 0.175f;
    public float DashRechargeTime = 1.5f;
    public float AttackSpeed = 0.3f;
    public float DamageInvulnerabilityTime = 3.0f;

    public bool DebugLogging = false;

    public int MaxDashes = 2;

    private AudioSource _audioSource;
    private Rigidbody2D _rigidBody;
    private SpriteRenderer _spriteRenderer;
    private BoxCollider2D _boxCollider;
    private BoxCollider2D _swordBoxCollider;
    private Collider2D _platformFallthrough;
    private Material _material;
    private TextMeshPro _floatingText;
    private GameUI _gameUI;

    private Vector2 _groundCheckSize = new(0.5f, 0.25f);
    private Vector2 _originalSize;
    private Vector2 _originalOffset;

    private int _currentDashes = 0;
    private int _currentHealth = 3;

    private bool _controlsAreActive = true;
    private bool _isAtDoorwayExit = false;
    private bool _isGrounded = false;
    private bool _isDashing = false;
    private bool _isCrouching = false;
    private bool _isAttacking = false;
    private bool _attackCharged = true;
    private bool _hasDamageInvulnerability = false;
    private bool _inDamageState = false;

    private float _facingDirX = 0f;
    private float _dashDirX = 0f;
    private float _dashTimer = 0f;
    private float _dashRegenTimer = 0f;

    private int _damageZoneLayer;

    private readonly int _defaultPlayerHealth = 3;

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

        var swordArea = transform.Find("SwordArea");

        if (swordArea == null)
        {
            Debug.LogError($"SwordArea not found as a child of {nameof(PlayerCharacter)} script");
        }

        if (!swordArea.TryGetComponent(out _swordBoxCollider))
        {
            Debug.LogError($"{nameof(BoxCollider2D)} not found on {nameof(PlayerCharacter)} SwordArea child");
        }

        _damageZoneLayer = LayerMask.NameToLayer("DamageZone");

        _gameUI = FindFirstObjectByType<GameUI>();

        if (_gameUI == null)
        {
            Debug.LogError($"{nameof(GameUI)} not found on {nameof(PlayerCharacter)}");
        }
    }

    private void Start()
    {
        _swordBoxCollider.gameObject.SetActive(false);

        _originalSize = _boxCollider.size;
        _originalOffset = _boxCollider.offset;

        _material.SetColor("_NewColor", new(0.21f, 0.25f, 0.3f));

        ResetPlayerInnerState();

        _currentHealth = _defaultPlayerHealth;
        _gameUI.SetHealth(_currentHealth);
    }

    void Update()
    {
        PlayerMovementControls();

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

        if (!_isAttacking && !_isDashing)
        {
            _spriteRenderer.flipX = _facingDirX < 0.0f;
        }

        // ------------
        // Get sprite

        Sprite usedSprite;

        if (_inDamageState)
        {
            usedSprite = HitSprite;
        }
        else if (_isAttacking)
        {
            usedSprite = AttackSprite;
        }
        else if (_isDashing)
        {
            usedSprite = DashSprite;
        }
        else if (_isCrouching)
        {
            usedSprite = CrouchSprite;
        }
        else if (!_isGrounded)
        {
            usedSprite = AirSprite;
        }
        else if (_isGrounded && 0.01f < Mathf.Abs(_rigidBody.linearVelocity.x))
        {
            usedSprite = MoveSprite;
        }
        else
        {
            usedSprite = IdleSprite;
        }

        _spriteRenderer.sprite = usedSprite;

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

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Doorway"))
        {
            _isAtDoorwayExit = true;
            ShowText("Enter", Color.white);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("PlayerSword") || _isAttacking)
        {
            return; // NOTE: Ignore damage recieve when attacking
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("DamageZone") 
            || other.gameObject.layer == LayerMask.NameToLayer("Enemy")
            || other.gameObject.layer == LayerMask.NameToLayer("FlyingEnemy"))
        {
            Vector2 collisionDirection = (transform.position - other.transform.position).normalized;
            TryRecieveDamage(collisionDirection);
        }
    }

    void TryRecieveDamage(Vector2 damageDir)
    {
        if (_hasDamageInvulnerability)
        {
            return;
        }

        _currentHealth -= 1;

        _gameUI.SetHealth(_currentHealth);

        Debug.Log($"Player took damage, health remaining: {_currentHealth}");

        if (_currentHealth <= 0)
        {
            Debug.Log($"Player DIED {_currentHealth}");
        }

        StopDash();

        StartCoroutine(ActivateDamageTakenTime(DamageInvulnerabilityTime));
        ApplyDamageKnockback(damageDir);
    }

    private void ApplyDamageKnockback(Vector2 knockbackDir)
    {
        var knockbackDirForce = new Vector2(knockbackDir.normalized.x, 6.5f);
        _rigidBody.linearVelocity = knockbackDirForce;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Doorway"))
        {
            _isAtDoorwayExit = false;
            HideText();
        }
    }

    private IEnumerator ActivateDamageTakenTime(float duration)
    {
        PlaySound(PlayerSounds.Hit);
        _controlsAreActive = false;
        _inDamageState = true;
        _hasDamageInvulnerability = true;

        _spriteRenderer.color = Color.red;

        const float controlsInactive = 0.35f;
        yield return new WaitForSeconds(controlsInactive);

        _spriteRenderer.color = Color.gray;

        _inDamageState = false;
        _controlsAreActive = true;

        float invulnerabilityTime = Mathf.Max(0, duration - controlsInactive);

        yield return new WaitForSeconds(invulnerabilityTime);
        _hasDamageInvulnerability = false;

        _spriteRenderer.color = Color.white;
    }


    public void ResetPlayerInnerState()
    {
        _isAtDoorwayExit = false;
        _isGrounded = false;
        _isDashing = false;
        _isCrouching = false;
        _isAttacking = false;
        _attackCharged = true;
        _controlsAreActive = true;

        _facingDirX = 1f; // NOTE: Facing right
        _dashDirX = 0f;
        _dashTimer = 0f;
        _dashRegenTimer = 0f;

        _currentDashes = MaxDashes;
        _gameUI.SetStamina(_currentDashes);

        HideText();
    }

    void PlayerMovementControls()
    {
        if (!_controlsAreActive)
        {
            return;
        }

        // -----------------------------------
        // Get horizontal movement and slide

        float moveInput = Input.GetAxisRaw("Horizontal");
        var newMovement = new Vector2(moveInput * MoveSpeed, _rigidBody.linearVelocity.y);

        if (Input.GetButton("Crouch") && _isGrounded && !_isDashing)
        {
            var crouchInit = !_isCrouching;

            if (crouchInit)
            {
                _rigidBody.linearVelocity = new();
            }

            _isCrouching = true;
        }
        else
        {
            _isCrouching = false;
            _rigidBody.linearVelocity = newMovement;
        }

        if (0f < Mathf.Abs(moveInput))
        {
            _facingDirX = Mathf.Ceil(moveInput);
        }

        // --------------------
        // Get action buttons

        if (_isCrouching && Input.GetButtonDown("Jump") && _isGrounded && !_isDashing && _isCrouching)
        {
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
        else if (Input.GetButtonDown("Jump") && _isGrounded && !_isDashing && !_isCrouching)
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

        if (Input.GetButtonDown("Attack") && !_isCrouching && !_isAttacking && _attackCharged && !_isDashing)
        {
            StartCoroutine(PlayerAttack(0f < _facingDirX));
        }

        if (_isAtDoorwayExit && !_isCrouching && _isGrounded && Input.GetButtonDown("Up"))
        {
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

        const float swordAreaXOffset = 0.9f;
        var attackSwordXOffset = rightSideAttack ? swordAreaXOffset : -swordAreaXOffset;
        _swordBoxCollider.offset = new Vector2(attackSwordXOffset, _swordBoxCollider.offset.y);

        const float attackVisibleTime = 0.125f;
        float elapsedTime1 = 0f;

        _swordBoxCollider.gameObject.SetActive(true);

        while (elapsedTime1 < attackVisibleTime)
        {
            elapsedTime1 += Time.deltaTime;
            yield return null;
        }

        _swordBoxCollider.gameObject.SetActive(false);
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
        _gameUI.SetStamina(_currentDashes);
    }

    void Dash()
    {
        _rigidBody.linearVelocity = new Vector2(_dashDirX * DashSpeed, 0f);
        _dashRegenTimer = 0.0f; // NOTE: Reset dash timer when using dash
    }

    void StopDash()
    {
        _rigidBody.constraints = _defaultRigidbodyConstraints;

        _isDashing = false;
        _rigidBody.linearVelocity = Vector2.zero;
    }

    void DashRegen()
    {
        if (_currentDashes < MaxDashes)
        {
            _dashRegenTimer += Time.deltaTime;

            if (DashRechargeTime <= _dashRegenTimer)
            {
                _currentDashes++;
                _dashRegenTimer = 0f;
                _gameUI.SetStamina(_currentDashes);
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
