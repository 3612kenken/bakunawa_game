using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 5;
    public float hitStunTime = 0.15f;
    public float invincibleTime = 0.3f;

    private int currentHealth;
    private bool isInvincible;
    private Rigidbody2D rb;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible) return;

        currentHealth -= damage;
        StartCoroutine(HitStun());
        StartCoroutine(Invincibility());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    IEnumerator HitStun()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(hitStunTime);
    }

    IEnumerator Invincibility()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibleTime);
        isInvincible = false;
    }

    void Die()
    {
        Destroy(gameObject);
    }
}
