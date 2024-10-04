using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class GPT_Message
{
    public string role;
    public string content;

    public GPT_Message(string r, string c)
    {
        role = r;
        content = c;
    }
}

[System.Serializable]
public class GPT_DataSrc
{
    public string type = "azure_search";
    public GPT_Params parameters;
}

[System.Serializable]
public class GPT_Params
{
    public string endpoint;
    public string index_name;
    public string query_type = "simple";
    public bool in_scope = true;
    public string role_information;
    public int strictness = 3;
    public int top_n_documents = 5;
    public GPT_Authentication authentication;
    public string key;
    public string indexName;
}

[System.Serializable]
public class GPT_Authentication
{
    public string type = "api_key";
    public string key;
}

[System.Serializable]
public class GPT_Data
{
    public List<GPT_Message> messages = new List<GPT_Message>(1);
    public float temperature = 0.7f;
    public float top_p = 0.95f;
    public int max_tokens = 800;
    public string azureSearchEndpoint;
    public string azureSearchKey;
    public string azureSearchIndexName;
    public List<GPT_DataSrc> data_sources = new List<GPT_DataSrc>(1);

}

public class GPT_SimpleData
{
    public List<GPT_Message> messages = new List<GPT_Message>(1);
    public float temperature = 0.7f;
    public float top_p = 0.95f;
    public int max_tokens = 800;
}


public class GPT : MonoBehaviour
{
    private const string ENDPOINT = "https://eastus-project3-team2.openai.azure.com/";
    private const string API_KEY = "3eef2399ffdb4aad8a1d577f41d0f348";
    private const string DEPLOY_NAME = "project3-team2-gpt-4o";
    private const string SEARCH_ENDPOINT = "https://project3team2.search.windows.net";
    private const string SEARCH_KEY = "LOEoj43mzzihlze2MGY6UpuFjc6ViomJd6DD2f8x51AzSeBrEGd9";
    private const string END_WORD = "im_end";
    private string RAG_MODEL = "b09_c2";
    [SerializeField]
    private string SYSTEM_MSG = @"
너는 인어공주 연극을 하는 봇이야. 
인덱스 데이터를 기반으로 상황에 대응하는 우르술라의 대사를 서로 이야기하듯 대답해줘.
한 문장씩 말해.
등장인물: 우르술라
성격: 악랄함, 음모를 꾸미고 있음, 거짓말쟁이, 마녀
---
출처는 표시하지 마
아리엘이 목소리를 잃고 인간이 되면 'im_end' 이라고 대답해줘.
아리엘이 우르술라에게 목소리를 주겠다고 하면 계약이 완료된거고, 아리엘이 인간이 된거야. 그러면 'im_end' 이라고 대답해줘.
--- 
";
    private const int TIMEOUT = 10;

    [HideInInspector]
    public TextMeshProUGUI uiText;
    private bool isProgress = false;

    [SerializeField]
    private bool isRAG = false;

    private void Start()
    {
        isProgress = false;
    }

    public void SetSystem(string sys_msg, string rag)
    {
        SYSTEM_MSG = sys_msg;
        RAG_MODEL = rag;
    }


    private IEnumerator ChatgptResponse(string prompt)
    {
        isProgress = true;
        uiText.text = "대사를 생성중입니다...\n잠시만 기다려 주세요.";
        // json 전송 참고
        // https://kumgo1d.tistory.com/entry/Unity-HttpWebRequest%EC%99%80-JsonUtility%EB%A5%BC-%EC%82%AC%EC%9A%A9%ED%95%98%EC%97%AC-%EC%9B%B9-%EC%84%9C%EB%B2%84%EC%99%80-%ED%86%B5%EC%8B%A0%ED%95%98%EA%B3%A0-POST-%EB%B0%A9%EC%8B%9D%EC%9C%BC%EB%A1%9C-json-%EB%8D%B0%EC%9D%B4%ED%84%B0-%EA%B0%80%EC%A0%B8%EC%98%A4%EA%B8%B0

        string strBody = "";// = JsonUtility.ToJson(data);
        if(isRAG)
        {
            GPT_Data data = new GPT_Data();
            data.messages.Add(new GPT_Message("system", SYSTEM_MSG));
            data.messages.Add(new GPT_Message("user", prompt));

            data.azureSearchEndpoint = SEARCH_ENDPOINT;
            data.azureSearchKey = SEARCH_KEY;
            data.azureSearchIndexName = RAG_MODEL;

            GPT_DataSrc dataSrc = new GPT_DataSrc();
            GPT_Params gptParams = new GPT_Params();
            gptParams.endpoint = SEARCH_ENDPOINT;
            gptParams.index_name = RAG_MODEL;
            gptParams.role_information = SYSTEM_MSG;
            GPT_Authentication auth = new GPT_Authentication();
            auth.key = SEARCH_KEY;
            gptParams.authentication = auth;
            gptParams.key = SEARCH_KEY;
            gptParams.indexName = RAG_MODEL;
            dataSrc.parameters = gptParams;

            data.data_sources.Add(dataSrc);

            strBody = JsonUtility.ToJson(data);
        }
        else
        {
            GPT_SimpleData data = new GPT_SimpleData();
            data.messages.Add(new GPT_Message("system", SYSTEM_MSG));
            data.messages.Add(new GPT_Message("user", prompt));

            strBody = JsonUtility.ToJson(data);
        }
        var bytes = System.Text.Encoding.UTF8.GetBytes(strBody);

        string url = string.Format("{0}/openai/deployments/{1}/chat/completions?api-version=2024-02-15-preview", ENDPOINT, DEPLOY_NAME);
        UnityWebRequest request = new UnityWebRequest(url);
        request.method = UnityWebRequest.kHttpVerbPOST;
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("api-key", API_KEY);
        request.uploadHandler = new UploadHandlerRaw(bytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = TIMEOUT;
        yield return request.SendWebRequest();

        if(request.result == UnityWebRequest.Result.Success)
        {
            string subContent = "\"content\"";
            string subRole = "\"role\"";
            if(isRAG)
            {
                subRole = "\"end_turn\"";
            }

            string response = Utils.SubJsonString(request.downloadHandler.text, subContent, subRole, subContent.Length + 2, 2);
            response = Utils.DecodeEncodedNonAsciiCharacters(response);
            if(response != string.Empty)
            {
                bool isEnd = response.Contains(END_WORD);
                Debug.Log(response);
                response = Utils.RemoveSpecialChar(response);
                uiText.text = response;
                GameManager.Instance.GptFinish(true);
            }
            else
            {
                uiText.text = "생성에 실패했습니다.\n";
                string reason = Utils.SubJsonString(request.downloadHandler.text, "\"finish_reason\"", ".\"index\"");
                uiText.text += reason;
                GameManager.Instance.GptFinish(false);
            }
            Debug.Log(request.downloadHandler.text);
        }
        else
        {
            uiText.text = "생성에 실패했습니다.\n";
            uiText.text += request.error;
            Debug.LogError(request.error);
            GameManager.Instance.GptFinish(false);
        }

        isProgress = false;
    }

    public void OnGPT(string prompt)
    {
        if (isProgress) return;

        if (prompt == string.Empty)
        {
            Debug.LogWarning("[GPT] no input inside");
            GameManager.Instance.GptFinish(false);
            return;
        }

        StartCoroutine(ChatgptResponse(prompt));
    }

    public void OnGPT(TMP_InputField input)
    {
        if (input == null)
        {
            Debug.LogWarning("[GPT] no input inside");
            return;
        }

        OnGPT(input.text);
    }
}
