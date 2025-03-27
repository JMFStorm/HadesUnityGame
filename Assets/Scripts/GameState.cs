using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public enum GameStateType
{
    IntroScreen,
    MainMenu,
    MainGame,
    Cutscene,
    PauseMenu,
    GameOverMenu
}

public class GameState : MonoBehaviour
{
    public List<Level> GameLevels = new();

    public PlayerCharacter PlayerPrefab;
    public MainCamera MainCameraPrefab;
    public Light2D GlobalLight;
    public bool SkipIntro = false;

    private GameStateType _gameState;

    private Level _currentLevel;
    private PlayerCharacter _player;
    private MainCamera _mainCamera;
    private Vector3 _prevCameraPosition;
    private GlobalAudio _globalAudio;
    private GameUI _gameUI;
    private Color _playerColor = PlayerColors.BloodstoneRedColor;

    private SpriteRenderer _backgroundRenderer;
    private Sprite _currentLevelBg = null;
    private Material _levelBGMaterial;
    private Vector2 _bgOffset = new();

    private float _bgUVMultiplier = new();

    private int _currentLevelIndex = 0;
    private readonly float _cameraZOffset = -1.0f;

    private int _savedLevelIndex = -1;
    private string _savedLevelName = string.Empty;

    private readonly Color _playerDefaultColor = PlayerColors.BloodstoneRedColor;

    private void Awake()
    {
        if (!transform.Find("UICanvas").TryGetComponent(out _gameUI))
        {
            Debug.LogError($"Did not find {nameof(GameUI)} in child of UICanvas in {nameof(GameState)}.");
        }

        var bgRenderer = transform.Find("BackgroundRenderer");

        if (bgRenderer == null)
        {
            Debug.LogError($"Did not find BackgroundRenderer in {nameof(GameState)}.");
        }

        if (!bgRenderer.TryGetComponent(out _backgroundRenderer))
        {
            Debug.LogError($"{nameof(SpriteRenderer)} not found on {nameof(GameState)}");
        }

        _globalAudio = FindFirstObjectByType<GlobalAudio>();

        if (_globalAudio == null)
        {
            Debug.LogError($"{nameof(GlobalAudio)} not found on {nameof(GameState)}");
        }

        _backgroundRenderer.sortingLayerName = "Background";
        _levelBGMaterial = _backgroundRenderer.material;

        LoadPersistentStorage();
    }

    private void Start()
    {
        _mainCamera = Instantiate(MainCameraPrefab, new(0, 0, -1f), Quaternion.identity);
        _mainCamera.SetDustFXStrength(0);
        _mainCamera.SetFogFXLevel(false, new(0, 0, 0));

        if (Debug.isDebugBuild && SkipIntro)
        {
            StartCoroutine(StartNewGame(true));
        }
        else
        {
            InitGameIntroTitle();
        }
    }

    private void Update()
    {
        if (_gameState == GameStateType.MainGame)
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

        var gameState = GetGameState();

        var cursorVisible = true;

        if (gameState == GameStateType.IntroScreen || gameState == GameStateType.MainGame || gameState == GameStateType.Cutscene)
        {
            cursorVisible = false;
        }

        Cursor.visible = cursorVisible;

        if (Input.GetButtonDown("Escape"))
        {
            if (gameState == GameStateType.IntroScreen)
            {
                _gameUI.SkipIntroSequence();
            }
            else if (gameState == GameStateType.MainGame)
            {
                _gameUI.ActivatePauseMenu(true);
            }
            else if (gameState == GameStateType.PauseMenu)
            {
                _gameUI.ActivatePauseMenu(false);
            }
            else if (gameState == GameStateType.Cutscene)
            {
                Debug.Log("_playCutsceneCoroutine");
                _gameUI.SkipCutscene();
            }
        }
    }

    public void SetGameState(GameStateType type)
    {
        _gameState = type;
    }

    public GameStateType GetGameState()
    {
        return _gameState;
    }

    void InitGameIntroTitle()
    {
        SetGameState(GameStateType.IntroScreen);

        _gameUI.HideMainMenu(true);
        _gameUI.HidePlayerStats(true);

        _globalAudio.PlayAnnouncerVoiceType(AnnouncerVoiceGroup.IntroTile);
        _gameUI.PlayIntroAndThenMainMenu();

        _globalAudio.PlayGlobalMusic(GlobalMusic.TensionBooster1, false, 0.7f);
    }

