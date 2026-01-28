using UnityEngine;
using TMPro;

public class TutorialsUI : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI instructionText;

    [Header("Player (Optional Input Lock)")]
    public PlayerController player;

    [Header("Tutorial Steps")]
    [TextArea(3, 6)]
    public string[] steps;

    private int currentStep = 0;
    private bool tutorialActive = true;

    void Start()
    {
        if (player != null)
            player.inputLocked = true; // 🔒 Lock player controls

        ShowStep();
    }

    void Update()
    {
        if (!tutorialActive) return;

        if (Input.GetKeyDown(KeyCode.Return))
        {
            NextStep();
        }
    }

    void ShowStep()
    {
        instructionText.text =
            steps[currentStep] +
            "\n\n<color=#FFD700><size=80%>Press ENTER to continue</size></color>";
    }

    void NextStep()
    {
        currentStep++;

        if (currentStep >= steps.Length)
        {
            EndTutorial();
        }
        else
        {
            ShowStep();
        }
    }

    void EndTutorial()
    {
        tutorialActive = false;
        instructionText.gameObject.SetActive(false);

        if (player != null)
            player.inputLocked = false; // 🔓 Unlock controls
    }
}
