using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController), typeof(Animator))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private Vector2 attackSize = new Vector2(1.4f, 0.8f); // width, height (horizontal attack)
    [SerializeField] private float attackOffset = 0.9f;
    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField] private LayerMask attackableLayers = ~0;

    [Header("Attack Visual")]
    [SerializeField] private bool showAttackBox = true;
    [SerializeField] private Color attackBoxColor = new Color(1f, 0f, 0f, 0.5f);
    [SerializeField] private float attackBoxDuration = 0.1f;

    Animator anim;
    PlayerController controller;
    InputAction moveAction, attackAction;

    GameObject attackBox;
    float nextAttackTime, hideBoxTime;
    readonly HashSet<IDamageable> hitThisAttack = new HashSet<IDamageable>();

    void Awake()
    {
        anim = GetComponent<Animator>();
        controller = GetComponent<PlayerController>();

        moveAction = InputSystem.actions.FindAction("Move");
        attackAction = InputSystem.actions.FindAction("Attack");

        CreateAttackBox();
    }

    void OnDestroy()
    {
        if (attackBox) Destroy(attackBox);
    }

    void Update()
    {
        if (attackAction.WasPressedThisFrame() && Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;
            Attack(moveAction.ReadValue<Vector2>());
        }

        if (attackBox.activeSelf && Time.time >= hideBoxTime)
            attackBox.SetActive(false);
    }

    // Up = up, Down = down (air only), left/right, nothing held = facing direction
    Vector2 GetAttackDirection(Vector2 move)
    {
        if (move.y > 0.5f) return Vector2.up;
        if (move.y < -0.5f && !controller.IsGrounded) return Vector2.down;
        if (move.x > 0.01f) return Vector2.right;
        if (move.x < -0.01f) return Vector2.left;
        return controller.FacingRight ? Vector2.right : Vector2.left;
    }

    // Horizontal attacks use (width, height); vertical attacks swap them
    Vector2 GetBoxSize(Vector2 dir) => dir.x != 0f ? attackSize : new Vector2(attackSize.y, attackSize.x);

    void Attack(Vector2 move)
    {
        Vector2 dir = GetAttackDirection(move);
        Vector2 size = GetBoxSize(dir);
        Vector2 center = (Vector2)transform.position + dir * attackOffset;

        anim.SetTrigger("attack");
        ShowAttackBox(center, size);

        // Each damageable is hit once per swing, even with multiple colliders
        hitThisAttack.Clear();
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, size, 0f, attackableLayers))
        {
            if (hit.transform.IsChildOf(transform)) continue; // never hit yourself

            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target != null && hitThisAttack.Add(target))
                target.TakeDamage(attackDamage);
        }
    }

    // Flat colored box built at runtime from Unity's built-in white texture
    void CreateAttackBox()
    {
        attackBox = new GameObject("AttackBox");
        var sr = attackBox.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        sr.color = attackBoxColor;
        sr.sortingOrder = 100;
        attackBox.SetActive(false);
    }

    void ShowAttackBox(Vector2 center, Vector2 size)
    {
        if (!showAttackBox) return;

        attackBox.transform.position = center;
        attackBox.transform.localScale = size;
        attackBox.GetComponent<SpriteRenderer>().color = attackBoxColor;
        attackBox.SetActive(true);
        hideBoxTime = Time.time + attackBoxDuration;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        foreach (Vector2 d in new[] { Vector2.right, Vector2.left, Vector2.up, Vector2.down })
            Gizmos.DrawWireCube((Vector2)transform.position + d * attackOffset, GetBoxSize(d));
    }
}