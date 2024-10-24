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

    // ���� ������ �̸��� �ε��� ����
    private Dictionary<string, int> blendShapeNameToIndex = new Dictionary<string, int>()
    {
        // ǥ��
        {"neutral", 0},    // Fcl_ALL_Neutral
        {"angry", 1},      // Fcl_ALL_Angry
        {"fun", 2},        // Fcl_ALL_Fun
        {"joy", 3},        // Fcl_ALL_Joy
        {"sorrow", 4},     // Fcl_ALL_Sorrow
        {"surprised", 5},  // Fcl_ALL_Surprised
        // �� ������
        {"eyeBlinkRight", 14}, // Fcl_EYE_Close_R
        {"eyeBlinkLeft", 15},  // Fcl_EYE_Close_L
        // �� ���
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
                    // �� ��翡 �ʿ��� Ű�� ����
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
            Debug.LogError("���� ������ ����ġ ������ �Ľ� �� ���� �߻�: " + ex.Message);
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
            // �ʿ��� ���� �������� ����ġ�� 0���� �ʱ�ȭ
            foreach (var index in blendShapeNameToIndex.Values)
            {
                faceMeshRenderer.SetBlendShapeWeight(index, 0f);
            }

            // ���� ����ġ ����
            foreach (var kvp in weights)
            {
                string blendShapeName = kvp.Key;
                float weight = kvp.Value * 100f; // Unity������ 0~100 ������ ���

                if (blendShapeNameToIndex.TryGetValue(blendShapeName, out int blendShapeIndex))
                {
                    faceMeshRenderer.SetBlendShapeWeight(blendShapeIndex, weight);
                    // Debug.Log($"���� ������ '{blendShapeName}' (�ε��� {blendShapeIndex}) ����ġ ����: {weight}");
                }
                else
                {
                    Debug.LogWarning($"BlendShape '{blendShapeName}'�� ���� �ε����� ã�� �� �����ϴ�.");
                }
            }
        }
        else
        {
            Debug.LogError("SkinnedMeshRenderer�� �Ҵ���� �ʾҽ��ϴ�.");
        }
    }

    void OnApplicationQuit()
    {
        if (udpReceiver != null)
            udpReceiver.Stop();
    }
}
