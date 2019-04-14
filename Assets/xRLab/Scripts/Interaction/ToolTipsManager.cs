using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;

public class ToolTipsManager : MonoBehaviour
{
    [SerializeField]
    ToolTip tipPrefab;
    public static ToolTipsManager Instance;
    public Dictionary<GameObject, ToolTip> tipsMap = new Dictionary<GameObject, ToolTip>();

    void Awake()
    {
        Instance = this;
    }

    public void OnConnect(GameObject target, string text)
    {
        var tooltip =  Instantiate(tipPrefab,target.transform);
        if (tooltip == null) return;
        tooltip.ToolTipText = text;
        tooltip.ShowBackground = false;
        tooltip.ShowConnector = false;

        var connector = tooltip.GetComponent<ToolTipConnector>();
        if (connector == null) return;

        connector.Target = target;
        connector.ConnectorFollowingType = ConnectorFollowType.PositionAndYRotation;
        connector.PivotMode = ConnectorPivotMode.Automatic;
        connector.PivotDirectionOrient = ConnectorOrientType.OrientToCamera;
        connector.PivotDirection = ConnectorPivotDirection.Northeast;
        connector.PivotDistance = 0.02f;

        tipsMap.Add(target, tooltip);
    }

    public void OnDisconnect(GameObject target)
    {
        if (!tipsMap.ContainsKey(target)) return;

        tipsMap.Remove(target);
    }
}
