using System.Collections;
using TMPro;
using UnityEngine;

public class MenuUI : MonoBehaviour
{
    public GameObject MainMenuPanel;
    public GameObject PauseMenuPanel;
    public GameObject GameOverPanel;

    private GameState _gameState;
    private TextMeshProUGUI _introText;

    private Coroutine _introCoroutine = null;

    void Start()
    {
        _gameState = FindFirstObjectByType<GameState>();

        if (_gameState == null)
        {
            Debug.LogError($"{nameof(GameState)} not found on {nameof(PlayerCharacter)}");
        }

        var introTitle = transform.Find("IntroTitle");

        if (introTitle == null)
        {
            Debug.LogError($"introTitle not found as a child of {nameof(MenuUI)} script");
        }

        if (!introTitle.TryGetComponent(out _introText))
        {
            Debug.LogError($"{nameof(TextMeshProUGUI)} not found on {nameof(MenuUI)} introTitle child");
        }

        HideMainMenu(true);

        _introText.gameObject.SetActive(false);
        PauseMenuPanel.SetActive(false);
        GameOverPanel.SetActive(false);
        MainMenuPanel.SetActive(false);
    }

    void Update()
    {
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

            _introText.gameObject.SetActive(false);
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
        _introText.gameObject.SetActive(true);

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

        _introText.gameObject.SetActive(false);
        MainMenuPanel.SetActive(true);
    }
}
