using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ToggleImageSwap : MonoBehaviour
{
    Button button;
    [SerializeField]
    GameObject IsOffSprite;
    [SerializeField]
    GameObject IsOnSprite;
    [SerializeField]
    bool IsOn = false;
    public UnityEvent SelectEvent = new UnityEvent();
    public UnityEvent DeselectEvent = new UnityEvent();
    // Start is called before the first frame update
    void Start()
    {
        button = this.GetComponent<Button>();
        button.onClick.AddListener(TriggerEvent);
    }

    public void ToggleState(bool isOn)
    {
        IsOn = isOn;
        IsOnSprite.SetActive(!IsOn);
        IsOffSprite.SetActive(IsOn);
    }

    void TriggerEvent()
    {
        ToggleState(!IsOn);

        if (IsOn)
            SelectEvent.Invoke();
        else
            DeselectEvent.Invoke();
    }
    
}
