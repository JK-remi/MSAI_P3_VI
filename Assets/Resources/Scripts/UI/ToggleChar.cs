using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToggleChar : MonoBehaviour
{
    public TextMeshProUGUI txtName;
    public TextMeshProUGUI txtName_streaming;
    public GameObject charObj;
    public CharInfo charInfo;
    private Toggle tgl;

    private Panel_Modify uiOwner = null;

    private void Awake()
    {
        tgl = this.GetComponent<Toggle>();
    }

    public void Init(CharInfo info)
    {
        charInfo = info;

        charObj = GameManager.Instance.GetCharObj(info.PrefabIdx);
        txtName.text = info.Name;

        tgl.onValueChanged.RemoveAllListeners();
        tgl.onValueChanged.AddListener(new UnityEngine.Events.UnityAction<bool>(charObj.SetActive));
        tgl.onValueChanged.AddListener(delegate { OnChange(); });

        uiOwner = null;
    }

    public void SetOwner(Panel_Modify owner)
    {
        uiOwner = owner;
    }

    public void OnChange()
    {
        if(tgl.isOn)
        {
            GameManager.Instance.ChangeCurChar(charInfo);
            if(txtName_streaming != null)
            {
                txtName_streaming.text = charInfo.Name;
            }

            if(uiOwner != null)
            {
                uiOwner.ChangeChar(charInfo);
            }
        }
    }

    public void ToggleOn(bool isOn)
    {
        tgl.isOn = isOn;
        charObj.SetActive(isOn);
    }
}
