using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum Controls { mobile, pc }

public class PlayerController : MonoBehaviour
{
    private float maxHPWidth;
    private float maxManaWidth;
    // 🔒 Tutorial input lock
    [HideInInspector] public bool inputLocked;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public string PlayerName = "Kenz";
    public float jumpForce = 8f;

    [Header("Jump Feel")]
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;

    [Header("Jump Settings")]
    public int maxJumps = 2;
    private int jumpCount;

    [Header("Dash")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    private int AirDashCount = 0;
    private bool isDashing;
    private float dashDirection;

    [Header("Wall")]
    public Transform wallCheck;
    public float wallCheckDistance = 0.4f;
    public LayerMask wallLayer;
    public float wallJumpForceX = 8f;
    public float wallJumpDuration = 0.1f; // 🟢 NEW: How long input is locked
    private bool isOnWall;
    private bool isWallJumping; // 🟢 NEW: State to lock movement

    [Header("Ladder")]
    public float climbSpeed = 4f;
    private bool isOnLadder = false;

    private bool isClimbing = false;


    [Header("Ground Check")]
    public LayerMask groundLayer;
    public Transform groundCheck;

    [Header("UI")]
    public RectTransform HPUI;
    public TextMeshProUGUI HPText;
    public RectTransform ManaUI;
    public TextMeshProUGUI ManaText;

    public int health = 100;
    public int maxHealth = 100;
    public int mana = 100;
    public int maxMana = 100;

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool jumpPressed;
    private float moveX;
    private bool isAttacking;

    [Header("Animator")]
    public Animator playeranim;

    [Header("Controls")]
    public Controls controlmode;

    [Header("Effects")]
    public ParticleSystem footsteps;
    private ParticleSystem.EmissionModule footEmissions;
    public ParticleSystem ImpactEffect;
    private bool wasonGround;

    [Header("Attack Hitbox")]
    public GameObject attackHitbox;
    private BoxCollider2D attackCollider;

    [Header("Combat")]
    public float attackCooldown = 0.3f;
    private float lastAttackTime;
    private int comboStep;
    private float comboResetTime = 0.8f;
    private float lastComboTime;
    public float attackColliderDuration = 0.2f;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        footEmissions = footsteps.emission;

        maxHPWidth = HPUI.sizeDelta.x;
        maxManaWidth = ManaUI.sizeDelta.x;

        UpdateHPUI();
        UpdateManaUI();

        if (attackHitbox != null)
        {
            attackCollider = attackHitbox.GetComponent<BoxCollider2D>();
            attackCollider.enabled = false;
        }

        if (controlmode == Controls.mobile)
            UIManager.instance.EnableMobileControls();
    }

    private void Update()
    {


        isGrounded = IsGrounded();
        isOnWall = IsTouchingWall() && !isGrounded;

        if (isGrounded)
        {
            jumpCount = 0;
            isWallJumping = false;
            AirDashCount = 0;
        }

        // INPUT
        if (!isAttacking && !isDashing)
        {
            if (controlmode == Controls.pc)
            {
                moveX = Input.GetAxisRaw("Horizontal");

                if (Input.GetButtonDown("Jump") && !isOnLadder)
                    jumpPressed = true;
            }
        }

        // DASH
        if (!isDashing && !isAttacking &&
            Mathf.Abs(moveX) > 0.1f &&
            Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (!isGrounded)
            {
                if (AirDashCount < 2)
                {
                    AirDashCount++;
                    StartCoroutine(Dash());
                }
            }
            else
            {
                StartCoroutine(Dash());
                // Reset air dash count when dashing from ground
            }

        }

        // ATTACK
        if (!isAttacking && controlmode == Controls.pc && Input.GetButtonDown("Fire1"))
            HandleAttack();

        // ===== LADDER (AUTO ENTER CLIMB, PAUSE WHEN IDLE) =====
        // ===== LADDER + STATE-AWARE ANIMATION CONTROL =====
        if (isOnLadder)
        {
            float climbInput = 0f;

            if (Input.GetKey(KeyCode.W))
                climbInput = 1f;
            else if (Input.GetKey(KeyCode.S))
                climbInput = -1f;

            // Movement
            rb.gravityScale = 0;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, climbInput * climbSpeed);

            // Check if animator is currently in Climb state
            AnimatorStateInfo state = playeranim.GetCurrentAnimatorStateInfo(0);
            bool inClimbState = state.IsName("Climb");

            // Resume animation if W, S, or A is pressed
            bool inputForAnimation =
                Input.GetKey(KeyCode.W) ||
                Input.GetKey(KeyCode.S) ||
                Input.GetKey(KeyCode.A);

            if (inClimbState)
            {
                playeranim.speed = inputForAnimation ? 1f : 0f;
                isClimbing = inputForAnimation;
            }
        }
        else
        {
            rb.gravityScale = 1;
            playeranim.speed = 1f;
            isClimbing = false;
        }
        // =================================================

        // ================================================

        UpdateAnimator();

        if (moveX != 0 && !isAttacking && !isWallJumping && !isOnLadder)
            FlipSprite(moveX);

        if (!wasonGround && isGrounded)
            ImpactEffect.Play();

