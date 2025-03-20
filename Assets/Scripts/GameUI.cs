using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class GameUI : MonoBehaviour
{
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

    public readonly int DefaultPlayerHealth = 3;
    public readonly int MaxPlayerHealth = 5;
    public readonly int MaxDashes = 2;

    void Start()
    {
        _currentHealth = DefaultPlayerHealth;
        UpdateUI();
    }

    public void HideUI(bool hide)
    {
        gameObject.SetActive(!hide);
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

    IEnumerator FadeInLoop(float fadeDuration)
    {
        float elapsedTime = 0f;
        Color color = FadeImage.color;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            color.a = 1f - Mathf.Clamp01(elapsedTime / fadeDuration);
            FadeImage.color = color;
            yield return null;
        }
    }

    IEnumerator FadeOutLoop(float fadeDuration)
    {
        float elapsedTime = 0f;
        Color color = FadeImage.color;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Clamp01(elapsedTime / fadeDuration);
            FadeImage.color = color;
            yield return null;
        }
    }
}
