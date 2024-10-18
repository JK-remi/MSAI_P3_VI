using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public int curCharCnt { get { return charObjList.Count; } }
    public int curCharIdx = 0;

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
        // UI modify toggle 삭제
        // GameManger character list에서 해당 character delete
    }

    public GameObject GetCharObj(int idx)
    {
        if (idx >= charObjList.Count) return null;

        return charObjList[idx];
    }
}
