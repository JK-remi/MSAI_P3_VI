using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading;

public class UnityChanPoseController : MonoBehaviour
{
    // 본 정의
    public Transform hips;
    public Transform spine;
    public Transform neck;
    public Transform head;

    public Transform leftShoulder;
    public Transform leftUpperArm;
    public Transform leftLowerArm;
    public Transform leftHand;

    public Transform rightShoulder;
    public Transform rightUpperArm;
    public Transform rightLowerArm;
    public Transform rightHand;

    public Transform leftUpperLeg;
    public Transform leftLowerLeg;
    public Transform leftFoot;

    public Transform rightUpperLeg;
    public Transform rightLowerLeg;
    public Transform rightFoot;

    // Mediapipe 랜드마크 인덱스
    private const int NOSE = 0;
    private const int LEFT_EYE_INNER = 1;
    private const int LEFT_EYE = 2;
    private const int LEFT_EYE_OUTER = 3;
    private const int RIGHT_EYE_INNER = 4;
    private const int RIGHT_EYE = 5;
    private const int RIGHT_EYE_OUTER = 6;
    private const int LEFT_EAR = 7;
    private const int RIGHT_EAR = 8;
    private const int MOUTH_LEFT = 9;
    private const int MOUTH_RIGHT = 10;
    private const int LEFT_SHOULDER = 11;
    private const int RIGHT_SHOULDER = 12;
    private const int LEFT_ELBOW = 13;
    private const int RIGHT_ELBOW = 14;
    private const int LEFT_WRIST = 15;
    private const int RIGHT_WRIST = 16;
    private const int LEFT_HIP = 23;
    private const int RIGHT_HIP = 24;
    private const int LEFT_KNEE = 25;
    private const int RIGHT_KNEE = 26;
    private const int LEFT_ANKLE = 27;
    private const int RIGHT_ANKLE = 28;

    private UdpClient client;
    private const int port = 5052;

    private Dictionary<int, Vector3> landmarks = new Dictionary<int, Vector3>();

    void Start()
    {
        client = new UdpClient(port);
        client.BeginReceive(new System.AsyncCallback(ReceiveData), null);
    }

    void ReceiveData(System.IAsyncResult result)
    {
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
        byte[] data = client.EndReceive(result, ref anyIP);
        string json = Encoding.UTF8.GetString(data);

        Dictionary<int, float[]> receivedLandmarks = JsonConvert.DeserializeObject<Dictionary<int, float[]>>(json);

        lock (landmarks)
        {
            landmarks.Clear();
            foreach (var kvp in receivedLandmarks)
            {
                // 좌표 변환 (Y축 반전)
                float x = kvp.Value[0];
                float y = -kvp.Value[1];
                float z = kvp.Value[2];
                Vector3 position = new Vector3(x, y, z);
                landmarks[kvp.Key] = position;
            }
        }

        client.BeginReceive(new System.AsyncCallback(ReceiveData), null);
    }

    void Update()
    {
        Dictionary<int, Vector3> currentLandmarks;
        lock (landmarks)
        {
            currentLandmarks = new Dictionary<int, Vector3>(landmarks);
        }

        if (currentLandmarks.Count == 0)
            return;

        // 포즈 업데이트
        UpdatePose(currentLandmarks);
    }

