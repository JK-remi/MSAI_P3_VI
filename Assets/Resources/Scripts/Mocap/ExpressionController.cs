using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

public class ExpressionController : MonoBehaviour
{
    public SkinnedMeshRenderer faceMeshRenderer;
    private UdpReceiver udpReceiver;

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

    public void Activate(UdpReceiver receiver)
    {
        udpReceiver = receiver;
    }

    public void Deactivate()
    {
        udpReceiver = null;
    }

    void Update()
    {
        if (udpReceiver == null) return;

        lock (MediapipeManager.Instance.faceLockObj)
        {
            if (MediapipeManager.Instance.newExpressionData)
            {
                ApplyBlendShapeWeights(MediapipeManager.Instance.newBlendShapeWeights);
                MediapipeManager.Instance.newExpressionData = false;
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
}
