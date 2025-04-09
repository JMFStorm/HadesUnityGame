using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class SettingsSliders : MonoBehaviour
{
    public Slider VolumeSlider;
    public TextMeshProUGUI VolumeText;

    public Volume PostProcessVolume;
    public Slider GammaSlider;
    public TextMeshProUGUI GammaText;

    private ColorAdjustments _colorAdjustments;

    void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        VolumeSlider.value = savedVolume;
        SetVolume(savedVolume);

        PostProcessVolume.profile.TryGet(out _colorAdjustments);

        float savedGamma = PlayerPrefs.GetFloat("Gamma", 0.5f);
        GammaSlider.value = savedGamma;
        SetGamma(savedGamma);
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;

        PlayerPrefs.SetFloat("MasterVolume", value);

        VolumeText.text = "Volume: " + value.ToString("F3");
    }

    public void SetGamma(float value)
    {
        if (_colorAdjustments != null)
        {
            var newValue = Mathf.Lerp(-3f, 3f, value);

            if (-0.15f < newValue && newValue < 0.15f)
            {
                newValue = 0f;
            }

            _colorAdjustments.postExposure.value = newValue;
            PlayerPrefs.SetFloat("Gamma", value);

            GammaText.text = "Gamma adjustment: " + newValue.ToString("F3");
        }
    }
}
