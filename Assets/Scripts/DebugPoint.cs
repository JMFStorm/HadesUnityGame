using UnityEngine;

public class GizmoExample : MonoBehaviour
{
    public float gizmoRadius = 0.1f;
    public Color gizmoColor = Color.green;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
    }
}
