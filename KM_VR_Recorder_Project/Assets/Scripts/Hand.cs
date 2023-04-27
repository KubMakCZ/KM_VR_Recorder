using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hand : MonoBehaviour
{
    [SerializeField] private GameObject followObject;
    [SerializeField] private float followSpeed = 30f;
    [SerializeField] private float rotateSpeed = 100f;
    [SerializeField] private Vector3 positionOffset;
    [SerializeField] private Vector3 rotationOffset;
    private Transform _followTarget;

    private void Start()
    {
        _followTarget = followObject.transform;
    }

    private void Update()
    {
        MoveToTarget();
    }

    private void MoveToTarget()
    {
        var positionWithOffset = _followTarget.TransformPoint(positionOffset);
        var distance = Vector3.Distance(positionWithOffset, transform.position);
        transform.position = Vector3.Lerp(transform.position, positionWithOffset, Time.deltaTime * followSpeed * distance);

        var rotationWithOffset = _followTarget.rotation * Quaternion.Euler(rotationOffset);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotationWithOffset, Time.deltaTime * rotateSpeed);
    }
}