    public void ClickReturnToMainMenuFromDeathScreen()
    {
        _gameUI.ActivatePauseMenu(false);
        _gameUI.GameOverScreen(false);
        _gameUI.HideMainMenu(false);

        _gameUI.HidePlayerStats(true);

        _globalAudio.StopAmbience();
        _globalAudio.StopMusic(1f);

        ClearBackgroundImage();

        if (_player != null)
        {
            Destroy(_player.gameObject);
        }

        SetGameState(GameStateType.MainMenu);

        LoadPersistentStorage();
    }

    public void ClickReturnToMainMenuFromPauseMenu()
    {
        _gameUI.ActivatePauseMenu(false);
        _gameUI.HideMainMenu(false);

        _gameUI.HidePlayerStats(true);

        ClearCurrentLevel();

        _globalAudio.StopAmbience();
        _globalAudio.StopMusic(1f);

        ClearBackgroundImage();

        if (_player != null)
        {
            Destroy(_player.gameObject);
        }

        SetGameState(GameStateType.MainMenu);

        LoadPersistentStorage();
    }

    public void ClickContinueGameMenuOption()
    {
        _gameUI.HideMainMenu(true);

        InstantiateGlobalGamePrefabs();

        _playerColor = GetSavedPlayerColor();

        _savedLevelIndex = PlayerPrefs.GetInt("LevelIndex");
        _currentLevelIndex = _savedLevelIndex;

        LoadLevelIndex(_savedLevelIndex, false);
    }

    public void ClickStartNewGame()
    {
        SetPlayerColor(_playerDefaultColor);

        _gameUI.HidePlayerColorPanel(false);
        _gameUI.HideMainMenu(true);
    }

    public void ClickPlayerColorPageBetBack()
    {
        _gameUI.HidePlayerColorPanel(true);
        _gameUI.HideMainMenu(false);
    }

    public void ClickStartNewGameFromColorPicker()
    {
        SavePlayerColorStorage(_playerColor);
        StartCoroutine(StartNewGame(false));
    }

    IEnumerator StartNewGame(bool skipCutscene)
    {
        _gameUI.HideMainMenu(true);
        _gameUI.HidePlayerColorPanel(true);

        if (!skipCutscene)
        {
            SetGameState(GameStateType.Cutscene);
            // yield return StartCoroutine(_gameUI.PlayOutroCutscene());
            yield return StartCoroutine(_gameUI.PlayIntroCutscene());
        }

        SetGameState(GameStateType.MainGame);
        InstantiateGlobalGamePrefabs();

        _currentLevelIndex = 0;

        LoadLevelIndex(_currentLevelIndex, false);

        yield return null;
    }

    void InstantiateGlobalGamePrefabs()
    {
        _gameUI.HidePlayerStats(false);
        _player = Instantiate(PlayerPrefab);
        _mainCamera.SetFollowTarget(_player.transform);
    }

    public void QuitGame()
    {
        Debug.Log("Game Quit");

        Application.Quit();
    }

    void ClearCurrentLevel()
    {
        if (_currentLevel != null)
        {
            _currentLevel.ClearArenaEvents();
            Destroy(_currentLevel.gameObject);
            _currentLevel = null;
        }
    }

