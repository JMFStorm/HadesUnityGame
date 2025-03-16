using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class GameUI : MonoBehaviour
{
    public List<Image> HeartSlots;
    public Sprite FullHeartSprite;
    public Sprite EmptyHeartSprite;

    public List<Image> DashSlots;
    public Sprite DashSprite;

    public Image FadeImage;

    private int _currentHealth;
    private int _currentStamina;

    void Start()
    {
        _currentHealth = 3;
        UpdateUI();
    }

    public void ShowUI()
    {
        gameObject.SetActive(true);
    }

    public void HideUI()
    {
        gameObject.SetActive(false);
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
        for (int i = 0; i < HeartSlots.Count; i++)
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

        for (int i = 0; i < DashSlots.Count; i++)
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
        StartCoroutine(FadeOutLoop(fadeDuration));
    }

    public void FadeIn(float fadeDuration)
    {
        StartCoroutine(FadeInLoop(fadeDuration));
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
