using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsSliders : MonoBehaviour
{
    public Slider VolumeSlider;

    public TextMeshProUGUI VolumeText;

    void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        VolumeSlider.value = savedVolume;
        SetVolume(savedVolume);
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;

        //var volume = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20; // Mixer expects volume in dB. 0.0001 avoids log(0).
        //AudioMixer.SetFloat("MasterVolume", volume);
        PlayerPrefs.SetFloat("MasterVolume", value);

        VolumeText.text = "Volume: " + value.ToString("F2");
    }
}
