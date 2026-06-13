using UnityEngine;

[ExecuteAlways]
[SelectionBase]
public class EditorPlanetAligner : MonoBehaviour
{
    [Header("Alignment Settings")]
    [Tooltip("How fast the object snaps flatly to the planet's surface normal. Set to 0 for instant snapping.")]
    public float alignmentSmoothSpeed = 0f;

    [Header("Editor Controls")]
    [Tooltip("Uncheck this if you want to temporarily freeze the rotation manually.")]
    public bool autoAlignInEditor = true;

    private Vector3 lastPosition;

    void Update()
    {
        if (!autoAlignInEditor) return;

        // Force an evaluation check if the object has been moved by your editor handles
        if (transform.position == lastPosition) return;

        AlignToNearestPlanet();

        lastPosition = transform.position;
    }

    private void AlignToNearestPlanet()
    {
        PlanetGravity targetPlanet = null;

        if (Application.isPlaying)
        {
            // 1. If the game is running, use your optimized runtime manager system
            targetPlanet = GravityManager.GetNearestPlanet(transform.position);
        }
        else
        {
            // 2. THE EDITOR FIX: Find all planets in the editor scene layout manually
            // This bypasses the empty manager lists when the application is stopped
            PlanetGravity[] editorPlanets = Object.FindObjectsByType<PlanetGravity>(FindObjectsSortMode.None);

            if (editorPlanets == null || editorPlanets.Length == 0) return;

            float closestDistance = float.MaxValue;
            foreach (PlanetGravity planet in editorPlanets)
            {
                // Simple distance calculation check relative to the planet's core
                float distance = Vector3.Distance(transform.position, planet.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    targetPlanet = planet;
                }
            }
        }

        if (targetPlanet == null) return;

        // 3. Extract the clean radial upward direction vector directly from the planet core center
        Vector3 targetUp = (transform.position - targetPlanet.transform.position).normalized;

        if (targetUp.sqrMagnitude < 0.001f) return;

        // 4. Calculate the target orientation matrix that aligns transform.up with targetUp
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, targetUp) * transform.rotation;

        if (Application.isPlaying)
        {
            if (alignmentSmoothSpeed > 0f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, alignmentSmoothSpeed * Time.deltaTime);
            }
            else
            {
                transform.rotation = targetRotation;
            }
        }
        else
        {
            // Snaps instantly in the editor window
            transform.rotation = targetRotation;

#if UNITY_EDITOR
            // Mark the scene layout dirty so Unity saves the object's rotation when saving the project
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(gameObject);

                // Forces the Scene View window to redraw immediately as you drag
                UnityEditor.SceneView.RepaintAll();
            }
#endif
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Safety wrapper to draw editor wires using the same lookups
        PlanetGravity[] editorPlanets = Object.FindObjectsByType<PlanetGravity>(FindObjectsSortMode.None);
        if (editorPlanets == null || editorPlanets.Length == 0) return;

        PlanetGravity closest = editorPlanets[0];
        float bestDist = Vector3.Distance(transform.position, closest.transform.position);

        for (int i = 1; i < editorPlanets.Length; i++)
        {
            float d = Vector3.Distance(transform.position, editorPlanets[i].transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                closest = editorPlanets[i];
            }
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + transform.up * 2f);
    }
}
