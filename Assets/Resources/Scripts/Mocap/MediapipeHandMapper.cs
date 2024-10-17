using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

public class MediapipeHandMapper : MonoBehaviour
{
    // 왼손 뼈대
    public Transform leftRootBone; // 왼손 루트 본
    public Transform[] leftIndexBones;  // 왼손 검지손가락의 뼈대
    public Transform[] leftMiddleBones;
    public Transform[] leftRingBones;
    public Transform[] leftLittleBones;
    public Transform[] leftThumbBones;
    public Transform[] leftOtherBones; // 기타 왼손 랜드마크에 해당하는 뼈대

    // 오른손 뼈대
    public Transform rightRootBone; // 오른손 루트 본
    public Transform[] rightIndexBones;  // 오른손 검지손가락의 뼈대
    public Transform[] rightMiddleBones;
    public Transform[] rightRingBones;
    public Transform[] rightLittleBones;
    public Transform[] rightThumbBones;
    public Transform[] rightOtherBones; // 기타 오른손 랜드마크에 해당하는 뼈대

    private Vector3[] leftHandLandmarks; // 왼손 랜드마크 저장
    private Vector3[] rightHandLandmarks; // 오른손 랜드마크 저장
    private Quaternion initialLeftRootRotation;
    private Quaternion initialRightRootRotation;
    private bool leftHandDetected;
    private bool rightHandDetected;

    private UdpReceiver udpReceiver;
    public int port = 5053; // Python 스크립트와 일치하는 포트 번호
    private object lockObject = new object(); // 스레드 안전성을 위한 락 오브젝트

    // 각 뼈대의 초기 로컬 회전값 저장
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
        leftHandLandmarks = new Vector3[21]; // 왼손에 대한 21개의 랜드마크
        rightHandLandmarks = new Vector3[21]; // 오른손에 대한 21개의 랜드마크
        initialLeftRootRotation = leftRootBone.rotation;
        initialRightRootRotation = rightRootBone.rotation;
        leftHandDetected = false;
        rightHandDetected = false;

        // 각 뼈대의 초기 로컬 회전값 저장
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

        // UDP 리스너 시작
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
                                float y = 1 - point[1]; // Y축 반전
                                float z = -point[2];
                                landmarks[i] = new Vector3(x, y, z);
                            }

                            // 단순히 hand_0을 왼손, hand_1을 오른손으로 매핑
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
                // 왼손이 감지되지 않은 경우 루트 본을 초기 회전 상태로 유지
                leftRootBone.rotation = initialLeftRootRotation;
            }

            if (rightHandDetected)
            {
                UpdateHand(rightHandLandmarks, rightRootBone, initialRightRootRotation, rightIndexBones, initialRightIndexRotations, rightMiddleBones, initialRightMiddleRotations, rightRingBones, initialRightRingRotations, rightLittleBones, initialRightLittleRotations, rightThumbBones, initialRightThumbRotations, rightOtherBones);
            }
            else
            {
                // 오른손이 감지되지 않은 경우 루트 본을 초기 회전 상태로 유지
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
        // 루트 본 회전을 랜드마크를 기반으로 업데이트
        UpdateRootBoneRotation(handLandmarks, rootBone, initialRootRotation);

        // 손가락 뼈대 업데이트
        MapFinger(handLandmarks, thumbBones, initialThumbRotations, new int[] { 1, 2, 3, 4 }); // 엄지손가락
        MapFinger(handLandmarks, indexBones, initialIndexRotations, new int[] { 5, 6, 7, 8 }); // 검지손가락
        MapFinger(handLandmarks, middleBones, initialMiddleRotations, new int[] { 9, 10, 11, 12 }); // 중지
        MapFinger(handLandmarks, ringBones, initialRingRotations, new int[] { 13, 14, 15, 16 }); // 약지
        MapFinger(handLandmarks, littleBones, initialLittleRotations, new int[] { 17, 18, 19, 20 }); // 새끼손가락

        // 기타 랜드마크를 해당 뼈대에 매핑
        MapOtherBones(handLandmarks, otherBones);
    }

    void UpdateRootBoneRotation(Vector3[] handLandmarks, Transform rootBone, Quaternion initialRootRotation)
    {
        // 랜드마크 0(손목)과 9(중지 기저부)를 사용하여 루트 본의 회전 계산
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
        // 기타 뼈대 매핑, 예를 들어 손목
        // otherBones[0]이 랜드마크 0(손목)에 해당한다고 가정
        if (otherBones.Length > 0)
        {
            otherBones[0].position = handLandmarks[0];
        }
    }
}
