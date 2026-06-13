using UnityEngine;
using DG.Tweening;

public class LaunchStarSpinner : MonoBehaviour
{
    [Header("Core Spinning")]
    [Tooltip("How long it takes (in seconds) to complete one full 360-degree spin.")]
    public float spinDuration = 1.5f;

    [Header("Hover Tilt (Floating Effect)")]
    [Tooltip("How far the star tilts side to side.")]
    public float tiltAngle = 15f;
    [Tooltip("How long it takes to tilt back and forth.")]
    public float tiltDuration = 2.5f;

    void Start()
    {
        // Start the automated animation loops
        StartLaunchStarAnimations();
    }

    private void StartLaunchStarAnimations()
    {
        // 1. FAST CONTINUOUS CORE SPIN
        // Rotate 360 degrees around the local Z-axis (Forward) infinitely
        transform.DOLocalRotate(new Vector3(0f, 0f, 360f), spinDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)               // Maintain a perfect, uniform speed
            .SetLoops(-1, LoopType.Incremental)  // Loop infinitely (-1) by continuously adding to the angle
            .SetLink(gameObject);               // Automatically cleans up the tween if the object is destroyed

        // 2. GENTLE HOVER TILT
        // Rotate slightly on the local X and Y axes to simulate floating in outer space
        transform.DOLocalRotate(new Vector3(tiltAngle, tiltAngle * 0.5f, 0f), tiltDuration)
            .SetEase(Ease.InOutSine)             // Smooth deceleration at the edges of the tilt
            .SetLoops(-1, LoopType.Yoyo)        // Reverse direction back and forth infinitely
            .SetLink(gameObject);
    }
}
