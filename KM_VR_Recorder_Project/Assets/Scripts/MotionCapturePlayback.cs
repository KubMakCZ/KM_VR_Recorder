using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MotionCapturePlayback : MonoBehaviour
{
    public GameObject characterToPlay;
    private Animator animatorComponent;
    private AnimatorOverrideController overrideController;
    private string animationClipName = "MotionCaptureAnimation";

    void Start()
    {
        animatorComponent = characterToPlay.GetComponent<Animator>();
    }

    void Update()
    {
        bool keyPressedThisFrame = Keyboard.current.eKey.wasPressedThisFrame;

        if (keyPressedThisFrame)
        {
            PlayAnimationClip();
        }
    }

    void PlayAnimationClip()
    {
        AnimationClip animationClip = FindAnimationClip(animationClipName);

        if (animationClip != null)
        {
            Debug.Log("Playing animation");
            characterToPlay.GetComponent<Animator>().Rebind();
            ReplaceBaseAnimationClip(animationClip);
            characterToPlay.GetComponent<Animator>().Play(animationClip.name, 0, 0f);
        }
        else
        {
            Debug.LogError("MotionCapturePlayback: Animation clip not found with the name " + animationClipName);
        }
    }

    void ReplaceBaseAnimationClip(AnimationClip newClip)
    {
        if (overrideController == null)
        {
            overrideController = new AnimatorOverrideController(animatorComponent.runtimeAnimatorController);
        }

        overrideController[animationClipName] = newClip;
        animatorComponent.runtimeAnimatorController = overrideController;
        animatorComponent.Rebind();
        animatorComponent.Update(0);
    }

    AnimationClip FindAnimationClip(string clipName)
    {
#if UNITY_EDITOR
        string assetPath = "Assets/" + clipName + ".anim";
        AnimationClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        return clip;
#else
        return null;
#endif
    }
}
