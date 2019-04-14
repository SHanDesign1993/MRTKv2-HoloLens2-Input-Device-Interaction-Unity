using UnityEngine;

public class TrackingIndicator : MonoBehaviour
{
    [Header("--- Setting ---")]
    [SerializeField]
    bool autoRecenterTarget = false;
    [SerializeField]
    float autoRecenterTime = 1.5f;
    float targetingTimer = 0;

    [SerializeField]
    float rotatingSpeed = 6f;
    [SerializeField]
    float distanceToCam = 2.0f;

    [Header("--- Status ---")]
    [SerializeField]
    bool IsPointing = false;
    Transform Target;

    public void PointToTarget(Transform target)
    {
        if (target == null) return;
        Target = target;
        IsPointing = true;
        SetVisibility(IsPointing);
    }

    public void ResetDefault()
    {
        transform.rotation = Quaternion.identity;
        IsPointing = false;
        SetVisibility(IsPointing);
    }

    void SetVisibility(bool visible)
    {
        gameObject.SetActive(visible);
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsPointing || Target == null) { return; }

        if (autoRecenterTarget)
        {
            targetingTimer += Time.deltaTime;
            if (targetingTimer >= autoRecenterTime)
            {
                targetingTimer = 0;
                TrackingManager.Instance.RecenterDevice();
            }
        }

        Vector3 newPos = CameraCache.Main.transform.position + CameraCache.Main.transform.forward * distanceToCam;
        transform.position = newPos;

        var lookPos = Target.position - transform.position;
        lookPos.z = 0;
        var rotation = Quaternion.LookRotation(lookPos);

        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * rotatingSpeed);
    }
}
