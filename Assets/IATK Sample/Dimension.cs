using IATK;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class Dimension : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI Text;

    public DimensionManager Manager { get; private set; }
    public DimensionFilter Filter { get; private set; }
    public bool IsSelected { get; private set; }

    public void Init(DimensionManager manager, DimensionFilter filter)
    {
        Manager = manager;
        Filter = filter;

        Text.text = filter.Attribute;
    }

    public void SetSelectionState(bool selected)
    {
        IsSelected = selected;

        if (IsSelected)
            Manager?.OnSelectedChanged(this);
    }
}
