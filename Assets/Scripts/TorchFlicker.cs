using UnityEngine;
using UnityEngine.Rendering.Universal;

public class TorchFlicker : MonoBehaviour
{
    public Light2D TorchLight;

    [Header("Intensity Settings")]
    public float minIntensity = 1.0f;
    public float maxIntensity = 2.5f;
    public float intensitySpeed = 0.5f;  // Flicker speed

    [Header("Color Settings (Optional)")]
    public Color color1 = new(1f, 0.6f, 0.3f);
    public Color color2 = new(1f, 0.4f, 0.2f);

    private float _minOuterRadius = 5.5f;
    private float _maxOuterRadius = 6.0f;

    private float _randomVariationOffset;

    void Start()
    {
        _randomVariationOffset = Random.Range(0f, 100f); // Add variation per torch

        _maxOuterRadius = TorchLight.pointLightOuterRadius;
        _minOuterRadius = TorchLight.pointLightOuterRadius * 0.9f;
    }

    void Update()
    {
        if (TorchLight != null)
        {
            float noise = Mathf.PerlinNoise(Time.time * intensitySpeed, _randomVariationOffset);
            TorchLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
            TorchLight.color = Color.Lerp(color1, color2, noise);
            TorchLight.pointLightOuterRadius = Mathf.Lerp(_minOuterRadius, _maxOuterRadius, noise);
        }
    }
}
