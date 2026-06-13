using System.Collections;
using UnityEngine;
using UnityEngine.Splines; // Required Unity Spline Namespace

public class LaunchStar : MonoBehaviour
{
    [Header("Spline Path")]
    public SplineContainer splineContainer; // Drag your Unity Spline component here

    [Header("Launch Settings")]
    public float launchDuration = 3f; // Absolute travel time along the path

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && splineContainer != null)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && player.enabled)
            {
                StartCoroutine(SplineLaunchRoutine(player));
            }
        }
    }

    private IEnumerator SplineLaunchRoutine(PlayerController player)
    {
        Rigidbody rb = player.GetComponent<Rigidbody>();

        // 1. Freeze player physics and player inputs entirely
        player.enabled = false;
        rb.isKinematic = true;

        float elapsed = 0f;

        // 2. THE BLUR FIX: Run the spline traversal exclusively inside the physics frame loop!
        // This ensures the player's position is updated on the exact same clock tick as the camera.
        while (elapsed < launchDuration)
        {
            // Use Time.fixedDeltaTime instead of Time.deltaTime because we are syncing with FixedUpdate
            elapsed += Time.fixedDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / launchDuration);

            // Sample the exact position on the Unity Spline Container
            Vector3 targetPosition = splineContainer.EvaluatePosition(normalizedTime);
            player.transform.position = targetPosition;

            // 3. Dynamic Mid-Air Orientation Alignment
            PlanetGravity currentPlanet = GravityManager.GetNearestPlanet(player.transform.position);
            if (currentPlanet != null)
            {
                // Pull gravity direction relative to the planet we are flying over
                Vector3 currentGravityDir = currentPlanet.GetGravityDirection(player.transform.position);
                Vector3 localPlanetUp = -currentGravityDir;

                // Sample the forward vector of the spline track to know where Mario is flying
                Vector3 splineForward = splineContainer.EvaluateTangent(normalizedTime);
                Vector3 cleanForward = Vector3.ProjectOnPlane(splineForward, localPlanetUp).normalized;

                if (cleanForward.sqrMagnitude > 0.01f)
                {
                    // Generate a look rotation that sits flat with gravity but faces along the track
                    Quaternion targetFlightRotation = Quaternion.LookRotation(cleanForward, localPlanetUp);

                    // Smoothly rotate the player mesh frame using fixed timings
                    player.transform.rotation = Quaternion.Slerp(player.transform.rotation, targetFlightRotation, 8f * Time.fixedDeltaTime);
                }
            }

            // CRITICAL SYNC HOOK: Force the loop to hold until the next physics loop (FixedUpdate tick)
            // Changing this from 'yield return null' stops the ghosting/blurring effect completely.
            yield return new WaitForFixedUpdate();
        }

        // 4. Reset simulation states cleanly on touchdown
        rb.isKinematic = false;
        player.enabled = true;
    }
}
