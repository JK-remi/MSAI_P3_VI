using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class VoiceData
{
    public string DisplayName;
    public string ShortName;
    public string Gender;
    public string Locale;
    public string LocaleName;
    public List<string> StyleList;
    public List<string> RolePlayList;
}

public class TTS_VoiceList : MonoBehaviour
{
    private const string SELECTED_ALL = "All";

    private VoiceData curVoice;

    private List<VoiceData> voiceList = new List<VoiceData>();

    public TMP_Dropdown dropdown_region;
    public TMP_Dropdown dropdown_Sex;
    public TMP_Dropdown dropdown_Name;
    public TMP_Dropdown dropdown_Role;
    public TMP_Dropdown dropdown_Style;

    public Slider slider_pitch;
    public Slider slider_rate;
    public Slider slider_volume;

    void Start()
    {
        StartCoroutine(GetVoiceList());
    }

    private IEnumerator GetVoiceList()
    {
        UnityWebRequest request = UnityWebRequest.Get(AzureUrls.VOICE_LIST_URL);
        request.SetRequestHeader("Ocp-Apim-Subscription-Key", AzureUrls.SPEECH_KEY);
        yield return request.SendWebRequest();
        while (!request.isDone)
        {
            yield return null;
        }

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;

            List<string> regions = new List<string>();
            List<string> names = new List<string>();
            regions.Add(SELECTED_ALL);
            while(json != string.Empty)
            {
                string subData = Utils.SubJsonString(json, "{", "}", 0, -1);
                VoiceData voice = JsonUtility.FromJson<VoiceData>(subData);
                voiceList.Add(voice);

                if(regions.Count == 0 || regions.Contains(voice.LocaleName) == false)
                {
                    regions.Add(voice.LocaleName);
                }
                names.Add(voice.DisplayName);
                json = Utils.SubString(json, "},", 2);
            }

            SetDropdownOptions(dropdown_region, regions);
            SetDropdownOptions(dropdown_Name, names);
            if(voiceList.Count > 0)
            {
                curVoice = voiceList[0];
            }
        }
        else
        {
            Debug.LogError(request.error);
        }
    }

    private void UpdateVoiceOption()
    {
        List<string> nameList = new List<string>();
        for (int i = 0; i < voiceList.Count; i++)
        {
            if ((dropdown_region.captionText.text == SELECTED_ALL || dropdown_region.captionText.text == voiceList[i].LocaleName) &&
                (dropdown_Sex.captionText.text == SELECTED_ALL || dropdown_Sex.captionText.text == voiceList[i].Gender))
            {
                nameList.Add(voiceList[i].DisplayName);
            }
        }

        SetDropdownOptions(dropdown_Name, nameList);
    }

    private void SetDropdownOptions(TMP_Dropdown dd, List<string> options)
    {
        if (dd == null) return;

        dd.ClearOptions();
        if(options != null)
        {
            dd.AddOptions(options);
        }
    }

    public void OnSelectRegion()
    {
        UpdateVoiceOption();
    }

    public void OnSelectSex()
    {
        UpdateVoiceOption();
    }

    public void OnSelectVoice()
    {
        VoiceData vd = voiceList.Find(voice => voice.DisplayName == dropdown_Name.captionText.text);
        if (vd != null)
        {
            curVoice = vd;

            SetDropdownOptions(dropdown_Role, vd.RolePlayList);
            SetDropdownOptions(dropdown_Style, vd.StyleList);
        }
    }

    public void OnPlay(GameObject btnPlay)
    {
        GameManager.Instance.SetVoice(curVoice.ShortName, slider_pitch.value, slider_rate.value, slider_volume.value);
        GameManager.Instance.Send2TTS(btnPlay);
        //parentTTS.SetVoice(curVoice.ShortName, dropdown_Role.captionText.text, dropdown_Style.captionText.text, 
        //    slider_pitch.value, slider_rate.value, slider_volume.value);
    }
}
