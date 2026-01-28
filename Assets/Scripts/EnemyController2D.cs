using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class Enemy2DController : MonoBehaviour
{
    public enum State { Patrol, Chase, Attack, Hurt, Dead }

    [Header("References")]
    public Transform patrolFrom;
    public string EnemyName = "Enemy";
    public Transform patrolTo;
    public Transform player;
    public SpriteRenderer spriteRenderer;
    public GameObject EnemyHealthUIContainer;
    public TextMeshProUGUI EnemyHealthText;
    public RectTransform EnemyHealthUI;

    [Header("Movement")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;
    public float stopDistance = 1.1f;
    public float minSeparationDistance = 0.95f;
    public float moveCooldown = 0.5f;

    [Header("Detection")]
    public float chaseDistance = 6f;
    public float attackDistance = 1.2f;
    public float attackExitDistance = 1.5f;

    [Header("Attack")]
    public Collider2D attackCollider;
    public float attackCooldown = 1.2f;

    [Header("Attack Effects")]
    public ParticleSystem attackParticles;

    [Header("Health")]
    public int health = 100;
    public int maxHealth = 100;

    private Rigidbody2D rb;
    private Animator animator;
    private State currentState;
    private Transform patrolTarget;
    private Vector2 movement;

    private bool canAttack = true;
    private bool isOnCooldown = false;
    private float maxBarWidth;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        maxBarWidth = EnemyHealthUI.sizeDelta.x;
        UpdateEnemyHPUI();

        attackCollider.enabled = false;

        if (attackParticles != null)
            attackParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        float dFrom = Vector2.Distance(transform.position, patrolFrom.position);
        float dTo = Vector2.Distance(transform.position, patrolTo.position);
        patrolTarget = dFrom < dTo ? patrolFrom : patrolTo;

        rb.bodyType = RigidbodyType2D.Kinematic;
        ChangeState(State.Patrol);
    }

    void Update()
    {
        if (currentState == State.Dead)
            return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // --- State transitions ---
        if (currentState == State.Attack)
        {
            if (distanceToPlayer > attackExitDistance)
                ChangeState(State.Chase);
        }
        else if (currentState != State.Hurt)
        {
            if (distanceToPlayer <= attackDistance)
                ChangeState(State.Attack);
            else if (distanceToPlayer <= chaseDistance)
                ChangeState(State.Chase);
            else
                ChangeState(State.Patrol);
        }

        HandleStateLogic();

        // --- Animator updates ---
        bool isActuallyMoving = movement.sqrMagnitude > 0.01f;  // movement magnitude
        bool isChasing = currentState == State.Chase;

        animator.SetBool("isMoving", isActuallyMoving);
        animator.SetBool("isChasing", isChasing);
    }

    void FixedUpdate()
    {
        if (currentState == State.Dead || currentState == State.Hurt)
            return;

        if (Vector2.Distance(rb.position, player.position) <= minSeparationDistance)
            return;

        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
    }

    void HandleStateLogic()
    {
        if (currentState == State.Dead || currentState == State.Hurt)
        {
            movement = Vector2.zero;
            return;
        }

        switch (currentState)
        {
            case State.Patrol: Patrol(); break;
            case State.Chase: Chase(); break;
            case State.Attack: Attack(); break;
        }

        UpdateFacing();
    }

    #region State Logic
    void Patrol()
    {
        if (isOnCooldown) return;

        Vector2 dir = (Vector2)patrolTarget.position - (Vector2)transform.position;
        float distance = dir.magnitude;

        if (distance > 0.05f)
            movement = dir.normalized * patrolSpeed;
        else
        {
            patrolTarget = patrolTarget == patrolFrom ? patrolTo : patrolFrom;
            movement = Vector2.zero;
            StartCoroutine(MoveCooldown());
        }
    }

    void Chase()
    {
        EnemyHealthUIContainer.SetActive(true);

        if (isOnCooldown) return;

        Vector2 dir = (Vector2)player.position - (Vector2)transform.position;
        float distance = dir.magnitude;

        if (distance > stopDistance)
            movement = dir.normalized * chaseSpeed;
        else
        {
            movement = Vector2.zero;
            StartCoroutine(MoveCooldown());
        }
    }

    void Attack()
    {
        movement = Vector2.zero;

        if (canAttack)
            StartCoroutine(AttackRoutine());
    }
    #endregion

    #region Facing
    void UpdateFacing()
    {
        if (Mathf.Abs(movement.x) < 0.01f) return;

        Vector3 scale = transform.localScale;
        scale.x = movement.x > 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        transform.localScale = scale;
    }
    #endregion

    #region Attack
    IEnumerator AttackRoutine()
    {
        canAttack = false;
        animator.SetTrigger("attack");

        yield return new WaitForSeconds(0.2f);

        attackCollider.enabled = true;
        GetComponent<BoxCollider2D>().isTrigger = false;

        if (attackParticles != null)
            attackParticles.Play();

        yield return new WaitForSeconds(0.3f);

        attackCollider.enabled = false;
        GetComponent<BoxCollider2D>().isTrigger = true;

        if (attackParticles != null)
            attackParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        StartCoroutine(MoveCooldown());

        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }
    #endregion

    #region Cooldown
    IEnumerator MoveCooldown()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(moveCooldown);
        isOnCooldown = false;
    }
    #endregion

    #region Health
    public void TakeDamage(int damage)
    {
        if (currentState == State.Dead)
            return;

        health -= damage;
        EnemyHealthUIContainer.SetActive(true);
        UpdateEnemyHPUI();

        if (health <= 0)
        {
            Die();
        }
        else
        {
            // Play Hurt animation
            StartCoroutine(HurtRoutine());
        }
    }

    IEnumerator HurtRoutine()
    {
        // Set state to Hurt
        currentState = State.Hurt;

        // Stop movement
        movement = Vector2.zero;

        // Trigger Hurt animation
        animator.SetTrigger("hurt");

        // Wait for animation to finish
        // Grab actual animation length dynamically for flexibility
        float hurtLength = GetAnimationLength("Hurt");
        yield return new WaitForSeconds(hurtLength);

        // Return to Chase only if not dead
        if (currentState != State.Dead)
            ChangeState(State.Chase);
    }

    void Die()
    {
        if (currentState == State.Dead) return;

        currentState = State.Dead;

        // Stop movement immediately
        movement = Vector2.zero;

        // Disable colliders so player can't interact with dead enemy
        foreach (var col in GetComponents<Collider2D>())
            col.enabled = false;

        // Stop Rigidbody movement
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Set the speed for the Die animation
        float dieSpeed = 0.5f; // 0.5 = half speed
        animator.SetFloat("DieSpeed", dieSpeed); // optional if using a speed parameter
        animator.speed = 1f; // make sure other animations are normal; speed param handles Die

        // Trigger Die animation
        animator.SetTrigger("die");

        // Calculate actual duration of Die animation with slow speed
        float dieLength = GetAnimationLength("Die") / dieSpeed;

        // Destroy after animation finishes, add small buffer
        StartCoroutine(DestroyAfterDelay(dieLength + 0.2f));
    }

    // Helper function to get the length of an animation clip
    float GetAnimationLength(string clipName)
    {
        RuntimeAnimatorController ac = animator.runtimeAnimatorController;
        foreach (var clip in ac.animationClips)
        {
            if (clip.name == clipName)
                return clip.length;
        }
        return 1f; // fallback if clip not found
    }

    IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    #endregion

    #region UI
    void UpdateEnemyHPUI()
    {
        health = Mathf.Clamp(health, 0, maxHealth);

        if (EnemyHealthUI != null)
        {
            float hpPercent = (float)health / maxHealth;
            EnemyHealthUI.sizeDelta = new Vector2(maxBarWidth * hpPercent, EnemyHealthUI.sizeDelta.y);
        }

        if (EnemyHealthText != null)
            EnemyHealthText.text = $"{health} / {maxHealth}";
    }
    #endregion

    #region State
    void ChangeState(State newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        switch (newState)
        {
            case State.Patrol:
            case State.Chase:
                break;

            case State.Attack:
                animator.SetTrigger("attack");
                break;

            case State.Hurt:
                animator.SetTrigger("hurt");
                break;

            case State.Dead:
                animator.SetTrigger("die");
                break;
        }
    }
    #endregion
}
