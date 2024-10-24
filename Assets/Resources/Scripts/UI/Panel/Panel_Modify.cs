using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Panel_Modify : Panel_Create
{
    [Header("TogglCharacter")]
    public ToggleGroup tglGroup;
    public GameObject tglCharPrefab;

    private CharInfo charInfo;
    private List<ToggleChar> charList = new List<ToggleChar>();

    protected override void Init()
    {
        uiType = ePanel.Modify;

        charList = GameManager.Instance.SetCharTglList(tglGroup, tglCharPrefab, null);
        if (charList.Count > 0)
        {
            ChangeChar(GameManager.Instance.curCharInfo);
            for(int i=0; i<charList.Count; i++)
            {
                charList[i].SetOwner(this);
                if(charList[i].charInfo == charInfo)
                {
                    charList[i].ToggleOn(true);
                }
            }
        }

        // panel voice setting
        inputLine.text = Utils.BASE_LINE;
        GameManager.Instance.SetTTSLine(inputLine.text);

        btnSave.interactable = false;
    }

    public override void Close()
    {
        if (charList.Count > 0)
        {
            int curIdx = -1;
            for (int i = 0; i < charList.Count; i++)
            {
                if (GameManager.Instance.curCharInfo != charList[i].charInfo)
                {
                    charList[i].ToggleOn(false);
                    DestroyImmediate(charList[i].gameObject);
                }
                else
                {
                    curIdx = i;
                }
            }

            DestroyImmediate(charList[curIdx].gameObject);
            charList.Clear();
        }

        ResetUI();

        base.Close();
    }

    public override void ResetUI()
    {
        for (int i = 0; i < listFewshot.Count; i++)
        {
            if (baseFewshot == listFewshot[i]) continue;
            DestroyImmediate(listFewshot[i].gameObject);
        }
        listFewshot.Clear();
        listFewshot.Add(baseFewshot);

        base.ResetUI();
    }

    public void ChangeChar(CharInfo info)
    {
        ResetUI();

        charInfo = info;

        // panel voice setting
        voiceList.Init(charInfo);

        // panel personal setting
        txtFileName.text = charInfo.FilePath;
        inputPersonality.text = charInfo.Personality;
        baseFewshot.Init(this, 0, charInfo);
        for (int i = 1; i < charInfo.Fewshots.Count; i++)
        {
            OnAddFewshot();
        }

        scrollFewshot.verticalNormalizedPosition = 0f;

        btnSave.interactable = false;
    }

    public new void OnNoticePolicy() 
    {
        GameManager.Instance.OpenNotice(eNotice.MODIFY_FILE_COPYRIGHT);
    }

    public new void OnSave()
    {
        charInfo.Voice = new VoiceInfo(voiceList);

        List<Fewshot> fewshots = new List<Fewshot>();
        for (int i = 0; i < listFewshot.Count; i++)
        {
            // Q/A 둘 중 하나라도 비어 있으면 fewshot 인정 X
            if (listFewshot[i].inputQ.text == string.Empty || listFewshot[i].inputA.text == string.Empty) continue;

            fewshots.Add(new Fewshot(listFewshot[i].inputQ.text, listFewshot[i].inputA.text));
        }
        charInfo.Fewshots = fewshots;

        charInfo.FilePath = txtFileName.text;
        charInfo.Personality = inputPersonality.text;

        GameManager.Instance.ModiCharInfo(charInfo);
    }

    public new void OnAddFewshot()
    {
        if (prefabFewshot == null) return;

        FewshotElement objFewshot = Instantiate<FewshotElement>(prefabFewshot, scrollFewshot.content);
        if (objFewshot == null) return;

        objFewshot.Init(this, listFewshot.Count, charInfo);
        listFewshot.Add(objFewshot);
    }

    public void OnNoticeDelete()
    {
        GameManager.Instance.OpenNotice(eNotice.MODIFY_DELETE);
    }

    public void OnDelete()
    {
        int idx = FindActivateTgl();
        if (idx == -1) return;

        charList[idx].ToggleOn(false);
        charList[idx].charObj.SetActive(false);
        DestroyImmediate(charList[idx].gameObject);
        charList.RemoveAt(idx);

        ChangeChar(charInfo);

        GameManager.Instance.DelCharInfo(charInfo.ID);
    }

    private int FindActivateTgl()
    {
        int idx = -1;
        for(int i=0; i<charList.Count; i++)
        {
            if(charList[i].charInfo == charInfo)
            {
                idx = i;
                break;
            }
        }

        return idx;
    }
}