    public void LoadLevelIndex(int index, bool isRetry)
    {
        if (index < 0 || GameLevels.Count <= index)
        {
            Debug.LogError("Invalid level index!");
            return;
        }

        ClearCurrentLevel();

        _currentLevel = Instantiate(GameLevels[index], Vector3.zero, Quaternion.identity);

        var (bl, tr) = _currentLevel.GetLevelBoundaries();
        _currentLevelBg = _currentLevel.GetLevelBackground();

        if (_currentLevelBg != null)
        {
            Debug.Log($"Used background sprite: {_currentLevelBg.name}");
            ApplyBackgroundImage(bl, tr, _currentLevelBg);
        }
        else
        {
            Debug.Log($"Used background seamless image: {""}");
            ApplySeamlessBackground(_currentLevel.SeamlessBackgrounds.First());
        }

        _globalAudio.StopAmbience();
        _globalAudio.PlayAmbience(_currentLevel.LevelSoundscape);
        _globalAudio.PlayGlobalMusic(_currentLevel.LevelMusic, true, 0.2f);

        var lightIntensity = GetLightLevelValue(_currentLevel.LightLevel);
        GlobalLight.intensity = lightIntensity;

        Debug.Log($"GlobalLight.intensity set = {GlobalLight.intensity}");

        _mainCamera.SetCameraBoundaries(bl, tr);
        _mainCamera.SetDustFXStrength(GlobalLight.intensity * 0.6f);
        _mainCamera.SetFogFXLevel(_currentLevel.HeavyFog, _currentLevel.FogColorMultiplier);

        var vignetteValue = GetVignetteIntensity(_currentLevel.LightLevel);
        _mainCamera.SetVignetteIntensity(vignetteValue);

        _gameUI.HidePlayerStats(false);
        _gameUI.FadeIn(2.0f);

        if (!isRetry && _currentLevel.AnnouncerIntro != null)
        {
            // _globalAudio.PlayAnnouncerVoiceClip(_currentLevel.AnnouncerIntro);

            // NOTE: Cut this feature, use trigger areas instead
        }

        var levelEnter = _currentLevel.GetLevelEntrance();

        if (levelEnter != null)
        {
            var newPlayerStart = levelEnter - new Vector3(0, 1.15f, 0);
            _player.transform.position = new Vector3(newPlayerStart.x, newPlayerStart.y, 0);
            _mainCamera.transform.position = new Vector3(newPlayerStart.x, newPlayerStart.y, _cameraZOffset);
            _prevCameraPosition = _mainCamera.transform.position;
        }
        else
        {
            Debug.LogError("LevelEnter not found!");
        }

        _currentLevel.MakeLevelArenaEvents();

        _player.ResetPlayerInnerState();
        SetPlayerColor(_playerColor);

        Time.timeScale = 1f;

        PlayerPrefs.SetInt("LevelIndex", index);
        PlayerPrefs.SetString("LevelName", _currentLevel.name);
        PlayerPrefs.Save();

        SetGameState(GameStateType.MainGame);
    }

    public void RestartLevel()
    {
        _gameUI.GameOverScreen(false);
        _gameUI.ActivatePauseMenu(false);
        LoadLevelIndex(_currentLevelIndex, true);
    }

    public void LoadNextLevel()
    {
        _currentLevelIndex = (_currentLevelIndex + 1) % GameLevels.Count;
        LoadLevelIndex(_currentLevelIndex, false);
    }

    public void GameOverScreen()
    {
        _currentLevel.gameObject.SetActive(false);
        _gameUI.HideFadeEffectRect(false);
        _gameUI.HidePlayerStats(true);
        _gameUI.HideFadeEffectRect(true);
        _gameUI.GameOverScreen(true);
        _globalAudio.PlayAnnouncerVoiceType(AnnouncerVoiceGroup.GameOver);

        SetGameState(GameStateType.GameOverMenu);
    }

    void ClearBackgroundImage()
    {
        _backgroundRenderer.enabled = false;
    }

    public void ApplyBackgroundImage(Vector2 bottomLeft, Vector2 topRight, Sprite background)
    {
        _backgroundRenderer.enabled = true;
        _backgroundRenderer.sprite = background;

        float width = topRight.x - bottomLeft.x;
        float height = topRight.y - bottomLeft.y;

        Vector2 spriteSize = _backgroundRenderer.sprite.bounds.size;

        float additionalScaleFactor = Level.ParallaxBackgroundSizeMultiplier;

        // Use the larger scale factor to ensure full coverage
        float scaleFactor = Mathf.Max(width / spriteSize.x, height / spriteSize.y) * additionalScaleFactor;
        _backgroundRenderer.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1);

        // Position the background at the center of the boundary
        Vector3 center = (bottomLeft + topRight) / 2f;
        _backgroundRenderer.transform.position = new Vector3(center.x, center.y, 40);
        _backgroundRenderer.transform.SetParent(null);

