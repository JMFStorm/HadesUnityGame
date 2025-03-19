using UnityEngine;

public class ShadowEnemyEffects : MonoBehaviour
{
    public GameObject ShadowFXPrefab;

    public readonly float ShadowOutlineThreshold = 0.1f;

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
        _material.SetFloat("_ShadowOutlineThreshold", ShadowOutlineThreshold);
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
