using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Physics;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Input.UnityInput;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Threading.Tasks;
using System.Linq;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Collections;

public class TrackingManager : MonoBehaviour
{
    string[] DataProviderName = new string[]
    {
        "Custom Mouse Service",
        "Custom Input Simulation Service"
    };

    public enum TrackingServiceEnum
    {
        MouseDevice = 0,
        InputSimulationService = 1
    }

    public static TrackingManager Instance;

    const int NumberOfRetries = 3;

    [Header(" ----Service Setting---- ")]
    [SerializeField]
    bool autoEnableService = true;
    [SerializeField]
    public TrackingServiceEnum trackingService = TrackingServiceEnum.InputSimulationService;
    [Help("Select handedness if currently tracking InputSimulation")]
    [SerializeField]
    Handedness defaultHandedness = Handedness.Right;
    public Handedness DefaultHandedness
    {
        get
        {
            return defaultHandedness;
        }
        set
        {
            defaultHandedness = value;
        }
    }

    [Header(" ----Tracking Object---- ")]
    [SerializeField]
    TrackingIndicator indicator;

    //the device manager of service
    Dictionary<TrackingServiceEnum, BaseInputDeviceManager> deviceMap = new Dictionary<TrackingServiceEnum, BaseInputDeviceManager>();

    [SerializeField]
    List<Transform> deviceInstances = new List<Transform>();
    [Header(" ----Status---- ")]
    [SerializeField]
    bool IsServiceRegistered = false;
    bool IsDeviceInstanceInView = false;
    [SerializeField]
    bool IsToggleDeviceInstance = false;
    bool IsToggleIndicator = false;

    private string GetTrackingServiceName(TrackingServiceEnum service)
    {
        return DataProviderName[(int)service];
    }

    private BaseInputDeviceManager TryGetServiceFromSDK(TrackingServiceEnum service)
    {
        BaseInputDeviceManager deviceManager = null;
        var retryCount = NumberOfRetries;

        while (deviceManager == null && retryCount > 0)
        {
            try
            {
                Debug.Log("trying to get " + GetTrackingServiceName(service));
                deviceManager = MixedRealityToolkit.Instance.GetService<IMixedRealityInputDeviceManager>(GetTrackingServiceName(service), false) as BaseInputDeviceManager;
            }
            catch (TimeoutException tex)
            {
                retryCount--;

                if (retryCount == 0)
                {
                    Debug.Log(GetTrackingServiceName(service) + " is not registered, please check MRTK profiles.");
                    return null;
                }
            }
        }
        return deviceManager;
    }
    
    void Awake()
    {
        Instance = this;
    }

    async void Start()
    {
        //wait MRTK initialized
        await new WaitUntil(() => MixedRealityToolkit.IsInitialized == true);

        bool success = Initialize();

        if (success && autoEnableService)
        {
            ToggleDevice(true);
        }
        //enable the service
        /*
        Task enabletask = EnableDevice();
        try { await enabletask; }
        catch (System.Exception e) { Debug.Log(e); }
        */
    }

    void Reset()
    {
        IsToggleDeviceInstance = false;
        IsToggleIndicator = false;
        IsDeviceInstanceInView = false;
        indicator.ResetDefault();
        deviceInstances.Clear();
    }

    bool Initialize() {
        Reset();
        deviceMap.Clear();
        foreach (TrackingServiceEnum type in Enum.GetValues(typeof(TrackingServiceEnum)))
        {
            deviceMap.Add(type, TryGetServiceFromSDK(type));
        }
        IsServiceRegistered = (deviceMap.Count == Enum.GetNames(typeof(TrackingServiceEnum)).Length);
        if (!IsServiceRegistered) return false;
        return true;
    }

    void Update()
    {
        if (!Input.mousePresent) return;

        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleDevice(IsToggleDeviceInstance = !IsToggleDeviceInstance);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RecenterDevice();
        }

        Cursor.visible = !IsToggleDeviceInstance;
        Cursor.lockState = (IsToggleDeviceInstance) ?CursorLockMode.Locked: CursorLockMode.None;

