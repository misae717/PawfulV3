using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerMovement : MonoBehaviour
{
    [System.Serializable]
    public class JumpConfiguration
    {
        [Header("Basic Settings")]
        public float jumpForce = 12f;
        public float doubleJumpForce = 10f;
        public float maxFallSpeed = 20f;
        
        [Header("Jump Feel")]
        public float risingGravityMult = 1f;
        public float fallingGravityMult = 2.5f;
        public float fastFallGravityMult = 3f;
        public float jumpCutMultiplier = 0.5f;
        
        [Header("Jump Control")]
        public float maxHorizontalSpeed = 8f;
        [Range(0f, 1f)]
        [Tooltip("Controls how much the player can influence their movement in the air.")]
        public float airControlStrength = 0.2f; // Slight air control
    }

    [System.Serializable]
    public class LongJumpConfiguration : JumpConfiguration
    {
        [Header("Long Jump Specific")]
        public float forceMultiplier = 1.5f;
        [Range(1f, 5f)]
        public float horizontalBoostMultiplier = 2.5f;
        [Range(0f, 1f)]
        public float verticalReducer = 0.5f;
        public float dashSpeed = 15f;

        [Header("Launch Settings")]
        [Tooltip("Launch speed for the long jump (combined horizontal and vertical)")]
        public float launchSpeed = 20f;

        [Tooltip("Launch angle in degrees. 0° is to the right, 90° is straight up.")]
        [Range(0f, 90f)]
        public float launchAngle = 45f;
    }

    [Header("Component References")]
    private Rigidbody2D rb;
    private Animator animator;
    private PlayerInput playerInput;
    private bool isFacingRight = true;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.2f;
    private bool isGrounded;

    [Header("Movement Parameters")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float deceleration = 50f;
    
    [Header("Controller Settings")]
    [SerializeField] private float walkThreshold = 0.25f;
    [SerializeField] private float deadzone = 0.1f;

    [Header("Jump Configurations")]
    [SerializeField] private JumpConfiguration normalJumpConfig = new JumpConfiguration();
    [SerializeField] private LongJumpConfiguration longJumpConfig = new LongJumpConfiguration();
    [SerializeField] private int maxJumpCount = 2;
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float jumpBufferTime = 0.2f;

    [Header("Dash Settings")]
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    private bool isDashing = false;
    private float dashTimeLeft;
    private float lastDashTime = -Mathf.Infinity;

    [Header("Double Jump Control Settings")]
    [SerializeField] private float doubleJumpControlDuration = 0.2f; // Duration to allow air control after double jump
    private bool isDoubleJumping = false;
    private float doubleJumpTimeLeft;

    [Header("Climbing Settings")]
    [SerializeField] private string ladderTag = "Ladder";
    [SerializeField] private string ropeTag = "Rope";
    [SerializeField] private float ladderClimbingSpeed = 5f;
    [SerializeField] private float ropeClimbingSpeed = 3f;
    [SerializeField] private float ropeSlideSpeed = 1f;

    [Header("Input Settings")]
    [SerializeField] private string climbActionName = "Climb"; // Name of the Climb action in the Input Action asset

    // State Variables
    private Vector2 moveInput;
    private float baseGravityScale;
    private string currentControlScheme;
    private int jumpCount;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool isJumpPressed;
    private bool isLongJumpPressed;
    private bool wasGroundedLastFrame;
    private bool isClimbing;
    private bool isOnLadder = false;
    private bool isClimbingLadder = false;
    private bool isOnRope = false;
    private bool isClimbingRope = false;
    private bool isClimbInputHeld = false;

    // Animation Hash IDs
    private static readonly int IsWalking = Animator.StringToHash("IsWalking");
    private static readonly int IsRunning = Animator.StringToHash("IsRunning");
    private static readonly int IsJumping = Animator.StringToHash("IsJumping");
    private static readonly int IsClimbing = Animator.StringToHash("IsClimbing");
    private static readonly int VerticalSpeed = Animator.StringToHash("VerticalSpeed");
    private static readonly int Dash = Animator.StringToHash("Dash"); 
    private static readonly int IsClimbingLadder = Animator.StringToHash("IsClimbingLadder");
    private static readonly int IsClimbingRope = Animator.StringToHash("IsClimbingRope");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();
        baseGravityScale = rb.gravityScale;
        SetupPhysicsMaterial();

        if (playerInput != null)
        {
            currentControlScheme = playerInput.currentControlScheme;
            playerInput.onControlsChanged += OnControlsChanged;
        }
    }

    private void OnDestroy()
    {
        if (playerInput != null)
        {
            playerInput.onControlsChanged -= OnControlsChanged;
        }
    }

    private void SetupPhysicsMaterial()
    {
        PhysicsMaterial2D material = new PhysicsMaterial2D("PlayerMaterial")
        {
            friction = 0f,
            bounciness = 0f
        };

        if (TryGetComponent<Collider2D>(out var collider))
        {
            collider.sharedMaterial = material;
        }
    }

    private void OnControlsChanged(PlayerInput input)
    {
        currentControlScheme = input.currentControlScheme;
    }

    private void Update()
    {
        UpdateGroundState();
        HandleCoyoteTime();
        HandleJumpBuffer();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        // Handle Dash Duration
        if (isDashing)
        {
            dashTimeLeft -= Time.fixedDeltaTime;
            if (dashTimeLeft <= 0)
            {
                isDashing = false;
            }
            else
            {
                // During dash, maintain velocity and restrict control
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y);
                return; // Skip regular movement controls
            }
        }

        // Handle Double Jump Control Duration
        if (isDoubleJumping)
        {
            doubleJumpTimeLeft -= Time.fixedDeltaTime;
            if (doubleJumpTimeLeft <= 0)
            {
                isDoubleJumping = false;
            }
        }

        // Apply Movement
        ApplyMovement();

        // Apply Gravity Multipliers
        ApplyGravityMultipliers();

        // Clamp Fall Speed
        ClampFallSpeed();

        // Handle Climbing
        HandleClimbing();
    }

    private void UpdateGroundState()
    {
        wasGroundedLastFrame = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded && !wasGroundedLastFrame)
        {
            jumpCount = 0;
        }
    }

    private void HandleCoyoteTime()
    {
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private void HandleJumpBuffer()
    {
        if (isJumpPressed || isLongJumpPressed)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    private void ApplyMovement()
    {
        // Apply movement only if not climbing
        if (isClimbingLadder || isClimbingRope)
            return;

        // Determine control strength based on grounded state
        float controlStrength = isGrounded ? 1f : normalJumpConfig.airControlStrength;

        if (!isClimbInputHeld)
        {
            // Only allow climbing when Climb input is held
            if (isClimbingLadder || isClimbingRope)
            {
                // If Climb input is released, stop climbing
                isClimbingLadder = false;
                isClimbingRope = false;
                rb.gravityScale = baseGravityScale;
                animator.SetBool(IsClimbingLadder, false);
                animator.SetBool(IsClimbingRope, false);
            }
        }

        if (Mathf.Abs(moveInput.x) < deadzone)
        {
            // Apply deceleration when no input is given
            float frictionForce = -rb.velocity.x * deceleration * controlStrength;
            rb.AddForce(Vector2.right * frictionForce);
            return;
        }

        // Determine target speed based on input
        float targetSpeed = (Mathf.Abs(moveInput.x) <= walkThreshold) ? walkSpeed : runSpeed;
        targetSpeed *= Mathf.Sign(moveInput.x);

        // Calculate speed difference
        float speedDiff = targetSpeed - rb.velocity.x;
        float force = speedDiff * acceleration * controlStrength;

        // Apply movement force
        rb.AddForce(Vector2.right * force);

        // Handle sprite flipping
        if (moveInput.x > 0 && !isFacingRight)
            Flip();
        else if (moveInput.x < 0 && isFacingRight)
            Flip();
    }

    private void ApplyGravityMultipliers()
    {
        var config = isLongJumpPressed ? longJumpConfig : normalJumpConfig;
        
        if (rb.velocity.y < 0)
        {
            rb.gravityScale = baseGravityScale * 
                (moveInput.y < -0.5f ? config.fastFallGravityMult : config.fallingGravityMult);
        }
        else if (rb.velocity.y > 0)
        {
            if (!isJumpPressed && !isLongJumpPressed)
            {
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * config.jumpCutMultiplier);
            }
            rb.gravityScale = baseGravityScale * config.risingGravityMult;
        }
        else
        {
            rb.gravityScale = baseGravityScale;
        }

        // Apply additional air control if in double jump control duration
        if (!isGrounded && !isDashing && isDoubleJumping)
        {
            float airControlMultiplier = isLongJumpPressed ? longJumpConfig.airControlStrength : normalJumpConfig.airControlStrength;
            rb.velocity = new Vector2(
                rb.velocity.x + (moveInput.x * acceleration * airControlMultiplier * Time.fixedDeltaTime),
                rb.velocity.y
            );
            
            float maxSpeed = isLongJumpPressed ? longJumpConfig.maxHorizontalSpeed : normalJumpConfig.maxHorizontalSpeed;
            rb.velocity = new Vector2(
                Mathf.Clamp(rb.velocity.x, -maxSpeed, maxSpeed),
                rb.velocity.y
            );
        }
    }

    private void ClampFallSpeed()
    {
        var config = isLongJumpPressed ? longJumpConfig : normalJumpConfig;
        if (rb.velocity.y < -config.maxFallSpeed)
        {
            rb.velocity = new Vector2(rb.velocity.x, -config.maxFallSpeed);
        }
    }

    private void HandleClimbing()
    {
        // Handle Ladder Climbing
        if (isClimbingLadder)
        {
            float verticalInput = moveInput.y;

            // Reset velocity upon latching to ladder
            if (isClimbingLadder && Mathf.Approximately(rb.velocity.y, 0f))
            {
                rb.velocity = Vector2.zero;
                jumpCount = 0; // Reset jump counter
            }

            if (Mathf.Abs(verticalInput) > deadzone)
            {
                rb.velocity = new Vector2(rb.velocity.x, verticalInput * ladderClimbingSpeed);
            }
            else
            {
                rb.velocity = new Vector2(rb.velocity.x, 0f);
            }

            rb.gravityScale = 0f; // Disable gravity while climbing
            animator.SetBool(IsClimbingLadder, true);
        }
        else
        {
            animator.SetBool(IsClimbingLadder, false);
        }

        // Handle Rope Climbing
        if (isClimbingRope)
        {
            float verticalInput = moveInput.y;

            // Reset velocity upon latching to rope
            if (isClimbingRope && Mathf.Approximately(rb.velocity.y, 0f))
            {
                rb.velocity = Vector2.zero;
                jumpCount = 0; // Reset jump counter
            }

            if (Mathf.Abs(verticalInput) > deadzone)
            {
                rb.velocity = new Vector2(rb.velocity.x, verticalInput * ropeClimbingSpeed);
            }
            else
            {
                rb.velocity = new Vector2(rb.velocity.x, 0f);
            }

            rb.gravityScale = 0f; // Disable gravity while climbing
            animator.SetBool(IsClimbingRope, true);
        }
        else
        {
            animator.SetBool(IsClimbingRope, false);
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        try 
        {
            bool isMoving = Mathf.Abs(rb.velocity.x) > 0.1f;
            bool isRunning = isMoving && Mathf.Abs(moveInput.x) > walkThreshold;
            
            animator.SetBool(IsWalking, isMoving && !isRunning);
            animator.SetBool(IsRunning, isRunning);
            animator.SetBool(IsJumping, !isGrounded && !isClimbingLadder && !isClimbingRope);
            animator.SetBool(IsClimbing, isClimbingLadder || isClimbingRope);
            animator.SetFloat(VerticalSpeed, rb.velocity.y);

            if (isDashing)
            {
                animator.SetTrigger(Dash);
            }
        }
        catch (System.Exception)
        {
            // Animation parameters not set up yet
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        isJumpPressed = value.isPressed;

        if (value.isPressed)
        {
            TryJump(false);
        }
        else if (rb.velocity.y > 0)
        {
            var config = normalJumpConfig;
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * config.jumpCutMultiplier);
        }
    }

    public void OnLongJump(InputValue value)
    {
        if (value.isPressed)
        {
            isLongJumpPressed = true;
            TryJump(true);
        }
        else
        {
            isLongJumpPressed = false;
        }
    }

    public void OnClimb(InputValue value)
    {
        isClimbInputHeld = value.isPressed;

        if (isClimbInputHeld)
        {
            // Attempt to start climbing if on ladder or rope
            if (isOnLadder || isOnRope)
            {
                if (isOnLadder && !isClimbingLadder)
                {
                    isClimbingLadder = true;
                    rb.velocity = Vector2.zero; // Reset velocity upon latching
                    jumpCount = 0; // Reset jump counter
                    rb.gravityScale = 0f; // Disable gravity
                    animator.SetBool(IsClimbingLadder, true);
                    Debug.Log("Started Climbing Ladder");
                }

                if (isOnRope && !isClimbingRope)
                {
                    isClimbingRope = true;
                    rb.velocity = Vector2.zero; // Reset velocity upon latching
                    jumpCount = 0; // Reset jump counter
                    rb.gravityScale = 0f; // Disable gravity
                    animator.SetBool(IsClimbingRope, true);
                    Debug.Log("Started Climbing Rope");
                }
            }
        }
        else
        {
            // Stop climbing if Climb input is released
            if (isClimbingLadder)
            {
                isClimbingLadder = false;
                rb.gravityScale = baseGravityScale;
                animator.SetBool(IsClimbingLadder, false);
                Debug.Log("Stopped Climbing Ladder");
            }

            if (isClimbingRope)
            {
                isClimbingRope = false;
                rb.gravityScale = baseGravityScale;
                animator.SetBool(IsClimbingRope, false);
                Debug.Log("Stopped Climbing Rope");
            }
        }
    }

    private void TryJump(bool isLongJump)
    {
        var config = isLongJump ? longJumpConfig : normalJumpConfig;
        
        if ((isGrounded || coyoteTimeCounter > 0f) && jumpBufferCounter > 0f)
        {
            float jumpPower = isLongJump ? 
                config.jumpForce * longJumpConfig.forceMultiplier : 
                config.jumpForce;
                
            PerformJump(jumpPower, isLongJump);
            jumpCount = 1;
        }
        else if (jumpCount < maxJumpCount)
        {
            float jumpPower = isLongJump ? 
                config.doubleJumpForce * longJumpConfig.forceMultiplier : 
                config.doubleJumpForce;
                
            PerformJump(jumpPower, isLongJump);
            jumpCount++;

            // Enable limited air control for double jump
            if (!isLongJump)
            {
                isDoubleJumping = true;
                doubleJumpTimeLeft = doubleJumpControlDuration;
            }
        }
    }

    private void PerformJump(float force, bool isLongJump)
    {
        if (isLongJump)
        {
            if (Time.time < lastDashTime + dashCooldown)
                return; // Prevent dashing if cooldown not finished

            isDashing = true;
            dashTimeLeft = dashDuration;
            lastDashTime = Time.time;

            // Calculate launch direction based on launch angle
            float angleRad = longJumpConfig.launchAngle * Mathf.Deg2Rad;
            float horizontalDirection = moveInput.x != 0 ? Mathf.Sign(moveInput.x) : (isFacingRight ? 1f : -1f);

            // Calculate velocity components
            float launchVelocityX = longJumpConfig.launchSpeed * Mathf.Cos(angleRad) * horizontalDirection;
            float launchVelocityY = longJumpConfig.launchSpeed * Mathf.Sin(angleRad);

            // Apply launch velocities without disrupting current velocity
            rb.velocity = new Vector2(launchVelocityX, rb.velocity.y + launchVelocityY);

            // Trigger dash animation
            animator.SetTrigger(Dash);

            // Play dash sound if any
            // dashSound.Play();
        }
        else
        {
            // Normal or Double Jump - mostly vertical with optional control
            rb.velocity = new Vector2(rb.velocity.x, force);

            // If it's a double jump, allow limited air control
            if (jumpCount > 1)
            {
                isDoubleJumping = true;
                doubleJumpTimeLeft = doubleJumpControlDuration;
            }

            // If climbing, reset climbing state
            if (isClimbingLadder)
            {
                isClimbingLadder = false;
                rb.gravityScale = baseGravityScale;
                animator.SetBool(IsClimbingLadder, false);
            }

            if (isClimbingRope)
            {
                isClimbingRope = false;
                rb.gravityScale = baseGravityScale;
                animator.SetBool(IsClimbingRope, false);
            }
        }

        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(ladderTag))
        {
            isOnLadder = true;
            Debug.Log("Entered Ladder Area");
        }
        else if (collision.CompareTag(ropeTag))
        {
            isOnRope = true;
            Debug.Log("Entered Rope Area");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag(ladderTag))
        {
            isOnLadder = false;
            isClimbingLadder = false;
            rb.gravityScale = baseGravityScale;
            animator.SetBool(IsClimbingLadder, false);
            Debug.Log("Exited Ladder Area");
        }
        else if (collision.CompareTag(ropeTag))
        {
            isOnRope = false;
            isClimbingRope = false;
            rb.gravityScale = baseGravityScale;
            animator.SetBool(IsClimbingRope, false);
            Debug.Log("Exited Rope Area");
        }
    }

    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        // Optional: Visualize climbing states
        if (isClimbingLadder)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
        }

        if (isClimbingRope)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 2f);
        }
    }
}