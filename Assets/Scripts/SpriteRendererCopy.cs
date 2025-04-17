using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteRendererCopy : MonoBehaviour
{
    public SpriteRenderer sourceRenderer;

    private SpriteRenderer targetRenderer;

    void Awake()
    {
        targetRenderer = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        targetRenderer.sprite = sourceRenderer.sprite;
        targetRenderer.flipX = sourceRenderer.flipX;
        targetRenderer.flipY = sourceRenderer.flipY;
        targetRenderer.transform.localPosition = Vector3.zero;
    }
}
