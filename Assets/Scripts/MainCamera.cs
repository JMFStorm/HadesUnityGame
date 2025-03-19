using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MainCamera : MonoBehaviour
{
    public Transform FollowTarget;
    public Vector3 CameraOffset;

    public float HorizontalFollowDeadzone = 1.0f;
    public float VerticalFollowDeadzpone = 0.5f;

    private Vector2 _botLeftBoundary = new(float.MinValue, float.MinValue);
    private Vector2 _topRightBoundary = new(float.MaxValue, float.MaxValue);

    private ParticleSystem _dustParticleSystem;
    private SpriteRenderer _fogFXSpriteRenderer;
    private Camera _camera;

    private Vignette _vignetteFX;

    private void Awake()
    {
        var dustFx = transform.Find("DustFX");

        if (!dustFx.TryGetComponent(out _dustParticleSystem))
        {
            Debug.LogError($"Did not find component DustFX {nameof(ParticleSystem)} on {nameof(MainCamera)}");
        }

        var fogFx = transform.Find("FogFX");

        if (!fogFx.TryGetComponent(out _fogFXSpriteRenderer))
        {
            Debug.LogError($"Did not find component FogFX {nameof(SpriteRenderer)} on {nameof(MainCamera)}");
        }

        var vignetteFX = transform.Find("VignetteFX");
        var volume = vignetteFX.GetComponent<Volume>();

        if (!volume.profile.TryGet(out _vignetteFX))
        {
            Debug.LogError($"Did not find component _vignetteFX {nameof(Vignette)} on {nameof(MainCamera)}");
        }

        if (!TryGetComponent(out _camera))
        {
            Debug.LogError($"{nameof(Camera)} not found on {nameof(MainCamera)}");
        }
    }

    void LateUpdate()
    {
        if (FollowTarget == null)
        {
            return;
        }

        FollowTheTarget();
    }

    public void SetVignetteIntensity(float intensity)
    {
        if (_vignetteFX != null)
        {
            var newValue = Mathf.Clamp01(intensity);
            _vignetteFX.intensity.value = newValue;

            Debug.Log($"Vignette set to intensity: {newValue}");
        }
    }

    public bool IsWorldPositionVisible(Vector2 worldPosition)
    {
        Vector3 viewportPoint = _camera.WorldToViewportPoint(worldPosition);

        return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
               viewportPoint.y >= 0 && viewportPoint.y <= 1 &&
               viewportPoint.z > 0;
    }

    public Camera GetCamera()
    {
        return _camera;
    }

    public Vector2 GetCameraViewSize()
    {
        float height = Camera.main.orthographicSize * 2f;
        float width = height * Camera.main.aspect;
        return new Vector2(width, height);
    }

    public void SetFollowTarget(Transform newTarget)
    {
        FollowTarget = newTarget;
    }

    public void SetCameraBoundaries(Vector2 botLeft, Vector2 topRight)
    {
        _botLeftBoundary = botLeft;
        _topRightBoundary = topRight;
    }

    void FollowTheTarget()
    {
        // --------------------------------
        // Follow target on deadzone exit

        Vector3 targetPosition = FollowTarget.position + CameraOffset;

        float diffx = targetPosition.x - transform.position.x;
        float diffxAbs = Mathf.Abs(diffx);

        if (HorizontalFollowDeadzone < diffxAbs)
        {
            var newX = diffx < 0.0f ? diffx + HorizontalFollowDeadzone : diffx - HorizontalFollowDeadzone;
            transform.position = new Vector3(transform.position.x + newX, transform.position.y, transform.position.z);
        }

        float diffy = targetPosition.y - transform.position.y;
        float diffyAbs = Mathf.Abs(diffy);

        if (VerticalFollowDeadzpone < diffyAbs)
        {
            var newY = diffy < 0.0f ? diffy + VerticalFollowDeadzpone : diffy - VerticalFollowDeadzpone;
            transform.position = new Vector3(transform.position.x, transform.position.y + newY, transform.position.z);
        }

        CameraBoundaries();

        DebugUtil.DrawRectangle(transform.position, new Vector2(HorizontalFollowDeadzone * 2, VerticalFollowDeadzpone * 2), Color.green);
    }

    void CameraBoundaries()
    {
        // -----------------------
        // Set min y coord limit

        float cameraBottomY = Camera.main.transform.position.y - Camera.main.orthographicSize;

        Vector2 lineStart2 = new(transform.position.x - (Camera.main.orthographicSize / 2), _botLeftBoundary.y);
        Vector2 lineEnd2 = new(transform.position.x + (Camera.main.orthographicSize / 2), _botLeftBoundary.y);
        Debug.DrawLine(lineStart2, lineEnd2, Color.blue);

        if (_botLeftBoundary.y != float.MinValue && cameraBottomY < _botLeftBoundary.y)
        {
            float offsetY = Mathf.Abs(cameraBottomY);
            float newY = transform.position.y + offsetY + _botLeftBoundary.y;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

            Vector2 lineStart = new(transform.position.x - (Camera.main.orthographicSize / 2), cameraBottomY);
            Vector2 lineEnd = new(transform.position.x + (Camera.main.orthographicSize / 2), cameraBottomY);
            Debug.DrawLine(lineStart, lineEnd, Color.magenta);
        }

        // -----------------------
        // Set max y coord limit

        float cameraTopY = Camera.main.transform.position.y + Camera.main.orthographicSize;

        Vector2 lineStart3 = new(transform.position.x - (Camera.main.orthographicSize / 2), _topRightBoundary.y);
        Vector2 lineEnd3 = new(transform.position.x + (Camera.main.orthographicSize / 2), _topRightBoundary.y);
        Debug.DrawLine(lineStart3, lineEnd3, Color.blue);

        if (_topRightBoundary.y != float.MaxValue && _topRightBoundary.y < cameraTopY)
        {
            float offsetY = Mathf.Abs(cameraTopY);
            float newY = transform.position.y - offsetY + _topRightBoundary.y;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

            Vector2 lineStart = new(transform.position.x - (Camera.main.orthographicSize / 2), cameraTopY);
            Vector2 lineEnd = new(transform.position.x + (Camera.main.orthographicSize / 2), cameraTopY);
            Debug.DrawLine(lineStart, lineEnd, Color.magenta);
        }

        // -----------------------
        // Set min x coord limit

        float cameraBottomX = Camera.main.transform.position.x - (Camera.main.orthographicSize * Camera.main.aspect);

        Vector2 lineStart4 = new(_botLeftBoundary.x, transform.position.y - (Camera.main.orthographicSize / 2));
        Vector2 lineEnd4 = new(_botLeftBoundary.x, transform.position.y + (Camera.main.orthographicSize / 2));
        Debug.DrawLine(lineStart4, lineEnd4, Color.blue);

        if (_botLeftBoundary.x != float.MinValue && cameraBottomX < _botLeftBoundary.x)
        {
            float offsetX = Mathf.Abs(cameraBottomX);
            float newX = transform.position.x + offsetX + _botLeftBoundary.x;
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);

            Vector2 lineStart = new(cameraBottomX, transform.position.y - (Camera.main.orthographicSize / 2));
            Vector2 lineEnd = new(cameraBottomX, transform.position.y + (Camera.main.orthographicSize / 2));
            Debug.DrawLine(lineStart, lineEnd, Color.magenta);
        }

        // -----------------------
        // Set max x coord limit

        float cameraTopX = Camera.main.transform.position.x + (Camera.main.orthographicSize * Camera.main.aspect);

        Vector2 lineStart5 = new(_topRightBoundary.x, transform.position.y - (Camera.main.orthographicSize / 2));
        Vector2 lineEnd5 = new(_topRightBoundary.x, transform.position.y + (Camera.main.orthographicSize / 2));
        Debug.DrawLine(lineStart5, lineEnd5, Color.blue);

        if (_topRightBoundary.x != float.MaxValue && _topRightBoundary.x < cameraTopX)
        {
            float offsetX = Mathf.Abs(cameraTopX);
            float newX = transform.position.x - offsetX + _topRightBoundary.x;
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);

            Vector2 lineStart = new(cameraTopX, transform.position.y - (Camera.main.orthographicSize / 2));
            Vector2 lineEnd = new(cameraTopX, transform.position.y + (Camera.main.orthographicSize / 2));
            Debug.DrawLine(lineStart, lineEnd, Color.magenta);
        }
    }

    public void SetDustFXStrength(float normalizedValue)
    {
        var main = _dustParticleSystem.main;
        main.startColor = new Color(main.startColor.color.r, main.startColor.color.g, main.startColor.color.b, normalizedValue);
    }

    public void SetFogFXLevel(bool heavyFog, Color colorMultiplier)
    {
        var alpha = heavyFog ? 30f / 256f : 12f / 256f;
        var diffuse = new Color(0.6f, 0.6f, 0.6f) * colorMultiplier;
        var usedColor = new Color(diffuse.r, diffuse.g, diffuse.b, alpha);

        _fogFXSpriteRenderer.material.SetColor("_FogColor", usedColor);

        var usedSpeed = heavyFog ? 5f : 2.5f;
        _fogFXSpriteRenderer.material.SetFloat("_FogSpeed", usedSpeed);
    }
}
