using System.Collections;
using TMPro;
using UnityEngine;

public static class PlayerColors
{
    public static Color BloodstoneRedColor = new(0.61f, 0.13f, 0.12f);
    public static Color DarkRustColor = new(0.4f, 0.14f, 0.14f);
    public static Color RoyalPlumColor = new(0.29f, 0.16f, 0.42f);
    public static Color ForestGreenColor = new(0.24f, 0.44f, 0.22f);
    public static Color DarkAquaColor = new(0.12f, 0.38f, 0.33f);
    public static Color StormyBlueColor = new(0.08f, 0.25f, 0.31f);
    public static Color OceanBlueColor = new(0.08f, 0.19f, 0.6f);
    public static Color AntiqueGoldColor = new(0.70f, 0.64f, 0.0f);
    public static Color IceSlayerColor = new(0.23f, 0.54f, 0.56f);
    public static Color OrangeManColor = new(0.79f, 0.40f, 0.13f);

    public const string BloodstoneRedStr = "BloodstoneRed";
    public const string DarkRustStr = "DarkRust";
    public const string RoyalPlumStr = "RoyalPlum";
    public const string ForestGreenStr = "ForestGreen";
    public const string DarkAquaStr = "DarkAqua";
    public const string StormyBlueStr = "StormyBlue";
    public const string OceanBlueStr = "OceanBlue";
    public const string AntiqueGoldStr = "AntiqueGold";
    public const string IceSlayerStr = "IceSlayer";
    public const string OrangeManStr = "OrangeMan";

