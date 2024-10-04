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

    public YouTubeChat panelLiveChat;

    private void Awake()
    {
        Init();
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
        if (panelLiveChat == null) return;

        isResponseEnd = false;
        Debug.Log("send to GPT");

        gpt.uiText = panelLiveChat.txtMessage;
        gpt.OnGPT(prompt);
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
}
