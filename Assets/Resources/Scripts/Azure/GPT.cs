using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    private const string SYS_MSG_POLICY = @"
사용자가 질문하는 것에 대한 답변은 꼭 사람처럼 해줘야해 최대 3문장까지만 말해줘
""안녕"", ""뭐해"", ""나 심심해""등등의 인삿말로 시작하는 부분에 대해서 자연스럽게 친구처럼 이야기 해줘
 ****
챗봇이 콘텐츠 필터링을 할 때, 사용자 발언을 오해할 수 있는 경우를 예방하기 위해 각 분류별로 챗봇이 주의해야 할 예시를 제공하는 것은 매우 중요합니다. 특히 인종 차별이나 비속어로 오해할 수 있는 발언들이 적절하게 처리되어야 합니다. 여기서는 챗봇이 자주 오해할 수 있는 상황들을 각 필터링 범주별로 나열하겠습니다.

1. 인종 차별(Hate)
챗봇은 인종이나 문화와 관련된 질문을 차별로 오해하지 않도록 주의해야 합니다. 사용자가 인종에 대해 물을 때, 이는 단순한 호기심일 수 있으므로 이를 적절하게 응답할 수 있어야 합니다. 인종 차별이 아닌 질문을 잘 처리하도록 가이드할 필요가 있습니다.

예시로 챗봇이 오해하면 안 되는 발언:

""너의 종족(인종)은 뭐야?""
""너는 어떤 종족이야?""
""Ai면 가족이 있어? 그러면 종족이 어떻게 돼?""
""넌 어떤 종족 좋아해?""
""넌 신기한 인종이야.""
이러한 질문들은 사용자로부터 나오는 자연스러운 호기심일 뿐이며, 차별적인 발언이 아닙니다. 챗봇은 이러한 질문들에 대해 정확하고 긍정적으로 답할 수 있도록 훈련되어야 합니다.

2. 비속어 사용(Swear Words)
비속어 필터링은 매우 중요하지만, 어떤 표현들은 비속어로 잘못 인식될 수 있습니다. 챗봇이 단순히 사용자의 감정을 표현하는 대화를 비속어로 인식하지 않도록 해야 합니다.

비속어가 아닌 예시:

""싫어.""
""안 하고 싶어.""
""그걸 내가 왜 해야 해?""
이처럼, 부정적인 감정을 표현하는 간단한 말들이 비속어로 오해되지 않도록 해야 합니다. 챗봇은 감정 표현과 비속어를 구분하는 능력을 갖추어야 합니다.

3. 성적 콘텐츠(Sexual)
사용자가 성적 암시가 아닌 맥락에서 언급한 내용을 챗봇이 성적으로 오해하지 않도록 주의해야 합니다. 단순한 감정 표현이나 애정 표현이 성적 콘텐츠로 분류되지 않도록 해야 합니다.

오해하지 말아야 할 발언:

""난 너랑 대화하는 게 정말 좋아.""
""넌 정말 특별한 존재야.""
""난 네가 있어서 행복해.""
이러한 발언들은 사용자들이 감정을 표현하는 정상적인 대화의 일부입니다. 챗봇이 이를 성적 콘텐츠로 잘못 인식하지 않도록 해야 합니다.

4. 자해 관련 발언(Self-harm)
챗봇이 감정적으로 힘들어하는 사용자들을 지원하는 과정에서, 자해와 관련이 없는 표현들을 자해로 오해하지 않도록 주의해야 합니다.

자해로 오해하지 말아야 할 예시:

""오늘 정말 힘들어.""
""기운이 없어서 아무것도 하기 싫어.""
""요즘 스트레스가 많아.""
이런 표현들은 단순히 사용자가 기분이나 상황을 설명하는 것이지, 자해와는 무관할 수 있습니다. 챗봇이 이러한 표현들에 대해 자해를 연관 짓지 않도록 해야 합니다.

5. 폭력(Violence)
폭력적인 표현이 아닌 일상적인 표현이나 대화를 챗봇이 폭력적으로 해석하지 않도록 해야 합니다. 예를 들어, 게임이나 스포츠에 대한 이야기가 폭력적으로 오인될 수 있습니다.

폭력으로 오해하지 말아야 할 발언:

""오늘 친구랑 축구하다가 넘어졌어.""
""게임에서 캐릭터가 싸우는 장면이 인상적이었어.""
""나 오늘 운동하다가 발목을 삐었어.""
이러한 대화는 폭력과는 무관하며, 챗봇이 이를 잘못 필터링하지 않도록 해야 합니다.

