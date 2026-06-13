using Animancer;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

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
    [Tooltip("How fast the camera/player leans into a new planet's gravity field when jumping between them.")]
    public float midAirGravitySmooth = 4f;

    [Header("Input")]
    public InputActionReference moveAction;
    public InputActionReference jumpAction;
    public InputActionReference sprintAction;

    [Header("Camera Engine")]
    [Tooltip("Drag your Main Camera here. The script only needs ONE single camera reference to handle controls!")]
    public Transform cameraTransform;

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

        // Set secure dynamic physics configurations
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

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

        // 1. Resolve Surface Normal (Safe multi-sampling mesh check)
        surfaceNormal = SurfaceNormalResolver.ResolveSurfaceNormal(
            transform.position,
            gravityDir,
            3f,
            collisionMask,
            surfaceNormal,
            capsule.radius,
            transform
        );

        // ---------------------------------------------------------------------
        // 2 & 3. DYNAMIC GRAVITY FIELD BLENDING (The Hard Snap Fix)
        // ---------------------------------------------------------------------
        Vector3 coreUp = -gravityDir;
        Vector3 targetPlayerUp = Vector3.Slerp(coreUp, surfaceNormal, 0.3f).normalized;

        // If grounded, conform to gravity quickly to keep physics rock-solid.
        // If mid-air (jumping between worlds), drop the blend speed down to 4f.
        // This lets the player and the Cinemachine Orbital view glide smoothly into the new horizon profile!
        float activeGravitySmooth = grounded ? rotationSmooth : midAirGravitySmooth;
        playerUp = Vector3.Slerp(playerUp, targetPlayerUp, activeGravitySmooth * Time.fixedDeltaTime).normalized;

        // Apply the smoothed planetary alignment orientation to the capsule root
        Quaternion currentRotationWithoutYaw = Quaternion.FromToRotation(transform.up, playerUp) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, currentRotationWithoutYaw, rotationSmooth * Time.fixedDeltaTime);


        // ---------------------------------------------------------------------
        // 4. PURE VIEWPORT SCREEN COORDINATE MATRIX
        // ---------------------------------------------------------------------
        Vector3 camRightAxis = cameraTransform.right;

        // Project the screen's absolute horizontal right vector flat onto Mario's planet tangent plane
        Vector3 cleanPlaneRight = Vector3.ProjectOnPlane(camRightAxis, playerUp).normalized;

        // Derive forward by rotating your screen-space Right vector exactly 90 degrees counter-clockwise along playerUp.
        Vector3 cleanPlaneForward = Quaternion.AngleAxis(-90f, playerUp) * cleanPlaneRight;

        if (cleanPlaneForward.sqrMagnitude < 0.01f) cleanPlaneForward = transform.forward;
        if (cleanPlaneRight.sqrMagnitude < 0.01f) cleanPlaneRight = transform.right;

        // Synthesize screen space stick inputs directly onto the curved planet coordinates
        Vector3 moveDir = (cleanPlaneForward * inputVector.y + cleanPlaneRight * inputVector.x).normalized;


        // 5. PRECISE DETECTIVE GROUND CHECKING (Fired from capsule center)
        Vector3 rayOrigin = transform.position + playerUp * (capsule.height * 0.5f);
        float totalRayLength = (capsule.height * 0.5f) + groundSnapDistance;
        grounded = Physics.Raycast(rayOrigin, -playerUp, out RaycastHit groundHit, totalRayLength, collisionMask);


        // ---------------------------------------------------------------------
        // 6. GROUNDING AND JUMP FORCES PIPELINE
        // ---------------------------------------------------------------------
        if (grounded)
        {
            coyoteCounter = coyoteTime;
            rb.AddForce(gravityDir * planet.gravityStrength * gravityMultiplier, ForceMode.Acceleration);

            if (jumpRequested)
            {
                verticalVel = jumpSpeed; // Directly load the vertical launch velocity vector
                grounded = false;
                coyoteCounter = 0f;
            }
            else
            {
                verticalVel = -1f; // Clamps the capsule cleanly to the planet surface while moving
            }
        }
        else
        {
            coyoteCounter -= Time.fixedDeltaTime;
            rb.AddForce(gravityDir * planet.gravityStrength * gravityMultiplier, ForceMode.Acceleration);

            // Extract current vertical velocity components relative to playerUp while falling
            verticalVel = Vector3.Dot(rb.linearVelocity, playerUp);
            verticalVel -= planet.gravityStrength * gravityMultiplier * Time.fixedDeltaTime;
        }

        // Reset read state
        jumpRequested = false;


        // ---------------------------------------------------------------------
        // 7. Rigidbody Movement Velocity Translation
        // ---------------------------------------------------------------------
        float targetSpeed = isSprinting ? sprintSpeed : moveSpeed;
        Vector3 targetHorizontalVelocity = moveDir * (inputVector.magnitude * targetSpeed);

        // Slide perfectly along slopes and curvatures
        if (grounded)
        {
            targetHorizontalVelocity = Vector3.ProjectOnPlane(targetHorizontalVelocity, groundHit.normal).normalized * targetHorizontalVelocity.magnitude;
        }

        // Apply velocities cleanly to the physics engine
        rb.linearVelocity = targetHorizontalVelocity + (playerUp * verticalVel);


        // ---------------------------------------------------------------------
        // 8. ABSOLUTE VIEWPORT MESH TURNING ENGINE
        // ---------------------------------------------------------------------
        if (moveDir.sqrMagnitude > 0.01f)
        {
            // Read your physical stick inputs as a raw, absolute 2D screen angle (-180 to 180 degrees)
            float joystickTargetAngle = Mathf.Atan2(inputVector.x, inputVector.y) * Mathf.Rad2Deg;

            // Generate a flat look rotation relative to our stable horizontal screen tracking line
            Vector3 camProjectedForward = Vector3.ProjectOnPlane(cameraTransform.forward, playerUp).normalized;
            if (camProjectedForward.sqrMagnitude < 0.01f) camProjectedForward = transform.forward;

            Quaternion cameraPlanarRotation = Quaternion.LookRotation(camProjectedForward, playerUp);
            Quaternion targetHeadingRotation = cameraPlanarRotation * Quaternion.AngleAxis(joystickTargetAngle, Vector3.up);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetHeadingRotation, rotationSmooth * Time.fixedDeltaTime);
        }
    }

}
