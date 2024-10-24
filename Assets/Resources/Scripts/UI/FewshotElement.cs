using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FewshotElement : MonoBehaviour
{
    private Panel_Create ownerUI;
    private int idx;

    [SerializeField]
    private TextMeshProUGUI txtName;

    public TMP_InputField inputQ;
    public TMP_InputField inputA;

    public void Init(Panel_Create owner, int i, string name)
    {
        ownerUI = owner;
        idx = i;
        txtName.text = string.Format("{0}:", name);
    }

    public void Init(Panel_Create owner, int fewshotIdx, CharInfo info)
    {
        Init(owner, fewshotIdx, info.Name);

        inputQ.text = info.Fewshots[fewshotIdx].q;
        inputA.text = info.Fewshots[fewshotIdx].a;
    }

    public void OnDelete()
    {
        ownerUI.DeleteFewshot(idx);
    }

    public void Reset()
    {
        inputQ.text = string.Empty;
        inputA.text = string.Empty;
    }
}
