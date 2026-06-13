using UnityEngine;

public class CameraAnchorController : MonoBehaviour
{
    [Header("Targets")]
    public Transform playerTransform;

    [Header("Settings")]
    public float positionSmooth = 10f;
    public float rotationSmooth = 5f;

    // Cache a stable baseline world direction to act as our unchanging global compass heading
    private Vector3 stableGlobalCompass;

    void Start()
    {
        if (playerTransform != null)
        {
            transform.position = playerTransform.position;
        }

        // Define a permanent directional vector (e.g., Global Forward) to act as the camera's reference pole.
        // This ensures the camera's viewing axis is permanently anchored to the cosmic frame.
        stableGlobalCompass = Vector3.forward;
    }

    void FixedUpdate()
    {
        if (playerTransform == null) return;

        PlanetGravity planet = GravityManager.GetNearestPlanet(playerTransform.position);
        if (planet == null) return;

        // 1. Extract the clean spherical "Up" vector directly away from the planet's core
        Vector3 planetUp = (playerTransform.position - planet.transform.position).normalized;

        // 2. Smoothly slide the anchor's position straight to Mario's location
        transform.position = Vector3.Lerp(transform.position, playerTransform.position, positionSmooth * Time.fixedDeltaTime);

        // 3. THE REVOLUTIONARY FIX: Derive a completely stable, non-swinging forward vector!
        // We project our unchanging global compass axis flat onto the planet's changing surface plane.
        // As Mario moves, this vector curves perfectly around the globe, but NEVER swings when Mario steers.
        Vector3 nonSwingingForward = Vector3.ProjectOnPlane(stableGlobalCompass, planetUp).normalized;

        // Handle edge-case pole singularities safely
        if (nonSwingingForward.sqrMagnitude < 0.01f)
        {
            nonSwingingForward = Vector3.ProjectOnPlane(Vector3.up, planetUp).normalized;
        }

        // 4. Force the anchor to align upright to the planet using our permanent compass horizon
        Quaternion targetRotation = Quaternion.LookRotation(nonSwingingForward, planetUp);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmooth * Time.fixedDeltaTime);
    }
}
