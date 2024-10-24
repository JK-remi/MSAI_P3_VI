using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

// Mediapipe 랜드마크 인덱스
public enum eLandmark
{
    NONE = -1,
    NOSE = 0,
    LEFT_EYE_INNER = 1,
    LEFT_EYE = 2,
    LEFT_EYE_OUTER = 3,
    RIGHT_EYE_INNER = 4,
    RIGHT_EYE = 5,
    RIGHT_EYE_OUTER = 6,
    LEFT_EAR = 7,
    RIGHT_EAR = 8,
    MOUTH_LEFT = 9,
    MOUTH_RIGHT = 10,
    LEFT_SHOULDER = 11,
    RIGHT_SHOULDER = 12,
    LEFT_ELBOW = 13,
    RIGHT_ELBOW = 14,
    LEFT_WRIST = 15,
    RIGHT_WRIST = 16,
    LEFT_HIP = 23,
    RIGHT_HIP = 24,
    LEFT_KNEE = 25,
    RIGHT_KNEE = 26,
    LEFT_ANKLE = 27,
    RIGHT_ANKLE = 28,
    LEFT_FOOT_INDEX = 32,
    RIGHT_FOOT_INDEX = 31,
}

public class UnityChanPoseController : MonoBehaviour
{
    private Animator animator;

    public PersistentCalibrationData calibrationData;
    private Dictionary<HumanBodyBones, CalibrationData> parentCalibrationData = new Dictionary<HumanBodyBones, CalibrationData>();
    private CalibrationData spineUpDown, hipsTwist, chest, head;

    private Dictionary<eLandmark, Vector3> landmarks;

    private Quaternion initialRotation;
    private Vector3 initialPosition;
    private Quaternion targetRot;

    private UdpReceiver udpReceiver;
    
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
        initialRotation = transform.rotation;
        initialPosition = transform.position;

        if (calibrationData)
        {
            animator = this.GetComponent<Animator>();
            CalibrateFromPersistent();
        }
    }

    public void CalibrateFromPersistent()
    {
        parentCalibrationData.Clear();

        if (calibrationData)
        {
            foreach (PersistentCalibrationData.CalibrationEntry d in calibrationData.parentCalibrationData)
            {
                parentCalibrationData.Add(d.bone, d.data.ReconstructReferences());
            }
            spineUpDown = calibrationData.spineUpDown.ReconstructReferences();
            hipsTwist = calibrationData.hipsTwist.ReconstructReferences();
            chest = calibrationData.chest.ReconstructReferences();
            head = calibrationData.head.ReconstructReferences();
        }

        animator.enabled = false; // Animator를 비활성화하여 간섭을 방지합니다.
    }

    void Update()
    {
        if (udpReceiver == null) return;

        lock (MediapipeManager.Instance.bodyLandmarks)
        {
            landmarks = MediapipeManager.Instance.bodyLandmarks;
            if (landmarks.Count == 0)
                return;

            // 포즈 업데이트
            UpdatePose();
        }
    }

    void UpdatePose()
    {
        // 본 회전 업데이트
        foreach (var i in parentCalibrationData)
        {
            Vector3 curDir = GetCurDirection(i.Value.lmChild, i.Value.lmParent);
            Quaternion deltaRotTracked = Quaternion.FromToRotation(i.Value.initialDir, curDir);
            i.Value.parent.rotation = deltaRotTracked * i.Value.initialRotation;
        }

        // 척추 체인 처리
        if (parentCalibrationData.Count > 0)
        {
            Vector3 hipCenter = GetAveragePosition(eLandmark.LEFT_HIP, eLandmark.RIGHT_HIP);
            Vector3 shoulderCenter = GetAveragePosition(eLandmark.LEFT_SHOULDER, eLandmark.RIGHT_SHOULDER);
            Vector3 hipTwistDir = GetCurDirection(hipsTwist.lmChild, hipsTwist.lmParent);

            Vector3 hd = GetCurDirection(landmarks[head.lmChild], shoulderCenter);
            Quaternion headr = Quaternion.FromToRotation(head.initialDir, hd);
            Quaternion twist = Quaternion.FromToRotation(hipsTwist.initialDir,
                Vector3.Slerp(hipsTwist.initialDir, hipTwistDir, .25f));
            Quaternion updown = Quaternion.FromToRotation(spineUpDown.initialDir,
                Vector3.Slerp(spineUpDown.initialDir, GetCurDirection(shoulderCenter, hipCenter), .25f));

            Quaternion h = updown * updown * updown * twist * twist;
            Quaternion s = h * twist * updown;
            Quaternion c = s * twist * twist;
            float speed = 10f;
            hipsTwist.Tick(h * hipsTwist.initialRotation, speed);
            spineUpDown.Tick(s * spineUpDown.initialRotation, speed);
            chest.Tick(c * chest.initialRotation, speed);
            head.Tick(updown * twist * headr * head.initialRotation, speed);
        }
    }

    Vector3 GetAveragePosition(eLandmark lm1, eLandmark lm2)
    {
        if (landmarks.ContainsKey(lm1) && landmarks.ContainsKey(lm2))
        {
            return (landmarks[lm1] + landmarks[lm2]) / 2f;
        }
        return Vector3.zero;
    }

    Vector3 GetCurDirection(Vector3 vChild, Vector3 vParent)
    {
        return (vChild - vParent).normalized;
    }

    Vector3 GetCurDirection(eLandmark lmChild, eLandmark lmParent)
    {
        if (!landmarks.ContainsKey(lmChild) || !landmarks.ContainsKey(lmParent))
        {
            return Vector3.zero;
        }
        return GetCurDirection(landmarks[lmChild], landmarks[lmParent]);
    }
}
