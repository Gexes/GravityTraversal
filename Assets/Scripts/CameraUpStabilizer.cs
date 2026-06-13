using UnityEngine;

public class CameraUpStabilizer : MonoBehaviour
{
    [Header("Tracking Focus")]
    public Transform playerTransform;

    [Header("Orientation Blending")]
    [Tooltip("How fast the camera horizon rolls and flips to accommodate a new planet's gravity angle mid-air. Lower values = smoother cinematic transitions.")]
    public float gravityRotationSmooth = 3f;

    // Cache the active tracking vector across frames to prevent sudden jumps
    private Vector3 smoothedPlanetaryUp = Vector3.up;

    void Start()
    {
        if (playerTransform != null)
        {
            // Establish baseline alignment at boot
            PlanetGravity planet = GravityManager.GetNearestPlanet(playerTransform.position);
            if (planet != null)
            {
                smoothedPlanetaryUp = (playerTransform.position - planet.transform.position).normalized;
                transform.up = smoothedPlanetaryUp;
            }
        }
    }

    void FixedUpdate()
    {
        if (playerTransform == null) return;

        // Query your existing GravityManager to find the active planet Mario is standing on
        PlanetGravity planet = GravityManager.GetNearestPlanet(playerTransform.position);
        if (planet == null) return;

        // 1. Calculate the raw, absolute vertical normal vector pointing straight away from the planet's core
        Vector3 rawPlanetaryUp = (playerTransform.position - planet.transform.position).normalized;

        // 2. THE CHOTIC SNAP FIX: Smoothly blend our tracking vector over time using Slerp.
        // Instead of hard-snapping when crossing into a new gravity bubble mid-air, 
        // the camera's reference horizon plane will glide gracefully toward the new axis!
        smoothedPlanetaryUp = Vector3.Slerp(smoothedPlanetaryUp, rawPlanetaryUp, gravityRotationSmooth * Time.fixedDeltaTime).normalized;

        // 3. Lock this object's rotation to face our smoothed planet curvature profile
        transform.up = smoothedPlanetaryUp;
    }
}
