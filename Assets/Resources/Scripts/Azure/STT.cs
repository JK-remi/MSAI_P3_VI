using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using System;
using System.IO;
using TMPro;
using UnityEngine;

public class STT : MonoBehaviour
{
    private const string SPEECH_KEY = "1ff2d7a7379b4e349aa1734718de89fc";   // 57d50f60f0aa4712a266501ca97e9ebb

    private const int HEADER_SIZE = 44;
    private const string FILE_NAME = "STT_attack.wav";
    private const int AUDIO_FREQUENCEY = 44100;
    private const int MAX_RECORD_TIME = 10;
    private const float MISS_CONF = 35f;

    public class PronunciationData
    {
        public string text;
        public float confidence = 0f;
        public PronunciationScoreData scores = new PronunciationScoreData();

        public int CalculateDamage(int dmgInfo)
        {
            if (confidence <= MISS_CONF) return 1;  // miss 판정

            float avg = (confidence * 0.5f + scores.AccuracyScore * 0.3f + scores.FluencyScore * 0.1f + scores.CompletenessScore * 0.1f) * 0.01f;
            int result = (int)(dmgInfo * avg);
            return result;
        }
    }

    public class PronunciationScoreData
    {
        public float AccuracyScore=0f;
        public float FluencyScore=0f;
        public float CompletenessScore=0f;
        public float PronScore=0f;

        public float Min()
        {
            return Mathf.Min(AccuracyScore, FluencyScore, CompletenessScore, PronScore);
        }
    }

    private SpeechConfig speechConfig;
    private int flagRecord = 0;
    public bool IsRecording
    { get { return flagRecord > 0; } }

    public string spellText;

    [SerializeField]
    public TextMeshProUGUI uiText;
    private GameObject recordBtn;

    private PronunciationData data;

    public AudioSource audioSource;
    private AudioClip recordClip;
    private float recordStartTime;

    private void Awake()
    {
        speechConfig = SpeechConfig.FromSubscription(SPEECH_KEY, Utils.SPEECH_REGION);
        speechConfig.SpeechRecognitionLanguage = Utils.LANGUAGE;

        data = new PronunciationData();

        Init();
    }

    private void Init()
    {
        flagRecord = 0;
        if (recordBtn != null)
        {
            recordBtn.SetActive(true);
            ChangeBtnText("RECORD");
        }
        Debug.Log("Recording Finished!!");
    }

    public void OnChangeSpell(TMP_InputField input)
    {
        spellText = input.text;
    }

