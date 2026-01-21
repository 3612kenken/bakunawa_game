using UnityEngine;

public class EnemyAttackHitbox : MonoBehaviour
{
    [Header("Damage Range")]
    public int minDamage = 5;
    public int maxDamage = 10;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            int damage = Random.Range(minDamage, maxDamage + 1);
            // +1 because int Random.Range is max-exclusive

            Debug.Log($"Player hit by enemy attack for {damage} damage");
            collision.GetComponent<PlayerController>()?.TakeDamage(damage);
        }
    }
}
