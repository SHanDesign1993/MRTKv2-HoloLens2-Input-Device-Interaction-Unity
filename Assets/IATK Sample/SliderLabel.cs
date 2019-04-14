using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderLabel : MonoBehaviour
{
    [SerializeField]
    Slider Slider;
    [SerializeField]
    TextMeshProUGUI Label;
    [SerializeField]
    string Prefix = string.Empty;

    void Start()
    {
        Label.text = string.Format("{0}\t{1:0.#}", Prefix, Slider.value);
    }

    public void OnValuechanged(Single value)
    {
        Label.text = string.Format("{0}\t{1:0.#}", Prefix, Slider.value);
    }
}