        wasonGround = isGrounded;
        if (inputLocked) return;
    }




    private void FixedUpdate()
    {


        if (isDashing) return;

        if (isAttacking)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // 🟢 CRITICAL FIX: Only apply movement velocity if NOT Wall Jumping
        // This prevents your input from cancelling the wall kick force
        if (!isWallJumping)
        {
            rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);
        }

        // JUMP + DOUBLE JUMP + WALL JUMP
        if (jumpPressed)
        {
            if (!isOnWall && jumpCount < maxJumps)
            {
                // Normal Jump
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                jumpCount++;
                playeranim.SetTrigger("jump");
                jumpPressed = false;
            }
            else if (isOnWall)
            {
                // Wall Jump
                // 🟢 Set flag to stop movement input for a moment
                StartCoroutine(StopMovementForWallJump());

                rb.linearVelocity = Vector2.zero; // Reset current velocity for clean jump

                // Determine direction AWAY from wall
                float jumpDirection = -transform.localScale.x;

                // Apply Force
                rb.AddForce(
                    new Vector2(jumpDirection * wallJumpForceX, jumpForce),
                    ForceMode2D.Impulse
                );

                jumpCount = 1; // Reset double jump so you can jump again after leaving wall

                // Force flip immediately
                FlipSprite(jumpDirection);

                playeranim.SetTrigger("jump");
                jumpPressed = false;
            }
        }

        // BETTER JUMP PHYSICS
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y *
                                 (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !Input.GetButton("Jump"))
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y *
                                 (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }

        jumpPressed = false;
        if (inputLocked) return;
    }

    // 🟢 NEW COROUTINE: Locks input briefly
    private IEnumerator StopMovementForWallJump()
    {
        isWallJumping = true;
        yield return new WaitForSeconds(wallJumpDuration);
        isWallJumping = false;
    }

    // ... [Rest of your code: Dash, Combat, Animations, etc. remain the same] ...

    private IEnumerator Dash()
    {
        isDashing = true;
        dashDirection = transform.localScale.x;

        playeranim.SetTrigger("dash");
        playeranim.SetBool("isDashing", true);

        float timer = 0f;
        while (timer < dashDuration)
        {
            rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0);
            timer += Time.deltaTime;
            yield return null;
        }

        playeranim.SetBool("isDashing", false);
        isDashing = false;
    }

    private void HandleAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return;

        isAttacking = true;
        StartCoroutine(FreezeMovement(0.5f));

        if (Time.time - lastComboTime > comboResetTime)
            comboStep = 0;

        comboStep = Mathf.Clamp(++comboStep, 1, 3);

        playeranim.SetInteger("comboStep", comboStep);
        playeranim.SetTrigger("attack");

        lastComboTime = Time.time;
        lastAttackTime = Time.time;

        if (attackCollider != null)
        {
            attackCollider.enabled = true;
            StopCoroutine(nameof(DisableAttackCollider));
            StartCoroutine(DisableAttackCollider());
        }
    }

    private IEnumerator FreezeMovement(float duration)
    {
        yield return new WaitForSeconds(duration);
        isAttacking = false;
    }

    private IEnumerator DisableAttackCollider()
    {
        yield return new WaitForSeconds(attackColliderDuration);
        if (attackCollider != null)
            attackCollider.enabled = false;
    }

    private void UpdateAnimator()
    {
        playeranim.SetBool("run", moveX != 0 && isGrounded && !isAttacking);
        playeranim.SetBool("isGrounded", isGrounded);
        playeranim.SetBool("isAttacking", isAttacking);
        playeranim.SetBool("isDashing", isDashing);
        playeranim.SetBool("isOnWall", isOnWall);
        playeranim.SetBool("isClimbing", isOnLadder);

        footEmissions.rateOverTime =
            (moveX != 0 && isGrounded && !isAttacking) ? 35f : 0f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))
            isOnLadder = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))
        {
            isOnLadder = false;
            playeranim.speed = 1f;
        }


    }

    private bool IsGrounded()
    {
        RaycastHit2D hit =
            Physics2D.Raycast(groundCheck.position, Vector2.down, 0.3f, groundLayer);
        return hit.collider != null;
    }

    private bool IsTouchingWall()
    {
        Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        RaycastHit2D hit =
            Physics2D.Raycast(wallCheck.position, dir, wallCheckDistance, wallLayer);
        return hit.collider != null;
    }

    private void FlipSprite(float direction)
    {
        transform.localScale = new Vector3(direction > 0 ? 1 : -1, 1, 1);
    }

    public void TakeDamage(int damage)
    {
        if (health <= 0) return;

        health -= damage;
        UpdateHPUI();
        playeranim.SetTrigger("hurt");

        if (health <= 0)
            StartCoroutine(DieWithAnimation());
    }

    public void UpdateHPUI()
    {
        health = Mathf.Clamp(health, 0, maxHealth);
        HPUI.sizeDelta =
            new Vector2(maxHPWidth * ((float)health / maxHealth), HPUI.sizeDelta.y);
        HPText.text = $"HP {health} / {maxHealth}";
    }

    public void UpdateManaUI()
    {
        mana = Mathf.Clamp(mana, 0, maxMana);
        ManaUI.sizeDelta =
            new Vector2(maxManaWidth * ((float)mana / maxMana), ManaUI.sizeDelta.y);
        ManaText.text = $"Mana {mana} / {maxMana}";
    }

    private IEnumerator DieWithAnimation()
    {
        playeranim.SetTrigger("death");
        yield return new WaitForSeconds(1.5f);
        GameManager.instance.Death(); // Uncomment when GameManager is ready
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "killzone")
        {
            StartCoroutine(DieWithAnimation());
        }
    }
}