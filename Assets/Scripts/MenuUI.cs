using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class MenuUI : MonoBehaviour
{
    public List<CutsceneData> IntroCutscene = new();

    public GameObject IntroTitle;
    public GameObject MainMenuPanel;
    public GameObject PauseMenuPanel;
    public GameObject GameOverPanel;
    public GameObject PlayerColorPanel;
    public GameObject CutsceneCanvas;

    private GlobalAudio _globalAudio;
    private GameState _gameState;
    private TextMeshProUGUI _introText;
    private Material _deathScreenMaterial;
    private Material _cutsceneMaterial;
    private Image _cutsceneImage;

    private bool _cutsceneCancelled = false;

    private Coroutine _introCoroutine = null;
    private Coroutine _cutsceneCoroutine = null;

    private void Awake()
    {
        _gameState = FindFirstObjectByType<GameState>();

        if (_gameState == null)
        {
            Debug.LogError($"{nameof(GameState)} not found on {nameof(MenuUI)}");
        }

        if (!IntroTitle.TryGetComponent(out _introText))
        {
            Debug.LogError($"{nameof(TextMeshProUGUI)} not found on {nameof(MenuUI)} introTitle child");
        }

        var gameOverPanel = GameOverPanel.transform.Find("PlayerImage");

        if (gameOverPanel == null)
        {
            Debug.LogError($"gameOverPanel not found on {nameof(MenuUI)}");
        }

        if (!gameOverPanel.TryGetComponent(out Image image))
        {
            Debug.LogError($"{nameof(Image)} not found on {nameof(MenuUI)} introTitle child");
        }

        _deathScreenMaterial = image.material;

        if (!CutsceneCanvas.TryGetComponent(out _cutsceneImage))
        {
            Debug.LogError($"{nameof(Image)} not found on {nameof(MenuUI)} cutscenecanvas child");
        }

        _cutsceneMaterial = _cutsceneImage.material;

        _globalAudio = FindFirstObjectByType<GlobalAudio>();

        if (_globalAudio == null)
        {
            Debug.LogError($"{nameof(GlobalAudio)} not found on {nameof(GameState)}");
        }

        CutsceneCanvas.SetActive(false);
        IntroTitle.SetActive(false);
        PauseMenuPanel.SetActive(false);
        GameOverPanel.SetActive(false);
        MainMenuPanel.SetActive(false);
        PlayerColorPanel.SetActive(false);
    }

    void Start()
    {
        HideMainMenu(true);
    }

    public IEnumerator PlayIntroCutscene()
    {
        _cutsceneCancelled = false;
        _cutsceneCoroutine = StartCoroutine(PlayCutsceneFromData(IntroCutscene));

        yield return _cutsceneCoroutine;

        _cutsceneCoroutine = null;
    }

    public void SkipCutscene()
    {
        if (_cutsceneCoroutine != null)
        {
            CutsceneCanvas.SetActive(false);

            _cutsceneCoroutine = null;
            _cutsceneCancelled = true;
        }
    }

    IEnumerator PlayCutsceneFromData(List<CutsceneData> cutsceneData)
    {
        CutsceneCanvas.SetActive(true);

        foreach (var cutscene in cutsceneData)
        {
            var imageRect = _cutsceneImage.GetComponent<RectTransform>();
            var parentRect = _cutsceneImage.transform.parent.GetComponent<RectTransform>();

            if (imageRect != null && parentRect != null)
            {
                imageRect.anchorMin = Vector2.zero; // Bottom-left
                imageRect.anchorMax = Vector2.one;  // Top-right
                imageRect.offsetMin = Vector2.zero; // No offset on left/bottom
                imageRect.offsetMax = Vector2.zero; // No offset on right/top
            }

            _cutsceneImage.sprite = cutscene.Image;

            if (cutscene.SoundEffect != null)
            {
                _globalAudio.PlaySoundEffect(cutscene.SoundEffect, 0.8f);
            }

            float elapsedTime = 0f;

            var initialScale = cutscene.ScaleStart;
            var targetScale = cutscene.ScaleEnd;

            while (elapsedTime < cutscene.DisplayTime)
            {
                var newScale = Vector2.Lerp(initialScale, targetScale, elapsedTime / cutscene.DisplayTime);
                _cutsceneImage.rectTransform.localScale = newScale;

                Debug.Log("loop" + elapsedTime);

                if (_cutsceneCancelled)
                {
                    yield break;
                }

                elapsedTime += Time.deltaTime;

                yield return null;
            }
        }

        CutsceneCanvas.SetActive(false);
    }

    public void SetPlayerColorOnUIImages(Color color)
    {
        _deathScreenMaterial.SetColor("_NewColor", color);
        _cutsceneMaterial.SetColor("_NewColor", color);
    }

    public void GameOverScreen(bool active)
    {
        if (active)
        {
            Time.timeScale = 0;
            GameOverPanel.SetActive(true);
        }
        else
        {
            Time.timeScale = 1f;
            GameOverPanel.SetActive(false);
        }
    }

    public void HidePlayerColorPanel(bool hide)
    {
        PlayerColorPanel.SetActive(!hide);
    }

    public void ActivatePauseMenu(bool setActive)
    {
        PauseMenuPanel.SetActive(setActive);
        Time.timeScale = setActive ? 0f : 1f;

        _gameState.SetGameState(setActive ? GameStateType.PauseMenu : GameStateType.MainGame);
    }

    public void HideMainMenu(bool setHide)
    {
        MainMenuPanel.SetActive(!setHide);
    }

    public void SkipIntroSequence()
    {
        if (_introCoroutine != null)
        {
            StopCoroutine(_introCoroutine);
            _gameState.SetGameState(GameStateType.MainMenu);

            IntroTitle.SetActive(false);
            MainMenuPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Tried to SkipIntroSequence, but not valid.");
        }
    }

    public void PlayIntroAndThenMainMenu()
    {
        _introCoroutine = StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        IntroTitle.SetActive(true);

        const float fadeInDuration = 7f;

        Color initialColor = new(0.2f, 0.2f, 0.2f, 0.0f);
        Color targetColor = new(1, 1, 1, 1.0f);
        Vector3 initialScale = new(0.9f, 0.9f, 1f);
        Vector3 targetScale = new(1f, 1f, 1f);

        float elapsedTime = 0f;

        while (elapsedTime < fadeInDuration)
        {
            float progress = elapsedTime / fadeInDuration;

            // Fade in the text
            _introText.color = Color.Lerp(initialColor, targetColor, progress);

            // Grow the text size
            _introText.transform.localScale = Vector3.Lerp(initialScale, targetScale, progress);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        _introText.color = targetColor;
        _introText.transform.localScale = targetScale;

        const float fadeOutDuration = 3f;

        initialColor = _introText.color;
        initialScale = _introText.transform.localScale;
        targetColor = new(0.4f, 0.4f, 0.4f, 0.0f);
        targetScale = new(1.04f, 1.04f, 1f);

        elapsedTime = 0f;

        while (elapsedTime < fadeOutDuration)
        {
            float progress = elapsedTime / fadeOutDuration;

            // Fade in the text
            _introText.color = Color.Lerp(initialColor, targetColor, progress);

            // Grow the text size
            _introText.transform.localScale = Vector3.Lerp(initialScale, targetScale, progress);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(1);

        IntroTitle.SetActive(false);
        MainMenuPanel.SetActive(true);
    }
}
