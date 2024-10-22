using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Panel_Notice : PanelBase
{
    public TextMeshProUGUI txtTitle;
    public TextMeshProUGUI txtDesc;
    public TextMeshProUGUI txtBtn;

    private PanelBase prevPanel;
    private eNotice typeNotice;

#if UNITY_EDITOR
    public eNotice tempNotiType;
    private void Awake()
    {
        SetNotice(tempNotiType);
    }
#endif

    protected override void Init()
    {
        uiType = ePanel.Notice;
    }

    public void SetNotice(eNotice notice)
    {
        typeNotice = notice;

        switch (notice)
        {
            case eNotice.CREATE_OVER:
                {
                    SetNoticeInfo(NoticeInfo.CREATE_OVER); 
                    break;
                }
            case eNotice.CREATE_FILE_COPYRIGHT:
                {
                    SetNoticeInfo(NoticeInfo.CREATE_COPYRIGHT); 
                    break;
                }
            case eNotice.MODIFY_DELETE:
                {
                    SetNoticeInfo(NoticeInfo.MODIFY_DELETE); 
                    break;
                }
            case eNotice.MODIFY_FILE_COPYRIGHT:
                {
                    SetNoticeInfo(NoticeInfo.MODIFY_FILE); 
                    break;
                }
            case eNotice.ETC_SUGGEST:
                {
                    SetNoticeInfo(NoticeInfo.ETC_SUGGEST);
                    break;
                }
            case eNotice.FILE_SIZE_OVER:
                {
                    SetNoticeInfo(NoticeInfo.FILE_SIZE_OVER); 
                    break;
                }
        }
    }

    private void SetNoticeInfo(NoticeInfo.Notice notice)
    {
        txtTitle.text = notice.title;
        txtDesc.text = notice.desc;

        if (notice.btn == string.Empty)
        {
            txtBtn.transform.parent.gameObject.SetActive(false);
        }
        else
        {
            txtBtn.transform.parent.gameObject.SetActive(true);
            txtBtn.text = notice.btn;
        }
    }

    public void OnCheckBtn()
    {
        switch (typeNotice)
        {
            case eNotice.CREATE_OVER:
                {
                    GameManager.Instance.OpenUI(ePanel.Modify);
                    break;
                }
            case eNotice.MODIFY_FILE_COPYRIGHT:
            case eNotice.CREATE_FILE_COPYRIGHT:
                {
                    // 파일 선택 창 띄우기
                    GameManager.Instance.curPanel.OnFileOpen();
                    break;
                }
            case eNotice.MODIFY_DELETE:
                {
                    Debug.Log("[Notice] 캐릭터 삭제해요.");
                    GameManager.Instance.DeleteCharacter();
                    break;
                }
            case eNotice.ETC_SUGGEST:
                {
                    Debug.Log("[Notice] 의견 제시 리셋중...");
                    // 메시지 전송 후 reset
                    GameManager.Instance.curPanel.ResetUI();
                    break;
                }
        }

        Close();
    }

    public void OnCloseBtn()
    {
        switch (typeNotice)
        {
            case eNotice.CREATE_OVER:
                {
                    GameManager.Instance.OpenUI(ePanel.Modify);
                    break;
                }
        }

        Close();
    }
}

