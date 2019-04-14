using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Physics;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.WSA;

public class SpatialPlacement : MonoBehaviour, IMixedRealityInputHandler
{
    [SerializeField]
    GameObject host;
    [SerializeField]
    GameObject model;
    [SerializeField]
    bool observerInited = false;
    [SerializeField]
    bool IsBeingPlaced = false;
    float offeset = .25f;

    async void Start()
    {
        if(host==null)
            host = this.gameObject;

        //wait MRTK register extended service
        await new WaitUntil(() => MixedRealityToolkit.Instance.RegisteredMixedRealityServices != null);
        foreach (var service in MixedRealityToolkit.Instance.RegisteredMixedRealityServices)
        {
            //get mouse service by name
            if (service.Item2.Name == "Windows Mixed Reality Spatial Mesh Observer")
            {
                observerInited = true;
            }
        }
    }

    void AttachWorldAnchor()
    {
        if (WorldAnchorManager.Instance != null && host.GetComponent<WorldAnchor>() == null)
        {
            var anchor= host.AddComponent<WorldAnchor>();
            // Add world anchor when object placement is done.
            WorldAnchorManager.Instance.SaveSceneObject(host.gameObject.name, anchor);
        }
    }

    void RemoveWorldAnchor()
    {
        if (WorldAnchorManager.Instance != null && host.GetComponent<WorldAnchor>()!=null)
        {
            //Removes existing world anchor if any exist.
            WorldAnchorManager.Instance.RemoveSceneObject(host.gameObject.name);
            var anchor = host.GetComponent<WorldAnchor>();
            Destroy(anchor);
        }
    }

    void HandlePlacement()
    {
        if (MixedRealityToolkit.SpatialAwarenessSystem == null || !observerInited) return;

        if (!IsBeingPlaced)
        {
            AttachWorldAnchor();
            //disable mesh and suspend observer
            MixedRealityToolkit.SpatialAwarenessSystem.SpatialAwarenessObjectParent.SetActive(false);
            MixedRealityToolkit.SpatialAwarenessSystem.SuspendObservers();
        }
        else
        {
            model.transform.localPosition = Vector3.zero;
            RemoveWorldAnchor();
            //enable mesh and resume observer
            MixedRealityToolkit.SpatialAwarenessSystem.SpatialAwarenessObjectParent.SetActive(true);
            MixedRealityToolkit.SpatialAwarenessSystem.ResumeObservers();
        }
    }

    public void TogglePlacement()
    {
        IsBeingPlaced = !IsBeingPlaced;
        HandlePlacement();
        //this.GetComponent<Collider>().enabled = !IsBeingPlaced;
        //this.GetComponent<Interactable>().enabled = !IsBeingPlaced;
        Debug.Log("Is Being Placed: "+ IsBeingPlaced);
    }

    void Update()
    {
        if (!IsBeingPlaced) { return; }

        
        // update the placement to match the mouse pos.
        host.transform.position = GetHostPlacementPos();
        model.transform.position = GetModelPlacementPos();

        // Rotate this object to face the user.
        host.transform.rotation = Quaternion.Euler(0, CameraCache.Main.transform.localEulerAngles.y, 0);

        if (Input.GetMouseButtonDown(1))
        {
            //host.transform.position = MouseAssistant.Instance.mouseCursor.transform.position;
            //TogglePlacement();
        }
        

    }

    Vector3 GetHostPlacementPos() {
        //return MouseAssistant.Instance.mouseCursor.transform.position + MouseAssistant.Instance.mouseCursor.transform.forward * -offeset;
        return Vector3.zero;
    }

    Vector3 GetModelPlacementPos() {
        //return MouseAssistant.Instance.mouseCursor.transform.position + MouseAssistant.Instance.mouseCursor.transform.forward * .1f*offeset;
        return Vector3.zero;
    }
   

    /// <inheritdoc />
    void IMixedRealityInputHandler.OnInputDown(InputEventData eventData) { TogglePlacement(); }
    /// <inheritdoc />
    void IMixedRealityInputHandler.OnInputUp(InputEventData eventData) { }

}
