using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System.Text;

namespace chatbot1

{

    public class GPTChatbot : MonoBehaviour
    {
        // Azure OpenAI API ����
        [Header("Azure OpenAI API Settings")]
        [SerializeField]
        private const string apiUrl = "https://eueastproject3-team2.openai.azure.com/openai/deployments/project3-team2-gpt-4o/chat/completions?api-version=2024-02-15-preview";
        [SerializeField]
        private const string apiKey = "a83ed49c38b54298bb690a721a87599b"; // ���� API Ű�� ��ü�ϼ���.

        // ������ �� �����̳� ����
        [Header("Prefabs and Containers")]
        public GameObject msgSendPrefab;       // Msg_Send ������
        public GameObject msgResponsePrefab;   // Msg_Response ������
        public Transform messageContainer;     // �޽������� ��� �θ� �����̳�

        // UI ��� ����
        [Header("UI Elements")]
        public TMP_InputField inputField;      // ����� �Է� �ʵ�
        public Button Btn_Play;                // ���� ��ư

        // ��ȭ ��� ����
        private List<GPT_Message> messageHistory = new List<GPT_Message>();

        // ��û ���� �� ���� üũ
        private bool isRequestInProgress = false;

        // Start �Լ�
        void Start()
        {
            // �ý��� �޽��� ���� (�ɼ�)
            GPT_Message systemMessage = new GPT_Message("system", "����� ģ���� ê���Դϴ�. ����ڿ��� ������ �Ǵ� �亯�� �����ϼ���.");
            messageHistory.Add(systemMessage);

            // ��ư Ŭ�� �̺�Ʈ�� �Լ� ����
            Btn_Play.onClick.AddListener(OnPlayButtonClicked);
        }

        // ���� ��ư Ŭ�� �� ȣ��Ǵ� �Լ�
        public void OnPlayButtonClicked()
        {
            string userInput = inputField.text;
            if (!string.IsNullOrEmpty(userInput) && !isRequestInProgress)
            {
                // ����� �޽����� ��ȭ ��Ͽ� �߰�
                GPT_Message userMessage = new GPT_Message("user", userInput);
                messageHistory.Add(userMessage);

                // ����� �޽��� ������Ʈ ����
                GameObject newMsgSend = Instantiate(msgSendPrefab, messageContainer);
                MsgSend msgSendComponent = newMsgSend.GetComponent<MsgSend>();
                msgSendComponent.msgText.text = userInput;

                // �Է� �ʵ� �ʱ�ȭ
                inputField.text = "";

                // GPT ��û �ڷ�ƾ ����
                StartCoroutine(SendRequestToGPT());
            }
        }

        // GPT�� ��û�� ������ �ڷ�ƾ
        private IEnumerator SendRequestToGPT()
        {
            isRequestInProgress = true;

            // �ε� ���� ǥ�ø� ���� �ε� �޽��� ����
            //GameObject loadingMsgResponse = Instantiate(msgResponsePrefab, messageContainer);
            //MsgResponse loadingMsgComponent = loadingMsgResponse.GetComponent<MsgResponse>();
            //loadingMsgComponent.msgText.text = "ê���� ���� ���Դϴ�...";

            // ��û ������ ����
            GPT_Data requestData = new GPT_Data();
            requestData.messages = messageHistory;

            // JSON ����ȭ
            string jsonData = JsonUtility.ToJson(requestData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            // UnityWebRequest ����
            UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            // ��û ��� ����
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("api-key", apiKey);

            // ��û ������
            yield return request.SendWebRequest();

            // ���� ó��
            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;

                // �ε� �޽����� ����
                //Destroy(loadingMsgResponse);

                // ���ο� ���� �޽��� ����
                ProcessResponse(responseText);
            }
            else
            {
                Debug.LogError("Error: " + request.error);

                // �ε� �޽����� �ؽ�Ʈ�� ���� �޽����� ������Ʈ
                //loadingMsgComponent.msgText.text = $"���� �߻�: {request.error}";
            }

            isRequestInProgress = false;
        }


        // ���� ó�� �Լ�
        private void ProcessResponse(string jsonResponse)
        {
            // JSON �Ľ�
            GPT_Response response = JsonUtility.FromJson<GPT_Response>(jsonResponse);

            if (response != null && response.choices != null && response.choices.Count > 0)
            {
                string assistantMessage = response.choices[0].message.content.Trim();

                // ��ȭ ��Ͽ� AI�� ���� �߰�
                GPT_Message assistantMessageObj = new GPT_Message("assistant", assistantMessage);
                messageHistory.Add(assistantMessageObj);

                // ���ο� ���� �޽��� ������Ʈ ����
                GameObject newMsgResponse = Instantiate(msgResponsePrefab, messageContainer);
                MsgResponse msgResponseComponent = newMsgResponse.GetComponent<MsgResponse>();
                msgResponseComponent.msgText.text = assistantMessage;
            }
            else
            {
                Debug.LogError("���� �Ľ� �� ���� �߻�");
                // ���� �޽��� ǥ�ø� ���� ������ ó�� ����
            }
        }
    }


    // �߰����� ������ Ŭ���� ����
    [System.Serializable]
    public class GPT_Message
    {
        public string role;
        public string content;

        public GPT_Message(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }

    [System.Serializable]
    public class GPT_Data
    {
        public List<GPT_Message> messages;
        public float temperature = 0.7f;
        public float top_p = 0.95f;
        public int max_tokens = 800;
    }

    [System.Serializable]
    public class GPT_Response
    {
        public List<GPT_Choice> choices;
    }

    [System.Serializable]
    public class GPT_Choice
    {
        public GPT_Message message;
    }

}