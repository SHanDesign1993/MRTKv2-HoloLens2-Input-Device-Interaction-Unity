using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Follower : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField]
    [Range(0.0f, 360.0f)]
    private float windowYawRotation = 20.0f;
    [SerializeField]
    [Range(0.0f, 100.0f)]
    private float windowFollowSpeed = 5.0f;
    private Quaternion windowRotation;

    private void Start()
    {
        windowRotation = Quaternion.AngleAxis(windowYawRotation, Vector3.right);
    }

    private void LateUpdate()
    {
        if (this.gameObject == null)
        {
            return;
        }

        // Update window position.
        Transform cameraTransform = Camera.main ? Camera.main.transform : null;

        if (this.gameObject.activeSelf && cameraTransform != null)
        {
            float windowDistance = Mathf.Max(16.0f / Camera.main.fieldOfView, Camera.main.nearClipPlane + 0.2f);
            Vector3 position = cameraTransform.position + (cameraTransform.forward * windowDistance);
            position -= cameraTransform.up * 0.1f;
            Quaternion rotation = cameraTransform.rotation * windowRotation;

            float t = Time.deltaTime * windowFollowSpeed;
            this.gameObject.transform.position = Vector3.Lerp(this.gameObject.transform.position, position, t);
            this.gameObject.transform.rotation = Quaternion.Slerp(this.gameObject.transform.rotation, rotation, t);
        }
    }
}
