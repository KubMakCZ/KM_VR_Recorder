using UnityEngine;

public class Head : MonoBehaviour
{
    [SerializeField] private Transform rootObject, followObject;
    [SerializeField] private Vector3 positionOffset, rotationOffset, headBodyOffset;
    [SerializeField] private float maxRotationSpeed = 150f;
    [SerializeField] private bool invertYRotation = false;

    private Quaternion initialBodyRotation;
    private float bodyRotationY = 0f;

    private void Start()
    {
        initialBodyRotation = rootObject.rotation;
    }

    private void LateUpdate()
    {
        rootObject.position = transform.position + headBodyOffset;

        // Calculate target body rotation based on followObject Y rotation
        float targetBodyRotationY = followObject.rotation.eulerAngles.y;
        if (invertYRotation)
        {
            targetBodyRotationY += 180f;
        }
        if (targetBodyRotationY > 180f)
        {
            targetBodyRotationY -= 360f;
        }
        float deltaRotationY = Mathf.MoveTowardsAngle(bodyRotationY, targetBodyRotationY, maxRotationSpeed * Time.deltaTime);
        bodyRotationY = deltaRotationY;

        Quaternion targetRotation = initialBodyRotation * Quaternion.Euler(0f, bodyRotationY, 0f);

        // check if the difference in rotation is too large
        float angleDiff = Quaternion.Angle(rootObject.rotation, targetRotation);
        if (angleDiff > 90f)
        {
            targetRotation = Quaternion.RotateTowards(rootObject.rotation, targetRotation, 90f);
        }

        rootObject.rotation = Quaternion.RotateTowards(
            rootObject.rotation,
            targetRotation,
            Time.deltaTime * maxRotationSpeed
        );

        transform.position = followObject.TransformPoint(positionOffset);
        transform.rotation = followObject.rotation * Quaternion.Euler(rotationOffset);
    }
}
