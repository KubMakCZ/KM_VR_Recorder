using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AnimationPlayer : MonoBehaviour
{
    public GameObject characterToPlay;
    public string animationStateName = "MotionCaptureAnimation";
    private bool animationTriggered = false;
    private Animator animatorComponent;

    void Start()
    {
        animatorComponent = characterToPlay.GetComponent<Animator>();
        if (animatorComponent == null)
        {
            Debug.LogError("AnimationPlayer: Animator component not found in characterToPlay.");
        }
    }

    void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            animationTriggered = !animationTriggered;
            if (animationTriggered)
            {
                PlayAnimation();
            }
            else
            {
                StopAnimation();
            }
        }
    }

    void PlayAnimation()
    {
        if (animatorComponent != null)
        {
            animatorComponent.enabled = true;
            animatorComponent.Play(animationStateName, 0, 0f);
        }
    }

    void StopAnimation()
    {
        if (animatorComponent != null)
        {
            animatorComponent.enabled = false;
        }
    }
}
