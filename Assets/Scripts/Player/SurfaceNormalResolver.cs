using UnityEngine;

public static class SurfaceNormalResolver
{
    public static Vector3 ResolveSurfaceNormal(
        Vector3 position,
        Vector3 gravityDir,
        float rayDistance,
        LayerMask mask,
        Vector3 previousNormal,
        float capsuleRadius,
        Transform playerTransform) // Added playerTransform to sample local space axes
    {
        Vector3 upDirection = -gravityDir;
        Vector3 accumulatedNormal = Vector3.zero;
        int hitCount = 0;

        // 1. Center Point Sample (Elevated slightly to prevent clipping)
        if (Physics.Raycast(position + (upDirection * 0.1f), gravityDir, out RaycastHit centerHit, rayDistance, mask))
        {
            accumulatedNormal += centerHit.normal;
            hitCount++;
        }

        // 2. THE ULTIMATE POLE FIX: Sample the perimeter using Mario's local directions.
        // By relying on the player's live local right/forward components instead of global assets, 
        // the cross-products never experience inversion flips or collapses at the South Pole.
        float offsetDist = capsuleRadius * 0.7f;

        Vector3 localRight = Vector3.ProjectOnPlane(playerTransform.right, upDirection).normalized;
        Vector3 localForward = Vector3.ProjectOnPlane(playerTransform.forward, upDirection).normalized;

        // Secure fallbacks if player transform isn't fully aligned yet
        if (localRight.sqrMagnitude < 0.01f) localRight = playerTransform.right;
        if (localForward.sqrMagnitude < 0.01f) localForward = playerTransform.forward;

        Vector3[] offsets = new Vector3[]
        {
            localForward * offsetDist,   // Front
            -localForward * offsetDist,  // Back
            localRight * offsetDist,     // Right
            -localRight * offsetDist     // Left
        };

        foreach (Vector3 offset in offsets)
        {
            Vector3 rayOrigin = position + offset + (upDirection * 0.1f);
            if (Physics.Raycast(rayOrigin, gravityDir, out RaycastHit edgeHit, rayDistance, mask))
            {
                accumulatedNormal += edgeHit.normal;
                hitCount++;
            }
        }

        // 3. Smooth Blending Matrix
        if (hitCount > 0)
        {
            Vector3 averageNormal = (accumulatedNormal / hitCount).normalized;
            // Use Time.fixedDeltaTime inside FixedUpdate calls for perfectly smooth steps
            return Vector3.Slerp(previousNormal, averageNormal, 10f * Time.fixedDeltaTime);
        }

        return Vector3.Slerp(previousNormal, upDirection, 5f * Time.fixedDeltaTime);
    }
}
