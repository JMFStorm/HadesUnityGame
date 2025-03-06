using UnityEngine;

public class MainCamera : MonoBehaviour
{
    public Transform FollowTarget;
    public Vector3 CameraOffset;

    public float HorizontalFollowDeadzone = 1.0f;
    public float VerticalFollowDeadzpone = 0.5f;

    private Vector2 _botLeftBoundary = new(float.MinValue, float.MinValue);
    private Vector2 _topRightBoundary = new(float.MaxValue, float.MaxValue);

    private void Awake()
    {
    }

    void LateUpdate()
    {
        if (FollowTarget == null)
        {
            return;
        }

        FollowTheTarget();
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
}
