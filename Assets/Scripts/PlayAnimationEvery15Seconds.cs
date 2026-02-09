using UnityEngine;

public class PlayAnimationEvery15Seconds : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        InvokeRepeating(nameof(PlayAngry), 15f, 15f); // wait 15s, then repeat
    }

    void PlayAngry()
    {
        animator.SetTrigger("Angry");
    }
}
