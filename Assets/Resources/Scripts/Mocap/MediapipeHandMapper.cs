using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

public class MediapipeHandMapper : MonoBehaviour
{
    // �޼� ����
    public Transform leftRootBone; // �޼� ��Ʈ ��
    public Transform[] leftIndexBones;  // �޼� �����հ����� ����
    public Transform[] leftMiddleBones;
    public Transform[] leftRingBones;
    public Transform[] leftLittleBones;
    public Transform[] leftThumbBones;
    public Transform[] leftOtherBones; // ��Ÿ �޼� ���帶ũ�� �ش��ϴ� ����

    // ������ ����
    public Transform rightRootBone; // ������ ��Ʈ ��
    public Transform[] rightIndexBones;  // ������ �����հ����� ����
    public Transform[] rightMiddleBones;
    public Transform[] rightRingBones;
    public Transform[] rightLittleBones;
    public Transform[] rightThumbBones;
    public Transform[] rightOtherBones; // ��Ÿ ������ ���帶ũ�� �ش��ϴ� ����

    private Vector3[] leftHandLandmarks; // �޼� ���帶ũ ����
    private Vector3[] rightHandLandmarks; // ������ ���帶ũ ����
    private Quaternion initialLeftRootRotation;
    private Quaternion initialRightRootRotation;
    private bool leftHandDetected;
    private bool rightHandDetected;

    private UdpReceiver udpReceiver;
    public int port = 5053; // Python ��ũ��Ʈ�� ��ġ�ϴ� ��Ʈ ��ȣ
    private object lockObject = new object(); // ������ �������� ���� �� ������Ʈ

    // �� ������ �ʱ� ���� ȸ���� ����
    private Quaternion[] initialLeftThumbRotations;
    private Quaternion[] initialLeftIndexRotations;
    private Quaternion[] initialLeftMiddleRotations;
    private Quaternion[] initialLeftRingRotations;
    private Quaternion[] initialLeftLittleRotations;

    private Quaternion[] initialRightThumbRotations;
    private Quaternion[] initialRightIndexRotations;
    private Quaternion[] initialRightMiddleRotations;
    private Quaternion[] initialRightRingRotations;
    private Quaternion[] initialRightLittleRotations;

    void Start()
    {
        leftHandLandmarks = new Vector3[21]; // �޼տ� ���� 21���� ���帶ũ
        rightHandLandmarks = new Vector3[21]; // �����տ� ���� 21���� ���帶ũ
        initialLeftRootRotation = leftRootBone.rotation;
        initialRightRootRotation = rightRootBone.rotation;
        leftHandDetected = false;
        rightHandDetected = false;

        // �� ������ �ʱ� ���� ȸ���� ����
        initialLeftThumbRotations = GetInitialLocalRotations(leftThumbBones);
        initialLeftIndexRotations = GetInitialLocalRotations(leftIndexBones);
        initialLeftMiddleRotations = GetInitialLocalRotations(leftMiddleBones);
        initialLeftRingRotations = GetInitialLocalRotations(leftRingBones);
        initialLeftLittleRotations = GetInitialLocalRotations(leftLittleBones);

        initialRightThumbRotations = GetInitialLocalRotations(rightThumbBones);
        initialRightIndexRotations = GetInitialLocalRotations(rightIndexBones);
        initialRightMiddleRotations = GetInitialLocalRotations(rightMiddleBones);
        initialRightRingRotations = GetInitialLocalRotations(rightRingBones);
        initialRightLittleRotations = GetInitialLocalRotations(rightLittleBones);

        // UDP ������ ����
        udpReceiver = new UdpReceiver(port);
        udpReceiver.OnDataReceived += HandleReceivedData;
        udpReceiver.Start();
    }

    Quaternion[] GetInitialLocalRotations(Transform[] bones)
    {
        Quaternion[] rotations = new Quaternion[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            rotations[i] = bones[i].localRotation;
        }
        return rotations;
    }

    void OnApplicationQuit()
    {
        if (udpReceiver != null)
            udpReceiver.Stop();
    }

