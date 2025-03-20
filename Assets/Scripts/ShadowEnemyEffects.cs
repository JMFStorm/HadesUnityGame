using UnityEngine;

public class ShadowEnemyEffects : MonoBehaviour
{
    public GameObject ShadowFXPrefab;

    public float OutlineThickness = 0.3f;

    public Color InlineColor = new(0.12f, 0.12f, 0.12f, 1f);
    public Color OutlineColor = new(0.22f, 0.22f, 0.22f, 1f);

    private Material _material;
    private GameObject _shadowFXInstanciated;

    private void Awake()
    {
        if (!TryGetComponent<SpriteRenderer>(out var spriteRenderer))
        {
            Debug.LogError($"{nameof(SpriteRenderer)} not found on {nameof(ShadowEnemyEffects)}");
        }

        _material = spriteRenderer.material;

        _shadowFXInstanciated = Instantiate(ShadowFXPrefab, transform.position, Quaternion.identity);
        _shadowFXInstanciated.transform.SetParent(transform);
    }

    void Start()
    {
        _material.SetFloat("_IsShadowVariant", 1f);
        _material.SetFloat("_OutlineThickness", OutlineThickness);
        _material.SetColor("_InlineColor", InlineColor);
        _material.SetColor("_OutlineColor", OutlineColor);

        _material.SetColor("_DamageColor", new(0,0,0));
    }


    void Update()
    {
        
    }

    private void OnDestroy()
    {
        if (_shadowFXInstanciated != null)
        {
            Destroy(_shadowFXInstanciated);
        }
    }
}
