using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
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

    public PlayerCharacter Player;
    public MainCamera MainCamera;

    public Light2D GlobalLight;
    public bool SkipIntro = false;
    public int DebugStartLevelIndex = 0;
    public Texture2D CursorIcon;

    private GameStateType _gameState;

    private Level _currentLevel;
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

        Cursor.SetCursor(CursorIcon, Vector2.zero, CursorMode.Auto);
    }

    private void Start()
    {
        LoadPersistentStorage();

        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        AudioListener.volume = savedVolume;

        Player.gameObject.SetActive(false);

        MainCamera.transform.position = new(0, 0, -1f);
        MainCamera.SetDustFXStrength(0);
        MainCamera.SetFogFXLevel(false, new(0, 0, 0));

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
        var gameState = GetGameState();

        DetectInputMethod();

        if (_usingController)
        {
            Cursor.visible = false;

            if (EventSystem.current.currentSelectedGameObject == null)
            {
                var selected = _gameUI.GetCurrentFirstSelectedObject();

                Debug.Log("selected: " + selected.name);

                EventSystem.current.SetSelectedGameObject(selected);
            }
        }
        else
        {
            EventSystem.current.SetSelectedGameObject(null);

            var cursorVisible = true;

            if (gameState == GameStateType.IntroScreen || gameState == GameStateType.MainGame || gameState == GameStateType.Cutscene)
            {
                cursorVisible = false;
            }

            Cursor.visible = cursorVisible;
        }

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

            _prevCameraPosition = MainCamera.transform.position;
        }

        if (Input.GetButtonDown("Escape"))
        {
            if (gameState == GameStateType.IntroScreen)
            {
                _gameUI.SkipIntroSequence();
            }
            else if (gameState == GameStateType.MainGame && !Player.IsDead())
            {
                _gameUI.ActivatePauseMenu(true);
            }
            else if (gameState == GameStateType.PauseMenu && !Player.IsDead())
            {
                _gameUI.ActivatePauseMenu(false);
            }
            else if (gameState == GameStateType.Cutscene)
            {
                _gameUI.SkipCutscene();
            }
        }
    }

    private void FixedUpdate()
    {
        if (Player != null)
        {
            MainCamera.SetFollowTargetXOffset(Player.FacingDirX * 0.75f);

            if (Player.IsCrouching)
            {
                MainCamera.SetFollowTargetYOffset(-1.0f);
                MainCamera.SetCameraSpeedMultiplier(0.33f);
            }
            else
            {
                MainCamera.SetFollowTargetYOffset(0f);
                MainCamera.SetCameraSpeedMultiplier(1f);
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
        Player.gameObject.SetActive(false);
        Player.ResetPlayerInnerState(false);

        _gameUI.ActivatePauseMenu(false);
        _gameUI.GameOverScreen(false);
        _gameUI.HideMainMenu(false);

        _gameUI.HidePlayerStats(true);

        _globalAudio.StopAmbience();
        _globalAudio.StopMusic(1f);

        ClearBackgroundImage();

        Player.gameObject.SetActive(false);

        SetGameState(GameStateType.MainMenu);

        LoadPersistentStorage();

        MainCamera.SetFogFXLevel(false, new Color(0, 0, 0, 0));
    }

    public void ClickReturnToMainMenuFromPauseMenu()
    {
        Player.gameObject.SetActive(false);
        Player.ResetPlayerInnerState(false);

        _gameUI.ActivatePauseMenu(false);
        _gameUI.HideMainMenu(false);

        _gameUI.HidePlayerStats(true);

        ClearCurrentLevel();

        _globalAudio.StopAmbience();
        _globalAudio.StopMusic(1f);

        ClearBackgroundImage();

        Player.gameObject.SetActive(false);

        SetGameState(GameStateType.MainMenu);

        LoadPersistentStorage();

        MainCamera.SetFogFXLevel(false, new Color(0, 0, 0, 0));
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

        if (Debug.isDebugBuild)
        {
            if (DebugStartLevelIndex < GameLevels.Count)
            {
                _currentLevelIndex = DebugStartLevelIndex;
            }
            else
            {
                Debug.LogWarning($"Invalid DebugStartLevelIndex {DebugStartLevelIndex}");
            }

        }

        LoadLevelIndex(_currentLevelIndex, false);

        yield return null;
    }

    void InstantiateGlobalGamePrefabs()
    {
        _gameUI.HidePlayerStats(false);
        MainCamera.SetFollowTarget(Player.transform);
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

    IEnumerator StartOutroCutsceneState()
    {
        SetGameState(GameStateType.Cutscene);

        yield return StartCoroutine(_gameUI.PlayOutroCutscene());

        InitGameIntroTitle();
    }

    public void LoadLevelIndex(int index, bool isRetry)
    {
        if (index < 0)
        {
            Debug.LogError("Invalid level index!");
            return;
        }

        ClearCurrentLevel();

        if (GameLevels.Count <= index)
        {
            Player.gameObject.SetActive(false);
            MainCamera.SetFogFXLevel(false, new(0, 0, 0));
            MainCamera.SetDustFXStrength(0f);
            _globalAudio.StopMusic(2f);
            _globalAudio.StopAmbience();
            ClearBackgroundImage();
            StartCoroutine(StartOutroCutsceneState());
            return;
        }

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

        var bgCol = Mathf.Max(Mathf.Min(_currentLevel.BackgroundBrightness, 1f), 0f);
        var bgColor = new Color(bgCol, bgCol, bgCol, 1f);
        _backgroundRenderer.material.SetColor("_Color", bgColor);

        Debug.Log($"GlobalLight.intensity set = {GlobalLight.intensity}");

        MainCamera.SetCameraBoundaries(bl, tr);
        MainCamera.SetDustFXStrength(GlobalLight.intensity * 0.6f);
        MainCamera.SetFogFXLevel(_currentLevel.HeavyFog, _currentLevel.FogColorMultiplier);

        var vignetteValue = GetVignetteIntensity(_currentLevel.LightLevel);
        MainCamera.SetVignetteIntensity(/*vignetteValue*/ 0f);

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
            var newPlayerStart = levelEnter;
            Player.transform.position = new Vector3(newPlayerStart.x, newPlayerStart.y, 0);
            MainCamera.transform.position = new Vector3(newPlayerStart.x, newPlayerStart.y, _cameraZOffset);
            Camera.main.transform.position = MainCamera.transform.position;
        }
        else
        {
            Debug.LogError("LevelEnter not found!");
        }

        _currentLevel.MakeLevelArenaEvents();

        Player.gameObject.SetActive(true);
        Player.ResetPlayerInnerState(isRetry);
        SetPlayerColor(_playerColor);

        _prevCameraPosition = MainCamera.transform.position;

        Time.timeScale = 1f;

        PlayerPrefs.SetInt("LevelIndex", index);
        PlayerPrefs.SetString("LevelName", _currentLevel.name);
        PlayerPrefs.Save();

        SetGameState(GameStateType.MainGame);
    }

    public void RestartLevel()
    {
        Player.StopAllCoroutines();
        _gameUI.GameOverScreen(false);
        _gameUI.ActivatePauseMenu(false);
        LoadLevelIndex(_currentLevelIndex, true);
    }

    public void LoadPreviousLevel()
    {
        _currentLevelIndex = _currentLevelIndex - 1;

        if (_currentLevelIndex < 0)
        {
            _currentLevelIndex = 0;
        }

        _gameUI.ActivatePauseMenu(false);

        LoadLevelIndex(_currentLevelIndex, false);
    }

    public void LoadNextLevel()
    {
        _currentLevelIndex = _currentLevelIndex + 1;

        _gameUI.ActivatePauseMenu(false);

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

        var cameraView = MainCamera.GetCameraViewSize();

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
        _backgroundRenderer.transform.SetParent(MainCamera.transform);
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
        Vector2 deltaMovement = MainCamera.transform.position - _prevCameraPosition;

        _backgroundRenderer.transform.position += (Vector3)deltaMovement * parallaxFactor;
    }

    void SeamlessBackgroundParallaxScroll()
    {
        Vector2 deltaMovement = MainCamera.transform.position - _prevCameraPosition;

        _bgOffset += (deltaMovement / 28f) * _bgUVMultiplier;

        _levelBGMaterial.SetVector("_UVOffset", (Vector4)_bgOffset);
    }

    float GetLightLevelValue(LevelLightLevels level)
    {
        return level switch
        {
            LevelLightLevels.PitchBlack => 0.04f,
            LevelLightLevels.VeryDark => 0.1f,
            LevelLightLevels.Dark => 0.25f,
            LevelLightLevels.Dim => 0.35f,
            LevelLightLevels.Normal => 0.5f,
            LevelLightLevels.Bright => 0.7f,
            LevelLightLevels.VeryBright => 0.9f,
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
        if (Player != null)
        {
            Player.SetPlayerColor(color);
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

    private Vector3 _lastMousePosition;
    private bool _usingController;

    void DetectInputMethod()
    {
        const float mouseMoveThreshold = 0.1f;
        Vector3 mouseDelta = Input.mousePosition - _lastMousePosition;
        _lastMousePosition = Input.mousePosition;

        if (mouseDelta.magnitude > mouseMoveThreshold || Input.GetMouseButtonDown(0))
        {
            _usingController = false;
        }

        if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0 ||
            Input.GetButtonDown("Submit") || Input.GetButtonDown("Cancel"))
        {
            _usingController = true;
        }
    }
}

