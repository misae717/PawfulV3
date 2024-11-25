using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    private Animator animator;
    private PlayerMovement playerMovement;
    private Rigidbody2D rb;

    // Animation State Machine Parameters
    private static readonly int IsWalking = Animator.StringToHash("IsWalking");
    private static readonly int IsRunning = Animator.StringToHash("IsRunning");
    private static readonly int IsJumping = Animator.StringToHash("IsJumping");
    private static readonly int IsDoubleJumping = Animator.StringToHash("IsDoubleJumping");
    private static readonly int IsClimbingLadder = Animator.StringToHash("IsClimbingLadder");
    private static readonly int IsClimbingRope = Animator.StringToHash("IsClimbingRope");
    private static readonly int ClimbDirection = Animator.StringToHash("ClimbDirection");
    private static readonly int VerticalSpeed = Animator.StringToHash("VerticalSpeed");

    [Header("Movement Thresholds")]
    [SerializeField] private float walkThreshold = 0.25f;    // Controller analog threshold for walking
    [SerializeField] private float runThreshold = 0.75f;     // Controller analog threshold for running
    [SerializeField] private float keyboardRunDelay = 0.5f;  // Time to hold key before running
    
    [Header("Animation Speeds")]
    [SerializeField] private float walkAnimationSpeed = 1f;
    [SerializeField] private float runAnimationSpeed = 1.5f;
    [SerializeField] private float climbAnimationSpeed = 1f;
    
    // State tracking
    private bool isGrounded;
    private bool isClimbing;
    private float keyHoldTime;
    private Vector2 moveInput;
    private bool isKeyboard;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody2D>();
        
        // Subscribe to control scheme changes
        if (playerMovement != null)
        {
            var playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.onControlsChanged += OnControlsChanged;
                isKeyboard = playerInput.currentControlScheme == "Keyboard";
            }
        }
    }

    private void OnDestroy()
    {
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.onControlsChanged -= OnControlsChanged;
        }
    }

    private void OnControlsChanged(PlayerInput input)
    {
        isKeyboard = input.currentControlScheme == "Keyboard";
        keyHoldTime = 0f; // Reset key hold time when switching control schemes
    }

    public void UpdateMovementInput(Vector2 input)
    {
        moveInput = input;
        
        if (isKeyboard)
        {
            // For keyboard, track how long the key has been held
            if (Mathf.Abs(input.x) > 0)
            {
                keyHoldTime += Time.deltaTime;
            }
            else
            {
                keyHoldTime = 0f;
            }
        }
        
        UpdateMovementAnimation();
    }

    private void UpdateMovementAnimation()
    {
        float absSpeed = Mathf.Abs(moveInput.x);
        bool isMoving = absSpeed > 0.01f;

        if (!isMoving)
        {
            // Idle state
            animator.SetBool(IsWalking, false);
            animator.SetBool(IsRunning, false);
            animator.speed = 1f;
            return;
        }

        bool isRunning = isKeyboard ? 
            keyHoldTime >= keyboardRunDelay : 
            absSpeed >= runThreshold;

        bool isWalking = isKeyboard ? 
            keyHoldTime < keyboardRunDelay : 
            absSpeed >= walkThreshold && absSpeed < runThreshold;

        // Set animation states
        animator.SetBool(IsWalking, isWalking);
        animator.SetBool(IsRunning, isRunning);

        // Set animation speed
        animator.speed = isRunning ? runAnimationSpeed : walkAnimationSpeed;
    }

    public void UpdateJumpState(bool isJumping, bool isDoubleJumping)
    {
        animator.SetBool(IsJumping, isJumping);
        animator.SetBool(IsDoubleJumping, isDoubleJumping);
    }

    public void UpdateClimbState(bool isClimbingLadder, bool isClimbingRope, float verticalInput)
    {
        animator.SetBool(IsClimbingLadder, isClimbingLadder);
        animator.SetBool(IsClimbingRope, isClimbingRope);
        
        if (isClimbingLadder || isClimbingRope)
        {
            // Set climb direction for animation speed/direction
            animator.SetFloat(ClimbDirection, verticalInput);
            
            // Reverse animation based on direction
            animator.speed = verticalInput >= 0 ? climbAnimationSpeed : -climbAnimationSpeed;
        }
    }

    public void UpdateVerticalSpeed(float speed)
    {
        animator.SetFloat(VerticalSpeed, speed);
    }
}