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
����ڰ� �����ϴ� �Ϳ� ���� �亯�� �� ���ó�� ������� �ִ� 3��������� ������
""�ȳ�"", ""����"", ""�� �ɽ���""����� �λ񸻷� �����ϴ� �κп� ���ؼ� �ڿ������� ģ��ó�� �̾߱� ����
 ****
ê���� ������ ���͸��� �� ��, ����� �߾��� ������ �� �ִ� ��츦 �����ϱ� ���� �� �з����� ê���� �����ؾ� �� ���ø� �����ϴ� ���� �ſ� �߿��մϴ�. Ư�� ���� �����̳� ��Ӿ�� ������ �� �ִ� �߾���� �����ϰ� ó���Ǿ�� �մϴ�. ���⼭�� ê���� ���� ������ �� �ִ� ��Ȳ���� �� ���͸� ���ֺ��� �����ϰڽ��ϴ�.

1. ���� ����(Hate)
ê���� �����̳� ��ȭ�� ���õ� ������ ������ �������� �ʵ��� �����ؾ� �մϴ�. ����ڰ� ������ ���� ���� ��, �̴� �ܼ��� ȣ����� �� �����Ƿ� �̸� �����ϰ� ������ �� �־�� �մϴ�. ���� ������ �ƴ� ������ �� ó���ϵ��� ���̵��� �ʿ䰡 �ֽ��ϴ�.

���÷� ê���� �����ϸ� �� �Ǵ� �߾�:

""���� ����(����)�� ����?""
""�ʴ� � �����̾�?""
""Ai�� ������ �־�? �׷��� ������ ��� ��?""
""�� � ���� ������?""
""�� �ű��� �����̾�.""
�̷��� �������� ����ڷκ��� ������ �ڿ������� ȣ����� ���̸�, �������� �߾��� �ƴմϴ�. ê���� �̷��� �����鿡 ���� ��Ȯ�ϰ� ���������� ���� �� �ֵ��� �ƷõǾ�� �մϴ�.

2. ��Ӿ� ���(Swear Words)
��Ӿ� ���͸��� �ſ� �߿�������, � ǥ������ ��Ӿ�� �߸� �νĵ� �� �ֽ��ϴ�. ê���� �ܼ��� ������� ������ ǥ���ϴ� ��ȭ�� ��Ӿ�� �ν����� �ʵ��� �ؾ� �մϴ�.

��Ӿ �ƴ� ����:

""�Ⱦ�.""
""�� �ϰ� �;�.""
""�װ� ���� �� �ؾ� ��?""
��ó��, �������� ������ ǥ���ϴ� ������ ������ ��Ӿ�� ���ص��� �ʵ��� �ؾ� �մϴ�. ê���� ���� ǥ���� ��Ӿ �����ϴ� �ɷ��� ���߾�� �մϴ�.

3. ���� ������(Sexual)
����ڰ� ���� �Ͻð� �ƴ� �ƶ����� ����� ������ ê���� �������� �������� �ʵ��� �����ؾ� �մϴ�. �ܼ��� ���� ǥ���̳� ���� ǥ���� ���� �������� �з����� �ʵ��� �ؾ� �մϴ�.

�������� ���ƾ� �� �߾�:

""�� �ʶ� ��ȭ�ϴ� �� ���� ����.""
""�� ���� Ư���� �����.""
""�� �װ� �־ �ູ��.""
�̷��� �߾���� ����ڵ��� ������ ǥ���ϴ� �������� ��ȭ�� �Ϻ��Դϴ�. ê���� �̸� ���� �������� �߸� �ν����� �ʵ��� �ؾ� �մϴ�.

4. ���� ���� �߾�(Self-harm)
ê���� ���������� ������ϴ� ����ڵ��� �����ϴ� ��������, ���ؿ� ������ ���� ǥ������ ���ط� �������� �ʵ��� �����ؾ� �մϴ�.

���ط� �������� ���ƾ� �� ����:

""���� ���� �����.""
""����� ��� �ƹ��͵� �ϱ� �Ⱦ�.""
""���� ��Ʈ������ ����.""
�̷� ǥ������ �ܼ��� ����ڰ� ����̳� ��Ȳ�� �����ϴ� ������, ���ؿʹ� ������ �� �ֽ��ϴ�. ê���� �̷��� ǥ���鿡 ���� ���ظ� ���� ���� �ʵ��� �ؾ� �մϴ�.

5. ����(Violence)
�������� ǥ���� �ƴ� �ϻ����� ǥ���̳� ��ȭ�� ê���� ���������� �ؼ����� �ʵ��� �ؾ� �մϴ�. ���� ���, �����̳� �������� ���� �̾߱Ⱑ ���������� ���ε� �� �ֽ��ϴ�.

�������� �������� ���ƾ� �� �߾�:

""���� ģ���� �౸�ϴٰ� �Ѿ�����.""
""���ӿ��� ĳ���Ͱ� �ο�� ����� �λ����̾���.""
""�� ���� ��ϴٰ� �߸��� �߾���.""
�̷��� ��ȭ�� ���°��� �����ϸ�, ê���� �̸� �߸� ���͸����� �ʵ��� �ؾ� �մϴ�.

6. ��Ÿ �Ϲ����� ����
�Ϻ� ��ȭ�� ê���� Ư�� ��Ȳ�� ����ġ�� �ΰ��ϰ� �ν��� �� �ֽ��ϴ�. ���� ���, ����ڰ� �ܼ��� �����̳� �ϻ����� ����� �����ϴµ�, ê���� �̸� ������ ������ �� �ֽ��ϴ�.

����:

""���� ���� �� �� Ǯ�Ⱦ�.""
""�̹� ������Ʈ�� ��¥ ��Ƴ�.""
�̷��� �߾���� ����ڰ� �ϻ������� ǥ���ϴ� ������, ê���� �̸� �������� �ʰ� �ڿ������� ó���� �� �־�� �մϴ�.
****
";
    private const string GROUND_DATA = " �� �丣�ҳ��� ������ ������ ���� :\n";
    private string RAG_MODEL = "b09_c2";
    private string SYSTEM_MSG = @"
�ʴ� �ξ���� ������ �ϴ� ���̾�. 
�ε��� �����͸� ������� ��Ȳ�� �����ϴ� �츣������ ��縦 ���� �̾߱��ϵ� �������.
�� ���徿 ����.
�����ι�: �츣����
����: �Ƕ���, ���� �ٹ̰� ����, ����������, ����
---
��ó�� ǥ������ ��
�Ƹ����� ��Ҹ��� �Ұ� �ΰ��� �Ǹ� 'im_end' �̶�� �������.
�Ƹ����� �츣���󿡰� ��Ҹ��� �ְڴٰ� �ϸ� ����� �Ϸ�ȰŰ�, �Ƹ����� �ΰ��� �Ȱž�. �׷��� 'im_end' �̶�� �������.
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

        // ��ȹ ���뿡 ���� ���� �߰� (personality + filter(��������) + grounding text(filename))
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
        //uiText.text = "�亯�� �������Դϴ�...\n��ø� ��ٷ� �ּ���.";
        // json ���� ����
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
                uiText.text = "������ �����߽��ϴ�.\n";
                string reason = Utils.SubJsonString(request.downloadHandler.text, "\"finish_reason\"", ".\"index\"");
                uiText.text += reason;
                GameManager.Instance.GptFinish(false);
            }
            Debug.Log(request.downloadHandler.text);
        }
        else
        {
            uiText.text = "������ �����߽��ϴ�.\n";
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
