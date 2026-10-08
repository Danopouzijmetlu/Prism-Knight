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

    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sprite;
    InputAction moveAction, jumpAction;

    // Ground = any non-trigger contact whose surface faces up
    ContactFilter2D groundFilter = new ContactFilter2D
    {
        useTriggers = false,
        useNormalAngle = true,
        minNormalAngle = 45f,
        maxNormalAngle = 135f
    };

    float coyoteTimer, jumpBufferTimer;

    public bool IsGrounded { get; private set; }
    public bool FacingRight { get; private set; } = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();

        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
    }

    void Update()
    {
        Vector2 vel = rb.linearVelocity;
        float moveX = moveAction.ReadValue<Vector2>().x;

        // Move + facing
        vel.x = moveX * speed;
        if (Mathf.Abs(moveX) > 0.01f) FacingRight = moveX > 0f;
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
        anim.SetFloat("xSpeed", Mathf.Abs(vel.x));
        anim.SetFloat("ySpeed", vel.y);
    }
}