        _levelBGMaterial.SetVector("_UVScale", new Vector4(1, 1, 1, 1));
        _bgOffset = new Vector4(0, 0, 0, 0);
        _levelBGMaterial.SetVector("_UVOffset", _bgOffset);
    }

    public void ApplySeamlessBackground(Sprite background)
    {
        _backgroundRenderer.enabled = true;
        _backgroundRenderer.sprite = background;
        _backgroundRenderer.transform.localScale = new Vector3(1, 1, 1);

        var cameraView = _mainCamera.GetCameraViewSize();

        float spriteHeight = _backgroundRenderer.sprite.bounds.size.y;
        float spriteWidth = _backgroundRenderer.sprite.bounds.size.x;

        Debug.Log($"spriteHeight: {spriteHeight}, spriteWidth: {spriteWidth}");

        float spriteAspectRatio = spriteWidth / spriteHeight;
        float cameraAspectRatio = cameraView.x / cameraView.y;

        Vector2 newScale = transform.localScale;

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
        _backgroundRenderer.transform.SetParent(_mainCamera.transform);
        _backgroundRenderer.transform.localPosition = new Vector3(0,0,1);

        float bgSideSize = Mathf.Min(spriteHeight, spriteWidth);
        float cameraSideSize = Mathf.Min(cameraView.x, cameraView.y);
        _bgUVMultiplier = background.pixelsPerUnit / spriteWidth;

        Debug.Log($"uvMultiplier: {_bgUVMultiplier}");

        _levelBGMaterial.SetVector("_UVScale", new Vector4(_bgUVMultiplier, _bgUVMultiplier, 1, 1));
        _bgOffset = new Vector4(0, 0, 0, 0);
        _levelBGMaterial.SetVector("_UVOffset", _bgOffset);
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

        _bgOffset += (deltaMovement / 28f) * _bgUVMultiplier;

        _levelBGMaterial.SetVector("_UVOffset", (Vector4)_bgOffset);
    }

    float GetLightLevelValue(LevelLightLevels level)
    {
        return level switch
        {
            LevelLightLevels.PitchBlack => 0f,
            LevelLightLevels.VeryDark => 0.025f,
            LevelLightLevels.Dark => 0.1f,
            LevelLightLevels.Dim => 0.25f,
            LevelLightLevels.Normal => 0.35f,
            LevelLightLevels.Bright => 0.5f,
            LevelLightLevels.VeryBright => 0.65f,
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

    public void SetGlobalPlayerColor(string colorName)
    {
        _playerColor = PlayerColors.StringToColor(colorName);
        SetPlayerColor(_playerColor);
        _globalAudio.PlayUIColorSelect();
    }

    void SetPlayerColor(Color color)
    {
        if (_player != null)
        {
            _player.SetPlayerColor(color);
        }

        if (_gameUI != null)
        {
            _gameUI.SetPlayerColorOnUIImages(color);
        }
    }

    void SavePlayerColorStorage(Color color)
    {
        PlayerPrefs.SetFloat("PlayerColorR", color.r);
        PlayerPrefs.SetFloat("PlayerColorG", color.g);
        PlayerPrefs.SetFloat("PlayerColorB", color.b);
    }

    Color GetSavedPlayerColor()
    {
        var r = PlayerPrefs.GetFloat("PlayerColorR", -1f);
        var g = PlayerPrefs.GetFloat("PlayerColorG", -1f);
        var b = PlayerPrefs.GetFloat("PlayerColorB", -1f);

        if ((r < 0f || g < 0 || b < 0f) || (1f < r || 1f < g || 1f < b))
        {
            return PlayerColors.BloodstoneRedColor;
        }

        return new Color(r, g, b, 1f); 
    }

    void LoadPersistentStorage()
    {
        var savedLevelIndex = PlayerPrefs.GetInt("LevelIndex", -1);
        var savedLevelName = PlayerPrefs.GetString("LevelName", string.Empty);

        Debug.Log($"LevelIndex: {savedLevelIndex}, LevelName: {savedLevelName}");

        if (0 <= savedLevelIndex && savedLevelIndex < GameLevels.Count && savedLevelName.Contains(GameLevels[savedLevelIndex].name))
        {
            Debug.Log($"Continue level is valid.");

            _savedLevelIndex = savedLevelIndex;
            _savedLevelName = savedLevelName;

            _currentLevelIndex = _savedLevelIndex;

            _gameUI.EnableContinueMenuOption(true);
        }
        else
        {
            Debug.Log($"Continue level is NOT VALID.");

            PlayerPrefs.DeleteKey("LevelIndex");
            PlayerPrefs.DeleteKey("LevelName");

            _gameUI.EnableContinueMenuOption(false);
        }

        _playerColor = GetSavedPlayerColor();
        SetPlayerColor(_playerColor);
    }
}

