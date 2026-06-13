using UnityEngine;

public class SphericalCamera : MonoBehaviour
{
    [Header("Targets")]
    public Transform playerTransform;

    [Header("Distance Settings")]
    public float distanceToPlayer = 8f;
    public float heightOffset = 3.5f;

    [Header("Smoothing")]
    public float positionSmooth = 10f;

    private Vector3 targetCameraPosition;

    void Start()
    {
        if (playerTransform != null)
        {
            UpdateCameraPlacement(true);
        }
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;
        UpdateCameraPlacement(false);
    }

    void UpdateCameraPlacement(bool immediate)
    {
        PlanetGravity planet = GravityManager.GetNearestPlanet(playerTransform.position);
        if (planet == null) return;

        Vector3 planetCenter = planet.transform.position;

        // 1. Core orientation vector relative to the planet's core
        Vector3 playerUpOnPlanet = (playerTransform.position - planetCenter).normalized;

        // 2. Extract Mario's local forward vector projected cleanly flat onto the planet tangent
        Vector3 playerForwardOnSphere = Vector3.ProjectOnPlane(playerTransform.forward, playerUpOnPlanet).normalized;

        // Secure baseline fallback if Mario is perfectly stationary at boot
        if (playerForwardOnSphere.sqrMagnitude < 0.01f)
        {
            playerForwardOnSphere = Vector3.ProjectOnPlane(Vector3.forward, playerUpOnPlanet).normalized;
            if (playerForwardOnSphere.sqrMagnitude < 0.01f)
            {
                playerForwardOnSphere = Vector3.ProjectOnPlane(Vector3.up, playerUpOnPlanet).normalized;
            }
        }

        // 3. Calculate ideal spatial trailing position behind Mario
        Vector3 idealPosition = playerTransform.position - (playerForwardOnSphere * distanceToPlayer) + (playerUpOnPlanet * heightOffset);

        // 4. Position application tracking smooth lag transitions
        if (immediate)
        {
            transform.position = idealPosition;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, idealPosition, positionSmooth * Time.deltaTime);
        }

        // 5. THE ULTIMATE ORIENTATION FIX (Cross Product Construction)
        // Lock the camera's true up vector outward from the planet core center.
        // This is what prevents the camera from ever flipping upside down!
        Vector3 cameraUp = (transform.position - planetCenter).normalized;

        // Find the absolute sideways direction of your screen frame by crossing Up with your target path
        Vector3 cameraRightClean = Vector3.Cross(cameraUp, playerForwardOnSphere).normalized;

        // Derive the true clean look vector by crossing Right with Up.
        // This guarantees a valid forward vector that can never collapse to zero length!
        Vector3 cameraForwardClean = Vector3.Cross(cameraRightClean, cameraUp).normalized;

        // Generate the stable matrix rotation frame
        transform.rotation = Quaternion.LookRotation(cameraForwardClean, cameraUp);
    }
}
