using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading;

// Mediapipe ���帶ũ �ε���
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
    // �� ����
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

    private UdpClient client;
    private const int port = 5052;

    private Dictionary<eLandmark, Vector3> landmarks = new Dictionary<eLandmark, Vector3>();

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
                // ��ǥ ��ȯ (Y�� ����)
                float x = kvp.Value[0];
                float y = -kvp.Value[1];
                float z = kvp.Value[2];
                Vector3 position = new Vector3(x, y, z);
                landmarks[(eLandmark)kvp.Key] = position;
            }
        }

        client.BeginReceive(new System.AsyncCallback(ReceiveData), null);
    }

    void Update()
    {
        Dictionary<eLandmark, Vector3> currentLandmarks;
        lock (landmarks)
        {
            currentLandmarks = new Dictionary<eLandmark, Vector3>(landmarks);
        }

        if (currentLandmarks.Count == 0)
            return;

        // ���� ������Ʈ
        UpdatePose(currentLandmarks);
    }

    void UpdatePose(Dictionary<eLandmark, Vector3> lm)
    {
        // �� ��ġ ������Ʈ
        //if (lm.ContainsKey(LEFT_HIP) && lm.ContainsKey(RIGHT_HIP))
        //{
        //    Vector3 leftHipPos = lm[LEFT_HIP];
        //    Vector3 rightHipPos = lm[RIGHT_HIP];
        //    Vector3 hipCenter = (leftHipPos + rightHipPos) / 2f;
        //    hips.position = hipCenter;
        //}

        // �� ȸ�� ������Ʈ
        // �� ������ ���� ���� ���͸� ����ϰ� �ش� ���� ȸ���� �����մϴ�.

        // ���� �����
        if (lm.ContainsKey(eLandmark.LEFT_HIP) && lm.ContainsKey(eLandmark.LEFT_KNEE))
        {
            SetBoneRotation(leftUpperLeg, lm[eLandmark.LEFT_HIP], lm[eLandmark.LEFT_KNEE]);
        }

        // ���� ���Ƹ�
        if (lm.ContainsKey(eLandmark.LEFT_KNEE) && lm.ContainsKey(eLandmark.LEFT_ANKLE))
        {
            SetBoneRotation(leftLowerLeg, lm[eLandmark.LEFT_KNEE], lm[eLandmark.LEFT_ANKLE]);
        }

        // ������ �����
        if (lm.ContainsKey(eLandmark.RIGHT_HIP) && lm.ContainsKey(eLandmark.RIGHT_KNEE))
        {
            SetBoneRotation(rightUpperLeg, lm[eLandmark.RIGHT_HIP], lm[eLandmark.RIGHT_KNEE]);
        }

        // ������ ���Ƹ�
        if (lm.ContainsKey(eLandmark.RIGHT_KNEE) && lm.ContainsKey(eLandmark.RIGHT_ANKLE))
        {
            SetBoneRotation(rightLowerLeg, lm[eLandmark.RIGHT_KNEE], lm[eLandmark.RIGHT_ANKLE]);
        }

        // ���� ���
        if (lm.ContainsKey(eLandmark.LEFT_SHOULDER) && lm.ContainsKey(eLandmark.LEFT_ELBOW))
        {
            SetBoneRotation(leftUpperArm, lm[eLandmark.LEFT_SHOULDER], lm[eLandmark.LEFT_ELBOW]);
        }

        // ���� �Ͽ�
        if (lm.ContainsKey(eLandmark.LEFT_ELBOW) && lm.ContainsKey(eLandmark.LEFT_WRIST))
        {
            SetBoneRotation(leftLowerArm, lm[eLandmark.LEFT_ELBOW], lm[eLandmark.LEFT_WRIST]);
        }

        // ������ ���
        if (lm.ContainsKey(eLandmark.RIGHT_SHOULDER) && lm.ContainsKey(eLandmark.RIGHT_ELBOW))
        {
            SetBoneRotation(rightUpperArm, lm[eLandmark.RIGHT_SHOULDER], lm[eLandmark.RIGHT_ELBOW]);
        }

        // ������ �Ͽ�
        if (lm.ContainsKey(eLandmark.RIGHT_ELBOW) && lm.ContainsKey(eLandmark.RIGHT_WRIST))
        {
            SetBoneRotation(rightLowerArm, lm[eLandmark.RIGHT_ELBOW], lm[eLandmark.RIGHT_WRIST]);
        }

        // ô��
        if (lm.ContainsKey(eLandmark.LEFT_HIP) && lm.ContainsKey(eLandmark.RIGHT_HIP) && lm.ContainsKey(eLandmark.LEFT_SHOULDER) && lm.ContainsKey(eLandmark.RIGHT_SHOULDER))
        {
            Vector3 hipCenter = (lm[eLandmark.LEFT_HIP] + lm[eLandmark.RIGHT_HIP]) / 2f;
            Vector3 shoulderCenter = (lm[eLandmark.LEFT_SHOULDER] + lm[eLandmark.RIGHT_SHOULDER]) / 2f;
            SetBoneRotation(spine, hipCenter, shoulderCenter);
        }

        // ��� �Ӹ�
        if (lm.ContainsKey(eLandmark.LEFT_SHOULDER) && lm.ContainsKey(eLandmark.RIGHT_SHOULDER) && lm.ContainsKey(eLandmark.NOSE))
        {
            Vector3 shoulderCenter = (lm[eLandmark.LEFT_SHOULDER] + lm[eLandmark.RIGHT_SHOULDER]) / 2f;
            SetBoneRotation(neck, shoulderCenter, lm[eLandmark.NOSE]);
            SetBoneRotation(head, shoulderCenter, lm[eLandmark.NOSE]);
        }
    }

    void SetBoneRotation(Transform bone, Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        if (direction == Vector3.zero)
            return;

        // ���� ���� �࿡ �°� ȸ�� ������ ���� (�ʿ� �� ����)
        Quaternion rotationOffset = Quaternion.Euler(180, 90, 0);

        Quaternion targetRotation = Quaternion.LookRotation(direction) * rotationOffset;

        // �ε巯�� ȸ���� ���� Slerp ���
        bone.rotation = Quaternion.Slerp(bone.rotation, targetRotation, Time.deltaTime * 10f);
    }

    void OnApplicationQuit()
    {
        if (client != null)
            client.Close();
    }
}
