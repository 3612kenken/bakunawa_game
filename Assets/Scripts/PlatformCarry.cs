using UnityEngine;

public class PlatformCarry : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 lastPosition;
    private Vector2 deltaMovement;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        lastPosition = rb.position;
    }

    void FixedUpdate()
    {
        // How much the platform moved this frame
        deltaMovement = rb.position - lastPosition;
        lastPosition = rb.position;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        // Check player is standing on TOP
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y < -0.5f)
            {
                Rigidbody2D playerRb = collision.rigidbody;
                playerRb.position += deltaMovement;
                break;
            }
        }
    }
}