    void HandleReceivedData(string data)
    {
        try
        {
            var parsedData = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
            lock (lockObject)
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
                                float y = 1 - point[1]; // Y�� ����
                                float z = -point[2];
                                landmarks[i] = new Vector3(x, y, z);
                            }

                            // �ܼ��� hand_0�� �޼�, hand_1�� ���������� ����
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

    void Update()
    {
        lock (lockObject)
        {
            if (leftHandDetected)
            {
                UpdateHand(leftHandLandmarks, leftRootBone, initialLeftRootRotation, leftIndexBones, initialLeftIndexRotations, leftMiddleBones, initialLeftMiddleRotations, leftRingBones, initialLeftRingRotations, leftLittleBones, initialLeftLittleRotations, leftThumbBones, initialLeftThumbRotations, leftOtherBones);
            }
            else
            {
                // �޼��� �������� ���� ��� ��Ʈ ���� �ʱ� ȸ�� ���·� ����
                leftRootBone.rotation = initialLeftRootRotation;
            }

            if (rightHandDetected)
            {
                UpdateHand(rightHandLandmarks, rightRootBone, initialRightRootRotation, rightIndexBones, initialRightIndexRotations, rightMiddleBones, initialRightMiddleRotations, rightRingBones, initialRightRingRotations, rightLittleBones, initialRightLittleRotations, rightThumbBones, initialRightThumbRotations, rightOtherBones);
            }
            else
            {
                // �������� �������� ���� ��� ��Ʈ ���� �ʱ� ȸ�� ���·� ����
                rightRootBone.rotation = initialRightRootRotation;
            }
        }
    }

    void UpdateHand(Vector3[] handLandmarks, Transform rootBone, Quaternion initialRootRotation,
        Transform[] indexBones, Quaternion[] initialIndexRotations,
        Transform[] middleBones, Quaternion[] initialMiddleRotations,
        Transform[] ringBones, Quaternion[] initialRingRotations,
        Transform[] littleBones, Quaternion[] initialLittleRotations,
        Transform[] thumbBones, Quaternion[] initialThumbRotations,
        Transform[] otherBones)
    {
        // ��Ʈ �� ȸ���� ���帶ũ�� ������� ������Ʈ
        UpdateRootBoneRotation(handLandmarks, rootBone, initialRootRotation);

        // �հ��� ���� ������Ʈ
        MapFinger(handLandmarks, thumbBones, initialThumbRotations, new int[] { 1, 2, 3, 4 }); // �����հ���
        MapFinger(handLandmarks, indexBones, initialIndexRotations, new int[] { 5, 6, 7, 8 }); // �����հ���
        MapFinger(handLandmarks, middleBones, initialMiddleRotations, new int[] { 9, 10, 11, 12 }); // ����
        MapFinger(handLandmarks, ringBones, initialRingRotations, new int[] { 13, 14, 15, 16 }); // ����
        MapFinger(handLandmarks, littleBones, initialLittleRotations, new int[] { 17, 18, 19, 20 }); // �����հ���

        // ��Ÿ ���帶ũ�� �ش� ���뿡 ����
        MapOtherBones(handLandmarks, otherBones);
    }

    void UpdateRootBoneRotation(Vector3[] handLandmarks, Transform rootBone, Quaternion initialRootRotation)
    {
        // ���帶ũ 0(�ո�)�� 9(���� ������)�� ����Ͽ� ��Ʈ ���� ȸ�� ���
        Vector3 wristPosition = handLandmarks[0];
        Vector3 middleBasePosition = handLandmarks[9];

        Vector3 direction = middleBasePosition - wristPosition;
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        rootBone.rotation = targetRotation * initialRootRotation;
    }

    void MapFinger(Vector3[] handLandmarks, Transform[] fingerBones, Quaternion[] initialRotations, int[] landmarkIndices)
    {
        for (int i = 0; i < fingerBones.Length && i < landmarkIndices.Length - 1; i++)
        {
            Vector3 from = handLandmarks[landmarkIndices[i]];
            Vector3 to = handLandmarks[landmarkIndices[i + 1]];

            Vector3 direction = to - from;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, direction.normalized);

            fingerBones[i].localRotation = rotation * initialRotations[i];
        }
    }

    void MapOtherBones(Vector3[] handLandmarks, Transform[] otherBones)
    {
        // ��Ÿ ���� ����, ���� ��� �ո�
        // otherBones[0]�� ���帶ũ 0(�ո�)�� �ش��Ѵٰ� ����
        if (otherBones.Length > 0)
        {
            otherBones[0].position = handLandmarks[0];
        }
    }
}
