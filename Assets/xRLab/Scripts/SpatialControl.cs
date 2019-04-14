using UnityEngine;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.WSA;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using Microsoft.MixedReality.Toolkit.Utilities;

public class SpatialControl : MonoBehaviour
{
    [SerializeField]
    GameObject scanText;
    [SerializeField]
    GameObject sceneContent;
    [SerializeField]
    bool IsSpatialAwarenessServiceUp = false;
    [SerializeField]
    bool IsSpatialObserverServiceUp = false;
    [SerializeField]
    bool IsSpatialMeshLoaded = false;

    async void Start()
    {
        //wait MRTK register extended service
        await new WaitUntil(() => MixedRealityToolkit.SpatialAwarenessSystem != null);
        IsSpatialAwarenessServiceUp = true;

        //wait MRTK register observer service
        await new WaitUntil(() => MixedRealityToolkit.Instance.GetDataProvider<IMixedRealitySpatialAwarenessObserver>() != null);

        var service = MixedRealityToolkit.Instance.GetDataProvider<IMixedRealitySpatialAwarenessObserver>();
        if( service!= null)
            IsSpatialObserverServiceUp = true;

        StartCoroutine(WaitSpatialMeshLoaded());
    }

    public void RecenterView()
    {
        //For a seated-scale experience, to let the user later recenter the seated origin, you can call the XR.InputTracking.Recenter method:
        InputTracking.Recenter();
    }

    IEnumerator WaitSpatialMeshLoaded()
    {
        var parent = MixedRealityToolkit.SpatialAwarenessSystem.SpatialAwarenessObjectParent;
        Debug.Log("start detecting child mesh("+ parent.transform.childCount + ") of "+parent.name);
        while (parent.transform.childCount == 0)
        {
            //Debug.Log("detecting spatial mesh");
            yield return null;
        }
        if (parent.transform.childCount > 0)
        {
            Debug.Log("spatial mesh inited.");
            IsSpatialMeshLoaded = true;
            scanText.gameObject.SetActive(false);
            sceneContent.gameObject.SetActive(true);
        }
           
    }
    
}
