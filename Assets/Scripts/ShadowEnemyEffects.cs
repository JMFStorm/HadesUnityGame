using UnityEngine;

public class ShadowEnemyEffects : MonoBehaviour
{
    public GameObject ShadowFXPrefab;

    public float OutlineThickness = 0.3f;

    public Color InlineColor = new(0.12f, 0.12f, 0.12f, 1f);
    public Color OutlineColor = new(0.22f, 0.22f, 0.22f, 1f);

    public float OutlineBlurSize = 1.5f;

    public Vector2 EffectOffset = new(0f, 0.5f);

    private GameObject _shadowFXInstanciated;

    private void Awake()
    {
        _shadowFXInstanciated = Instantiate(ShadowFXPrefab, transform.position, Quaternion.identity);
        _shadowFXInstanciated.transform.SetParent(transform);

        _shadowFXInstanciated.transform.localPosition += (Vector3)EffectOffset;
    }

    public void EnableShadowEffects(bool enable)
    {
        _shadowFXInstanciated.SetActive(enable);
    }

    private void OnDestroy()
    {
        if (_shadowFXInstanciated != null)
        {
            Destroy(_shadowFXInstanciated);
        }
    }
}
