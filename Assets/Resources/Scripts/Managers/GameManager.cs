using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private const string CHAR_INFO_FILE = "/data.json";
    private const string CHAT_LOG_FILE = "chat.json";
    private const int CHAR_LIMIT = 6;

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
    private TTS_VoiceList voiceList;
    private GPT gpt;
    private bool isResponseEnd = false;

    [SerializeField]
    private List<GameObject> charObjList = new List<GameObject>();
    private Dictionary<string, CharInfo> charDic = new Dictionary<string, CharInfo>();
    private Dictionary<string, List<Fewshot>> chatDic = new Dictionary<string, List<Fewshot>>();
    private Fewshot chatOneShot;

    public int curCharCnt { get { return charDic.Count; } }
    public CharInfo curCharInfo;

    [Header("UI")]
    public List<PanelBase> uiObjects = new List<PanelBase>();
    public PanelBase curPanel { get; private set; }

    private void Awake()
    {
        Application.targetFrameRate = 60;
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
        voiceList = this.GetComponent<TTS_VoiceList>();
        voiceList.InitVoiceList();

        isResponseEnd = true;

        LoadCharInfo();
    }

    private string GetChatFilePath(string ID)
    {
        return Application.persistentDataPath + "/" + ID + CHAT_LOG_FILE;
    }

    public void Send2GPT(ChatMsg send, ChatMsg response)
    {
        if (isResponseEnd == false) return;

        isResponseEnd = false;
        Debug.Log("send to GPT");

        chatOneShot = new Fewshot(send.txtMessage.text, string.Empty);

        gpt.uiText = response.txtMessage;
        gpt.msgBox = response;
        response.gameObject.SetActive(false);
        gpt.OnGPT(send.txtMessage.text);
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
            if(curPanel.uiType == ePanel.Streaming)
            {
                tts.StartTTS(gpt.uiText.text);
            }
            else
            {
                gpt.msgBox.gameObject.SetActive(true);
                isResponseEnd = true;
                ((Panel_Create)curPanel).OnResponse();

                if(curPanel.uiType == ePanel.Modify)
                {
                    chatOneShot.a = gpt.uiText.text;
                }
            }
        }
        else
        {
            isResponseEnd = true;
            if(curPanel.uiType == ePanel.Create || curPanel.uiType == ePanel.Modify)
            {
                //DestroyImmediate(gpt.msgBox.gameObject);
                //chatOneShot.a = gpt.uiText.text;
                ((Panel_Create)curPanel).OnResponse();
            }
        }

        if(curPanel.uiType == ePanel.Modify)
        {
            GetChatLog().Add(chatOneShot);
            SaveChatLog();
            chatOneShot = null;
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

        if(ui == ePanel.Create || ui == ePanel.Modify)
        {
            if(ui == ePanel.Create && curCharCnt >= CHAR_LIMIT)
            {
                OpenNotice(eNotice.CREATE_OVER);
                curPanel = prevPanel;
                return;
            }

            ((Panel_Create)curPanel).voiceList.InitVoiceList(voiceList.voiceList);
        }

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
            chatDic.Add(info.ID, new List<Fewshot>());

            SaveCharInfo();
        }

        curCharInfo = info;
        OpenUI(ePanel.Modify);
    }

    public void ModiCharInfo(CharInfo info)
    {
        if (charDic.ContainsKey(info.ID) == true)
        {
            charDic[info.ID] = info;
            chatDic[info.ID].Clear();

            SaveCharInfo();
        }
    }

    public void DelCharInfo(string ID)
    {
        if(charDic.ContainsKey(ID))
        {
            charDic.Remove(ID);
            chatDic.Remove(ID);

            string filePath = GetChatFilePath(ID);
            if(File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            SaveCharInfo();
        }

        if(charDic.Count == 0)
        {
            curCharInfo = null;
            OpenUI(ePanel.Create);
        }
    }

    private void SaveCharInfo()
    {
        string json = JsonConvert.SerializeObject(charDic);
        File.WriteAllText(Application.persistentDataPath + CHAR_INFO_FILE, json);

        SaveChatLog();
    }

    private void SaveChatLog()
    {
        foreach (var log in chatDic)
        {
            string chatFilePath = GetChatFilePath(log.Key);
            string chatJson = JsonConvert.SerializeObject(log.Value);
            File.WriteAllText(chatFilePath, chatJson);
        }
    }

    private void LoadCharInfo()
    {
        string filePath = Application.persistentDataPath + CHAR_INFO_FILE;
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            charDic = JsonConvert.DeserializeObject<Dictionary<string, CharInfo>>(json);
        }

        foreach(var key in charDic.Keys)
        {
            string chatFilePath = GetChatFilePath(key);
            if (File.Exists(chatFilePath))
            {
                string chatJson = File.ReadAllText(chatFilePath);
                chatDic.Add(key, JsonConvert.DeserializeObject<List<Fewshot>>(chatJson));
            }
        }

        if (charDic.Count > 0)
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

    public void SetGPTInfo(CharInfo info)
    {
        gpt.Init(info);
    }

    public void ChangeCurChar(CharInfo info)
    {
        curCharInfo = info;

        gpt.Init(info);
        tts.SetVoice(info.Voice);
    }

    public List<Fewshot> GetChatLog()
    {
        if (chatDic.ContainsKey(curCharInfo.ID) == false)
        {
            chatDic.Add(curCharInfo.ID, new List<Fewshot>());
        }

        return chatDic[curCharInfo.ID];
    }

    public void StopGPT()
    {
        gpt.Stop();
        isResponseEnd = true;
    }

    public void OnFinishVoiceList()
    {
        if (curPanel.uiType == ePanel.Create || curPanel.uiType == ePanel.Modify)
        {
            ((Panel_Create)curPanel).voiceList.InitVoiceList(voiceList.voiceList);
        }
    }
}
