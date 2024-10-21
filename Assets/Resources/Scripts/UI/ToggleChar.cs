using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToggleChar : MonoBehaviour
{
    public TextMeshProUGUI txtName;
    public GameObject charObj;
    private int charIdx;
    private Toggle tgl;

    private void Awake()
    {
        tgl = this.GetComponent<Toggle>();
    }

    public void Init(int idx)
    {
        charIdx = idx;

        charObj = GameManager.Instance.GetCharObj(idx);
        txtName.text = charObj.name;

        tgl.onValueChanged.RemoveAllListeners();
        tgl.onValueChanged.AddListener(new UnityEngine.Events.UnityAction<bool>(charObj.SetActive));
        tgl.onValueChanged.AddListener(delegate { OnChange(); });
    }

    public void OnChange()
    {
        if(tgl.isOn)
        {
            GameManager.Instance.curCharIdx = charIdx;
        }
    }

    public void ToggleOn(bool isOn)
    {
        tgl.isOn = isOn;
    }
}