    private void ChangeBtnText(string text)
    {
        TextMeshProUGUI btnText = recordBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null) btnText.text = text;
    }

    bool isSttOnly = false;
    public void OnRecordSttOnly(GameObject btnObj)
    {
        isSttOnly = true;
        OnRecord(btnObj);
    }

    public void OnRecordPronun(GameObject btnObj)
    {
        isSttOnly=false;
        OnRecord(btnObj);
    }

    private void OnRecord(GameObject btnObj)
    {
        recordBtn = btnObj;

        if (flagRecord == 0)
        {
            ChangeBtnText("STOP");
            Debug.Log("Recording Started!!");
            recordClip = Microphone.Start(Microphone.devices[0], true, MAX_RECORD_TIME, AUDIO_FREQUENCEY);
            Invoke("CreateAudioClip", MAX_RECORD_TIME);
            recordStartTime = Time.time;
        }
        else if (flagRecord == 1)
        {
            uiText.text = "분석 중 입니다.";
            Microphone.End(Microphone.devices[0]);
            CreateAudioClip();
        }

        flagRecord++;
    }

    private void CreateAudioClip()
    {
        CancelInvoke();
        if (recordBtn != null) recordBtn.SetActive(false);

        int lastTime = Mathf.Clamp((int)(Time.time - recordStartTime), 0, (int)recordClip.length);
        if (lastTime == 0)
        {
            Init();
            RequestFailed();

            Debug.Log("Have no record info");
            return;
        }

        lastTime += 1;
        float[] samples = new float[recordClip.samples];
        recordClip.GetData(samples, 0);

        float[] cutSamples = new float[lastTime * AUDIO_FREQUENCEY];    // 시간 & 오디오 주파수 맞춰서 배열 생성
        Array.Copy(samples, cutSamples, Mathf.Min(cutSamples.Length, samples.Length));
        recordClip = AudioClip.Create("STT_attack", cutSamples.Length, 1, AUDIO_FREQUENCEY, false);
        recordClip.SetData(cutSamples, 0);

        audioSource.clip = recordClip;

        if(SaveRecordClip())
        {
            SpeechRecognize();
        }
        audioSource.Play();
    }

    private FileStream CreateEmpty(string filepath)
    {
        FileStream fileStream = null;
        try
        {
            fileStream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.Write);
        }
        catch (IOException ex)
        {
            Debug.LogError(ex.Message);
        }

        if (fileStream != null)
        {
            byte emptyByte = new byte();

            for (int i = 0; i < HEADER_SIZE; i++) //preparing the header
            {
                fileStream.WriteByte(emptyByte);
            }
        }

        return fileStream;
    }

    private void ConvertAndWrite(FileStream fileStream, AudioClip clip)
    {
        float[] samples = new float[clip.samples];

        clip.GetData(samples, 0);

        Int16[] intData = new Int16[samples.Length];
        //converting in 2 float[] steps to Int16[], //then Int16[] to Byte[]

        Byte[] bytesData = new Byte[samples.Length * 2];
        //bytesData array is twice the size of
        //dataSource array because a float converted in Int16 is 2 bytes.

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * Utils.AUDIO_REACTOR_FACTOR);
            Byte[] byteArr = new Byte[2];
            byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        fileStream.Write(bytesData, 0, bytesData.Length);
    }

    private void WriteHeader(FileStream fileStream, AudioClip clip)
    {

        var hz = clip.frequency;
        var channels = clip.channels;
        var samples = clip.samples;

        fileStream.Seek(0, SeekOrigin.Begin);

        Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        fileStream.Write(riff, 0, 4);

        Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
        fileStream.Write(chunkSize, 0, 4);

        Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        fileStream.Write(wave, 0, 4);

        Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        fileStream.Write(fmt, 0, 4);

        Byte[] subChunk1 = BitConverter.GetBytes(16);
        fileStream.Write(subChunk1, 0, 4);

        UInt16 two = 2;
        UInt16 one = 1;

        Byte[] audioFormat = BitConverter.GetBytes(one);
        fileStream.Write(audioFormat, 0, 2);

        Byte[] numChannels = BitConverter.GetBytes(channels);
        fileStream.Write(numChannels, 0, 2);

        Byte[] sampleRate = BitConverter.GetBytes(hz);
        fileStream.Write(sampleRate, 0, 4);

        Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
        fileStream.Write(byteRate, 0, 4);

        UInt16 blockAlign = (ushort)(channels * 2);
        fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

        UInt16 bps = 16;
        Byte[] bitsPerSample = BitConverter.GetBytes(bps);
        fileStream.Write(bitsPerSample, 0, 2);

        Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
        fileStream.Write(datastring, 0, 4);

        Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
        fileStream.Write(subChunk2, 0, 4);

        //		fileStream.Close();
    }

    private bool SaveRecordClip()
    {
        string filePath = Utils.GetFilePath(FILE_NAME);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        FileStream fileStream = CreateEmpty(filePath);        // Make sure directory exists if user is saving to sub dir.
        if(fileStream == null)
        {
            Init();
            RequestFailed();

            return false;
        }

        ConvertAndWrite(fileStream, recordClip);
        WriteHeader(fileStream, recordClip);
        fileStream.Close();

        return true;
    }

    private async void SpeechRecognize()
    {
        Debug.Log("Recognize start");

        string filePath = Utils.GetFilePath(FILE_NAME);
        AudioConfig audioConfig = AudioConfig.FromWavFileInput(filePath);
        SpeechRecognizer speechRecognizer = new SpeechRecognizer(speechConfig, Utils.LANGUAGE, audioConfig);
        var speechResult = await speechRecognizer.RecognizeOnceAsync();
        OutputSpeechRecognitionResult(speechResult);

        audioConfig.Dispose();
        speechRecognizer.Dispose();

        if (data.text == string.Empty) 
        {
            Init();
            RequestFailed();

            return;
        }

        if (isSttOnly == false)
            PronunciationConfig();
        else
        {
            Init();
            //GameManager.Instance.SttFinish(true);
        }
    }

    private async void PronunciationConfig()
    {
        Debug.Log("pronunciation start");

        string filePath = Utils.GetFilePath(FILE_NAME);
        AudioConfig audioConfig = AudioConfig.FromWavFileInput(filePath);
        SpeechRecognizer speechRecognizer = new SpeechRecognizer(speechConfig, Utils.LANGUAGE, audioConfig);
        PronunciationAssessmentConfig pronunciationConfig = new PronunciationAssessmentConfig(
            referenceText: spellText,
            gradingSystem: GradingSystem.HundredMark,
            granularity: Granularity.FullText,
            enableMiscue: false
            );
        pronunciationConfig.EnableProsodyAssessment();

        pronunciationConfig.ApplyTo(speechRecognizer);
        var speechResult = await speechRecognizer.RecognizeOnceAsync();
        string pronunciationJson = speechResult.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult);
        JsonParsing(pronunciationJson);

        audioConfig.Dispose();
        speechRecognizer.Dispose();

        Init();
    }

    private void JsonParsing(string json)
    {
        const string subConfidence = "\"Confidence\"";
        const string subLexical = "\"Lexical\"";
        const string subPronunce = "\"PronunciationAssessment\"";
        const string subWords = "\"Words\"";

        string confidence = Utils.SubJsonString(json, subConfidence, subLexical, subConfidence.Length + 1, 1);
        if (confidence == string.Empty)
        {
            data = new PronunciationData();
        }
        else
        {
            data.confidence = float.Parse(confidence) * 100f;

            string jsonPronunciationAssessment = Utils.SubJsonString(json, subPronunce, subWords, subPronunce.Length + 1, 1);
            data.scores = JsonUtility.FromJson<PronunciationScoreData>(jsonPronunciationAssessment);
        }

        //uiText.text += string.Format("Confidence: {0:0.00}\n", data.confidence);
        //uiText.text += string.Format("Accuracy: {0:0.00}\n", data.scores.AccuracyScore);
        //uiText.text += string.Format("Fluency: {0:0.00}\n", data.scores.FluencyScore);
        //uiText.text += string.Format("Complete: {0:0.00}\n", data.scores.CompletenessScore);
        //uiText.text += string.Format("Pron: {0:0.00}\n", data.scores.PronScore);

        float tempScore = Mathf.Min(data.scores.Min(), data.confidence);
        uiText.text += MakeScoreText(tempScore) + "\n";
        //GameManager.Instance.HitTarget(data);
    }

    private string MakeScoreText(float confidence)
    {
        string result;
        if (confidence > 90f)
            result = "[PERFECT] 강력한 공격 발동!";
        else if(confidence > 75f)
            result = "[EXCELLENT] 높은 공격 발동!";
        else if (confidence > 50f)
            result = "[GREAT] 중간 공격 발동...";
        else if (confidence > MISS_CONF)
            result = "[GOOD] 약한 공격 발동...";
        else
            result = "[MISS] 최소 데미지 적용.";

        return result;
    }

    private void OutputSpeechRecognitionResult(SpeechRecognitionResult result)
    {
        switch (result.Reason)
        {
            case ResultReason.RecognizedSpeech:
                {
                    Debug.Log($"RECOGNIZED: Text={result.Text}");
                    uiText.text = result.Text + "\n";
                    data.text = result.Text;
                    break;
                }
            case ResultReason.NoMatch:
                {
                    Debug.Log($"NOMATCH: Speech could not be recognized.");
                    uiText.text = "일치하는 음성이 없습니다. 다시 녹음해주세요.";
                    data.text = "";
                    break;
                }
            case ResultReason.Canceled:
                {
                    var cancellation = CancellationDetails.FromResult(result);
                    Debug.LogWarning($"CANCELED: Reason={cancellation.Reason}");
                    uiText.text = "취소되었습니다. 다시 녹음해주세요.\n";
                    uiText.text += cancellation.Reason;
                    data.text = "";

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        uiText.text = "죄송합니다. 에러가 발생했습니다. 다시 녹음해주세요.\n";
                        uiText.text += cancellation.ErrorDetails;
                        Debug.LogError($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                        Debug.LogError($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                    }
                    break;
                }
        }
    }

    private void RequestFailed()
    {
        //if (isSttOnly == false)
        //    GameManager.Instance.HitTarget(null);
        //else
        //    GameManager.Instance.SttFinish(false);
    }
}
