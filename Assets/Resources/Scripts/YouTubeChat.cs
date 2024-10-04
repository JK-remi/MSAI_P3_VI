using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Newtonsoft.Json;
using UnityEngine.UI; // ScrollRect를 사용하기 위해 필요

public class YouTubeChat : MonoBehaviour
{
    [SerializeField]
    private string apiKey = "AIzaSyASpLRFVN1dx_uACO039EQf6JTw44fd7BM"; // 발급받은 API 키를 입력하세요.
    [SerializeField]
    private string videoId = "80BCd_EKWm8"; // 라이브 스트림의 비디오 ID를 입력하세요.

    public GameObject panelVideoID;
    public TextMeshProUGUI txtName;
    public TextMeshProUGUI txtMessage;

    public GameObject chatMessagePrefab;
    public ScrollRect scrollRect; // ScrollRect 변수를 추가

    private string liveChatId;
    private string nextPageToken;

    private Coroutine corLiveChat = null;

    private void OnDisable()
    {
        Reset();
    }

    private void OnDestroy()
    {
        Reset();
    }

    private void Reset()
    {
        if (corLiveChat != null)
        {
            StopCoroutine(corLiveChat);
            corLiveChat = null;
        }

        if (panelVideoID != null) panelVideoID.SetActive(true);
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
            VideoID_Failed("라이브 채팅 ID를 가져오는 중 오류 발생: " + request.error);
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
                    VideoID_Failed("라이브 채팅 ID를 찾을 수 없습니다.");
                }
            }
            else
            {
                VideoID_Failed("라이브 스트리밍 정보가 없습니다.");
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
                VideoID_Failed("라이브 채팅 메시지를 가져오는 중 오류 발생: " + request.error);
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
            VideoID_Failed("chatMessagePrefab이 설정되지 않았습니다.");
            return;
        }

        GameObject newMessage = Instantiate(chatMessagePrefab, scrollRect.content.transform);

        var textComponent = newMessage.GetComponent<TMP_Text>();
        if (textComponent == null)
        {
            VideoID_Failed("chatMessagePrefab에 TMP_Text 컴포넌트가 없습니다.");
            return;
        }

        textComponent.text = message;

        // 스크롤뷰를 아래로 스크롤
        Canvas.ForceUpdateCanvases();

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
        else
        {
            VideoID_Failed("scrollRect가 설정되지 않았습니다.");
        }
    }

    public void SetVideoID(TMP_InputField input)
    {
        if (input == null) return;

        if(input.text != string.Empty)
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
}

public class VideoListResponse
{
    public List<VideoItem> items;
}

public class VideoItem
{
    public LiveStreamingDetails liveStreamingDetails;
}

public class LiveStreamingDetails
{
    public string activeLiveChatId;
}

public class LiveChatMessageListResponse
{
    public List<LiveChatMessageItem> items;
    public string nextPageToken;
    public int pollingIntervalMillis;
}

public class LiveChatMessageItem
{
    public LiveChatSnippet snippet;
    public LiveChatAuthorDetails authorDetails;
}

public class LiveChatSnippet
{
    public string displayMessage;
    public string publishedAt;
}

public class LiveChatAuthorDetails
{
    public string displayName;
}
