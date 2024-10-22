using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Panel_Create : PanelBase
{
    protected int selectedIdx = -1;
    protected GameObject charObj;

    public List<Toggle> tglMenu = new List<Toggle>();

    [Header("Panel_Appearance")]
    public List<Toggle> tglCharList = new List<Toggle>();
    public TMP_InputField inputName;
    public UnityEngine.UI.Button btnNext;

    [Header("Panel_Voice")]
    public TTS_VoiceList voiceList;
    public TMP_InputField inputLine;

    [Header("Panel_Personal")]
    public TextMeshProUGUI txtFileName;

    public TextMeshProUGUI txtPersonalTitle;
    public TMP_InputField inputPersonality;

    public TextMeshProUGUI txtFewshotTitle;
    public ScrollRect scrollFewshot;
    public FewshotElement baseFewshot;
    public FewshotElement prefabFewshot;
    private List<FewshotElement> listFewshot = new List<FewshotElement>();

    private OpenFileDialog dialogOpen;
    private Stream streamOpen = null;

    protected void Start()
    {
        dialogOpen = new OpenFileDialog();
        dialogOpen.Filter = "txt files (*.txt)|*.txt";
        dialogOpen.FilterIndex = 1;
        //dialogOpen.Title
    }

    protected override void Init()
    {
        tglMenu[0].isOn = true;

        // panel appearance setting
        selectedIdx = -1;
        for (int i=0; i<tglCharList.Count; i++)
        {
            tglCharList[i].isOn = false;
        }
        inputName.text = string.Empty;
        btnNext.interactable = false;

        // panel voice setting
        inputLine.text = Utils.BASE_LINE;
        GameManager.Instance.SetTTSLine(inputLine.text);

        // panel personal setting
        txtFileName.text = string.Empty;
        inputPersonality.text = string.Empty;
        baseFewshot.Reset();
        for(int i=0; i<listFewshot.Count; i++)
        {
            if (baseFewshot == listFewshot[i]) continue;
            DestroyImmediate(listFewshot[i].gameObject);
        }
        listFewshot.Clear();
        listFewshot.Add(baseFewshot);

        scrollFewshot.verticalNormalizedPosition = 0f;
    }

    public override void Close()
    {
        if(streamOpen != null)
        {
            streamOpen.Close();
            streamOpen = null;
        }

        base.Close();
    }

    public void OnSelectChar(Toggle tgl)
    {
        if (tgl.isOn == false) return;
        
        selectedIdx = tglCharList.IndexOf(tgl);
        if(inputName.text != string.Empty)
        {
            btnNext.interactable = true;
        }
    }

    public void OnInputName()
    {
        btnNext.interactable = selectedIdx >= 0 && inputName.text != string.Empty;
        txtPersonalTitle.text = inputName.text + Utils.PERSONALITY_TITLE;
        txtFewshotTitle.text = inputName.text + Utils.FEWSHOT_TITLE;
        baseFewshot.Init(this, 0, inputName.text);
    }

    public void OnPlay(GameObject btn)
    {
        GameManager.Instance.SetTTSLine(inputLine.text);
        voiceList.OnPlay(btn);
    }

    public void OnAddFewshot()
    {
        if (prefabFewshot == null) return;

        FewshotElement objFewshot = Instantiate<FewshotElement>(prefabFewshot, scrollFewshot.content);
        if (objFewshot == null) return;

        objFewshot.Init(this, listFewshot.Count, inputName.text);
        listFewshot.Add(objFewshot);
    }

    public void OnNoticePolicy()
    {
        GameManager.Instance.OpenNotice(eNotice.CREATE_FILE_COPYRIGHT);
    }

    public override void OnFileOpen()
    {
        txtFileName.text = FileOpen();
    }

    private string FileOpen()
    {
        if (dialogOpen.ShowDialog() == DialogResult.OK)
        {
            streamOpen = dialogOpen.OpenFile();
            if (streamOpen != null)
            {
                if (streamOpen.Length > Utils.FILE_SIZE_LIMIT)
                {
                    MessageBox.Show(NoticeInfo.FILE_SIZE_OVER.desc, NoticeInfo.FILE_SIZE_OVER.title);
                    return null;
                }
                else
                {
                    return dialogOpen.FileName;
                }
            }
        }
        return null;
    }

    public void DeleteFewshot(int idx)
    {
        DestroyImmediate(listFewshot[idx].gameObject);
        listFewshot.RemoveAt(idx);
    }

    public void OnSave()
    {

    }
}
