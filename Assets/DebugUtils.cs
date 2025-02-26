using UnityEngine;

public static class DebugUtil
{
    public static void DrawCircle(Vector3 center, float radius, Color color, int segments = 20)
    {
        float angle = 0;
        Vector3 lastPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);

        for (int i = 1; i <= segments; i++)
        {
            angle += 2 * Mathf.PI / segments;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);

            Debug.DrawLine(lastPoint, nextPoint, color);
            lastPoint = nextPoint;
        }
    }

    public static void DrawRectangle(Vector2 position, Vector2 size, Color color)
    {
        // Calculate the four corners of the rectangle
        Vector2 bottomLeft = position - size / 2;
        Vector2 bottomRight = new Vector2(position.x + size.x / 2, position.y - size.y / 2);
        Vector2 topLeft = new Vector2(position.x - size.x / 2, position.y + size.y / 2);
        Vector2 topRight = position + size / 2;

        // Draw the four edges of the rectangle
        Debug.DrawLine(bottomLeft, bottomRight, color); // Bottom edge
        Debug.DrawLine(bottomRight, topRight, color);   // Right edge
        Debug.DrawLine(topRight, topLeft, color);       // Top edge
        Debug.DrawLine(topLeft, bottomLeft, color);     // Left edge
    }
}