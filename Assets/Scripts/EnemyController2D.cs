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
    public SpriteRenderer spriteRenderer; // Still kept in case you need it
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

        float dFrom = Vector2.Distance(transform.position, patrolFrom.position);
        float dTo = Vector2.Distance(transform.position, patrolTo.position);
        patrolTarget = dFrom < dTo ? patrolFrom : patrolTo;

        rb.bodyType = RigidbodyType2D.Kinematic;
        currentState = State.Patrol;
    }

    void Update()
    {
        if (currentState == State.Dead || currentState == State.Hurt)
            return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // --- State selection with hysteresis ---
        if (currentState == State.Attack)
        {
            if (distanceToPlayer > attackExitDistance)
                currentState = State.Chase;
        }
        else
        {
            if (distanceToPlayer <= attackDistance)
                currentState = State.Attack;
            else if (distanceToPlayer <= chaseDistance)
                currentState = State.Chase;
            else
                currentState = State.Patrol;
        }

        HandleStateLogic();
    }

    private void UpdateEnemyHPUI()
    {
        health = Mathf.Clamp(health, 0, maxHealth);
        if (EnemyHealthUI != null)
        {
            float hpPercent = (float)health / maxHealth;
            EnemyHealthUI.sizeDelta = new Vector2(maxBarWidth * hpPercent, EnemyHealthUI.sizeDelta.y);
        }

        if (EnemyHealthText != null)
            EnemyHealthText.text = health + " / " + maxHealth;
    }

    void FixedUpdate()
    {
        if (currentState == State.Dead || currentState == State.Hurt)
            return;

        float dist = Vector2.Distance(rb.position, player.position);

        if (dist <= minSeparationDistance)
            return;

        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
    }

    void HandleStateLogic()
    {
        movement = Vector2.zero;

        // ✅ Do nothing during Hurt or Dead so animation plays
        if (currentState == State.Dead || currentState == State.Hurt)
            return;

        if (isOnCooldown)
            return;

        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                break;
            case State.Chase:
                Chase();
                break;
            case State.Attack:
                Attack();
                break;
        }

        UpdateFacing(); // Flips the GameObject based on movement
    }

    #region States

    void Patrol()
    {
        animator.SetBool("isMoving", true);
        animator.SetBool("isChasing", false);

        Vector2 dir = patrolTarget.position - transform.position;
        movement = dir.normalized * patrolSpeed;

        if (dir.magnitude < 0.1f && !isOnCooldown)
        {
            patrolTarget = patrolTarget == patrolFrom ? patrolTo : patrolFrom;
            StartCoroutine(MoveCooldown());
        }
    }

    IEnumerator HideEnemyHealthUIContainer()
    {
        yield return new WaitForSeconds(10.0f); // Match hurt animation length
        EnemyHealthUIContainer.SetActive(false);
    }

    void Chase()
    {
        animator.SetBool("isMoving", true);
        animator.SetBool("isChasing", true);
        EnemyHealthUIContainer.SetActive(true);

        Vector2 dir = player.position - transform.position;
        float distance = dir.magnitude;

        if (distance > stopDistance)
            movement = dir.normalized * chaseSpeed;
        else if (!isOnCooldown)
            StartCoroutine(MoveCooldown());
    }

    void Attack()
    {
        movement = Vector2.zero;
        animator.SetBool("isMoving", false);
        EnemyHealthUIContainer.SetActive(true);

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

        yield return new WaitForSeconds(0.3f);
        attackCollider.enabled = false;

        StartCoroutine(MoveCooldown());

        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    #endregion

    #region Cooldown

    IEnumerator MoveCooldown()
    {
        isOnCooldown = true;
        movement = Vector2.zero;

        yield return new WaitForSeconds(moveCooldown);

        isOnCooldown = false;
    }

    #endregion

    #region Health

    public void TakeDamage(int damage)
    {
        if (currentState == State.Dead || currentState == State.Hurt)
            return;

        health -= damage;
        EnemyHealthUIContainer.SetActive(true);
        currentState = State.Hurt;
        UpdateEnemyHPUI();

        // Force Hurt animation to play from start
        animator.Play("Hurt", 0, 0f);
        animator.SetTrigger("hurt");

        if (health <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(HurtRecovery());
        }
    }

    IEnumerator HurtRecovery()
    {
        yield return new WaitForSeconds(0.3f); // Match hurt animation length
        currentState = State.Chase;
    }

    void Die()
    {
        if (currentState == State.Dead) return;

        currentState = State.Dead;
        animator.SetTrigger("die");

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        StartCoroutine(DestroyAfterDelay(1.5f));
    }

    IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    #endregion
}
