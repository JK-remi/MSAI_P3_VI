using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Panel_ETC : PanelBase
{
    public TMP_InputField inputPros;
    public TMP_InputField inputCons;
    public TMP_InputField inputEmail;

    protected override void Init()
    {
        uiType = ePanel.ETC;

        inputPros.text = string.Empty;
        inputCons.text = string.Empty;
        inputEmail.text = string.Empty;
    }

    public override void ResetUI()
    {
        Init();
    }

    public void OnSummit()
    {
        GameManager.Instance.OpenNotice(eNotice.ETC_SUGGEST);
    }
}
