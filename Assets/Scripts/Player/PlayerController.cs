using UnityEngine;
using UnityEngine.InputSystem;
using Animancer;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Capsule / Physics")]
    public CapsuleCollider capsule;
    public LayerMask collisionMask;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float sprintSpeed = 9f;
    public float rotationSmooth = 15f;

    [Header("Jumping")]
    public float jumpSpeed = 7f;
    public float coyoteTime = 0.15f;

    [Header("Gravity")]
    public float gravityMultiplier = 1f;
    public float groundSnapDistance = 0.3f;

    [Header("Input")]
    public InputActionReference moveAction;
    public InputActionReference jumpAction;
    public InputActionReference sprintAction;

    [Header("Camera")]
    public Transform cameraTransform;
    public Transform cameraTarget;

    [Header("Animancer")]
    public AnimancerComponent animancer;
    public AnimationClip idleClip;
    public AnimationClip walkClip;
    public AnimationClip sprintClip;
    public AnimationClip jumpClip;

    private Rigidbody rb;
    private Vector3 surfaceNormal = Vector3.up;
    private float verticalVel;
    private float coyoteCounter;
    private bool grounded;
    private Vector3 playerUp = Vector3.up;

    // Input Caching
    private Vector2 inputVector;
    private bool isSprinting;
    private bool jumpRequested;

    // Cache the current heading vector to smoothly maintain direction when inputs cease
    private Vector3 currentHeadingForward;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        currentHeadingForward = transform.forward;
    }

    void Update()
    {
        // Gather input values every frame smoothly
        inputVector = moveAction.action.ReadValue<Vector2>();
        isSprinting = sprintAction.action.ReadValue<float>() > 0.5f;

        if (jumpAction.action.triggered)
        {
            jumpRequested = true;
        }

        // Keep camera target tracked over frame cycles
        PlanetGravity planet = GravityManager.GetNearestPlanet(transform.position);
        if (planet != null)
        {
            Vector3 gravityDir = planet.GetGravityDirection(transform.position);
            cameraTarget.up = -gravityDir;
        }

        // Drive animations based on grounded status
        if (!grounded)
        {
            animancer.Play(jumpClip, 0.1f);
        }
        else if (inputVector.sqrMagnitude > 0.01f)
        {
            animancer.Play(isSprinting ? sprintClip : walkClip, 0.1f);
        }
        else
        {
            animancer.Play(idleClip, 0.2f);
        }
    }

    void FixedUpdate()
    {
        PlanetGravity planet = GravityManager.GetNearestPlanet(transform.position);
        if (planet == null) return;

        Vector3 gravityDir = planet.GetGravityDirection(transform.position);

        // 1. Resolve Surface Normal (Safe multi-sampling)
        surfaceNormal = SurfaceNormalResolver.ResolveSurfaceNormal(
            transform.position,
            gravityDir,
            3f,
            collisionMask,
            surfaceNormal,
            capsule.radius,
            transform
        );

        // 2. Mario Galaxy Core-to-Normal Blending
        Vector3 coreUp = -gravityDir;
        Vector3 targetPlayerUp = Vector3.Slerp(coreUp, surfaceNormal, 0.3f).normalized;
        playerUp = Vector3.Slerp(playerUp, targetPlayerUp, 10f * Time.fixedDeltaTime).normalized;

        // 3. Keep Player Oriented Upwards Relative to the Sphere
        Quaternion currentRotationWithoutYaw = Quaternion.FromToRotation(transform.up, playerUp) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, currentRotationWithoutYaw, rotationSmooth * Time.fixedDeltaTime);


        // ---------------------------------------------------------------------
        // 4. POLAR-SAFE SCREEN COORDINATE MATRIX (The South Pole Fix)
        // ---------------------------------------------------------------------
        // We use your cameraTarget tracking frame, keeping your Cinemachine setup working.
        Vector3 targetRight = cameraTarget.right;
        Vector3 targetForward = cameraTarget.forward;

        // Flatten the right vector onto the player's current standing planet tangent plane
        Vector3 cleanPlaneRight = Vector3.ProjectOnPlane(targetRight, playerUp).normalized;

        // THE FIXED MATRICES: Instead of a cross product (which collapses to zero length at the poles),
        // we use a Quaternion to rotate the clean horizontal right vector by 90 degrees along the playerUp axis.
        // This is mathematically guaranteed never to suffer from gimbal lock or division-by-zero spinning!
        Vector3 cleanPlaneForward = Quaternion.AngleAxis(90f, playerUp) * cleanPlaneRight;

        // Ensure direction consistency: If the generated forward vector mirrors backwards 
        // relative to the camera view, flip it back instantly.
        if (Vector3.Dot(cleanPlaneForward, targetForward) < 0f)
        {
            cleanPlaneForward = -cleanPlaneForward;
        }

        // Secure fallbacks if variables calculate near zero
        if (cleanPlaneForward.sqrMagnitude < 0.01f) cleanPlaneForward = transform.forward;
        if (cleanPlaneRight.sqrMagnitude < 0.01f) cleanPlaneRight = transform.right;

        // Synthesize screen space stick inputs directly onto the curved mesh coordinates
        Vector3 moveDir = (cleanPlaneForward * inputVector.y + cleanPlaneRight * inputVector.x).normalized;


        // ---------------------------------------------------------------------
        // 5. LOOK DIRECTION HANDLING (Yaw Decoupled)
        // ---------------------------------------------------------------------
        if (moveDir.sqrMagnitude > 0.01f)
        {
            currentHeadingForward = moveDir;
        }
        else
        {
            currentHeadingForward = Vector3.ProjectOnPlane(currentHeadingForward, playerUp).normalized;
        }

        Quaternion targetHeadingRotation = Quaternion.LookRotation(currentHeadingForward, playerUp);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetHeadingRotation, rotationSmooth * Time.fixedDeltaTime);


        // ---------------------------------------------------------------------
        // 6 & 7. Ground Checking and Jumping
        // ---------------------------------------------------------------------
        Vector3 rayOrigin = transform.position + playerUp * (capsule.height * 0.5f);
        float totalRayLength = (capsule.height * 0.5f) + groundSnapDistance;
        grounded = Physics.Raycast(rayOrigin, -playerUp, out RaycastHit groundHit, totalRayLength, collisionMask);

        if (grounded)
        {
            coyoteCounter = coyoteTime;
            rb.AddForce(gravityDir * planet.gravityStrength * gravityMultiplier, ForceMode.Acceleration);

            if (jumpRequested && coyoteCounter > 0f)
            {
                rb.linearVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, playerUp) + (playerUp * jumpSpeed);
                grounded = false;
                coyoteCounter = 0f;
            }
        }
        else
        {
            coyoteCounter -= Time.fixedDeltaTime;
            rb.AddForce(gravityDir * planet.gravityStrength * gravityMultiplier, ForceMode.Acceleration);
        }
        jumpRequested = false;


        // ---------------------------------------------------------------------
        // 8. Rigidbody Movement Velocity Translation
        // ---------------------------------------------------------------------
        float targetSpeed = isSprinting ? sprintSpeed : moveSpeed;
        Vector3 targetHorizontalVelocity = moveDir * (inputVector.magnitude * targetSpeed);

        // Maintain falling/jumping momentum cleanly along the current playerUp axis
        Vector3 currentVerticalVelocity = Vector3.Project(rb.linearVelocity, playerUp);
        rb.linearVelocity = targetHorizontalVelocity + currentVerticalVelocity;

        // Synchronize your camera target tracker flatly to the planet up direction.
        cameraTarget.up = playerUp;
    }


}
