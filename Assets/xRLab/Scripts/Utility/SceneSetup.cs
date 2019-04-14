using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneSetup : MonoBehaviour
{
    void Start()
    {
        var mainCamera = CameraCache.Refresh(Camera.main);

        if (mainCamera == null)
        {
            Debug.LogWarning("Could not find a valid \"MainCamera\"!  Unable to update camera position.");
        }
        else
        {
            mainCamera.transform.position = Vector3.zero;
        }
    }
}
