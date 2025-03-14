using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class VoiceData
{
    public string DisplayName;  // short name (ex. Adri)
    public string ShortName;    // full name (ex. af-ZA-AdriNeural)
    public string Gender;       
    public string Locale;       // region initial (ex. af-ZA)
    public string LocaleName;   // region full (ex. Afrikaans (Sourth Africa))
    public List<string> StyleList;
    public List<string> RolePlayList;
}

public class TTS_VoiceList : MonoBehaviour
{
    private const string SELECTED_ALL = "All";

    private VoiceData curVoice;
    public VoiceData CurVoice { get { return curVoice; } }
    public List<VoiceData> voiceList = new List<VoiceData>();

    public TMP_Dropdown dropdown_region;
    public TMP_Dropdown dropdown_Sex;
    public TMP_Dropdown dropdown_Name;
    public TMP_Dropdown dropdown_Role;
    public TMP_Dropdown dropdown_Style;

    public Slider slider_pitch;
    public Slider slider_rate;
    public Slider slider_volume;

    public void InitVoiceList()
    {
        StartCoroutine(GetVoiceList());
    }

    public void InitVoiceList(List<VoiceData> voices)
    {
        if (voiceList.Count > 0 || voices.Count == 0) return;

        voiceList = voices;

        List<string> regions = new List<string>();
        List<string> names = new List<string>();
        for (int i = 0; i < voices.Count; i++)
        {
            regions.Add(voices[i].LocaleName);
            names.Add(voices[i].DisplayName);
        }

        SetDropdownOptions(dropdown_region, regions);
        SetDropdownOptions(dropdown_Name, names);
        if (voiceList.Count > 0)
        {
            curVoice = voiceList[0];
        }
    }

    public void Init(CharInfo info)
    {
        dropdown_region.value = dropdown_region.options.FindIndex(option => option.text == info.Voice.region);
        dropdown_Sex.value = dropdown_Sex.options.FindIndex(option => option.text == info.Voice.gender);
        dropdown_Name.value = dropdown_Name.options.FindIndex(option => option.text == info.Voice.displayName);
        dropdown_Role.value = dropdown_Role.options.FindIndex(option => option.text == info.Voice.role);
        dropdown_Style.value = dropdown_Style.options.FindIndex(option => option.text == info.Voice.style);

        slider_pitch.value = info.Voice.pitch;
        slider_rate.value = info.Voice.rate;
        slider_volume.value = info.Voice.volume;
    }

    public void Init()
    {
        dropdown_region.value = -1;
        dropdown_Sex.value = -1;
        dropdown_Name.value = -1;
        dropdown_Role.value = -1;
        dropdown_Style.value = -1;
     
        slider_pitch.value = 0;
        slider_rate.value = 0;
        slider_volume.value = 0;
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
                json = Utils.SubString(json, "},", 2);
            }

            GameManager.Instance.OnFinishVoiceList();
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
    }

    public bool IsAllSetting()
    {
        return curVoice != null;
    }
}
