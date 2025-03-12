using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameUI : MonoBehaviour
{
    public List<Image> HeartSlots;
    public Sprite FullHeartSprite;
    public Sprite EmptyHeartSprite;

    public List<Image> DashSlots;
    public Sprite DashSprite;

    private int _currentHealth;
    private int _currentStamina;

    void Start()
    {
        _currentHealth = 3;
        UpdateUI();
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
}
