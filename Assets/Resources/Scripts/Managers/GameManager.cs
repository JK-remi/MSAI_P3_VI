using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance = null;
    public static GameManager Instance
    {
        get
        {
            _instance = GameObject.FindObjectOfType<GameManager>();
            if (_instance == null)
            {
                GameObject container = new GameObject("GameManager");
                _instance = container.AddComponent<GameManager>();
            }

            return _instance;
        }
    }

    private AudioSource audioSource;
    private STT stt;
    private TTS tts;
    private GPT gpt;
    private bool isResponseEnd = false;

    [SerializeField]
    private List<GameObject> charObjList = new List<GameObject>();
    private Dictionary<string, CharInfo> charDic = new Dictionary<string, CharInfo>();
    public int curCharCnt { get { return charDic.Count; } }
    public CharInfo curCharInfo;

    [Header("UI")]
    public List<PanelBase> uiObjects = new List<PanelBase>();
    public PanelBase curPanel { get; private set; }

    private void Awake()
    {
        Init();
    }

    private void Start()
    {
        OpenUI(ePanel.FirstInfo);
    }

    private void Init()
    {
        audioSource = this.gameObject.GetComponent<AudioSource>();

        stt = this.GetComponent<STT>();
        tts = this.GetComponent<TTS>();
        gpt = this.GetComponent<GPT>();

        isResponseEnd = true;

        LoadCharInfo();
    }

    public void Send2GPT(string prompt)
    {
        if (isResponseEnd == false) return;

        Panel_Streaming panelLiveChat = uiObjects[(int)ePanel.Streaming] as Panel_Streaming;
        if (panelLiveChat == null) return;

        isResponseEnd = false;
        Debug.Log("send to GPT");

        gpt.uiText = panelLiveChat.txtMessage;
        gpt.OnGPT(prompt);
    }

    public void Send2TTS(GameObject playBtn)
    {
        tts.OnPlay(playBtn);
    }

    public void PlayAudio(AudioClip clip)
    {
        if (clip == null) return;
        if (audioSource.isPlaying && audioSource.clip == clip) return; // 현재 재생 중인 clip과 같으면 다시 재생 필요 X

        audioSource.clip = clip;
        audioSource.Play();
    }

    public void GptFinish(bool isSuccess)
    {
        if (isSuccess)
        {
            tts.StartTTS(gpt.uiText.text);
        }
        else
        {
            isResponseEnd = true;
        }
    }

    public void TtsFinish(float wait)
    {
        if (isResponseEnd) return;
        Invoke("ResponseEnd", wait);
    }

    public void ResponseEnd()
    {
        isResponseEnd = true;
    }

    public void SetVoice(string name, float pitch, float rate, float vol)
    {
        tts.SetVoice(name, pitch, rate, vol);
    }

    public void SetTTSLine(string text)
    {
        tts.OnChangeLine(text);
    }

    public void OpenUI(ePanel ui)
    {
        if (uiObjects == null || uiObjects.Count == 0) return;

        PanelBase prevPanel = curPanel;
        curPanel = uiObjects[(int)ui];

        if(prevPanel != null)
        {
            prevPanel.Close();
        }
        curPanel.Open();
    }

    public void OpenNotice(eNotice noticeType)
    {
        if (uiObjects == null || uiObjects.Count == 0) return;

        Panel_Notice uiNotice = (Panel_Notice)uiObjects[(int)ePanel.Notice];
        uiNotice.SetNotice(noticeType);
        uiNotice.Open();
    }

    public void DeleteCharacter()
    {
        if (uiObjects == null || uiObjects.Count == 0) return;

        Panel_Modify uiNotice = (Panel_Modify)uiObjects[(int)ePanel.Modify];
        uiNotice.OnDelete();
    }

    public GameObject GetCharObj(int idx)
    {
        if (idx >= charObjList.Count) return null;

        return charObjList[idx];
    }

    public void AddCharInfo(CharInfo info)
    {
        if(charDic.ContainsKey(info.ID) == false)
        {
            charDic.Add(info.ID, info);

            SaveCharInfo();
        }
    }

    public void ModiCharInfo(CharInfo info)
    {
        if (charDic.ContainsKey(info.ID) == true)
        {
            charDic[info.ID] = info;

            SaveCharInfo();
        }
    }

    public void DelCharInfo(string ID)
    {
        if(charDic.ContainsKey(ID))
        {
            charDic.Remove(ID);

            SaveCharInfo();
        }
    }

    private const string CHAR_INFO_FILE = "/data.json";
    private void SaveCharInfo()
    {
        string json = JsonConvert.SerializeObject(charDic);
        File.WriteAllText(Application.persistentDataPath + CHAR_INFO_FILE, json);
    }

    private void LoadCharInfo()
    {
        string json = File.ReadAllText(Application.persistentDataPath + CHAR_INFO_FILE);
        charDic = JsonConvert.DeserializeObject<Dictionary<string, CharInfo>>(json);

        if(charDic.Count > 0)
        {
            curCharInfo = charDic.Values.ElementAt<CharInfo>(0);
        }
    }

    public List<ToggleChar> SetCharTglList(ToggleGroup tglGroup, GameObject tglPrefab, TextMeshProUGUI txtName)
    {
        List<ToggleChar> charList = new List<ToggleChar>();

        foreach(var info in charDic)
        {
            ToggleChar tglChar = AddCharacter(tglGroup, tglPrefab, info.Value);
            if(tglChar != null)
            {
                tglChar.txtName_streaming = txtName;
                charList.Add(tglChar);
            }
        }

        return charList;
    }

    private ToggleChar AddCharacter(ToggleGroup tglGroup, GameObject tglPrefab, CharInfo info)
    {
        Toggle tgl = Utils.AddCharToggle(tglGroup, tglPrefab);
        if (tgl == null) return null;

        ToggleChar tglChar = tgl.GetComponent<ToggleChar>();
        SetCharacterInfo(tglChar, info);
        
        return tglChar;
    }

    private void SetCharacterInfo(ToggleChar tglChar, CharInfo info)
    {
        if (tglChar == null) return;

        // char info에 맞는 object를 game manager에서 가져와서 toggle에 설정(임시)
        tglChar.Init(info);
        tglChar.ToggleOn(false);
    }

    public void ChangeCurChar(CharInfo info)
    {
        curCharInfo = info;

        gpt.Init(info);
        tts.SetVoice(info.Voice);
    }
}
