using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum Controls { mobile, pc }

public class PlayerController : MonoBehaviour
{
    private float maxHPWidth;
    private float maxManaWidth;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public string PlayerName = "Kenz";
    public float jumpForce = 10f;

    [Header("Dash")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    private bool isDashing;

    [Header("Wall")]
    public Transform wallCheck;
    public float wallCheckDistance = 0.4f;
    public LayerMask wallLayer;
    private bool isOnWall;

    [Header("Ladder")]
    public float climbSpeed = 4f;
    private bool isOnLadder;

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
        isOnWall = IsTouchingWall() && !isGrounded && rb.linearVelocity.y <= 0;

        if (!isAttacking && !isDashing)
        {
            if (controlmode == Controls.pc)
            {
                moveX = Input.GetAxis("Horizontal");

                if (Input.GetButtonDown("Jump"))
                    jumpPressed = true;
            }
        }

        // Dash input
        if (!isDashing && !isAttacking && Input.GetKeyDown(KeyCode.LeftShift))
            StartCoroutine(Dash());

        // Attack input
        if (!isAttacking && controlmode == Controls.pc && Input.GetButtonDown("Fire1"))
            HandleAttack();

        // Ladder climbing
        if (isOnLadder && Input.GetKey(KeyCode.W))
        {
            rb.gravityScale = 0;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, climbSpeed);
        }
        else if (!isOnLadder)
        {
            rb.gravityScale = 1;
        }

        UpdateAnimator();

        if (moveX != 0 && !isAttacking)
            FlipSprite(moveX);

        // Landing effect
        if (!wasonGround && isGrounded)
        {
            ImpactEffect.Play();
        }

        wasonGround = isGrounded;
    }

    private void FixedUpdate()
    {
        if (isDashing) return;

        if (isAttacking)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);

        if (jumpPressed)
        {
            if (isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                playeranim.SetTrigger("jump");
            }
            else if (isOnWall)
            {
                rb.linearVelocity = Vector2.zero;
                rb.AddForce(new Vector2(-transform.localScale.x * 8f, jumpForce),
                    ForceMode2D.Impulse);
                FlipSprite(-transform.localScale.x);
                playeranim.SetTrigger("jump");
            }
        }

        jumpPressed = false;
    }

    // =========================
    // DASH
    // =========================
    private IEnumerator Dash()
    {
        isDashing = true;
        playeranim.SetTrigger("dash");
        playeranim.SetBool("isDashing", true);

        float dir = transform.localScale.x;
        float timer = 0f;

        while (timer < dashDuration)
        {
            rb.linearVelocity = new Vector2(dir * dashSpeed, 0);
            timer += Time.deltaTime;
            yield return null;
        }

        playeranim.SetBool("isDashing", false);
        isDashing = false;
    }

    // =========================
    // COMBAT
    // =========================
    private void HandleAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return;

        isAttacking = true;
        StartCoroutine(FreezeMovement(0.5f));

        if (Time.time - lastComboTime > comboResetTime)
            comboStep = 0;

        comboStep++;
        comboStep = Mathf.Clamp(comboStep, 1, 3);

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

    // =========================
    // ANIMATION
    // =========================
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

    // =========================
    // COLLISIONS
    // =========================
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))
            isOnLadder = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))
            isOnLadder = false;
    }

    // =========================
    // HELPERS
    // =========================
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

    // =========================
    // UI / DAMAGE
    // =========================
    public void TakeDamage(int damage)
    {
        if (health <= 0) return;

        health -= damage;
        UpdateHPUI();
        playeranim.SetTrigger("hurt");

        if (health <= 0)
            StartCoroutine(DieWithAnimation());
    }

    private void UpdateHPUI()
    {
        health = Mathf.Clamp(health, 0, maxHealth);
        HPUI.sizeDelta =
            new Vector2(maxHPWidth * ((float)health / maxHealth), HPUI.sizeDelta.y);
        HPText.text = $"HP {health} / {maxHealth}";
    }

    private void UpdateManaUI()
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
        GameManager.instance.Death();
    }
}
