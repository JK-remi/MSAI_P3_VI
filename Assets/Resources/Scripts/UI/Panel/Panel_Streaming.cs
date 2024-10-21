using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Panel_Streaming : PanelBase
{
    [SerializeField]
    private string apiKey = "AIzaSyASpLRFVN1dx_uACO039EQf6JTw44fd7BM"; // �߱޹��� API Ű�� �Է��ϼ���.
    [SerializeField]
    private string videoId = "80BCd_EKWm8"; // ���̺� ��Ʈ���� ���� ID�� �Է��ϼ���.

    public GameObject panelVideoID;
    public TextMeshProUGUI txtName;
    public TextMeshProUGUI txtMessage;

    public ToggleGroup tglCharList;

    public GameObject chatMessagePrefab;
    public GameObject tglCharPrefab;
    public ScrollRect scrollRect; // ScrollRect ������ �߰�

    private string liveChatId;
    private string nextPageToken;

    private Coroutine corLiveChat = null;

    private List<ToggleChar> charList = new List<ToggleChar>();

    protected override void Init()
    {
        for(int i=0; i<GameManager.Instance.curCharCnt; i++)
        {
            // �ش� index(ID)�� ĳ���͸� �߰�
            AddCharacter(i);
        }

        if (charList.Count > 0)
        {
            charList[GameManager.Instance.curCharIdx].ToggleOn(true);
        }
    }

    public override void ResetUI()
    {
        if (corLiveChat != null)
        {
            StopCoroutine(corLiveChat);
            corLiveChat = null;
        }

        if (panelVideoID != null) panelVideoID.SetActive(true);

        if(charList.Count > 0)
        {
            for (int i=0; i<charList.Count; i++)
            {
                if(GameManager.Instance.curCharIdx != i)
                {
                    charList[i].ToggleOn(false);
                    DestroyImmediate(charList[i].gameObject);
                }
            }

            DestroyImmediate(charList[GameManager.Instance.curCharIdx].gameObject);
            charList.Clear();
        }
    }

    public override void Close()
    {
        ResetUI();

        base.Close();
    }

    IEnumerator GetLiveChatId()
    {
        string url = $"https://www.googleapis.com/youtube/v3/videos?part=liveStreamingDetails&id={videoId}&key={apiKey}";

        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        bool isError = false;

#if UNITY_2020_1_OR_NEWER
        if (request.result != UnityWebRequest.Result.Success)
        {
            isError = true;
        }
#else
        if (request.isNetworkError || request.isHttpError)
        {
            isError = true;
        }
#endif

        if (isError)
        {
            VideoID_Failed("���̺� ä�� ID�� �������� �� ���� �߻�: " + request.error);
        }
        else
        {
            var json = request.downloadHandler.text;
            var response = JsonConvert.DeserializeObject<VideoListResponse>(json);

            if (response.items.Count > 0 && response.items[0].liveStreamingDetails != null)
            {
                liveChatId = response.items[0].liveStreamingDetails.activeLiveChatId;
                if (!string.IsNullOrEmpty(liveChatId))
                {
                    StartCoroutine(GetLiveChatMessages());
                }
                else
                {
                    VideoID_Failed("���̺� ä�� ID�� ã�� �� �����ϴ�.");
                }
            }
            else
            {
                VideoID_Failed("���̺� ��Ʈ���� ������ �����ϴ�.");
            }
        }
    }

    IEnumerator GetLiveChatMessages()
    {
        while (true)
        {
            string url = $"https://www.googleapis.com/youtube/v3/liveChat/messages?liveChatId={liveChatId}&part=snippet,authorDetails&key={apiKey}";
            if (!string.IsNullOrEmpty(nextPageToken))
            {
                url += $"&pageToken={nextPageToken}";
            }

            UnityWebRequest request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();

            bool isError = false;

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
            {
                isError = true;
            }
#else
            if (request.isNetworkError || request.isHttpError)
            {
                isError = true;
            }
#endif

            if (isError)
            {
                VideoID_Failed("���̺� ä�� �޽����� �������� �� ���� �߻�: " + request.error);
            }
            else
            {
                var json = request.downloadHandler.text;
                var response = JsonConvert.DeserializeObject<LiveChatMessageListResponse>(json);

                foreach (var item in response.items)
                {
                    string author = item.authorDetails.displayName;
                    string message = item.snippet.displayMessage;
                    AddMessageToChatWindow($"{author}: {message}");
                }

                nextPageToken = response.nextPageToken;
                float pollingInterval = response.pollingIntervalMillis / 1000f;
                yield return new WaitForSeconds(pollingInterval);
            }
        }
    }

    void AddMessageToChatWindow(string message)
    {
        if (chatMessagePrefab == null)
        {
            VideoID_Failed("chatMessagePrefab�� �������� �ʾҽ��ϴ�.");
            return;
        }

        GameObject newMessage = Instantiate(chatMessagePrefab, scrollRect.content.transform);

        var textComponent = newMessage.GetComponent<TMP_Text>();
        if (textComponent == null)
        {
            VideoID_Failed("chatMessagePrefab�� TMP_Text ������Ʈ�� �����ϴ�.");
            return;
        }

        textComponent.text = message;

        // ��ũ�Ѻ並 �Ʒ��� ��ũ��
        Canvas.ForceUpdateCanvases();

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
        else
        {
            VideoID_Failed("scrollRect�� �������� �ʾҽ��ϴ�.");
        }
    }

    public void SetVideoID(TMP_InputField input)
    {
        if (input == null) return;

        if (input.text != string.Empty)
        {
            videoId = input.text;
        }

        if (corLiveChat == null)
        {
            corLiveChat = StartCoroutine(GetLiveChatId());

            if (panelVideoID != null) panelVideoID.SetActive(false);
        }
    }

    private void VideoID_Failed(string errMsg)
    {
        Debug.LogError(errMsg);

        if (panelVideoID != null)
        {
            panelVideoID.SetActive(true);
        }
    }

    private void AddCharacter(int idx)
    {
        Toggle tgl = Utils.AddCharToggle(tglCharList, tglCharPrefab);
        if (tgl == null) return;

        ToggleChar info = tgl.GetComponent<ToggleChar>();
        SetCharacterInfo(info, idx);
        if(info != null)
        {
            charList.Add(info);
        }
    }

    private void SetCharacterInfo(ToggleChar info, int idx)
    {
        if (info == null) return;

        // char info�� �´� object�� game manager���� �����ͼ� toggle�� ����(�ӽ�)
        info.Init(idx);

        info.ToggleOn(false);
    }
}
