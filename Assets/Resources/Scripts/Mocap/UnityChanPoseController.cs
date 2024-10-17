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

    private Dictionary<eLandmark, Vector3> landmarks = new Dictionary<eLandmark, Vector3>();

    private Quaternion initialRotation;
    private Vector3 initialPosition;
    private Quaternion targetRot;

    private UdpReceiver udpReceiver;
    private const int port = 5052;

    void Start()
    {
        initialRotation = transform.rotation;
        initialPosition = transform.position;

        if (calibrationData)
        {
            animator = this.GetComponent<Animator>();
            CalibrateFromPersistent();
        }

        udpReceiver = new UdpReceiver(port);
        udpReceiver.OnDataReceived += HandleReceivedData;
        udpReceiver.Start();
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

    void HandleReceivedData(string data)
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
                lock (landmarks)
                {
                    landmarks.Clear(); // 이전 데이터를 지웁니다.
                    for (int i = 0; i < receivedLandmarks.Count; i++)
                    {
                        List<float> kvp = receivedLandmarks[i];

                        // 좌표 변환 (Y축 반전)
                        float x = kvp[0];
                        float y = -kvp[1];
                        float z = kvp[2];
                        Vector3 position = new Vector3(x, y, z);

                        eLandmark landmarkKey = (eLandmark)i;
                        landmarks[landmarkKey] = position;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error parsing data: " + ex.Message);
        }
    }

    void Update()
    {
        lock (landmarks)
        {
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

    void OnApplicationQuit()
    {
        if (udpReceiver != null)
            udpReceiver.Stop();
    }
}
