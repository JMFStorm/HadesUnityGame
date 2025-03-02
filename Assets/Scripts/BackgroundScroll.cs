using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    public Transform target;
    public float parallaxSpeed = 0.25f;

    private Vector3 _lastPlayerPosition;

    void Start()
    {
        _lastPlayerPosition = target.position;
    }

    void Update()
    {
        Vector3 deltaMovement = target.position - _lastPlayerPosition;

        transform.position += new Vector3(deltaMovement.x * parallaxSpeed, deltaMovement.y * parallaxSpeed, 0);

        _lastPlayerPosition = target.position;
    }
}
