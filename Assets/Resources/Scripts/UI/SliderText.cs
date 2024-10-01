using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderText : MonoBehaviour
{
    public Slider parentSlider;
    public TMP_InputField valueInput;

    private void Start()
    {
        if(parentSlider == null)
        {
            parentSlider = this.GetComponent<Slider>();
        }
        SliderChange();
    }

    public void SliderChange()
    {
        if (parentSlider == null) return;

        if (valueInput)
        {
            valueInput.text = ConvertFloat2Percentage(parentSlider.value);
        }
    }

    public void InputChange()
    {
        if (parentSlider == null) return;

        float f = float.Parse(valueInput.text) / 100f;
        parentSlider.value = float.Parse(valueInput.text) / 100f;
    }

    private string ConvertFloat2Percentage(float f)
    {
        return string.Format("{0}", (int)(f * 100f));
    }
}
