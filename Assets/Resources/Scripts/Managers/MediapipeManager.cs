using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MediapipeManager : MonoBehaviour
{
    private static MediapipeManager _instance = null;
    public static MediapipeManager Instance
    {
        get
        {
            _instance = GameObject.FindObjectOfType<MediapipeManager>();
            if (_instance == null)
            {
                GameObject container = new GameObject("MediapipeManager");
                _instance = container.AddComponent<MediapipeManager>();
            }

            return _instance;
        }
    }

    private const int BODY_PORT = 5052;
    private const int HAND_PORT = 5053;
    private const int FACE_PORT = 5054;

    public UdpReceiver bodyUdpRecv;
    public UdpReceiver handUdpRecv;
    public UdpReceiver faceUdpRecv;

    public Dictionary<eLandmark, Vector3> bodyLandmarks = new Dictionary<eLandmark, Vector3>();

    public object handLockObj = new object();
    public Vector3[] leftHandLandmarks;
    public Vector3[] rightHandLandmarks;
    public bool leftHandDetected;
    public bool rightHandDetected;

    public readonly object faceLockObj = new object();
    public Dictionary<string, float> newBlendShapeWeights = new Dictionary<string, float>();
    public bool newExpressionData = false;

    private void Awake()
    {
        ActivateUdpReceiver(BODY_PORT, HandleReceivedData_Body, out bodyUdpRecv);
        ActivateUdpReceiver(HAND_PORT, HandleReceivedData_Hand, out handUdpRecv);
        ActivateUdpReceiver(FACE_PORT, HandleReceivedData_Face, out faceUdpRecv);

        if(curCharacter != null)
        {
            ActivateCharacter(curCharacter);
        }
    }

    private void ActivateUdpReceiver(int port, UdpReceiver.DataReceivedHandler handler, out UdpReceiver receiver)
    {
        receiver = new UdpReceiver(port);
        receiver.OnDataReceived += handler;
        receiver.Start();
    }

    void HandleReceivedData_Body(string data)
    {
        string json = data;

        // Debug.Log("Received data: " + json);

        try
        {
            Dictionary<string, object> receivedDatas = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            List<List<float>> receivedLandmarks = null;
            if (receivedDatas.ContainsKey("pose"))
            {
                receivedLandmarks = JsonConvert.DeserializeObject<List<List<float>>>(receivedDatas["pose"].ToString());
            }

            if (receivedLandmarks != null)
            {
                lock (bodyLandmarks)
                {
                    bodyLandmarks.Clear(); // 이전 데이터를 지웁니다.
                    for (int i = 0; i < receivedLandmarks.Count; i++)
                    {
                        List<float> kvp = receivedLandmarks[i];

                        // 좌표 변환 (Y축 반전)
                        float x = kvp[0];
                        float y = -kvp[1];
                        float z = kvp[2];
                        Vector3 position = new Vector3(x, y, z);

                        eLandmark landmarkKey = (eLandmark)i;
                        bodyLandmarks[landmarkKey] = position;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error parsing data: " + ex.Message);
        }
    }
    void HandleReceivedData_Hand(string data)
    {
        try
        {
            var parsedData = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
            lock (handLockObj)
            {
                leftHandDetected = false;
                rightHandDetected = false;

                foreach (var key in parsedData.Keys)
                {
                    if (key.StartsWith("hand_"))
                    {
                        var handDataRaw = parsedData[key];
                        var handDataJson = handDataRaw.ToString();

                        List<List<float>> handData = JsonConvert.DeserializeObject<List<List<float>>>(handDataJson);

                        if (handData.Count == 21)
                        {
                            Vector3[] landmarks = new Vector3[21];
                            for (int i = 0; i < 21; i++)
                            {
                                var point = handData[i];
                                float x = point[0];
                                float y = 1 - point[1];
                                float z = -point[2];
                                landmarks[i] = new Vector3(x, y, z);
                            }

                            if (key == "hand_0")
                            {
                                leftHandLandmarks = landmarks;
                                leftHandDetected = true;
                            }
                            else if (key == "hand_1")
                            {
                                rightHandLandmarks = landmarks;
                                rightHandDetected = true;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to parse JSON: " + ex.Message);
        }
    }
    void HandleReceivedData_Face(string data)
    {
        try
        {
            Dictionary<string, object> receivedData = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);

            Dictionary<string, float> receivedWeights = new Dictionary<string, float>();

            if (receivedData.ContainsKey("BlendShapeWeights"))
            {
                var blendShapeWeightsJson = receivedData["BlendShapeWeights"].ToString();
                Dictionary<string, float> blendShapeWeights = JsonConvert.DeserializeObject<Dictionary<string, float>>(blendShapeWeightsJson);

                foreach (var kvp in blendShapeWeights)
                {
                    receivedWeights[kvp.Key] = kvp.Value;
                }
            }

            if (receivedData.ContainsKey("blendshapes"))
            {
                var blendshapesJson = receivedData["blendshapes"].ToString();
                Dictionary<string, float> blendshapes = JsonConvert.DeserializeObject<Dictionary<string, float>>(blendshapesJson);

                foreach (var kvp in blendshapes)
                {
                    // 입 모양에 필요한 키만 추출
                    if (kvp.Key == "eyeBlinkLeft" || kvp.Key == "eyeBlinkRight" ||
                        kvp.Key == "mouthShapeA" || kvp.Key == "mouthShapeI" ||
                        kvp.Key == "mouthShapeU" || kvp.Key == "mouthShapeE" ||
                        kvp.Key == "mouthShapeO")
                    {
                        receivedWeights[kvp.Key] = kvp.Value;
                    }
                }
            }

            lock (faceLockObj)
            {
                newBlendShapeWeights = receivedWeights;
                newExpressionData = true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("블렌드 쉐이프 가중치 데이터 파싱 중 오류 발생: " + ex.Message);
        }
    }

    public GameObject curCharacter;
    public void ActivateCharacter(GameObject go)
    {
        UnityChanPoseController body = go.GetComponent<UnityChanPoseController>();
        if (body) body.Activate(bodyUdpRecv);
        MediapipeHandMapper hand = go.GetComponent<MediapipeHandMapper>();
        if (hand) hand.Activate(handUdpRecv);
        ExpressionController face = go.GetComponent<ExpressionController>();
        if (face) face.Activate(faceUdpRecv);

        curCharacter = go;
    }

    public void DeActivateCharacter(GameObject go)
    {
        UnityChanPoseController body = go.GetComponent<UnityChanPoseController>();
        if (body) body.Deactivate();
        MediapipeHandMapper hand = go.GetComponent<MediapipeHandMapper>();
        if (hand) hand.Deactivate();
        ExpressionController face = go.GetComponent<ExpressionController>();
        if (face) face.Deactivate();
    }

    void OnApplicationQuit()
    {
        if (bodyUdpRecv != null) bodyUdpRecv.Stop();
        if (handUdpRecv != null) handUdpRecv.Stop();
        if (faceUdpRecv != null) faceUdpRecv.Stop();
    }
}
