using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

public class LaunchStar : MonoBehaviour
{
    [Header("Spline Path")]
    public SplineContainer splineContainer;

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

        // 1. Freeze player physics loops and standard keyboard inputs
        player.enabled = false;
        rb.isKinematic = true;

        // 2. THE ANIMATION FIX: Command Animancer to override the player state
        // and force-play the 'flyClip' defined inside your player script.
        if (player.animancer != null && player.flyClip != null)
        {
            player.animancer.Play(player.flyClip, 0.15f); // 0.15s crossfade blend time
        }

        float elapsed = 0f;

        // 3. Traversal loop along the custom spline track (Physics-synchronized)
        while (elapsed < launchDuration)
        {
            elapsed += Time.fixedDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / launchDuration);

            // Sample the exact position on the Unity Spline Container
            Vector3 targetPosition = splineContainer.EvaluatePosition(normalizedTime);
            player.transform.position = targetPosition;

            // Dynamic Mid-Air Orientation Alignment
            PlanetGravity currentPlanet = GravityManager.GetNearestPlanet(player.transform.position);
            if (currentPlanet != null)
            {
                Vector3 currentGravityDir = currentPlanet.GetGravityDirection(player.transform.position);
                Vector3 localPlanetUp = -currentGravityDir;

                // Sample the forward vector of the spline track to orient the body direction
                Vector3 splineForward = splineContainer.EvaluateTangent(normalizedTime);
                Vector3 cleanForward = Vector3.ProjectOnPlane(splineForward, localPlanetUp).normalized;

                if (cleanForward.sqrMagnitude > 0.01f)
                {
                    Quaternion targetFlightRotation = Quaternion.LookRotation(cleanForward, localPlanetUp);
                    player.transform.rotation = Quaternion.Slerp(player.transform.rotation, targetFlightRotation, 8f * Time.fixedDeltaTime);
                }
            }

            yield return new WaitForFixedUpdate();
        }

        // 4. Reset simulation states cleanly on touchdown. 
        // When player.enabled turns back true, the standard Update loop animation tree 
        // will automatically take back over and clear the flight state!
        rb.isKinematic = false;
        player.enabled = true;
    }
}
