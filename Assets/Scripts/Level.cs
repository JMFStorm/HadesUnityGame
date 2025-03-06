using UnityEngine;
using UnityEngine.Tilemaps;

public class Level : MonoBehaviour
{
    private Tilemap _tilemap;

    private Vector3 _tilemapBotLeft;
    private Vector3 _tilemapTopRight;

    protected virtual void Awake()
    {
        // TODO: Get level boundaries
    }

    protected virtual void Start()
    {
    }

    protected virtual void Update()
    {
        DebugUtil.DrawCircle(_tilemapBotLeft, 0.1f, new Color(0, 0, 1.0f));
        DebugUtil.DrawCircle(_tilemapTopRight, 0.1f, new Color(0, 0, 1.0f));
    }

    public (Vector3 botLeft, Vector3 topRight) GetLevelBoundaries()
    {
        Debug.Log($"Tilemap Bounds - Bottom Left: {_tilemapBotLeft}, Top Right: {_tilemapTopRight}");
        return (_tilemapBotLeft, _tilemapTopRight);
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
