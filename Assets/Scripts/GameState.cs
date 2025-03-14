using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }

    public List<Level> GameLevels = new();

    public PlayerCharacter PlayerPrefab;
    public MainCamera MainCameraPrefab;
    public Light2D GlobalLight;

    private SpriteRenderer _backgroundRenderer;
    private Level _currentLevel;
    private PlayerCharacter _player;
    private MainCamera _mainCamera;
    private Vector3 _prevCameraPosition;

    private int _currentLevelIndex = 0;
    private readonly float _cameraZOffset = -50.0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        var bgRenderer = transform.Find("BackgroundRenderer");

        if (bgRenderer == null)
        {
            Debug.LogError($"Did not find BackgroundRenderer in {nameof(GameState)}script.");
        }

        if (!bgRenderer.TryGetComponent(out _backgroundRenderer))
        {
            Debug.LogError($"{nameof(SpriteRenderer)} not found on {nameof(GameState)}");
        }

        _backgroundRenderer.sortingLayerName = "Background";

        _player = Instantiate(PlayerPrefab, Vector3.zero, Quaternion.identity);
        _mainCamera = Instantiate(MainCameraPrefab, new(0, 0, -10f), Quaternion.identity);
    }

    private void Start()
    {
        GameLevels = CreateGameLevelLayout();

        _prevCameraPosition = _mainCamera.transform.position;
        _mainCamera.SetFollowTarget(_player.transform);

        LoadNextLevel();

        GlobalLight.intensity = _currentLevel.GlobalLightLevel;
    }

    private void Update()
    {
        if (_currentLevel.ParallaxBackground)
        {
            BGParallaxEffect();
        }

        _prevCameraPosition = _mainCamera.transform.position;
    }

    private List<Level> CreateGameLevelLayout()
    {
        return GameLevels;
    }

    private void BGParallaxEffect()
    {
        float parallaxFactor = _currentLevel.ParallaxEffectFactor;
        Vector3 deltaMovement = _mainCamera.transform.position - _prevCameraPosition;

        _backgroundRenderer.transform.position += deltaMovement * parallaxFactor;
    }

    public void LoadLevelIndex(int index)
    {
        if (index < 0 || GameLevels.Count <= index)
        {
            Debug.LogError("Invalid level index!");
            return;
        }

        if (_currentLevel != null)
        {
            Destroy(_currentLevel.gameObject);
            _currentLevel = null;
        }

        _currentLevel = Instantiate(GameLevels[index], Vector3.zero, Quaternion.identity);

        var (bl, tr) = _currentLevel.GetLevelBoundaries();
        var levelBgs = _currentLevel.GetLevelBackgrounds();

        if (levelBgs.Length == 0)
        {
            Debug.LogError($"Level {_currentLevel.name} does not have backgrounds!");
        }

        var usedBgIndex = Random.Range(1, 100) % levelBgs.Length;
        var usedSprite = levelBgs[usedBgIndex];

        Debug.Log($"Used background sprite: {usedSprite}");

        ApplyBackground(bl, tr, usedSprite);

        GlobalLight.intensity = _currentLevel.GlobalLightLevel;
        Debug.Log($"GlobalLight.intensity set = {GlobalLight.intensity}");

        _mainCamera.SetCameraBoundaries(bl, tr);
        _mainCamera.SetDustFXStrength(GlobalLight.intensity * 0.6f);
        _mainCamera.SetFogFXLevel(_currentLevel.HeavyFog);

        var levelEnter = _currentLevel.GetLevelEntrance();

        if (levelEnter != null)
        {
            var newPlayerStart = levelEnter - new Vector3(0, 0.5f, 0);
            _player.transform.position = new Vector3(newPlayerStart.x, newPlayerStart.y, 0);
            _mainCamera.transform.position = new Vector3(newPlayerStart.x, newPlayerStart.y, _cameraZOffset);
            _prevCameraPosition = _mainCamera.transform.position;
        }
        else
        {
            Debug.LogError("LevelEnter not found!");
        }

        _player.ResetPlayerInnerState();
    }

    public void LoadNextLevel()
    {
        LoadLevelIndex(_currentLevelIndex);

        _currentLevelIndex = (_currentLevelIndex + 1) % GameLevels.Count;
    }

    public void ApplyBackground(Vector2 bottomLeft, Vector2 topRight, Sprite background)
    {
        _backgroundRenderer.sprite = background;

        float width = topRight.x - bottomLeft.x;
        float height = topRight.y - bottomLeft.y;

        if (_backgroundRenderer.sprite != null)
        {
            Vector2 spriteSize = _backgroundRenderer.sprite.bounds.size;

            float additionalScaleFactor = _currentLevel.ParallaxBackground ? _currentLevel.ParallaxBackgroundSizeMultiplier : 1.0f;

            // Use the larger scale factor to ensure full coverage
            float scaleFactor = Mathf.Max(width / spriteSize.x, height / spriteSize.y) * additionalScaleFactor;
            _backgroundRenderer.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1);
        }

        // Position the background at the center of the boundary
        Vector3 center = (bottomLeft + topRight) / 2f;
        _backgroundRenderer.transform.position = new Vector3(center.x, center.y, 40);
    }
}