    void UpdatePose(Dictionary<int, Vector3> lm)
    {
        // 힙 위치 업데이트
        //if (lm.ContainsKey(LEFT_HIP) && lm.ContainsKey(RIGHT_HIP))
        //{
        //    Vector3 leftHipPos = lm[LEFT_HIP];
        //    Vector3 rightHipPos = lm[RIGHT_HIP];
        //    Vector3 hipCenter = (leftHipPos + rightHipPos) / 2f;
        //    hips.position = hipCenter;
        //}

        // 본 회전 업데이트
        // 각 사지에 대해 방향 벡터를 계산하고 해당 본의 회전을 설정합니다.

        // 왼쪽 허벅지
        if (lm.ContainsKey(LEFT_HIP) && lm.ContainsKey(LEFT_KNEE))
        {
            SetBoneRotation(leftUpperLeg, lm[LEFT_HIP], lm[LEFT_KNEE]);
        }

        // 왼쪽 종아리
        if (lm.ContainsKey(LEFT_KNEE) && lm.ContainsKey(LEFT_ANKLE))
        {
            SetBoneRotation(leftLowerLeg, lm[LEFT_KNEE], lm[LEFT_ANKLE]);
        }

        // 오른쪽 허벅지
        if (lm.ContainsKey(RIGHT_HIP) && lm.ContainsKey(RIGHT_KNEE))
        {
            SetBoneRotation(rightUpperLeg, lm[RIGHT_HIP], lm[RIGHT_KNEE]);
        }

        // 오른쪽 종아리
        if (lm.ContainsKey(RIGHT_KNEE) && lm.ContainsKey(RIGHT_ANKLE))
        {
            SetBoneRotation(rightLowerLeg, lm[RIGHT_KNEE], lm[RIGHT_ANKLE]);
        }

        // 왼쪽 상완
        if (lm.ContainsKey(LEFT_SHOULDER) && lm.ContainsKey(LEFT_ELBOW))
        {
            SetBoneRotation(leftUpperArm, lm[LEFT_SHOULDER], lm[LEFT_ELBOW]);
        }

        // 왼쪽 하완
        if (lm.ContainsKey(LEFT_ELBOW) && lm.ContainsKey(LEFT_WRIST))
        {
            SetBoneRotation(leftLowerArm, lm[LEFT_ELBOW], lm[LEFT_WRIST]);
        }

        // 오른쪽 상완
        if (lm.ContainsKey(RIGHT_SHOULDER) && lm.ContainsKey(RIGHT_ELBOW))
        {
            SetBoneRotation(rightUpperArm, lm[RIGHT_SHOULDER], lm[RIGHT_ELBOW]);
        }

        // 오른쪽 하완
        if (lm.ContainsKey(RIGHT_ELBOW) && lm.ContainsKey(RIGHT_WRIST))
        {
            SetBoneRotation(rightLowerArm, lm[RIGHT_ELBOW], lm[RIGHT_WRIST]);
        }

        // 척추
        if (lm.ContainsKey(LEFT_HIP) && lm.ContainsKey(RIGHT_HIP) && lm.ContainsKey(LEFT_SHOULDER) && lm.ContainsKey(RIGHT_SHOULDER))
        {
            Vector3 hipCenter = (lm[LEFT_HIP] + lm[RIGHT_HIP]) / 2f;
            Vector3 shoulderCenter = (lm[LEFT_SHOULDER] + lm[RIGHT_SHOULDER]) / 2f;
            SetBoneRotation(spine, hipCenter, shoulderCenter);
        }

        // 목과 머리
        if (lm.ContainsKey(LEFT_SHOULDER) && lm.ContainsKey(RIGHT_SHOULDER) && lm.ContainsKey(NOSE))
        {
            Vector3 shoulderCenter = (lm[LEFT_SHOULDER] + lm[RIGHT_SHOULDER]) / 2f;
            SetBoneRotation(neck, shoulderCenter, lm[NOSE]);
            SetBoneRotation(head, shoulderCenter, lm[NOSE]);
        }
    }

    void SetBoneRotation(Transform bone, Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        if (direction == Vector3.zero)
            return;

        // 본의 로컬 축에 맞게 회전 오프셋 적용 (필요 시 조정)
        Quaternion rotationOffset = Quaternion.Euler(180, 90, 0);

        Quaternion targetRotation = Quaternion.LookRotation(direction) * rotationOffset;

        // 부드러운 회전을 위해 Slerp 사용
        bone.rotation = Quaternion.Slerp(bone.rotation, targetRotation, Time.deltaTime * 10f);
    }

    void OnApplicationQuit()
    {
        if (client != null)
            client.Close();
    }
}
