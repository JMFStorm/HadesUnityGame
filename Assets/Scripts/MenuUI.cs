using System.Collections;
using UnityEngine;

public class MenuUI : MonoBehaviour
{
    public GameObject IntroText;
    public GameObject MainMenuPanel;

    void Start()
    {
        HideMainMenu(true);
    }

    void Update()
    {
        
    }

    public void HideMainMenu(bool setHide)
    {
        IntroText.SetActive(!setHide);
        MainMenuPanel.SetActive(!setHide);
    }

    public void PlayIntroAndThenMainMenu()
    {
        StartCoroutine(IntroSequence());
    }

    IEnumerator IntroSequence()
    {
        Debug.Log("Title visible");

        IntroText.SetActive(true);

        yield return new WaitForSeconds(2);

        Debug.Log("Main menu visible");

        IntroText.SetActive(false);
        MainMenuPanel.SetActive(true);
    }
}
