using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ePanel
{
    FirstInfo = 0,
    Create,
    Modify,
    Streaming,
    ETC,
    Notice
}

public class PanelBase : MonoBehaviour
{
    protected virtual void Init() { }
    public virtual void Open() 
    {
        this.gameObject.SetActive(true);
        Init();
    }

    public virtual void Close()
    {
        this.gameObject.SetActive(false);
    }
}