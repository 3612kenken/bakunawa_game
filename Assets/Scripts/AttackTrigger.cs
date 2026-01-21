using UnityEngine;

public class AttackTrigger : MonoBehaviour
{
    [Header("Damage Range")]
    public int minDamage = 5;
    public int maxDamage = 15;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only damage enemies
        if (other.CompareTag("Enemy"))
        {
            int damage = Random.Range(minDamage, maxDamage + 1);
            // int Random.Range is max-exclusive

            Debug.Log($"Enemy hit by attack trigger for {damage} damage");

            Enemy2DController enemy = other.GetComponent<Enemy2DController>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
    }
}
