#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using System.Threading.Tasks;
using TMPro;
using System;
using Assimp;

public class MotionCaptureRecorder : MonoBehaviour
{
    public GameObject characterToRecord;
    public GameObject characterToPlay;
    public string fileName = "MotionCaptureAnimation";
    private bool isRecording = false;
    private ActionBasedController controller;
    private AnimationClip animationClip;

    public XRController xrController;
    public InputAction menuAction; // Pøidejte tuto novou øádku

    private Dictionary<string, Dictionary<string, AnimationCurve>> curves = new Dictionary<string, Dictionary<string, AnimationCurve>>();
    private Dictionary<string, AnimationCurve> rootCurves = new Dictionary<string, AnimationCurve>();
    private float recordingStartTime;

    private bool buttonPressedLastFrame = false;

    private Animator animatorComponent;
    private AnimatorOverrideController overrideController;
    private string baseAnimationClipName = "MotionCaptureAnimation";

    private Vector3 originalWorldPosition;
    private UnityEngine.Quaternion originalWorldRotation;

    public AudioClip startAppSound;
    public AudioClip startRecordingSound;
    public AudioClip stopRecordingSound;
    public AudioClip saveAnimationSound;
    public AudioClip startPlayingAnimationSound;
    public AudioSource audioSource;

    public TextMeshProUGUI processingText;

