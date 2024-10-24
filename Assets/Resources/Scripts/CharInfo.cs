using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum eCharName
{
    Min,
    JK,
    Wynter,
    Erika,
    Luis,
    NEXA
}

public class VoiceInfo
{
    public string displayName;
    public string region;
    public string gender;
    public string name;
    public string style = string.Empty;
    public string role = string.Empty;

    public float pitch;
    public float rate;
    public float volume;

    public VoiceInfo() { }

    public VoiceInfo (TTS_VoiceList voiceList)
    {
        this.displayName = voiceList.CurVoice.DisplayName;
        this.region = voiceList.CurVoice.LocaleName;
        this.gender = voiceList.CurVoice.Gender;
        this.name = voiceList.CurVoice.ShortName;
        this.style = voiceList.dropdown_Style.captionText.text;
        this.role = voiceList.dropdown_Role.captionText.text;

        this.pitch = voiceList.slider_pitch.value;
        this.rate = voiceList.slider_rate.value;
        this.volume = voiceList.slider_volume.value;
    }
}

public class Fewshot
{
    public string q;
    public string a;

    public Fewshot(string q, string a)
    {
        this.q = q;
        this.a = a;
    }
}

public class CharInfo
{
    public string ID;
    public string Name;
    public int PrefabIdx;

    public VoiceInfo Voice;

    public string FilePath;
    public string Personality;
    public List<Fewshot> Fewshots;

    public CharInfo(string id, string name, int prefabName, VoiceInfo voice, string fileName, string personality, List<Fewshot> fewshots)
    {
        ID = id;
        Name = name;
        PrefabIdx = prefabName;
        Voice = voice;
        FilePath = fileName;
        Personality = personality;
        Fewshots = fewshots;
    }
}
