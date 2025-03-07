using UnityEngine;

public class Level : MonoBehaviour
{
    private Vector3 _levelBotLeft;
    private Vector3 _levelTopRight;

    protected virtual void Awake()
    {
        var bgBoundaries = transform.Find("BGBoundaries");

        if (bgBoundaries != null)
        {
            var colliderArea = bgBoundaries.GetComponent<BoxCollider2D>();

            _levelBotLeft = (Vector2)transform.position + colliderArea.offset - (colliderArea.size / 2);
            _levelTopRight = (Vector2)transform.position + colliderArea.offset + (colliderArea.size / 2);
        }
    }

    protected virtual void Start()
    {
    }

    protected virtual void Update()
    {
        DebugUtil.DrawCircle(_levelBotLeft, 0.1f, new Color(0, 0, 1.0f));
        DebugUtil.DrawCircle(_levelTopRight, 0.1f, new Color(0, 0, 1.0f));
    }

    public (Vector3 botLeft, Vector3 topRight) GetLevelBoundaries()
    {
        Debug.Log($"Tilemap Bounds - Bottom Left: {_levelBotLeft}, Top Right: {_levelTopRight}");
        return (_levelBotLeft, _levelTopRight);
    }

    public Vector3 GetLevelEntrance()
    {
        Transform doorway = gameObject.transform.Find("DoorwayEnter");

        if (doorway != null)
        {
            return doorway.position;
        }
        else
        {
            Debug.LogError("DoorwayEnter not found!");
        }

        return new();
    }
}
