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

    [HideInInspector] private STT stt;
    [HideInInspector] private TTS tts;
    [HideInInspector] private GPT gpt;

    public YouTubeChat panelLiveChat;

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        stt = this.GetComponent<STT>();
        tts = this.GetComponent<TTS>();
        gpt = this.GetComponent<GPT>();
    }

    public void Send2GPT(string prompt)
    {
        if (panelLiveChat == null) return;

        gpt.uiText = panelLiveChat.txtMessage;
        gpt.OnGPT(prompt);
    }
}
