using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

public class MediapipeHandMapper : MonoBehaviour
{
    public Transform leftRootBone; // 왼손 루트 본
    public List<Transform> leftIndexBones = new List<Transform>(3);
    public List<Transform> leftMiddleBones = new List<Transform>(3);
    public List<Transform> leftRingBones = new List<Transform>(3);
    public List<Transform> leftLittleBones = new List<Transform>(3);
    public List<Transform> leftThumbBones = new List<Transform>(3);
    public List<Transform> leftOtherBones;

    public Transform rightRootBone;
    public List<Transform> rightIndexBones = new List<Transform>(3);
    public List<Transform> rightMiddleBones = new List<Transform>(3);
    public List<Transform> rightRingBones = new List<Transform>(3);
    public List<Transform> rightLittleBones = new List<Transform>(3);
    public List<Transform> rightThumbBones = new List<Transform>(3);
    public List<Transform> rightOtherBones;

    private Quaternion initialLeftRootRotation;
    private Quaternion initialRightRootRotation;

    private UdpReceiver udpReceiver;

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

    public void Activate(UdpReceiver receiver)
    {
        udpReceiver = receiver;
    }

    public void Deactivate()
    {
        udpReceiver = null;
    }

    void Start()
    {
        initialLeftRootRotation = leftRootBone.rotation;
        initialRightRootRotation = rightRootBone.rotation;

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
    }

    Quaternion[] GetInitialLocalRotations(List<Transform> bones)
    {
        Quaternion[] rotations = new Quaternion[bones.Count];
        for (int i = 0; i < bones.Count; i++)
        {
            rotations[i] = bones[i].localRotation;
        }
        return rotations;
    }

    void Update()
    {
        if (udpReceiver == null) return;

        lock (MediapipeManager.Instance.handLockObj)
        {
            if (MediapipeManager.Instance.leftHandDetected)
            {
                UpdateHand(MediapipeManager.Instance.leftHandLandmarks, leftRootBone, initialLeftRootRotation, leftIndexBones, initialLeftIndexRotations, leftMiddleBones, initialLeftMiddleRotations, leftRingBones, initialLeftRingRotations, leftLittleBones, initialLeftLittleRotations, leftThumbBones, initialLeftThumbRotations, leftOtherBones);
            }
            else
            {
                leftRootBone.rotation = initialLeftRootRotation;
            }

            if (MediapipeManager.Instance.rightHandDetected)
            {
                UpdateHand(MediapipeManager.Instance.rightHandLandmarks, rightRootBone, initialRightRootRotation, rightIndexBones, initialRightIndexRotations, rightMiddleBones, initialRightMiddleRotations, rightRingBones, initialRightRingRotations, rightLittleBones, initialRightLittleRotations, rightThumbBones, initialRightThumbRotations, rightOtherBones);
            }
            else
            {
                rightRootBone.rotation = initialRightRootRotation;
            }
        }
    }

    void UpdateHand(Vector3[] handLandmarks, Transform rootBone, Quaternion initialRootRotation,
        List<Transform> indexBones, Quaternion[] initialIndexRotations,
        List<Transform> middleBones, Quaternion[] initialMiddleRotations,
        List<Transform> ringBones, Quaternion[] initialRingRotations,
        List<Transform> littleBones, Quaternion[] initialLittleRotations,
        List<Transform> thumbBones, Quaternion[] initialThumbRotations,
        List<Transform> otherBones)
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

    void MapFinger(Vector3[] handLandmarks, List<Transform> fingerBones, int[] landmarkIndices)
    {
        for (int i = 0; i < fingerBones.Count && i < landmarkIndices.Length - 1; i++)
        {
            Vector3 from = handLandmarks[landmarkIndices[i]];
            Vector3 to = handLandmarks[landmarkIndices[i + 1]];

            Vector3 direction = to - from;

            // 벡터의 순서를 바꿉니다.
            Quaternion rotation = Quaternion.FromToRotation(direction.normalized, Vector3.up);

            fingerBones[i].localRotation = rotation;
        }
    }

    void MapOtherBones(Vector3[] handLandmarks, List<Transform> otherBones)
    {
        if (otherBones.Count > 0)
        {
            otherBones[0].position = handLandmarks[0];
        }
    }
}
