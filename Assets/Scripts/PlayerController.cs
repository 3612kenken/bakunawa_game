using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public enum Controls { mobile, pc }

public class PlayerController : MonoBehaviour
{
    private float maxHPWidth;
    private float maxManaWidth;
    [Header("Movement")]
    public float moveSpeed = 5f;
    public string PlayerName = "Kenz";
    public float jumpForce = 10f;
    public LayerMask groundLayer;
    public Transform groundCheck;
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

    [Header("Animator")]
    public Animator playeranim;

    [Header("Controls")]
    public Controls controlmode;

    private float moveX;
    private bool isAttacking;

    [Header("Effects")]
    public ParticleSystem footsteps;
    private ParticleSystem.EmissionModule footEmissions;
    public ParticleSystem ImpactEffect;
    private bool wasonGround;

    [Header("Attack Hitbox")]
    public GameObject attackHitbox; // assign in inspector
    private BoxCollider2D attackCollider;

    [Header("Combat")]
    public float attackCooldown = 0.3f;
    private float lastAttackTime;
    private int comboStep = 0;
    private float comboResetTime = 0.8f;
    private float lastComboTime;
    [Header("Attack Collider Settings")]
    public float attackColliderDuration = 0.2f;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        footEmissions = footsteps.emission;

        maxHPWidth = HPUI.sizeDelta.x;
        maxManaWidth = ManaUI.sizeDelta.x;


        UpdateHPUI();
        UpdateManaUI();

        // Attack collider setup
        if (attackHitbox != null)
        {
            attackCollider = attackHitbox.GetComponent<BoxCollider2D>();
            attackCollider.enabled = false; // off by default
        }