    public static Color StringToColor(string str)
    {
        return str switch
        {
            BloodstoneRedStr => BloodstoneRedColor,
            DarkRustStr => DarkRustColor,
            RoyalPlumStr => RoyalPlumColor,
            ForestGreenStr => ForestGreenColor,
            DarkAquaStr => DarkAquaColor,
            StormyBlueStr => StormyBlueColor,
            OceanBlueStr => OceanBlueColor,
            AntiqueGoldStr => AntiqueGoldColor,
            IceSlayerStr => IceSlayerColor,
            OrangeManStr => OrangeManColor,
            _ => throw new System.Exception($"Unhandled color str {str}"),
        };
    }
}

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerCharacter : MonoBehaviour
{
    private AudioSource[] _playerSoundAudioSources = new AudioSource[3];
    private AudioSource _playerVoiceAudioSource;

    public AudioClip DamageTakenVoiceClip;
    public AudioClip DeathVoiceClip;
    public AudioClip FallDeathVoiceClip;

    public AudioClip[] WalkSoundClips;

    public AudioClip AttackSoundClip;
    public AudioClip AttackMissSoundClip;
    public AudioClip DashsoundClip;
    public AudioClip DamageTakenSoundClip;
    public AudioClip JumpSoundClip;
    public AudioClip JumpLandSoundClip;
    public AudioClip PowerAttackSound;

    public Transform GroundCheck;

    public LayerMask GroundLayer;
    public LayerMask DamageEnvLayer;
    public LayerMask PlatformLayer;

    public Material NormalMaterial;
    public Material ShadowMaterial;

    public Sprite AttackSprite;
    public Sprite IdleSprite;
    public Sprite AirSprite;
    public Sprite DashSprite;
    public Sprite CrouchSprite;
    public Sprite HitSprite;
    public Sprite MoveSprite;
    public Sprite DeadSprite;

    public float MoveSpeed = 5f;
    public float JumpForce = 17f;
    public float DashSpeed = 15f;
    public float DashingTime = 0.175f;
    public float DashRechargeTime = 1.5f;
    public float AttackSpeed = 0.5f;
    public float DamageInvulnerabilityTime = 3.0f;

    public bool DebugLogging = false;
    public int MaxDashes = 2;

    private Animator _animator;

    private Rigidbody2D _rigidBody;
    private SpriteRenderer _spriteRenderer;
    private CapsuleCollider2D _physicsCollider;
    private CapsuleCollider2D _swordBoxCollider;
    private Collider2D _platformFallthrough;
    private Material _material;
    private TextMeshPro _floatingText;
    private GameUI _gameUI;
    private GameState _gameState;
    private GlobalAudio _globalAudio;
    private ShadowEnemyEffects _shadowEnemyEffects;

    private Coroutine _playerAttackCoroutine;

    private Vector2 _groundCheckSize = new(0.225f, 0.25f);
    private Vector2 _originalSize;
    private Vector2 _originalOffset;

    private int _currentDashes = 0;
    private int _currentHealth = 3;
    private int _lastSoundSourceIndex = 0;

    private bool _controlsAreActive = true;
    private bool _isAtDoorwayExit = false;
    private bool _hasGroundedFeet = false;
    private bool _isPlatformGrounded = false;
    private bool _isGroundGrounded = false;
    private bool _isDamageEnvGrounded = false;
    private bool _isDashing = false;
    public bool IsCrouching = false;
    private bool _isAttacking = false;
    private bool _hasDamageInvulnerability = false;
    private bool _hasAttackDamageInvulnerability = false;
    private bool _inDamageState = false;
    private bool _isDead = false;
    private bool _isLanding = false;

    public bool HasShadowPowers = false;

    public float _lastAttackTime = 0f;
    public float FacingDirX = 0f;
    private float _dashDirX = 0f;
    private float _landedTime = 0f;
    private float _dashTimer = 0f;
    private float _dashRegenTimer = 0f;
    private float _groundedTime = 0f;
    private float _inAirTime = 0f;

    private int _damageZoneLayer;

    private Color _playerColor;

    private readonly float _platformFallthroughRaycastDistance = 1.0f;
    private readonly float _newJumpVelocityThreshold = 0.05f;
    private readonly float _newJumpTimeCooldown = 0.05f;

    private readonly RigidbodyConstraints2D _defaultRigidbodyConstraints = RigidbodyConstraints2D.None | RigidbodyConstraints2D.FreezeRotation;
    private readonly RigidbodyConstraints2D _dashingRigidbodyConstraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
    private readonly RigidbodyConstraints2D _crouchingRigidbodyConstraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;

    void Awake()
    {
        var soundSource1 = gameObject.AddComponent<AudioSource>();
        var soundSource2 = gameObject.AddComponent<AudioSource>();

        _playerSoundAudioSources[0] = soundSource1;
        _playerSoundAudioSources[1] = soundSource2;
        _playerSoundAudioSources[2] = soundSource2;

        _playerVoiceAudioSource = gameObject.AddComponent<AudioSource>();

        _playerSoundAudioSources[0].playOnAwake = false;
        _playerSoundAudioSources[1].playOnAwake = false;
        _playerSoundAudioSources[2].playOnAwake = false;
        _playerVoiceAudioSource.playOnAwake = false;

        _playerSoundAudioSources[0].spatialBlend = 1.0f;
        _playerSoundAudioSources[1].spatialBlend = 1.0f;
        _playerSoundAudioSources[2].spatialBlend = 1.0f;
        _playerVoiceAudioSource.spatialBlend = 1.0f;

        _playerSoundAudioSources[0].minDistance = 1.0f;
        _playerSoundAudioSources[1].minDistance = 1.0f;
        _playerSoundAudioSources[2].minDistance = 1.0f;
        _playerVoiceAudioSource.minDistance = 1.0f;

        _playerSoundAudioSources[0].maxDistance = 10.0f;
        _playerSoundAudioSources[1].maxDistance = 10.0f;
        _playerSoundAudioSources[2].maxDistance = 10.0f;
        _playerVoiceAudioSource.maxDistance = 10.0f;

        _playerSoundAudioSources[0].rolloffMode = AudioRolloffMode.Linear;
        _playerSoundAudioSources[1].rolloffMode = AudioRolloffMode.Linear;
        _playerSoundAudioSources[2].rolloffMode = AudioRolloffMode.Linear;
        _playerVoiceAudioSource.rolloffMode = AudioRolloffMode.Linear;

        if (!TryGetComponent(out _shadowEnemyEffects))
        {
            Debug.LogError($"{nameof(ShadowEnemyEffects)} not found on {nameof(PlayerCharacter)}");
        }
        
        if (!TryGetComponent(out _animator))
        {
            Debug.LogError($"{nameof(Animator)} not found on {nameof(PlayerCharacter)}");
        }

        if (!TryGetComponent(out _rigidBody))
        {
            Debug.LogError($"{nameof(Rigidbody2D)} not found on {nameof(PlayerCharacter)}");
        }

        if (!transform.Find("Sprite").TryGetComponent(out _spriteRenderer))
        {
            Debug.LogError($"{nameof(SpriteRenderer)} not found on {nameof(PlayerCharacter)}");
        }

        if (!TryGetComponent(out _physicsCollider))
        {
            Debug.LogError($"{nameof(CapsuleCollider2D)} not found on {nameof(PlayerCharacter)}");
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
            Debug.LogError($"{nameof(CapsuleCollider2D)} not found on {nameof(PlayerCharacter)} SwordArea child");
        }

        _damageZoneLayer = LayerMask.NameToLayer("DamageZone");

        _gameUI = FindFirstObjectByType<GameUI>();

        if (_gameUI == null)
        {
            Debug.LogError($"{nameof(GameUI)} not found on {nameof(PlayerCharacter)}");
        }

        _globalAudio = FindFirstObjectByType<GlobalAudio>();

        if (_globalAudio == null)
        {
            Debug.LogError($"{nameof(GlobalAudio)} not found on {nameof(PlayerCharacter)}");
        }

        _gameState = FindFirstObjectByType<GameState>();

        if (_gameState == null)
        {
            Debug.LogError($"{nameof(GameState)} not found on {nameof(PlayerCharacter)}");
        }

        _swordBoxCollider.gameObject.SetActive(false);

        _originalSize = _physicsCollider.size;
        _originalOffset = _physicsCollider.offset;

        _material.SetColor("_NewColor", new(0f, 0f, 1f));

        _playerVoiceAudioSource = gameObject.AddComponent<AudioSource>();
        _playerVoiceAudioSource.spatialBlend = 0.9f;
    }

    private void Start()
    {
        ResetPlayerInnerState(false);
        SetPlayerHealth(_gameUI.DefaultPlayerHealth);
    }

    void Update()
    {
        if (_gameState.GetGameState() == GameStateType.PauseMenu)
        {
            return;
        }
        
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

        if (IsCrouching || _isDashing)
        {
            _physicsCollider.size = new Vector2(_originalSize.x, 0.33f);
            _physicsCollider.offset = new Vector2(_originalOffset.x, _originalOffset.y - 0.33f);

            Debug.DrawRay(transform.position, Vector2.down * _platformFallthroughRaycastDistance, Color.red);
        }
        else
        {
            _physicsCollider.size = _originalSize;
            _physicsCollider.offset = _originalOffset;
        }

        Color color = _hasGroundedFeet ? Color.green : Color.red;
        DebugUtil.DrawRectangle(GroundCheck.position, _groundCheckSize, color);

        if (!_isAttacking && !_isDashing)
        {
            _spriteRenderer.flipX = FacingDirX < 0.0f;
        }

        bool moving = _hasGroundedFeet && 0.01f < Mathf.Abs(_rigidBody.linearVelocity.x);

        // ---------------
        // Get animation

        string usedAnim;

        if (_isDead)
        {
            usedAnim = "PlayerDeath1";
        }
        else if (_inDamageState)
        {
            usedAnim = "PlayerHit1";
        }
        else if (_isAttacking)
        {
            usedAnim = "PlayerAttack1";
        }
        else if (_isDashing)
        {
            usedAnim = "PlayerDash1";
        }
        else if (IsCrouching)
        {
            usedAnim = "PlayerCrouch1";
        }
        else if (_isLanding)
        {
            usedAnim = "PlayerLand1";
        }
        else if (!_isPlatformGrounded && _rigidBody.linearVelocityY < -0.5f)
        {
            usedAnim = "PlayerFall1";
        }
        else if (!IsReadyToJumpAgain() 
            && !(_isPlatformGrounded && _rigidBody.linearVelocityY < 1.0f)) // NOTE: Case to ignore when clipping at platform edges
        {
            usedAnim = "PlayerAir1";
        }
        else if (moving)
        {
            usedAnim = "PlayerMove1";
        }
        else
        {
            usedAnim = "PlayerIdle1";
        }

        _animator.Play(usedAnim);
    }

    public void FixedUpdate()
    {
        _isPlatformGrounded = Physics2D.OverlapBox(GroundCheck.position, _groundCheckSize, 0, PlatformLayer);
        _isGroundGrounded = Physics2D.OverlapBox(GroundCheck.position, _groundCheckSize, 0, DamageEnvLayer);
        _isDamageEnvGrounded = Physics2D.OverlapBox(GroundCheck.position, _groundCheckSize, 0, GroundLayer);

        bool previousFrameGrounded = _hasGroundedFeet;

        _hasGroundedFeet = _isPlatformGrounded || _isGroundGrounded || _isDamageEnvGrounded;

        const float stompSoundTreshold = -3f;

        if (!previousFrameGrounded && _hasGroundedFeet && _rigidBody.linearVelocityY < stompSoundTreshold)
        {
            _isLanding = true;
            _landedTime = Time.time;
            PlaySoundSource(JumpLandSoundClip, 0.5f);
        }

        const float landingTime = 0.12f;

        if (_isLanding && landingTime < Mathf.Abs(_landedTime - Time.time))
        {
            _isLanding = false;
        }

        if (_hasGroundedFeet)
        {
            _inAirTime = 0f;
            _groundedTime += Time.fixedDeltaTime;
        }
        else
        {
            _groundedTime = 0f;
            _inAirTime += Time.fixedDeltaTime;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_hasGroundedFeet)
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
            ShowText("Exit Level", Color.white);
        }
        else if (other.CompareTag("PlayerFallDeath"))
        {
            TriggerPlayerFallDeath();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Spikes") && _hasAttackDamageInvulnerability)
        {
            return;
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("DamageZone") || other.gameObject.layer == LayerMask.NameToLayer("EnvDamageZone"))
        {
            Vector2 collisionDirection = (transform.position - other.transform.position).normalized;
            TryRecieveDamage(collisionDirection);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Doorway"))
        {
            _isAtDoorwayExit = false;
            HideText();
        }
    }

    public void SetNormalVisuals()
    {
        _spriteRenderer.material = NormalMaterial;
        _material = _spriteRenderer.material;

        SetPlayerColor(_playerColor);
        _shadowEnemyEffects.EnableShadowEffects(false);
    }

    public void SetShadowVisuals()
    {
        _spriteRenderer.material = ShadowMaterial;
        _material = _spriteRenderer.material;

        if (TryGetComponent<ShadowEnemyEffects>(out var shadowEffect))
        {
            _material.SetFloat("_OutlineThickness", shadowEffect.OutlineThickness);
            _material.SetColor("_InlineColor", shadowEffect.InlineColor);
            _material.SetColor("_OutlineColor", shadowEffect.OutlineColor);
            _material.SetColor("_DamageColor", new(0, 0, 0));
        }
        else
        {
            Debug.LogError($"{nameof(ShadowEnemyEffects)} not found on {nameof(PlayerCharacter)}");
        }

        _material.SetFloat("_IsShadowVariant", 1f);

        _shadowEnemyEffects.EnableShadowEffects(true);
    }

    private void TriggerPlayerFallDeath()
    {
        StartCoroutine(PlayerFallingDieAndLevelRestart());
    }

    public void SetPlayerColor(Color color)
    {
        _playerColor = color;

        if (_material != null)
        {
            _material.SetColor("_NewColor", _playerColor);
        }
    }

    public void TryRecieveDamage(Vector2 damageDir)
    {
        if (_hasDamageInvulnerability || HasShadowPowers)
        {
            return;
        }

        _currentHealth -= 1;

        _gameUI.SetHealth(_currentHealth);

        Debug.Log($"Player took damage, health remaining: {_currentHealth}");

        StopDash();
        ApplyDamageKnockback(damageDir);

        if (_currentHealth <= 0)
        {
            StartCoroutine(PlayerDieAndLevelRestart());
        }
        else
        {
            StartCoroutine(ActivateDamageTakenTime(DamageInvulnerabilityTime));
        }
    }

    IEnumerator PlayerFallingDieAndLevelRestart()
    {
        StopPlayerAttack();
        ControlsEnabled(false);

        _globalAudio.StopMusic(0f);

        _globalAudio.PlayFallFailureSoundFX();

        _isDead = true;

        yield return new WaitForSeconds(0.1f);

        _globalAudio.PlaySoundEffect(FallDeathVoiceClip, 0.3f);

        yield return new WaitForSeconds(0.5f);

        _gameUI.HidePlayerStats(true);
        _gameUI.FadeOut(1.5f);

        yield return new WaitForSeconds(2.5f);

        _gameState.GameOverScreen();
    }

    IEnumerator PlayerDieAndLevelRestart()
    {
        _rigidBody.constraints = _defaultRigidbodyConstraints;

        StopPlayerAttack();
        PlaySoundSource(DamageTakenSoundClip);
        PlayVoiceSource(DamageTakenVoiceClip, 0.5f);
        ControlsEnabled(false);

        _globalAudio.StopMusic(4f);

        _hasDamageInvulnerability = true;
        _inDamageState = true;

        yield return new WaitForSeconds(0.8f);

        _globalAudio.PlayFallFailureSoundFX();
        PlayVoiceSource(DeathVoiceClip, 1f);
        _isDead = true;

        yield return new WaitForSeconds(2.0f);

        _gameUI.HidePlayerStats(true);
        _gameUI.FadeOut(1.5f);

        yield return new WaitForSeconds(2.5f);

        _gameState.GameOverScreen();
    }

    private void ApplyDamageKnockback(Vector2 knockbackDir)
    {
        var knockbackDirForce = new Vector2(knockbackDir.normalized.x, 6.5f);
        _rigidBody.linearVelocity = knockbackDirForce;
    }

    private IEnumerator ActivateDamageTakenTime(float duration)
    {
        StopPlayerAttack();
        PlaySoundSource(DamageTakenSoundClip);
        PlayVoiceSource(DamageTakenVoiceClip, 0.5f);
        ControlsEnabled(false);
        _inDamageState = true;
        _hasDamageInvulnerability = true;

        _spriteRenderer.color = Color.red;

        const float controlsInactive = 0.35f;
        yield return new WaitForSeconds(controlsInactive);

        _spriteRenderer.color = Color.gray;

        _inDamageState = false;
        ControlsEnabled(true);

        float invulnerabilityTime = Mathf.Max(0, duration - controlsInactive);

        yield return new WaitForSeconds(invulnerabilityTime);
        _hasDamageInvulnerability = false;

        _spriteRenderer.color = Color.white;
    }


    public void ResetPlayerInnerState(bool restartLevel)
    {
        _isLanding = false;
        HasShadowPowers = false;
        _isAtDoorwayExit = false;
        _hasGroundedFeet = false;
        _isDashing = false;
        IsCrouching = false;
        _isAttacking = false;
        _hasDamageInvulnerability = false;
        _hasAttackDamageInvulnerability = false;
        _isDead = false;
        _inDamageState = false;

        ControlsEnabled(true);

        FacingDirX = 1f; // NOTE: Facing right
        _dashDirX = 0f;
        _dashTimer = 0f;
        _dashRegenTimer = 0f;

        if (_currentHealth < 3 || restartLevel)
        {
            _currentHealth = 3;
            _gameUI.SetHealth(_currentHealth);
        }

        _currentDashes = MaxDashes;

        if (_gameUI != null)
        {
            _gameUI.SetStamina(_currentDashes);
        }

        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = Color.white;
        }

        SetNormalVisuals();

        HideText();
    }

    void PlayerMovementControls()
    {
        if (!_controlsAreActive || _isDead)
        {
            return;
        }

        // -----------------------------------
        // Get horizontal movement and slide

        float moveInput = Input.GetAxisRaw("Horizontal");

        if (moveInput < -0.001f)
        {
            moveInput = -1f;
        }
        else if (0.001f < moveInput)
        {
            moveInput = 1f;
        }

        var speed = MoveSpeed * (HasShadowPowers ? 1.5f : 1f);

        var newMovement = new Vector2(moveInput * speed, _rigidBody.linearVelocity.y);

        if (Input.GetButton("Crouch") && _hasGroundedFeet && !_isDashing)
        {
            var crouchInit = !IsCrouching;

            if (crouchInit)
            {
                _rigidBody.linearVelocity = new();
            }

            IsCrouching = true;
        }
        else
        {
            IsCrouching = false;
            _rigidBody.linearVelocity = newMovement;
        }

        if (0f < Mathf.Abs(moveInput))
        {
            FacingDirX = Mathf.Ceil(moveInput);
        }

        if (IsCrouching)
        {
            _rigidBody.constraints = _crouchingRigidbodyConstraints;
        }
        else if (_isDashing)
        {
            _rigidBody.constraints = _dashingRigidbodyConstraints;
        }
        else
        {
            _rigidBody.constraints = _defaultRigidbodyConstraints;
        }

        // --------------------
        // Get action buttons

        if (IsCrouching && Input.GetButtonDown("Jump") && _hasGroundedFeet && !_isDashing && IsCrouching)
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
        else if (Input.GetButtonDown("Jump") && _hasGroundedFeet && !_isDashing && !IsCrouching)
        {
            if (IsReadyToJumpAgain())
            {
                var pitch = Random.Range(0.95f, 1.1f);
                PlaySoundSource(JumpSoundClip, pitch: pitch);
                _rigidBody.linearVelocity = new Vector2(_rigidBody.linearVelocity.x, JumpForce);
                _groundedTime = 0.0f;
            }
        }
        else if (Input.GetButtonDown("Dash") && !_isDashing && !_isAttacking)
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

        if (Input.GetButton("Attack") && !IsCrouching && !_isDashing)
        {
            if (!_isAttacking)
            {
                _playerAttackCoroutine = StartCoroutine(PlayerAttack(0f < FacingDirX));
            }
        }

        float crouchAxis = Input.GetAxisRaw("Vertical");

        if (_isAtDoorwayExit && !IsCrouching && _hasGroundedFeet && (Input.GetButton("Up") || crouchAxis < -0.01f))
        {
            _gameState.LoadNextLevel();
        }
    }

    bool IsReadyToJumpAgain()
    {
        return _rigidBody.linearVelocityY <= _newJumpVelocityThreshold && _newJumpTimeCooldown < _groundedTime;
    }

    private void StopPlayerAttack()
    {
        if (_playerAttackCoroutine != null)
        {
            StopCoroutine(_playerAttackCoroutine);
        }

        _isAttacking = false;
        _hasAttackDamageInvulnerability = false;
        _swordBoxCollider.gameObject.SetActive(false);
    }

    private IEnumerator PlayerAttack(bool rightSideAttack)
    {
        _isAttacking = true;

        while (Input.GetButton("Attack") && _isAttacking)
        {
            _animator.Play("PlayerAttack1", -1, 0f);

            _lastAttackTime = Time.time;

            const float attackPreSwingTime = 0.125f;
            yield return new WaitForSeconds(attackPreSwingTime);

            var attackPitch = Random.Range(0.95f, 1.15f);
            PlaySoundSource(AttackSoundClip, pitch: attackPitch);

            if (HasShadowPowers)
            {
                PlaySoundSource(PowerAttackSound, 0.55f, attackPitch);
            }

            if (_isDashing)
            {
                StopPlayerAttack();
                yield break;
            }

            _hasAttackDamageInvulnerability = true;
            _swordBoxCollider.gameObject.SetActive(true);

            float swordAreaXOffset = Mathf.Abs(_swordBoxCollider.offset.x);
            var attackSwordXOffset = rightSideAttack ? swordAreaXOffset : -swordAreaXOffset;
            _swordBoxCollider.offset = new Vector2(attackSwordXOffset, _swordBoxCollider.offset.y);

            const float attackVisibleTime = 0.35f;
            const float part01 = attackVisibleTime - 0.15f;
            yield return new WaitForSeconds(part01);

            if (_hasGroundedFeet)
            {
                var pitch = Random.Range(0.95f, 1.1f);
                PlaySoundSource(AttackMissSoundClip, pitch);
            }

            yield return new WaitForSeconds(attackVisibleTime - part01);

            if (_isDashing)
            {
                StopPlayerAttack();
                yield break;
            }

            _hasAttackDamageInvulnerability = false;
            _swordBoxCollider.gameObject.SetActive(false);
        }

        _isAttacking = false;
    }

    private IEnumerator DisablePlatformCollisionForTime(Collider2D platformCollider, float time)
    {
        DebugLog($"DisableCollisionForTime start {platformCollider.name}");

        Physics2D.IgnoreCollision(_physicsCollider, platformCollider, true);

        yield return new WaitForSeconds(time);

        Physics2D.IgnoreCollision(_physicsCollider, platformCollider, false);
    }

    public void GetHealthFromPickup(int health)
    {
        if (health <= _currentHealth)
        {
            return;
        }

        SetPlayerHealth(health);
    }

    public void SetPlayerHealth(int health)
    {
        if (health <= 0)
        {
            Debug.LogWarning($"Unexpected route to player death from SetPlayerHealth().");

            health = 0;
            StartCoroutine(PlayerDieAndLevelRestart());
        }

        if (_gameUI.MaxPlayerHealth < health)
        {
            Debug.LogWarning($"SetPlayerHealth() called with {health}, but {_gameUI.MaxPlayerHealth} is max player health.");

            health = _gameUI.MaxPlayerHealth;
        }

        _currentHealth = health;
        _gameUI.SetHealth(_currentHealth);
    }

    void ControlsEnabled(bool isEnabled)
    {
        _controlsAreActive = isEnabled;
    }

    void StartDash()
    {
        var pitch = Random.Range(0.9f, 1.15f);
        PlaySoundSource(DashsoundClip, pitch: pitch);

        _isDashing = true;
        _dashTimer = 0f;
        _dashDirX = FacingDirX;
        _currentDashes--;
        _gameUI.SetStamina(_currentDashes);

        _isAttacking = false;
    }

    void Dash()
    {
        var speed = _dashDirX * DashSpeed * (HasShadowPowers ? 1.5f : 1f);
        _rigidBody.linearVelocity = new Vector2(speed, 0f);
        _dashRegenTimer = 0.0f; // NOTE: Reset dash timer when using dash
    }

    void StopDash()
    {
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

    public void PlayVoiceSource(AudioClip sound, float volume = 1f)
    {
        _playerVoiceAudioSource.loop = false;
        _playerVoiceAudioSource.volume = volume;
        _playerVoiceAudioSource.clip = sound;
        _playerVoiceAudioSource.Play();
    }

    public void PlaySoundSource(AudioClip sound, float volume = 1f, float pitch = 1f)
    {
        var usedIndex = _lastSoundSourceIndex++ % _playerSoundAudioSources.Length;

        _playerSoundAudioSources[usedIndex].pitch = pitch;
        _playerSoundAudioSources[usedIndex].loop = false;
        _playerSoundAudioSources[usedIndex].volume = volume;
        _playerSoundAudioSources[usedIndex].clip = sound;
        _playerSoundAudioSources[usedIndex].Play();
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

    public bool IsDead()
    {
        return _isDead;
    }

    void PlayFootstep()
    {
        AudioClip usedClip = WalkSoundClips[Random.Range(0, WalkSoundClips.Length)];

        var pitch = Random.Range(0.85f, 1.15f);
        var volume = Random.Range(0.17f, 0.23f);

        PlaySoundSource(usedClip, volume, pitch);
    }

    private Coroutine _godModeCoroutine = null;

    public void StartGodmode(float time)
    {
        if (_godModeCoroutine != null)
        {
            StopCoroutine(_godModeCoroutine);
        }

        _godModeCoroutine = StartCoroutine(GodModeTimer(time));
    }

    IEnumerator GodModeTimer(float time)
    {
        HasShadowPowers = true;

        SetShadowVisuals();

        yield return new WaitForSeconds(time);

        SetNormalVisuals();

        yield return new WaitForSeconds(1f);

        HasShadowPowers = false;
    }
}
