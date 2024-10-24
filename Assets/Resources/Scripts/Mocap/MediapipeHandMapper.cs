using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

public class MediapipeHandMapper : MonoBehaviour
{
    public Transform leftRootBone; // �޼� ��Ʈ ��
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
        // �ո� ȸ�� ������Ʈ
        UpdateRootBoneRotation(handLandmarks, rootBone, initialRootRotation);

        // �հ��� ���� ������Ʈ
        MapFinger(handLandmarks, thumbBones, new int[] { 1, 2, 3, 4 }); // �����հ���
        MapFinger(handLandmarks, indexBones, new int[] { 5, 6, 7, 8 }); // �����հ���
        MapFinger(handLandmarks, middleBones, new int[] { 9, 10, 11, 12 }); // ����
        MapFinger(handLandmarks, ringBones, new int[] { 13, 14, 15, 16 }); // ����
        MapFinger(handLandmarks, littleBones, new int[] { 17, 18, 19, 20 }); // �����հ���

        // ��Ÿ ���帶ũ ó��
        MapOtherBones(handLandmarks, otherBones);
    }


    void UpdateRootBoneRotation(Vector3[] handLandmarks, Transform rootBone, Quaternion initialRootRotation)
    {
        Vector3 wristPosition = handLandmarks[0];
        Vector3 middleBasePosition = handLandmarks[9];

        Vector3 direction = middleBasePosition - wristPosition;

        // �չٴ� ��� ���� ���
        Vector3 palmRight = handLandmarks[17] - handLandmarks[5];
        Vector3 palmNormal = Vector3.Cross(direction, palmRight).normalized;

        // �� ���͸� ����Ͽ� ȸ�� ���
        Quaternion targetRotation = Quaternion.LookRotation(direction, palmNormal);

        // �߰� ȸ�� ���� (-90�� X��, -180�� Y��)
        Quaternion additionalRotation = Quaternion.Euler(-90f, -180f, 90f);

        // ���� ȸ�� ����
        rootBone.rotation = targetRotation * additionalRotation * initialRootRotation;
    }

    void MapFinger(Vector3[] handLandmarks, List<Transform> fingerBones, int[] landmarkIndices)
    {
        for (int i = 0; i < fingerBones.Count && i < landmarkIndices.Length - 1; i++)
        {
            Vector3 from = handLandmarks[landmarkIndices[i]];
            Vector3 to = handLandmarks[landmarkIndices[i + 1]];

            Vector3 direction = to - from;

            // ������ ������ �ٲߴϴ�.
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
