using UnityEngine;

public class FixedChild : MonoBehaviour
{
    private Vector3 worldPosition;

    private void Start()
    {
        worldPosition = transform.position;
    }

    private void LateUpdate()
    {
        transform.position = worldPosition;
    }
}