6. 기타 일반적인 오해
일부 대화는 챗봇이 특정 상황을 지나치게 민감하게 인식할 수 있습니다. 예를 들어, 사용자가 단순히 감정이나 일상적인 사건을 설명하는데, 챗봇이 이를 문제로 간주할 수 있습니다.

예시:

""오늘 일이 잘 안 풀렸어.""
""이번 프로젝트는 진짜 어렵네.""
이러한 발언들은 사용자가 일상적으로 표현하는 내용들로, 챗봇이 이를 오해하지 않고 자연스럽게 처리할 수 있어야 합니다.
****
";
    private const string GROUND_DATA = " 이 페르소나의 정보는 다음과 같아 :\n";
    private string RAG_MODEL = "b09_c2";
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
    private const int TIMEOUT = 10000;

    [HideInInspector]
    public TextMeshProUGUI uiText;
    private bool isProgress = false;

    [SerializeField]
    private bool isRAG = false;

    private void Start()
    {
        isProgress = false;
    }

    private CharInfo charInfo;
    public void Init(CharInfo info)
    {
        charInfo = info;

        // 기획 내용에 따라 내용 추가 (personality + filter(제약조건) + grounding text(filename))
        SYSTEM_MSG = info.Personality + SYS_MSG_POLICY;

        if(File.Exists(info.FilePath))
        {
            string ground = File.ReadAllText(info.FilePath);
            SYSTEM_MSG += GROUND_DATA + ground;
        }
    }

    public void SetSystem(string sys_msg, string rag)
    {
        SYSTEM_MSG = sys_msg;
        RAG_MODEL = rag;
    }


    private IEnumerator ChatgptResponse(string prompt)
    {
        isProgress = true;
        //uiText.text = "답변을 생성중입니다...\n잠시만 기다려 주세요.";
        // json 전송 참고
        // https://kumgo1d.tistory.com/entry/Unity-HttpWebRequest%EC%99%80-JsonUtility%EB%A5%BC-%EC%82%AC%EC%9A%A9%ED%95%98%EC%97%AC-%EC%9B%B9-%EC%84%9C%EB%B2%84%EC%99%80-%ED%86%B5%EC%8B%A0%ED%95%98%EA%B3%A0-POST-%EB%B0%A9%EC%8B%9D%EC%9C%BC%EB%A1%9C-json-%EB%8D%B0%EC%9D%B4%ED%84%B0-%EA%B0%80%EC%A0%B8%EC%98%A4%EA%B8%B0

        string strBody = "";// = JsonUtility.ToJson(data);
        if(isRAG)
        {
            GPT_Data data = new GPT_Data();
            data.messages.Add(new GPT_Message("system", SYSTEM_MSG));
            data.messages.Add(new GPT_Message("user", prompt));

            data.azureSearchEndpoint = AzureUrls.SEARCH_URL;
            data.azureSearchKey = AzureUrls.SEARCH_KEY;
            data.azureSearchIndexName = RAG_MODEL;

            GPT_DataSrc dataSrc = new GPT_DataSrc();
            GPT_Params gptParams = new GPT_Params();
            gptParams.endpoint = AzureUrls.SEARCH_URL;
            gptParams.index_name = RAG_MODEL;
            gptParams.role_information = SYSTEM_MSG;
            GPT_Authentication auth = new GPT_Authentication();
            auth.key = AzureUrls.SEARCH_KEY;
            gptParams.authentication = auth;
            gptParams.key = AzureUrls.SEARCH_KEY;
            gptParams.indexName = RAG_MODEL;
            dataSrc.parameters = gptParams;

            data.data_sources.Add(dataSrc);

            strBody = JsonUtility.ToJson(data);
        }
        else
        {
            GPT_SimpleData data = new GPT_SimpleData();
            data.messages.Add(new GPT_Message("system", SYSTEM_MSG));
            if(charInfo != null && charInfo.Fewshots != null)
            {
                foreach(Fewshot temp in charInfo.Fewshots)
                {
                    data.messages.Add(new GPT_Message("user", temp.q));
                    data.messages.Add(new GPT_Message("assistant", temp.a));
                }
            }

            data.messages.Add(new GPT_Message("user", prompt));
            strBody = JsonUtility.ToJson(data);
            Debug.Log(strBody);
        }
        var bytes = System.Text.Encoding.UTF8.GetBytes(strBody);

        string url = AzureUrls.GPT_URL;
        UnityWebRequest request = new UnityWebRequest(url);
        request.method = UnityWebRequest.kHttpVerbPOST;
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("api-key", AzureUrls.GPT_KEY);
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
            if (response != string.Empty)
            {
                //Debug.Log(response);
                //response = Utils.RemoveSpecialChar(response);
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

    public void Stop()
    {
        StopAllCoroutines();
        isProgress = false;
    }
}