        MonitorDeviceInstancePosition();
    }

    async Task EnableDevice()
    {
        if (trackingService == TrackingServiceEnum.MouseDevice)
        {
            var mouseManager = deviceMap[trackingService] as CustomMouseDeviceManager;
            mouseManager.IsAvailable = true;
        }
        else if (trackingService == TrackingServiceEnum.InputSimulationService)
        {
            var simulationService = deviceMap[trackingService] as CustomInputSimulationService;
            simulationService.IsAvailable = true;
        }

        deviceMap[trackingService].Enable();

        if (trackingService == TrackingServiceEnum.MouseDevice)
        {
            var mouseManager = deviceMap[trackingService] as CustomMouseDeviceManager;
            if (mouseManager.Controller.InputSource.Pointers.Length > 0)
            {
                //get pointer gameobject
                var pointer = (MousePointer)mouseManager.Controller.InputSource.Pointers[0];
                pointer.SetCursor();

                await new WaitUntil(() => pointer.transform.childCount > 0);

                //get cursor gameobject 
                pointer.BaseCursor = pointer.transform.GetChild(0).GetComponent<MeshCursor>();
                deviceInstances.Add(pointer.transform.GetChild(0).transform);
            }
        }
        else if (trackingService == TrackingServiceEnum.InputSimulationService)
        {
            var simulationService = deviceMap[trackingService] as CustomInputSimulationService;
            //awake simulationhand
            switch (defaultHandedness)
            {
                case Handedness.Right:
                    simulationService.handDataProvider.stateRight.IsAlwaysTracked = true;
                    break;

                case Handedness.Left:
                    simulationService.handDataProvider.stateLeft.IsAlwaysTracked = true;
                    break;

                case Handedness.Both:
                    simulationService.handDataProvider.stateRight.IsAlwaysTracked = true;
                    simulationService.handDataProvider.stateLeft.IsAlwaysTracked = true;
                    break;

                default:
                    simulationService.handDataProvider.stateRight.IsAlwaysTracked = false;
                    simulationService.handDataProvider.stateLeft.IsAlwaysTracked = false;
                    break;
            }
           
            await new WaitUntil(() => simulationService.trackedHands.Count > 0);

            Transform instance = null;
            simulationService.GetHandTransform(defaultHandedness,out instance);
            deviceInstances.Add(instance.GetChild(1));
            ToolTipsManager.Instance.OnConnect(instance.gameObject,"R:Left alt \nL:Left ctrl");
        }
        IsToggleDeviceInstance = true;
        RecenterDevice();
    }

    void DisableDevice()
    {
        deviceMap[trackingService].Disable();

        if (trackingService == TrackingServiceEnum.MouseDevice)
        {
            var mouseManager = deviceMap[trackingService] as CustomMouseDeviceManager;
            mouseManager.IsAvailable = false;
        }
        else if (trackingService == TrackingServiceEnum.InputSimulationService)
        {
            ToolTipsManager.Instance.OnDisconnect(deviceInstances[0].gameObject);
            var simulationService = deviceMap[trackingService] as CustomInputSimulationService;
            simulationService.IsAvailable = false;
        }

        Reset();
    }

    public void ToggleDevice(bool toggle)
    {
        IsToggleDeviceInstance = toggle;

        if (toggle)
        {
            EnableDevice();
        }
        else
        {
            DisableDevice();
        }
    }

    public void RecenterDevice()
    {
        if (trackingService == TrackingServiceEnum.MouseDevice)
        {
            var mouseManager = deviceMap[trackingService] as CustomMouseDeviceManager;
            var pointer = (MousePointer)mouseManager.Controller.InputSource.Pointers[0];
            //pointer.ResetToGazePosition();
        }
        else if (trackingService == TrackingServiceEnum.InputSimulationService)
        {
            var simulationService = deviceMap[trackingService] as CustomInputSimulationService;
            simulationService.handDataProvider.RecenterHand(defaultHandedness);
        }
    }

    public void SwitchInputDevice()
    {
        StartCoroutine(SwitchInputCoroutine());
    }

    IEnumerator SwitchInputCoroutine()
    {
        yield return null;
        ToggleDevice(false);
        if (trackingService == TrackingServiceEnum.MouseDevice)
        {
            trackingService = TrackingServiceEnum.InputSimulationService;
        }
        else if (trackingService == TrackingServiceEnum.InputSimulationService)
        {
            trackingService = TrackingServiceEnum.MouseDevice;
        }
        ToggleDevice(true);
    }

    void MonitorDeviceInstancePosition()
    {
        if (!IsToggleDeviceInstance || deviceInstances.Count == 0) return;

        Vector3 screenPoint = CameraCache.Main.WorldToViewportPoint(deviceInstances[0].transform.position);
        IsDeviceInstanceInView = screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;

        if (IsToggleIndicator == IsDeviceInstanceInView)
        {
            IsToggleIndicator = !IsDeviceInstanceInView;

            if (IsDeviceInstanceInView)
            {
                indicator.ResetDefault();
            }
            else
            {
                indicator.PointToTarget(deviceInstances[0]);
            }
        }
    }
}

[CustomEditor(typeof(TrackingManager))]
public class ObjectTrackingManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        /*
        var main = target as ObjectTrackingManager;

        EditorGUILayout.Space();
        EditorGUI.BeginDisabledGroup(main.trackingService != ObjectTrackingManager.TrackingServiceEnum.InputSimulationService);
        EditorGUILayout.HelpBox("Select handedness if currently tracking InputSimulation", MessageType.Info);
        main.DefaultHandedness = (Handedness)EditorGUILayout.EnumFlagsField("defaulthand",main.DefaultHandedness);
        EditorGUI.EndDisabledGroup();
        */
    }
}
