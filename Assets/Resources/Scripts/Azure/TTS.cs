using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class TTS : MonoBehaviour
{
    private const string FILE_NAME = "TTS";
    private const string TOKEN_URL = "https://westus2.api.cognitive.microsoft.com/sts/v1.0/issueToken";
    private const string TTS_URL = "https://westus2.tts.speech.microsoft.com/cognitiveservices/v1";
    private const string API_KEY = "ae7b95c9067549e5b0f28cb9f4a72544";   // 1f8394e0f667401bb5c783e09b7c6f30
    private const int AUDIO_FREQUENCEY = 24000;     // "X-Microsoft-OutputFormat", "riff-24khz-16bit-mono-pcm"

    // https://learn.microsoft.com/en-us/azure/ai-services/speech-service/speech-synthesis-markup-voice
    private string VOICE_NAME = "en-US-EmmaNeural";
    private string VOICE_ROLE = "SeniorFemale";
    private string VOICE_STYLE = "disgruntled";
    private string VOICE_STYLE_DEG = "2";  // 0.01 ~ 2
    private string PROSODY_PITCH = "high";
    private string PROSODY_RATE = "+20.00%";
    private string PROSODY_VOL = "+20.00%";

    [SerializeField]
    private string line;

    private string token = "";

    [HideInInspector]
    public AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private IEnumerator GetToken()
    {
        Debug.Log("Get Token");
        token = "";

        UnityWebRequest request = UnityWebRequest.Post(TOKEN_URL, new WWWForm());
        request.SetRequestHeader("Ocp-Apim-Subscription-Key", API_KEY);
        yield return request.SendWebRequest();
        while (!request.isDone)
        {
            yield return null;
        }

        if(request.result == UnityWebRequest.Result.Success) 
        {
            token = request.downloadHandler.text;
        }
        else
        {
            Debug.LogError(request.error);
            //GameManager.Instance.TtsFinish(2f);
        }
    }

    private IEnumerator RequestTTS()
    {
        Debug.Log("TTS start");

        string ssml =
            "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xmlns:mstts=\"https://www.w3.org/2001/mstts\" xml:lang=\"" + Utils.LANGUAGE + "\">" +
                "<voice name=\"" + VOICE_NAME + "\">" +
                    "<mstts:express-as role=\"" + VOICE_ROLE + "\" style=\"" + VOICE_STYLE + "\" styledegree=\"" + VOICE_STYLE_DEG + "\">" +
                        "<prosody pitch=\"" + PROSODY_PITCH + "\" rate=\"" + PROSODY_RATE + "\" volume=\"" + PROSODY_VOL + "\">" +
                            line +
                        "</prosody>" +
                    "</mstts:express-as>" +
                "</voice>" +
            "</speak>";
        byte[] data = System.Text.Encoding.UTF8.GetBytes(ssml);

        UnityWebRequest request = new UnityWebRequest(TTS_URL);
        request.method = UnityWebRequest.kHttpVerbPOST;
        request.uploadHandler = new UploadHandlerRaw(data);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", "Bearer " + token);
        request.SetRequestHeader("Content-Type", "application/ssml+xml");
        request.SetRequestHeader("User-Agent", "testForEducation");
        request.SetRequestHeader("X-Microsoft-OutputFormat", "riff-24khz-16bit-mono-pcm");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Success!");

            //string filepath = Path.Combine(Application.persistentDataPath, FILE_NAME + ".wav");
            //File.WriteAllBytes(filepath, request.downloadHandler.data);

            audioSource.clip = Byte2AudioClip(request.downloadHandler.data);
            audioSource.Play();
            //GameManager.Instance.TtsFinish(audioSource.clip.length);
        }
        else
        {
            Debug.LogError(request.error);
            //GameManager.Instance.TtsFinish(2f);
        }
    }

    private AudioClip Byte2AudioClip(byte[] data)
    {
        float[] samples = new float[data.Length / 2];
        for(int i=0; i<samples.Length; i++)
        {
            samples[i] = BitConverter.ToInt16(data, i * 2) / Utils.AUDIO_REACTOR_FACTOR;
        }

        AudioClip clip = AudioClip.Create(FILE_NAME, samples.Length, 1, AUDIO_FREQUENCEY, false);
        clip.SetData(samples, 0);

        return clip;
    }

    private IEnumerator TTS_Procedure(GameObject btn)
    {
        if (btn != null)
            btn.SetActive(false);

        yield return GetToken();

        if (token == string.Empty)
        {
            Debug.LogError("Get Token FAILED!!");
        }
        else
        {
            yield return RequestTTS();
        }

        if (btn != null)
            btn.SetActive(true);
    }

    public void StartTTS(string text)
    {
        line = text;
        StartCoroutine(TTS_Procedure(null));
    }
    public void OnPlay(GameObject btn)
    {
        StartCoroutine(TTS_Procedure(btn));
    }

    public void OnChangeLine(TMP_InputField input)
    {
        line = input.text;
    }

    public void SetVoice(string name, string role, string style, float fitch, float rate, float vol)
    {
        VOICE_NAME = name;
        VOICE_ROLE = role;
        VOICE_STYLE = style;
        //VOICE_STYLE_DEG = "2";  // 0.01 ~ 2
        PROSODY_PITCH = string.Format("{0:00}%", fitch * 100);
        PROSODY_RATE = string.Format("{0:00}%", rate * 100);
        PROSODY_VOL = string.Format("{0:00}%", vol * 100);
    }
}