        if (controlmode == Controls.mobile)
        {
            UIManager.instance.EnableMobileControls();
        }
    }


    private void Update()
    {
        isGrounded = IsGrounded();
        //Debug.Log(isGrounded);
        if (!isAttacking)
        {
            if (controlmode == Controls.pc)
            {
                moveX = Input.GetAxis("Horizontal");

                if (Input.GetButtonDown("Jump"))
                    jumpPressed = true;


            }
        }

        // Attack input
        if (!isAttacking && controlmode == Controls.pc && Input.GetButtonDown("Fire1"))
            HandleAttack();

        UpdateAnimator();

        if (moveX != 0 && !isAttacking)
            FlipSprite(moveX);

        // Landing effect
        if (!wasonGround && isGrounded)
        {
            ImpactEffect.gameObject.SetActive(true);
            ImpactEffect.Stop();
            ImpactEffect.transform.position =
                new Vector2(footsteps.transform.position.x, footsteps.transform.position.y - 0.2f);
            ImpactEffect.Play();
        }

        wasonGround = isGrounded;
    }

    private void FixedUpdate()
    {
        // Horizontal movement blocked if attacking
        if (isAttacking)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        // Normal movement
        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);

        // Jump
        if (jumpPressed && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            playeranim.SetTrigger("jump");
        }

        jumpPressed = false;
    }


    // ==============================
    // COMBAT
    // ==============================
    private void HandleAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return;

        // Only freeze movement if grounded
        if (isGrounded)
        {
            isAttacking = true;
            StartCoroutine(FreezeMovement(0.5f)); // freeze for 1 second
        }

        moveX = 0f;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // Combo step logic
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
            attackCollider.enabled = true; // enable hitbox
            Debug.Log("Attack collider enabled");
            StopCoroutine("DisableAttackCollider");
            StartCoroutine(DisableAttackCollider());
        }
    }
    private IEnumerator FreezeMovement(float duration)
    {
        float timer = 0f;

        // Keep velocity 0 while attacking
        while (timer < duration)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            timer += Time.deltaTime;
            yield return null;
        }

        isAttacking = false;
    }


    // Called via animation events
    // public void StartAttack()
    // {
    //     isAttacking = true;

    //     if (attackCollider != null)
    //     {
    //         attackCollider.enabled = true; // enable hitbox
    //         StopCoroutine("DisableAttackCollider"); // stop previous if still running
    //         StartCoroutine(DisableAttackCollider()); // start countdown to disable
    //     }
    // }
    private IEnumerator DisableAttackCollider()
    {
        yield return new WaitForSeconds(attackColliderDuration);
        if (attackCollider != null)
            attackCollider.enabled = false; // disable after cooldown
    }



    public void EndAttack()
    {
        isAttacking = false;
        //if (attackCollider != null)
        // attackCollider.enabled = false; // disable hitbox
    }

    // ==============================
    // ANIMATION
    // ==============================
    private void UpdateAnimator()
    {
        playeranim.SetBool("run", moveX != 0 && isGrounded && !isAttacking);
        playeranim.SetBool("isGrounded", isGrounded);
        playeranim.SetBool("isAttacking", isAttacking);

        footEmissions.rateOverTime =
            (moveX != 0 && isGrounded && !isAttacking) ? 35f : 0f;
    }

    // ==============================
    // MOBILE INPUT
    // ==============================
    public void MobileMove(float value)
    {
        if (!isAttacking)
            moveX = value;
    }

    public void MobileJump()
    {
        if (!isAttacking && isGrounded)
            jumpPressed = true;
    }

    public void MobileAttack()
    {
        HandleAttack();
    }

    // ==============================
    // HELPER METHODS
    // ==============================
    private bool IsGrounded()
    {
        float rayLength = 0.3f; // slightly longer than before
        Vector2 rayOrigin = new Vector2(groundCheck.position.x, groundCheck.position.y);

        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, rayLength, groundLayer);

        Debug.DrawRay(rayOrigin, Vector2.down * rayLength, hit.collider ? Color.green : Color.red);

        // if (hit.collider)
        //     Debug.Log("Grounded on: " + hit.collider.gameObject.name);


        return hit.collider != null;

    }


    private void FlipSprite(float direction)
    {
        transform.localScale = new Vector3(direction > 0 ? 1 : -1, 1, 1);
    }
    public void TakeDamage(int damage)
    {
        if (health <= 0) return; // already dead, ignore further damage

        health -= damage;
        UpdateHPUI();

        // Play hurt animation
        if (playeranim != null)
            playeranim.SetTrigger("hurt");

        if (health <= 0)
        {
            StartCoroutine(DieWithAnimation());
        }
    }
    public void AddHeal(int amount)
    {
        health += amount;
        UpdateHPUI();
    }

    public void AddMana(int amount)
    {
        mana += amount;
        UpdateManaUI();
    }


    private void UpdateHPUI()
    {
        health = Mathf.Clamp(health, 0, maxHealth);

        float hpPercent = (float)health / maxHealth;
        HPUI.sizeDelta = new Vector2(maxHPWidth * hpPercent, HPUI.sizeDelta.y);

        if (HPText != null)
            HPText.text = "HP " + health + " / " + maxHealth;
    }

    private void UpdateManaUI()
    {
        mana = Mathf.Clamp(mana, 0, maxMana);

        float manaPercent = (float)mana / maxMana;
        ManaUI.sizeDelta = new Vector2(maxManaWidth * manaPercent, ManaUI.sizeDelta.y);

        if (ManaText != null)
            ManaText.text = "Mana " + mana + " / " + maxMana;
    }



    private IEnumerator DieWithAnimation()
    {
        // Disable player input
        // isPaused = true;

        // Play death animation
        if (playeranim != null)
            playeranim.SetTrigger("death");



        // Wait for animation to finish (adjust time to your animation length)
        float deathAnimLength = 1.5f; // seconds, adjust to your clip
        yield return new WaitForSeconds(deathAnimLength);

        // Call GameManager death logic
        GameManager.instance.Death();

        // Optionally, reset player collider (if respawning)
        GetComponent<Collider2D>().enabled = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "killzone")
        {
            GameManager.instance.Death();
        }
    }
}
