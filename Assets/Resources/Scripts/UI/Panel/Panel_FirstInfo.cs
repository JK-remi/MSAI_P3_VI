using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Panel_FirstInfo : PanelBase
{
    private const string PLAYER_PREF = "isFirst";

#if UNITY_EDITOR
    public bool bForceOpen = false;
    private void Awake()
    {
        if (bForceOpen) PlayerPrefs.SetInt(PLAYER_PREF, 0);
    }
#endif

    public bool isVisited = false;

    protected override void Init()
    {
        uiType = ePanel.FirstInfo;

        if(isVisited || PlayerPrefs.GetInt(PLAYER_PREF, 0) > 0)
        {
            isVisited = true;

            if (GameManager.Instance.curCharCnt > 0)
            {
                GameManager.Instance.OpenUI(ePanel.Modify);
            }
            else
            {
                GameManager.Instance.OpenUI(ePanel.Create);
            }
        }
    }

    public void OnOK()
    {
        isVisited = true;
        PlayerPrefs.SetInt(PLAYER_PREF, 1);
        PlayerPrefs.Save();
        GameManager.Instance.OpenUI(ePanel.Create);
    }
}
