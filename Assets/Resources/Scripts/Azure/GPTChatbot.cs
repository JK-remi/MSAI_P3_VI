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
        // Azure OpenAI API 설정
        [Header("Azure OpenAI API Settings")]
        [SerializeField]
        private const string apiUrl = "https://eueastproject3-team2.openai.azure.com/openai/deployments/project3-team2-gpt-4o/chat/completions?api-version=2024-02-15-preview";
        [SerializeField]
        private const string apiKey = "a83ed49c38b54298bb690a721a87599b"; // 실제 API 키로 대체하세요.

        // 프리팹 및 컨테이너 연결
        [Header("Prefabs and Containers")]
        public GameObject msgSendPrefab;       // Msg_Send 프리팹
        public GameObject msgResponsePrefab;   // Msg_Response 프리팹
        public Transform messageContainer;     // 메시지들이 담길 부모 컨테이너

        // UI 요소 연결
        [Header("UI Elements")]
        public TMP_InputField inputField;      // 사용자 입력 필드
        public Button Btn_Play;                // 전송 버튼

        // 대화 기록 저장
        private List<GPT_Message> messageHistory = new List<GPT_Message>();

        // 요청 진행 중 여부 체크
        private bool isRequestInProgress = false;

        // Start 함수
        void Start()
        {
            // 시스템 메시지 설정 (옵션)
            GPT_Message systemMessage = new GPT_Message("system", "당신은 친절한 챗봇입니다. 사용자에게 도움이 되는 답변을 제공하세요.");
            messageHistory.Add(systemMessage);

            // 버튼 클릭 이벤트에 함수 연결
            Btn_Play.onClick.AddListener(OnPlayButtonClicked);
        }

        // 전송 버튼 클릭 시 호출되는 함수
        public void OnPlayButtonClicked()
        {
            string userInput = inputField.text;
            if (!string.IsNullOrEmpty(userInput) && !isRequestInProgress)
            {
                // 사용자 메시지를 대화 기록에 추가
                GPT_Message userMessage = new GPT_Message("user", userInput);
                messageHistory.Add(userMessage);

                // 사용자 메시지 오브젝트 생성
                GameObject newMsgSend = Instantiate(msgSendPrefab, messageContainer);
                MsgSend msgSendComponent = newMsgSend.GetComponent<MsgSend>();
                msgSendComponent.msgText.text = userInput;

                // 입력 필드 초기화
                inputField.text = "";

                // GPT 요청 코루틴 시작
                StartCoroutine(SendRequestToGPT());
            }
        }

        // GPT에 요청을 보내는 코루틴
        private IEnumerator SendRequestToGPT()
        {
            isRequestInProgress = true;

            // 로딩 상태 표시를 위한 로딩 메시지 생성
            //GameObject loadingMsgResponse = Instantiate(msgResponsePrefab, messageContainer);
            //MsgResponse loadingMsgComponent = loadingMsgResponse.GetComponent<MsgResponse>();
            //loadingMsgComponent.msgText.text = "챗봇이 응답 중입니다...";

            // 요청 데이터 생성
            GPT_Data requestData = new GPT_Data();
            requestData.messages = messageHistory;

            // JSON 직렬화
            string jsonData = JsonUtility.ToJson(requestData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            // UnityWebRequest 설정
            UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            // 요청 헤더 설정
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("api-key", apiKey);

            // 요청 보내기
            yield return request.SendWebRequest();

            // 응답 처리
            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;

                // 로딩 메시지를 삭제
                //Destroy(loadingMsgResponse);

                // 새로운 응답 메시지 생성
                ProcessResponse(responseText);
            }
            else
            {
                Debug.LogError("Error: " + request.error);

                // 로딩 메시지의 텍스트를 에러 메시지로 업데이트
                //loadingMsgComponent.msgText.text = $"에러 발생: {request.error}";
            }

            isRequestInProgress = false;
        }


        // 응답 처리 함수
        private void ProcessResponse(string jsonResponse)
        {
            // JSON 파싱
            GPT_Response response = JsonUtility.FromJson<GPT_Response>(jsonResponse);

            if (response != null && response.choices != null && response.choices.Count > 0)
            {
                string assistantMessage = response.choices[0].message.content.Trim();

                // 대화 기록에 AI의 응답 추가
                GPT_Message assistantMessageObj = new GPT_Message("assistant", assistantMessage);
                messageHistory.Add(assistantMessageObj);

                // 새로운 응답 메시지 오브젝트 생성
                GameObject newMsgResponse = Instantiate(msgResponsePrefab, messageContainer);
                MsgResponse msgResponseComponent = newMsgResponse.GetComponent<MsgResponse>();
                msgResponseComponent.msgText.text = assistantMessage;
            }
            else
            {
                Debug.LogError("응답 파싱 중 오류 발생");
                // 에러 메시지 표시를 위해 별도의 처리 가능
            }
        }
    }


    // 추가적인 데이터 클래스 정의
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