    void Start()
    {
        processingText.gameObject.SetActive(false);
        controller = GetComponent<ActionBasedController>();
        if (controller == null)
        {
            Debug.LogError("MotionCaptureRecorder: ActionBasedController not found in the scene.");
        }
        InitRootCurves();
        animatorComponent = characterToPlay.GetComponent<Animator>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        PlaySound(startAppSound); // Pøehrajte zvuk pøi zapnutí aplikace

    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    void Update()
    {
        bool buttonPressedThisFrame = menuAction.ReadValue<float>() > 0.5f;
        bool keyPressedThisFrame = Keyboard.current.rKey.wasPressedThisFrame;

        if (buttonPressedThisFrame && !buttonPressedLastFrame || keyPressedThisFrame)
        {
            if (!isRecording)
            {
                ResetAnimation();
                StartRecording();
            }
            else
            {
                StopRecording();
            }
        }

        buttonPressedLastFrame = buttonPressedThisFrame;

        if (isRecording)
        {
            RecordMotion();
        }
    }

    void StartRecording()
    {
        if (animationClip != null)
        {
            ClearAnimationClip();
        }

        recordingStartTime = Time.time;
        isRecording = true;
        animationClip = new AnimationClip
        {
            name = "MotionCaptureAnimation"
        };
        Debug.Log("Recording started");
        PlaySound(startRecordingSound); // Pøehrajte zvuk pøi zahájení nahrávání
    }

    void StopRecording()
    {
        Debug.Log("Recording stopped");
        PlaySound(stopRecordingSound); // Pøehrajte zvuk pøi ukonèení nahrávání
        isRecording = false;
        SaveAnimationClip();
        PlayAnimationClip();

    }

    void ClearAnimationClip()
    {
        if (animationClip != null)
        {
            curves.Clear();
            InitRootCurves();
            animationClip.ClearCurves();
        }
    }

    void RecordMotion()
    {
        Transform[] bones = characterToRecord.GetComponentsInChildren<Transform>();

        foreach (Transform bone in bones)
        {
            if (!ShouldRecord(bone)) continue;

            if (bone != characterToRecord.transform)
            {
                if (!curves.ContainsKey(bone.name))
                {
                    curves[bone.name] = new Dictionary<string, AnimationCurve>();
                    curves[bone.name]["localPosition.x"] = new AnimationCurve();
                    curves[bone.name]["localPosition.y"] = new AnimationCurve();
                    curves[bone.name]["localPosition.z"] = new AnimationCurve();
                    curves[bone.name]["localRotation.x"] = new AnimationCurve();
                    curves[bone.name]["localRotation.y"] = new AnimationCurve();
                    curves[bone.name]["localRotation.z"] = new AnimationCurve();
                    curves[bone.name]["localRotation.w"] = new AnimationCurve();
                }

                Vector3 position = bone.localPosition;
                UnityEngine.Quaternion rotation = bone.localRotation;
                float currentTime = Time.time - recordingStartTime;

                curves[bone.name]["localPosition.x"].AddKey(currentTime, position.x);
                curves[bone.name]["localPosition.y"].AddKey(currentTime, position.y);
                curves[bone.name]["localPosition.z"].AddKey(currentTime, position.z);
                curves[bone.name]["localRotation.x"].AddKey(currentTime, rotation.x);
                curves[bone.name]["localRotation.y"].AddKey(currentTime, rotation.y);
                curves[bone.name]["localRotation.z"].AddKey(currentTime, rotation.z);
                curves[bone.name]["localRotation.w"].AddKey(currentTime, rotation.w);
            }
            else if (bone == characterToRecord.transform)
            {
                Vector3 rootPosition = bone.position;
                UnityEngine.Quaternion rootRotation = bone.rotation;
                float currentTime = Time.time - recordingStartTime;

                rootCurves["position.x"].AddKey(currentTime, rootPosition.x);
                rootCurves["position.y"].AddKey(currentTime, rootPosition.y);
                rootCurves["position.z"].AddKey(currentTime, rootPosition.z);

                rootCurves["rotation.x"].AddKey(currentTime, rootRotation.x);
                rootCurves["rotation.y"].AddKey(currentTime, rootRotation.y);
                rootCurves["rotation.z"].AddKey(currentTime, rootRotation.z);
                rootCurves["rotation.w"].AddKey(currentTime, rootRotation.w);
            }
        }
    }

    void InitRootCurves()
    {
        rootCurves["position.x"] = new AnimationCurve();
        rootCurves["position.y"] = new AnimationCurve();
        rootCurves["position.z"] = new AnimationCurve();
        rootCurves["rotation.x"] = new AnimationCurve();
        rootCurves["rotation.y"] = new AnimationCurve();
        rootCurves["rotation.z"] = new AnimationCurve();
        rootCurves["rotation.w"] = new AnimationCurve();
    }

    private bool ShouldRecord(Transform bone)
    {
        Transform parent = bone.parent;
        while (parent != null)
        {
            if (parent.name.StartsWith("IK_") || parent.name.StartsWith("Rig") || parent.name == "collider")
            {
                return false;
            }
            parent = parent.parent;
        }
        return true;
    }

    void PlayAnimationClip()
    {
        if (animationClip != null)
        {
            Debug.Log("Play");
            originalWorldPosition = characterToPlay.transform.position;
            originalWorldRotation = characterToPlay.transform.rotation;
            characterToPlay.GetComponent<Animator>().Rebind();
            ReplaceBaseAnimationClip(animationClip);
            characterToPlay.GetComponent<Animator>().applyRootMotion = true;
            characterToPlay.GetComponent<Animator>().Play(baseAnimationClipName, 0, 0f);
            StartCoroutine(ResetPositionAfterAnimation());
            PlaySound(startPlayingAnimationSound); // Pøehrajte zvuk pøi zaèátku pøehrávání animace
        }
    }

    IEnumerator ResetPositionAfterAnimation()
    {
        Animator animator = characterToPlay.GetComponent<Animator>();
        while (true)
        {
            yield
            return new WaitForSeconds(0.1f); // Poèkejte na zaèátek animace
            float animationLength = animator.GetCurrentAnimatorStateInfo(0).length;
            yield
            return new WaitForSeconds(animationLength);

            float transitionDuration = 1.0f; // Doba trvání pøechodu v sekundách
            float elapsedTime = 0f;

            Vector3 startPosition = characterToPlay.transform.position;
            UnityEngine.Quaternion startRotation = characterToPlay.transform.rotation;

            while (elapsedTime < transitionDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / transitionDuration;

                characterToPlay.transform.position = Vector3.Lerp(startPosition, originalWorldPosition, t);
                characterToPlay.transform.rotation = UnityEngine.Quaternion.Lerp(startRotation, originalWorldRotation, t);

                yield
                return null;
            }

            characterToPlay.transform.position = originalWorldPosition;
            characterToPlay.transform.rotation = originalWorldRotation;
            animator.Play(baseAnimationClipName, 0, 0f); // Pøehrajte "Idle" animaci nebo jakoukoli jinou základní animaci po skonèení nahrávky
            PlaySound(startPlayingAnimationSound); // Pøehrajte zvuk pøi zaèátku pøehrávání animace
        }
    }

    void ReplaceBaseAnimationClip(AnimationClip newClip)
    {
        if (overrideController == null)
        {
            overrideController = new AnimatorOverrideController(animatorComponent.runtimeAnimatorController);
        }

        overrideController[baseAnimationClipName] = newClip;
        animatorComponent.runtimeAnimatorController = overrideController;
        animatorComponent.Rebind();
        animatorComponent.Update(0);
    }

    void ResetAnimation()
    {
        if (animationClip != null)
        {
            foreach (Dictionary<string, AnimationCurve> boneCurves in curves.Values)
            {
                foreach (AnimationCurve curve in boneCurves.Values)
                {
                    curve.keys = new Keyframe[0];
                }
            }

            curves.Clear();
        }
    }




    public async void SaveAnimationClip()
    {
        processingText.gameObject.SetActive(true);
        await SaveAnimation(() => {
            processingText.gameObject.SetActive(false);
            PlaySound(saveAnimationSound); // Pøehrajte zvuk pøi zaèátku pøehrávání animace
        });
    }

    public async Task SaveAnimation(Action onComplete)
    {
        await UnityMainThreadDispatcher.Instance.EnqueueAsync(async () => {
            foreach (KeyValuePair<string, Dictionary<string, AnimationCurve>> boneCurves in curves)
            {
                string boneName = boneCurves.Key;
                if (!ShouldRecord(FindDeepChild(characterToRecord.transform, boneName))) continue;

                string boneHierarchyPath = GetBonePath(characterToRecord.transform, boneName);

                foreach (KeyValuePair<string, AnimationCurve> curve in boneCurves.Value)
                {
                    string curveName = curve.Key;
                    AnimationCurve animationCurve = curve.Value;

                    animationClip.SetCurve(boneHierarchyPath, typeof(Transform), curveName, animationCurve);
                }
            }

            AnimationCurve posXCurve = new AnimationCurve(rootCurves["position.x"].keys);
            AnimationCurve posYCurve = new AnimationCurve(rootCurves["position.y"].keys);
            AnimationCurve posZCurve = new AnimationCurve(rootCurves["position.z"].keys);

            AnimationCurve rotXCurve = new AnimationCurve(rootCurves["rotation.x"].keys);
            AnimationCurve rotYCurve = new AnimationCurve(rootCurves["rotation.y"].keys);
            AnimationCurve rotZCurve = new AnimationCurve(rootCurves["rotation.z"].keys);
            AnimationCurve rotWCurve = new AnimationCurve(rootCurves["rotation.w"].keys);

            animationClip.SetCurve("", typeof(Animator), "RootT.x", posXCurve);
            animationClip.SetCurve("", typeof(Animator), "RootT.y", posYCurve);
            animationClip.SetCurve("", typeof(Animator), "RootT.z", posZCurve);

            animationClip.SetCurve("", typeof(Animator), "RootQ.x", rotXCurve);
            animationClip.SetCurve("", typeof(Animator), "RootQ.y", rotYCurve);
            animationClip.SetCurve("", typeof(Animator), "RootQ.z", rotZCurve);
            animationClip.SetCurve("", typeof(Animator), "RootQ.w", rotWCurve);

#if UNITY_EDITOR
        string path = "Assets/" + fileName + ".anim";
        UnityEditor.AssetDatabase.CreateAsset(animationClip, path);
        UnityEditor.AssetDatabase.Refresh();
        Debug.Log("Animation saved to: " + path);
#else
            string exportPath = "C:/ExportedModel/"; // Cesta, kam chcete exportovat FBX soubor
            string modelName = "YourModelName"; // Jméno modelu a animace, které chcete exportovat

            if (!Directory.Exists(exportPath))
            {
                Directory.CreateDirectory(exportPath);
            }

            FBXExporter exporter = new FBXExporter();
            exporter.SaveMeshes(Path.Combine(exportPath, modelName + ".fbx"), modelName, characterToRecord.transform, true, true);
            Debug.Log("FBX model and animation saved to: " + exportPath);
#endif

            if (overrideController == null)
            {
                overrideController = new AnimatorOverrideController(animatorComponent.runtimeAnimatorController);
            }

            AnimationClip newAnimationClip = new AnimationClip();
            UnityEditor.EditorUtility.CopySerialized(animationClip, newAnimationClip);
            newAnimationClip.name = animationClip.name;

            overrideController[baseAnimationClipName] = newAnimationClip;

            animatorComponent.runtimeAnimatorController = overrideController;
            animatorComponent.Rebind();
            animatorComponent.Update(0);

            PlayAnimationClip();

            onComplete?.Invoke();
        });
    }

    void UpdateAnimatorOverrideController()
    {
        if (overrideController == null)
        {
            overrideController = new AnimatorOverrideController(animatorComponent.runtimeAnimatorController);
        }
        overrideController[baseAnimationClipName] = animationClip;
    }

    private Transform FindDeepChild(Transform parent, string childName)
    {
        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == childName)
            {
                return child;
            }
        }
        return null;
    }

    string GetBonePath(Transform root, string boneName)
    {
        Transform[] bones = root.GetComponentsInChildren<Transform>();
        Transform boneTransform = null;

        foreach (Transform bone in bones)
        {
            if (bone.name == boneName)
            {
                boneTransform = bone;
                break;
            }
        }

        if (boneTransform == null)
        {
            Debug.LogError("MotionCaptureRecorder: Cannot find bone in the hierarchy: " + boneName);
            return "";
        }

        string path = boneTransform.name;
        Transform parent = boneTransform.parent;

        while (parent != null && parent != root)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

}