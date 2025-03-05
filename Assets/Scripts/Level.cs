using UnityEngine;

public class Level : MonoBehaviour
{
    void Start()
    {
        
    }

    void Update()
    {
        
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

    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}
