using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

public class MediapipeHandMapper : MonoBehaviour
{
    public Transform leftRootBone; // 왼손 루트 본
    public Transform[] leftIndexBones;
    public Transform[] leftMiddleBones;
    public Transform[] leftRingBones;
    public Transform[] leftLittleBones;
    public Transform[] leftThumbBones;
    public Transform[] leftOtherBones;

    public Transform rightRootBone;
    public Transform[] rightIndexBones;
    public Transform[] rightMiddleBones;
    public Transform[] rightRingBones;
    public Transform[] rightLittleBones;
    public Transform[] rightThumbBones;
    public Transform[] rightOtherBones;

    private Vector3[] leftHandLandmarks;
    private Vector3[] rightHandLandmarks;
    private Quaternion initialLeftRootRotation;
    private Quaternion initialRightRootRotation;
    private bool leftHandDetected;
    private bool rightHandDetected;

    private UdpReceiver udpReceiver;
    public int port = 5053;
    private object lockObject = new object();

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
        leftHandLandmarks = new Vector3[21];
        rightHandLandmarks = new Vector3[21];
        initialLeftRootRotation = leftRootBone.rotation;
        initialRightRootRotation = rightRootBone.rotation;
        leftHandDetected = false;
        rightHandDetected = false;

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
                leftRootBone.rotation = initialLeftRootRotation;
            }

            if (rightHandDetected)
            {
                UpdateHand(rightHandLandmarks, rightRootBone, initialRightRootRotation, rightIndexBones, initialRightIndexRotations, rightMiddleBones, initialRightMiddleRotations, rightRingBones, initialRightRingRotations, rightLittleBones, initialRightLittleRotations, rightThumbBones, initialRightThumbRotations, rightOtherBones);
            }
            else
            {
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
        // 손목 회전 업데이트
        UpdateRootBoneRotation(handLandmarks, rootBone, initialRootRotation);

        // 손가락 뼈대 업데이트
        MapFinger(handLandmarks, thumbBones, new int[] { 1, 2, 3, 4 }); // 엄지손가락
        MapFinger(handLandmarks, indexBones, new int[] { 5, 6, 7, 8 }); // 검지손가락
        MapFinger(handLandmarks, middleBones, new int[] { 9, 10, 11, 12 }); // 중지
        MapFinger(handLandmarks, ringBones, new int[] { 13, 14, 15, 16 }); // 약지
        MapFinger(handLandmarks, littleBones, new int[] { 17, 18, 19, 20 }); // 새끼손가락

        // 기타 랜드마크 처리
        MapOtherBones(handLandmarks, otherBones);
    }


    void UpdateRootBoneRotation(Vector3[] handLandmarks, Transform rootBone, Quaternion initialRootRotation)
    {
        Vector3 wristPosition = handLandmarks[0];
        Vector3 middleBasePosition = handLandmarks[9];

        Vector3 direction = middleBasePosition - wristPosition;

        // 손바닥 노멀 벡터 계산
        Vector3 palmRight = handLandmarks[17] - handLandmarks[5];
        Vector3 palmNormal = Vector3.Cross(direction, palmRight).normalized;

        // 업 벡터를 사용하여 회전 계산
        Quaternion targetRotation = Quaternion.LookRotation(direction, palmNormal);

        // 추가 회전 적용 (-90도 X축, -180도 Y축)
        Quaternion additionalRotation = Quaternion.Euler(-90f, -180f, 90f);

        // 최종 회전 적용
        rootBone.rotation = targetRotation * additionalRotation * initialRootRotation;
    }



    void MapFinger(Vector3[] handLandmarks, Transform[] fingerBones, int[] landmarkIndices)
    {
        for (int i = 0; i < fingerBones.Length && i < landmarkIndices.Length - 1; i++)
        {
            Vector3 from = handLandmarks[landmarkIndices[i]];
            Vector3 to = handLandmarks[landmarkIndices[i + 1]];

            Vector3 direction = to - from;

            // 벡터의 순서를 바꿉니다.
            Quaternion rotation = Quaternion.FromToRotation(direction.normalized, Vector3.up);

            fingerBones[i].localRotation = rotation;
        }
    }



    void MapOtherBones(Vector3[] handLandmarks, Transform[] otherBones)
    {
        if (otherBones.Length > 0)
        {
            otherBones[0].position = handLandmarks[0];
        }
    }
}
