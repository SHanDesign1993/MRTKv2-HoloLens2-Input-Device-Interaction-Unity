using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.WSA;

public class RemotingExample : MonoBehaviour
{
    [SerializeField]
    private string IP;
    private bool connected = false;

    void Connect()
    {
        if (HolographicRemoting.ConnectionState != HolographicStreamerConnectionState.Connected)
        {
            HolographicRemoting.Connect(IP);
        }
    }

    void Update()
    {
        if (!connected && HolographicRemoting.ConnectionState == HolographicStreamerConnectionState.Connected)
        {
            connected = true;

            StartCoroutine(LoadDevice("WindowsMR"));
        }
    }

    IEnumerator LoadDevice(string newDevice)
    {
        XRSettings.LoadDeviceByName(newDevice);
        yield return null;
        XRSettings.enabled = true;
    }

    private void OnGUI()
    {
        //IP = GUI.TextField(new Rect(10, 10, 200, 30), IP, 25);
        //GUI.Label(new Rect(60, 20, 200, 20), "IP: " + IP);
    }

    public void ToggleConnection(bool connect)
    {
        if (!connect)
        {
            HolographicRemoting.Disconnect();
            connected = false;
        }
        else
            Connect();
    }

    void OnApplicationQuit()
    {
        if (connected)
        {
            ToggleConnection(false);
        }
    }
}