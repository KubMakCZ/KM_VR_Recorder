using UnityEngine;

public class RootMotionApplier : MonoBehaviour
{
    public GameObject characterToPlay;
    private Animator animator;
    private Vector3 lastRootPosition;
    private Quaternion lastRootRotation;

    void Start()
    {
        animator = characterToPlay.GetComponent<Animator>();
        lastRootPosition = animator.rootPosition;
        lastRootRotation = animator.rootRotation;
    }

    void LateUpdate()
    {
        Vector3 rootPositionDelta = animator.rootPosition - lastRootPosition;
        Quaternion rootRotationDelta = animator.rootRotation * Quaternion.Inverse(lastRootRotation);

        transform.position += rootPositionDelta;
        transform.rotation *= rootRotationDelta;

        lastRootPosition = animator.rootPosition;
        lastRootRotation = animator.rootRotation;
    }
}
