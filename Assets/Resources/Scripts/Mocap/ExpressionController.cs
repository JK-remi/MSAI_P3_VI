using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

public class ExpressionController : MonoBehaviour
{
    public SkinnedMeshRenderer faceMeshRenderer;
    private UdpReceiver udpReceiver;
    private const int port = 5054;

    private readonly object expressionLock = new object();
    private Dictionary<string, float> newBlendShapeWeights = new Dictionary<string, float>();
    private bool newExpressionData = false;

    // 블렌드 쉐이프 이름과 인덱스 매핑
    private Dictionary<string, int> blendShapeNameToIndex = new Dictionary<string, int>()
    {
        // 표정
        {"neutral", 0},    // Fcl_ALL_Neutral
        {"angry", 1},      // Fcl_ALL_Angry
        {"fun", 2},        // Fcl_ALL_Fun
        {"joy", 3},        // Fcl_ALL_Joy
        {"sorrow", 4},     // Fcl_ALL_Sorrow
        {"surprised", 5},  // Fcl_ALL_Surprised
        // 눈 깜빡임
        {"eyeBlinkRight", 14}, // Fcl_EYE_Close_R
        {"eyeBlinkLeft", 15},  // Fcl_EYE_Close_L
        // 입 모양
        {"mouthShapeA", 39}, // Fcl_MTH_A
        {"mouthShapeI", 40}, // Fcl_MTH_I
        {"mouthShapeU", 41}, // Fcl_MTH_U
        {"mouthShapeE", 42}, // Fcl_MTH_E
        {"mouthShapeO", 43}, // Fcl_MTH_O
    };

    void Start()
    {
        udpReceiver = new UdpReceiver(port);
        udpReceiver.OnDataReceived += HandleReceivedData;
        udpReceiver.Start();
    }

    void OnDestroy()
    {
        if (udpReceiver != null)
        {
            udpReceiver.Stop();
            udpReceiver = null;
        }
    }

    void HandleReceivedData(string data)
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

            lock (expressionLock)
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

    void Update()
    {
        lock (expressionLock)
        {
            if (newExpressionData)
            {
                ApplyBlendShapeWeights(newBlendShapeWeights);
                newExpressionData = false;
            }
        }
    }

    void ApplyBlendShapeWeights(Dictionary<string, float> weights)
    {
        if (faceMeshRenderer != null)
        {
            // 필요한 블렌드 쉐이프의 가중치를 0으로 초기화
            foreach (var index in blendShapeNameToIndex.Values)
            {
                faceMeshRenderer.SetBlendShapeWeight(index, 0f);
            }

            // 받은 가중치 적용
            foreach (var kvp in weights)
            {
                string blendShapeName = kvp.Key;
                float weight = kvp.Value * 100f; // Unity에서는 0~100 범위를 사용

                if (blendShapeNameToIndex.TryGetValue(blendShapeName, out int blendShapeIndex))
                {
                    faceMeshRenderer.SetBlendShapeWeight(blendShapeIndex, weight);
                    // Debug.Log($"블렌드 쉐이프 '{blendShapeName}' (인덱스 {blendShapeIndex}) 가중치 설정: {weight}");
                }
                else
                {
                    Debug.LogWarning($"BlendShape '{blendShapeName}'에 대한 인덱스를 찾을 수 없습니다.");
                }
            }
        }
        else
        {
            Debug.LogError("SkinnedMeshRenderer가 할당되지 않았습니다.");
        }
    }

    void OnApplicationQuit()
    {
        if (udpReceiver != null)
            udpReceiver.Stop();
    }
}
