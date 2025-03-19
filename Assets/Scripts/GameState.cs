using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GameState : MonoBehaviour
{
    public List<Level> GameLevels = new();
    public List<LevelTheme> LevelThemes = new();

    public PlayerCharacter PlayerPrefab;
    public MainCamera MainCameraPrefab;
    public Light2D GlobalLight;

    private Level _currentLevel;
    private PlayerCharacter _player;
    private MainCamera _mainCamera;
    private Vector3 _prevCameraPosition;
    private GameUI _gameUI;

    private SpriteRenderer _backgroundRenderer;
    private Sprite _currentLevelBg = null;
    private Material _levelBGMaterial;
    private Vector2 _bgOffset = new();

    private int _currentLevelIndex = 0;
    private readonly float _cameraZOffset = -1.0f;

    private void Awake()
    {
        var bgRenderer = transform.Find("BackgroundRenderer");

        if (bgRenderer == null)
        {
            Debug.LogError($"Did not find BackgroundRenderer in {nameof(GameState)}script.");
        }

        if (!bgRenderer.TryGetComponent(out _backgroundRenderer))
        {
            Debug.LogError($"{nameof(SpriteRenderer)} not found on {nameof(GameState)}");
        }

        _gameUI = FindFirstObjectByType<GameUI>();

        if (_gameUI == null)
        {
            Debug.LogError($"{nameof(GameUI)} not found on {nameof(PlayerCharacter)}");
        }

        _backgroundRenderer.sortingLayerName = "Background";
        _levelBGMaterial = _backgroundRenderer.material;

        _player = Instantiate(PlayerPrefab, Vector3.zero, Quaternion.identity);
        _mainCamera = Instantiate(MainCameraPrefab, new(0, 0, -10f), Quaternion.identity);
    }

    private void Start()
    {
        GameLevels = CreateGameLevelLayout();

        _prevCameraPosition = _mainCamera.transform.position;
        _mainCamera.SetFollowTarget(_player.transform);

        LoadAndSetLevelIndex(0);
    }

    private void Update()
    {
        if (_currentLevelBg)
        {
            BGImageParallaxScroll();
        }
        else
        {
            SeamlessBackgroundParallaxScroll();
        }

        _prevCameraPosition = _mainCamera.transform.position;
    }

    private List<Level> CreateGameLevelLayout()
    {
        return GameLevels;
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
        _currentLevelBg = _currentLevel.GetLevelBackground();

        var theme = LevelThemes.First(x => x.Theme == _currentLevel.LevelTheme);

        if (_currentLevelBg != null)
        {
            Debug.Log($"Used background sprite: {_currentLevelBg.name}");
            ApplyBackgroundImage(bl, tr, _currentLevelBg);
        }
        else
        {
            Debug.Log($"Used background seamless image: {""}");
            ApplySeamlessBackground(theme.SeamlessBackgrounds.First());
        }

        var lightIntensity = GetLightLevelValue(_currentLevel.LightLevel);
        GlobalLight.intensity = lightIntensity;

        Debug.Log($"GlobalLight.intensity set = {GlobalLight.intensity}");

        _mainCamera.SetCameraBoundaries(bl, tr);
        _mainCamera.SetDustFXStrength(GlobalLight.intensity * 0.6f);
        _mainCamera.SetFogFXLevel(_currentLevel.HeavyFog, _currentLevel.FogColorMultiplier);

        var vignetteValue = GetVignetteIntensity(_currentLevel.LightLevel);
        _mainCamera.SetVignetteIntensity(vignetteValue);

        _gameUI.FadeIn(2.0f);

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
        _currentLevelIndex = (_currentLevelIndex + 1) % GameLevels.Count;
        LoadLevelIndex(_currentLevelIndex);
    }

    public void LoadAndSetLevelIndex(int levelIndex)
    {
        _currentLevelIndex = levelIndex;
        LoadLevelIndex(_currentLevelIndex);
    }

    public void RestartLevel()
    {
        LoadLevelIndex(_currentLevelIndex);
    }

    public void ApplyBackgroundImage(Vector2 bottomLeft, Vector2 topRight, Sprite background)
    {
        _backgroundRenderer.sprite = background;

        float width = topRight.x - bottomLeft.x;
        float height = topRight.y - bottomLeft.y;

        if (_backgroundRenderer.sprite != null)
        {
            Vector2 spriteSize = _backgroundRenderer.sprite.bounds.size;

            float additionalScaleFactor = _currentLevel.ParallaxBackground ? Level.ParallaxBackgroundSizeMultiplier : 1.0f;

            // Use the larger scale factor to ensure full coverage
            float scaleFactor = Mathf.Max(width / spriteSize.x, height / spriteSize.y) * additionalScaleFactor;
            _backgroundRenderer.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1);
        }

        // Position the background at the center of the boundary
        Vector3 center = (bottomLeft + topRight) / 2f;
        _backgroundRenderer.transform.position = new Vector3(center.x, center.y, 40);
    }

    public void ApplySeamlessBackground(Sprite background)
    {
        _backgroundRenderer.sprite = background;

        var cameraView = _mainCamera.GetCameraViewSize();

        float spriteHeight = _backgroundRenderer.sprite.bounds.size.y;
        float spriteWidth = _backgroundRenderer.sprite.bounds.size.x;

        float spriteAspectRatio = spriteWidth / spriteHeight;
        float cameraAspectRatio = cameraView.x / cameraView.y;

        Vector3 newScale = transform.localScale;

        if (cameraAspectRatio > spriteAspectRatio)
        {
            // If the camera is wider
            newScale.x = cameraView.x / spriteWidth;
            newScale.y = newScale.x; 
        }
        else
        {
            // If the camera is taller
            newScale.y = cameraView.y / spriteHeight;
            newScale.x = newScale.y;
        }

        _backgroundRenderer.transform.localScale = newScale * 1.1f;
        _backgroundRenderer.transform.position = (Vector2)_mainCamera.transform.position;
    }

    private void BGImageParallaxScroll()
    {
        float parallaxFactor = Level.ParallaxEffectFactor;
        Vector2 deltaMovement = _mainCamera.transform.position - _prevCameraPosition;

        _backgroundRenderer.transform.position += (Vector3)deltaMovement * parallaxFactor;
    }

    void SeamlessBackgroundParallaxScroll()
    {
        Vector2 deltaMovement = _mainCamera.transform.position - _prevCameraPosition;

        _bgOffset += deltaMovement / 28f;

        _levelBGMaterial.SetVector("_UVOffset", (Vector4)_bgOffset);
        _backgroundRenderer.transform.position = (Vector2)_mainCamera.transform.position;
    }

    float GetLightLevelValue(LevelLightLevels level)
    {
        return level switch
        {
            LevelLightLevels.PitchBlack => 0f,
            LevelLightLevels.VeryDark => 0.025f,
            LevelLightLevels.Dark => 0.1f,
            LevelLightLevels.Dim => 0.25f,
            LevelLightLevels.Normal => 0.4f,
            LevelLightLevels.Bright => 0.55f,
            LevelLightLevels.VeryBright => 0.75f,
            _ => 0f
        };
    }

    float GetVignetteIntensity(LevelLightLevels level)
    {
        const float min = 0.2f;
        const float max = 0.5f;

        return level switch
        {
            LevelLightLevels.PitchBlack => max,
            LevelLightLevels.VeryDark => max,
            LevelLightLevels.Dark => max,

            LevelLightLevels.Normal => min,
            LevelLightLevels.Bright => min,
            LevelLightLevels.VeryBright => min,

            _ => min
        };
    }
}
