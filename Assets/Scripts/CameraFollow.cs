using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform FollowTarget;
    public Vector3 CameraOffset;

    public float MinYBotEdge = 0.0f;

    public float HorizontalFollowDeadzone = 1.0f;
    public float VerticalFollowDeadzpone = 0.5f;

    private void Awake()
    {
        if (FollowTarget == null)
        {
            Debug.LogError("Transform target component not found on " + gameObject.name);
        }
    }

    void LateUpdate()
    {
        if (FollowTarget != null)
        {
            // --------------------------------
            // Follow target on deadzone exit
            {
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
            }

            // -----------------------
            // Set min y coord limit
            {
                float cameraBottomY = Camera.main.transform.position.y - Camera.main.orthographicSize;

                Vector2 lineStart2 = new(transform.position.x - (Camera.main.orthographicSize / 2), MinYBotEdge);
                Vector2 lineEnd2 = new(transform.position.x + (Camera.main.orthographicSize / 2), MinYBotEdge);
                Debug.DrawLine(lineStart2, lineEnd2, Color.blue);

                if (cameraBottomY < MinYBotEdge)
                {
                    float offsetY = Mathf.Abs(cameraBottomY);
                    float newY = transform.position.y + offsetY + MinYBotEdge;
                    transform.position = new Vector3(transform.position.x, newY, transform.position.z);

                    Vector2 lineStart = new(transform.position.x - (Camera.main.orthographicSize / 2), cameraBottomY);
                    Vector2 lineEnd = new(transform.position.x + (Camera.main.orthographicSize / 2), cameraBottomY);
                    Debug.DrawLine(lineStart, lineEnd, Color.magenta);
                }
            }
        }

        DebugUtil.DrawRectangle(transform.position, new Vector2(HorizontalFollowDeadzone * 2, VerticalFollowDeadzpone * 2), Color.green);
    }
}
