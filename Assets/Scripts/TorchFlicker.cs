using UnityEngine;
using UnityEngine.Rendering.Universal;

public class TorchFlicker : MonoBehaviour
{
    public Light2D TorchLight;

    public float minIntensity = 1.0f;
    public float maxIntensity = 2.5f;
    public float intensitySpeed = 0.5f;  // Flicker speed

    public Color color1 = new(1f, 0.6f, 0.3f);
    public Color color2 = new(1f, 0.4f, 0.2f);

    public float MinOuterRadius = 5.5f;
    public float MaxOuterRadius = 6.0f;

    private float _randomFlickerOffset;

    private Animator _flameAnimator;

    private void Awake()
    {
        if (!transform.Find("Flame").TryGetComponent(out _flameAnimator))
        {
            Debug.LogError($"{nameof(Animator)} not fuond on {nameof(TorchFlicker)}");
        }

        _randomFlickerOffset = Random.Range(0f, 100f);

        MaxOuterRadius = TorchLight.pointLightOuterRadius;
        MinOuterRadius = TorchLight.pointLightOuterRadius * 0.9f;
    }

    void Start()
    {
        float randomOffset = Random.Range(0f, 1f);
        _flameAnimator.Play("Flame", 0, randomOffset);
    }

    void Update()
    {
        if (TorchLight != null)
        {
            float noise = Mathf.PerlinNoise(Time.time * intensitySpeed, _randomFlickerOffset);
            TorchLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
            TorchLight.color = Color.Lerp(color1, color2, noise);
            TorchLight.pointLightOuterRadius = Mathf.Lerp(MinOuterRadius, MaxOuterRadius, noise);
        }
    }
}
