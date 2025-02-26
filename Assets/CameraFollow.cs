using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 cameraOffset;

    public float minYBotEdge = 0.0f;

    public float horizontalFollowThreshold = 2.0f;
    public float verticalFollowThreshold = 2.0f;

    private void Awake()
    {
        if (target == null)
        {
            Debug.LogError("Transform target component not found on " + gameObject.name);
        }
    }

    void LateUpdate()
    {
        if (target != null)
        {
            // --------------------------------
            // Follow target on deadzone exit
            {
                Vector3 targetPosition = target.position + cameraOffset;

                float diffx = targetPosition.x - transform.position.x;
                float diffxAbs = Mathf.Abs(diffx);

                if (horizontalFollowThreshold < diffxAbs)
                {
                    var newX = diffx < 0.0f ? diffx + horizontalFollowThreshold : diffx - horizontalFollowThreshold;
                    transform.position = new Vector3(transform.position.x + newX, transform.position.y, transform.position.z);
                }

                float diffy = targetPosition.y - transform.position.y;
                float diffyAbs = Mathf.Abs(diffy);

                if (verticalFollowThreshold < diffyAbs)
                {
                    var newY = diffy < 0.0f ? diffy + verticalFollowThreshold : diffy - verticalFollowThreshold;
                    transform.position = new Vector3(transform.position.x, transform.position.y + newY, transform.position.z);
                }
            }

            // -----------------------
            // Set min y coord limit
            {
                float cameraBottomY = Camera.main.transform.position.y - Camera.main.orthographicSize;

                Vector2 lineStart = new(transform.position.x - (Camera.main.orthographicSize / 2), cameraBottomY);
                Vector2 lineEnd = new(transform.position.x + (Camera.main.orthographicSize / 2), cameraBottomY);
                Debug.DrawLine(lineStart, lineEnd, Color.magenta);

                Vector2 lineStart2 = new(transform.position.x - (Camera.main.orthographicSize / 2), minYBotEdge);
                Vector2 lineEnd2 = new(transform.position.x + (Camera.main.orthographicSize / 2), minYBotEdge);
                Debug.DrawLine(lineStart2, lineEnd2, Color.blue);

                if (cameraBottomY < minYBotEdge)
                {
                    float offsetY = Mathf.Abs(cameraBottomY);
                    transform.position = new Vector3(transform.position.x, transform.position.y + offsetY, transform.position.z);
                }
            }
        }

        DebugUtil.DrawRectangle(transform.position, new Vector2(horizontalFollowThreshold * 2, verticalFollowThreshold * 2), Color.green);
    }
}
