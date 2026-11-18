using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpForce = 7f;

    [Header("Jump Feel")]
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField, Range(0f, 1f)] private float jumpCutMultiplier = 0.5f;

    [Header("Attack")]
    [SerializeField] private float attackDuration = 0.4f;   // match your attack clip length
    [SerializeField] private bool lockMovementWhileAttacking = true;

    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sprite;
    InputAction moveAction, jumpAction, attackAction;

    ContactFilter2D groundFilter = new ContactFilter2D
    {
        useTriggers = false,
        useNormalAngle = true,
        minNormalAngle = 45f,
        maxNormalAngle = 135f
    };

    float coyoteTimer, jumpBufferTimer, attackTimer;

    public bool IsGrounded { get; private set; }
    public bool IsAttacking => attackTimer > 0f;
    public bool FacingRight { get; private set; } = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        rb.sharedMaterial = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };

        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        attackAction = InputSystem.actions.FindAction("Attack");
    }

    void Update()
    {
        Vector2 vel = rb.linearVelocity;
        float moveX = moveAction.ReadValue<Vector2>().x;

        // Attack
        attackTimer -= Time.deltaTime;
        if (attackAction != null && attackAction.WasPressedThisFrame() && !IsAttacking)
        {
            attackTimer = attackDuration;
            anim.SetTrigger("Attack");
        }

        // Move + facing
        bool canMove = !(IsAttacking && lockMovementWhileAttacking);
        vel.x = canMove ? moveX * speed : 0f;
        if (canMove && Mathf.Abs(moveX) > 0.01f) FacingRight = moveX > 0f;
        sprite.flipX = !FacingRight;

        // Timers
        IsGrounded = rb.IsTouching(groundFilter);
        coyoteTimer = (IsGrounded && vel.y <= 0.1f) ? coyoteTime : coyoteTimer - Time.deltaTime;
        jumpBufferTimer = jumpAction.WasPressedThisFrame() ? jumpBufferTime : jumpBufferTimer - Time.deltaTime;

        // Jump
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            vel.y = jumpForce;
            jumpBufferTimer = coyoteTimer = 0f;
        }

        // Release early = shorter jump
        if (jumpAction.WasReleasedThisFrame() && vel.y > 0f)
            vel.y *= jumpCutMultiplier;

        rb.linearVelocity = vel;

        // Animator parameters
        anim.SetFloat("xSpeed", Mathf.Abs(vel.x));
        anim.SetFloat("ySpeed", vel.y);
        anim.SetBool("isGrounded", IsGrounded);
    }
}