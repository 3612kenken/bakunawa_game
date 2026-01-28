using UnityEngine;
using TMPro;

public class Shrine : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI instructionText; // Assign TMP in inspector
    public string instructionMessage = "Press [R] to recover Health and Mana";

    [Header("Recovery Settings")]
    public int recoverHealth = 50;
    public int recoverMana = 50;

    [Header("Effects")]
    public ParticleSystem healEffect;

    private PlayerController player;
    private bool playerInside = false;

    private void Start()
    {
        // if (instructionText != null)
        //     instructionText.gameObject.SetActive(false); // Hide instruction initially
    }

    private void Update()
    {
        if (playerInside && player != null)
        {
            // Wait for player to press R
            if (Input.GetKeyDown(KeyCode.R))
            {
                RecoverPlayer();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                playerInside = true;

                // Show TMP instruction
                if (instructionText != null)
                {
                    instructionText.text = instructionMessage;
                    instructionText.gameObject.SetActive(true);
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            player = null;

            // Hide TMP instruction
            if (instructionText != null)
                instructionText.gameObject.SetActive(false);
        }
    }

    private void RecoverPlayer()
    {
        if (player == null) return;

        // Recover health and mana
        player.health = Mathf.Min(player.maxHealth, player.health + recoverHealth);
        player.mana = Mathf.Min(player.maxMana, player.mana + recoverMana);

        // Update UI
        player.UpdateHPUI();
        player.UpdateManaUI();

        // Restart particle effect
        if (healEffect != null)
        {
            healEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            healEffect.Play();
        }

        // Hide instruction after use
        if (instructionText != null)
            instructionText.gameObject.SetActive(false);

        // Optional: prevent repeated usage immediately
        playerInside = false;
    }

}
