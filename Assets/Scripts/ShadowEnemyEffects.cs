using UnityEngine;

public class ShadowEnemyEffects : MonoBehaviour
{
    public GameObject ShadowFXPrefab;

    public float OutlineThickness = 0.3f;

    public Color InlineColor = new(0.12f, 0.12f, 0.12f, 1f);
    public Color OutlineColor = new(0.22f, 0.22f, 0.22f, 1f);

    private GameObject _shadowFXInstanciated;

    private void Awake()
    {
        _shadowFXInstanciated = Instantiate(ShadowFXPrefab, transform.position, Quaternion.identity);
        _shadowFXInstanciated.transform.SetParent(transform);
    }

    private void OnDestroy()
    {
        if (_shadowFXInstanciated != null)
        {
            Destroy(_shadowFXInstanciated);
        }
    }
}
