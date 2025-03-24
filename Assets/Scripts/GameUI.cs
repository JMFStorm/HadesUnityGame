using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    public List<CutsceneData> IntroCutscene = new();

    public GameObject IntroTitle;
    public GameObject MainMenuPanel;
    public GameObject PauseMenuPanel;
    public GameObject GameOverPanel;
    public GameObject PlayerColorPanel;
    public GameObject CutsceneCanvas;
    public GameObject CutsceneText;

    public GameObject ContinueMenuButton;

    private GlobalAudio _globalAudio;
    private GameState _gameState;
    private TextMeshProUGUI _introTitleText;
    private TextMeshProUGUI _cutsceneText;
    private Material _deathScreenMaterial;
    private Material _cutsceneMaterial;
    private Image _cutsceneImage;

    private bool _cutsceneCancelled = false;

    private Coroutine _introCoroutine = null;
    private Coroutine _cutsceneCoroutine = null;

    public List<Image> HeartSlots;

    public Sprite FullHeartSprite;
    public Sprite EmptyHeartSprite;
    public Sprite ReinforcedHeartSprite;

    public List<Image> DashSlots;
    public Sprite DashSprite;

    public Image FadeImage;

    private Coroutine _fadeCoroutine;

    private int _currentHealth;
    private int _currentStamina;

    private Transform _playerStats;

    public readonly int DefaultPlayerHealth = 3;
    public readonly int MaxPlayerHealth = 5;
    public readonly int MaxDashes = 2;

    private void Awake()
    {
        _playerStats = transform.Find("PlayerStats");

        _gameState = FindFirstObjectByType<GameState>();

        if (_gameState == null)
        {
            Debug.LogError($"{nameof(GameState)} not found on {nameof(GameUI)}");
        }

        if (!IntroTitle.TryGetComponent(out _introTitleText))
        {
            Debug.LogError($"{nameof(TextMeshProUGUI)} not found on {nameof(GameUI)} introTitle child");
        }

        var gameOverPanel = GameOverPanel.transform.Find("PlayerImage");

        if (gameOverPanel == null)
        {
            Debug.LogError($"gameOverPanel not found on {nameof(GameUI)}");
        }

        if (!gameOverPanel.TryGetComponent(out Image image))
        {
            Debug.LogError($"{nameof(Image)} not found on {nameof(GameUI)} introTitle child");
        }

        _deathScreenMaterial = image.material;

        if (!CutsceneCanvas.TryGetComponent(out _cutsceneImage))
        {
            Debug.LogError($"{nameof(Image)} not found on {nameof(GameUI)} cutscenecanvas child");
        }

        _cutsceneMaterial = _cutsceneImage.material;

        _globalAudio = FindFirstObjectByType<GlobalAudio>();

        if (_globalAudio == null)
        {
            Debug.LogError($"{nameof(GlobalAudio)} not found on {nameof(GameState)}");
        }

        if (!CutsceneText.TryGetComponent(out _cutsceneText))
        {
            Debug.LogError($"{nameof(TextMeshProUGUI)} not found on {nameof(GameUI)} CutsceneText child");
        }

        CutsceneText.SetActive(false);
        CutsceneCanvas.SetActive(false);
        IntroTitle.SetActive(false);
        PauseMenuPanel.SetActive(false);
        GameOverPanel.SetActive(false);
        MainMenuPanel.SetActive(false);
        PlayerColorPanel.SetActive(false);
    }

    void Start()
    {
        _currentHealth = DefaultPlayerHealth;
        UpdateUI();
        FadeImage.gameObject.SetActive(false);

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
            CutsceneText.SetActive(false);

            _cutsceneCoroutine = null;
            _cutsceneCancelled = true;
        }
    }

    IEnumerator PlayCutsceneFromData(List<CutsceneData> cutsceneData)
    {
        CutsceneCanvas.SetActive(true);
        CutsceneText.SetActive(true);

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

            _cutsceneText.text = cutscene.DisplayText;
            _cutsceneImage.sprite = cutscene.Image;

            if (cutscene.SoundEffect != null)
            {
                _globalAudio.PlaySoundEffect(cutscene.SoundEffect, 0.8f);
            }

            float usedFadeTime = Mathf.Min(cutscene.DisplayTime / 2f, 3f);

            float elapsedTime = 0f;

            var initialScale = new Vector2(1, 1) * cutscene.ZoomStart;
            var targetScale = new Vector2(1, 1) * cutscene.ZoomEnd;

            bool fadeInStarted = false;
            bool fadeOutStarted = false;

            while (elapsedTime < cutscene.DisplayTime)
            {
                var newScale = Vector2.Lerp(initialScale, targetScale, elapsedTime / cutscene.DisplayTime);
                _cutsceneImage.rectTransform.localScale = newScale;

                if (!fadeInStarted)
                {
                    FadeIn(usedFadeTime);
                    fadeInStarted = true;
                }
                else if (!fadeOutStarted && ((cutscene.DisplayTime - usedFadeTime) < elapsedTime))
                {
                    FadeOut(usedFadeTime);
                    fadeOutStarted = true;
                }

                if (_cutsceneCancelled)
                {
                    yield break;
                }

                elapsedTime += Time.deltaTime;

                yield return null;
            }
        }

        HideFadeEffectRect(true);
        CutsceneText.SetActive(false);
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
            HideFadeEffectRect(true);
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

        Vector3 initialScale = new(0.9f, 0.9f, 1f);
        Vector3 targetScale = new(1f, 1f, 1f);

        Color initialColor = new(1, 1, 1, 0f);
        Color targetColor = new(1, 1, 1, 1f);

        float elapsedTime = 0f;

        const float fadeInDuration = 7f;

        _introTitleText.color = initialColor;

        while (elapsedTime < fadeInDuration)
        {
            float progress = elapsedTime / fadeInDuration;
            _introTitleText.transform.localScale = Vector3.Lerp(initialScale, targetScale, progress);

            float progressColor = Mathf.Pow(elapsedTime / fadeInDuration, 2f);
            _introTitleText.color = Color.Lerp(initialColor, targetColor, progressColor);
            elapsedTime += Time.deltaTime;

            yield return null;
        }

        _introTitleText.transform.localScale = targetScale;
        _introTitleText.color = targetColor;

        initialScale = _introTitleText.transform.localScale;
        targetScale = new(1.04f, 1.04f, 1f);

        const float fadeOutDuration = 3f;
        elapsedTime = 0f;

        while (elapsedTime < fadeOutDuration)
        {
            float progress = elapsedTime / fadeOutDuration;
            _introTitleText.transform.localScale = Vector3.Lerp(initialScale, targetScale, progress);

            float progressColor = 1f - Mathf.Pow(1f - (elapsedTime / fadeOutDuration), 2.5f);
            _introTitleText.color = Color.Lerp(targetColor, initialColor, progressColor);
            elapsedTime += Time.deltaTime;

            yield return null;
        }

        yield return new WaitForSeconds(1);

        IntroTitle.SetActive(false);
        MainMenuPanel.SetActive(true);

        HideFadeEffectRect(true);

        _gameState.SetGameState(GameStateType.MainGame);
    }

    public void HidePlayerStats(bool hide)
    {
        _playerStats.gameObject.SetActive(!hide);
    }

    public void SetHealth(int healthAmount)
    {
        _currentHealth = healthAmount;
        UpdateUI();
    }

    public void SetStamina(int staminaAmount)
    {
        _currentStamina = staminaAmount;
        UpdateUI();
    }

    private void UpdateUI()
    {
        for (int i = 0; i < MaxPlayerHealth; i++)
        {
            HeartSlots[i].enabled = true;

            if (i < DefaultPlayerHealth)
            {
                if (i < _currentHealth)
                {
                    HeartSlots[i].sprite = FullHeartSprite;
                }
                else
                {
                    HeartSlots[i].sprite = EmptyHeartSprite;
                }
            }
            else if (DefaultPlayerHealth < _currentHealth && i < _currentHealth)
            {
                HeartSlots[i].sprite = ReinforcedHeartSprite;
            }
            else
            {
                HeartSlots[i].enabled = false;
            }
        }

        for (int i = 0; i < MaxDashes; i++)
        {
            if (i < _currentStamina)
            {
                DashSlots[i].enabled = true;
                DashSlots[i].sprite = DashSprite;
            }
            else
            {
                DashSlots[i].enabled = false;
            }
        }
    }

    public void FadeOut(float fadeDuration)
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        _fadeCoroutine = StartCoroutine(FadeOutLoop(fadeDuration));
    }

    public void FadeIn(float fadeDuration)
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        _fadeCoroutine = StartCoroutine(FadeInLoop(fadeDuration));
    }

    public void HideFadeEffectRect(bool hide)
    {
        FadeImage.gameObject.SetActive(!hide);
    }

    IEnumerator FadeInLoop(float fadeDuration)
    {
        HideFadeEffectRect(false);

        float elapsedTime = 0f;
        Color color = new(0,0,0,0);

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Pow(elapsedTime / fadeDuration, 2f);

            color.a = 1f - newAlpha;
            FadeImage.color = color;

            yield return null;
        }

        HideFadeEffectRect(true);
    }

    IEnumerator FadeOutLoop(float fadeDuration)
    {
        HideFadeEffectRect(false);

        float elapsedTime = 0f;
        Color color = new(0,0,0,1);

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Pow(elapsedTime / fadeDuration, 2f);

            color.a = newAlpha;
            FadeImage.color = color;

            yield return null;
        }
    }

    public void EnableContinueMenuOption(bool enable)
    {
        ContinueMenuButton.SetActive(enable);
    }
}
