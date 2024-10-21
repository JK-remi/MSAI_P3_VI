using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleMenu : MonoBehaviour
{
    private PanelBase parentUI;

    private void Awake()
    {
        parentUI = this.transform.parent.GetComponent<PanelBase>();
    }

    public void OnClick(int uiIdx)
    {
        if (parentUI == null) return;
        if (parentUI.uiType == (ePanel)uiIdx) return;

        GameManager.Instance.OpenUI((ePanel)uiIdx);
    }
}
