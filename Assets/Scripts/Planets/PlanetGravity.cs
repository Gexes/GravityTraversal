using UnityEngine;

[ExecuteAlways]
public class PlanetGravity : MonoBehaviour
{
    [Header("Planet Settings")]
    public float gravityStrength = 30f;
    public float planetRadius = 10f;
    public float gravityFieldRadius = 25f; // Outer limit of the planet's pull

    [Header("Debug")]
    public bool drawGizmos = true;
    public Color surfaceColor = new Color(0.3f, 0.7f, 1f, 0.4f);
    public Color fieldColor = new Color(1f, 0.5f, 0f, 0.15f);

    public Vector3 GetGravityDirection(Vector3 playerPos)
    {
        Vector3 direction = transform.position - playerPos;

        // CORE INTEGRITY CHECK: If the player somehow glitches straight into the epicenter,
        // fallback to a clean global up vector to prevent division-by-zero camera spins!
        if (direction.sqrMagnitude < 0.001f)
        {
            return Vector3.down;
        }

        return direction.normalized;
    }

    public float GetDistanceToSurface(Vector3 playerPos)
    {
        return Vector3.Distance(playerPos, transform.position) - planetRadius;
    }

    public bool IsPositionInField(Vector3 playerPos)
    {
        return Vector3.Distance(playerPos, transform.position) <= gravityFieldRadius;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        Gizmos.color = surfaceColor;
        Gizmos.DrawWireSphere(transform.position, planetRadius);
        Gizmos.color = fieldColor;
        Gizmos.DrawWireSphere(transform.position, gravityFieldRadius);
    }
}
