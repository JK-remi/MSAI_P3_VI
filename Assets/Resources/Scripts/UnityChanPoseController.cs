using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading;

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

    // Mediapipe ���帶ũ �ε���
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
                // ��ǥ ��ȯ (Y�� ����)
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

        // ���� ������Ʈ
        UpdatePose(currentLandmarks);
    }

    void UpdatePose(Dictionary<int, Vector3> lm)
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
        if (lm.ContainsKey(LEFT_HIP) && lm.ContainsKey(LEFT_KNEE))
        {
            SetBoneRotation(leftUpperLeg, lm[LEFT_HIP], lm[LEFT_KNEE]);
        }

        // ���� ���Ƹ�
        if (lm.ContainsKey(LEFT_KNEE) && lm.ContainsKey(LEFT_ANKLE))
        {
            SetBoneRotation(leftLowerLeg, lm[LEFT_KNEE], lm[LEFT_ANKLE]);
        }

        // ������ �����
        if (lm.ContainsKey(RIGHT_HIP) && lm.ContainsKey(RIGHT_KNEE))
        {
            SetBoneRotation(rightUpperLeg, lm[RIGHT_HIP], lm[RIGHT_KNEE]);
        }

        // ������ ���Ƹ�
        if (lm.ContainsKey(RIGHT_KNEE) && lm.ContainsKey(RIGHT_ANKLE))
        {
            SetBoneRotation(rightLowerLeg, lm[RIGHT_KNEE], lm[RIGHT_ANKLE]);
        }

        // ���� ���
        if (lm.ContainsKey(LEFT_SHOULDER) && lm.ContainsKey(LEFT_ELBOW))
        {
            SetBoneRotation(leftUpperArm, lm[LEFT_SHOULDER], lm[LEFT_ELBOW]);
        }

        // ���� �Ͽ�
        if (lm.ContainsKey(LEFT_ELBOW) && lm.ContainsKey(LEFT_WRIST))
        {
            SetBoneRotation(leftLowerArm, lm[LEFT_ELBOW], lm[LEFT_WRIST]);
        }

        // ������ ���
        if (lm.ContainsKey(RIGHT_SHOULDER) && lm.ContainsKey(RIGHT_ELBOW))
        {
            SetBoneRotation(rightUpperArm, lm[RIGHT_SHOULDER], lm[RIGHT_ELBOW]);
        }

        // ������ �Ͽ�
        if (lm.ContainsKey(RIGHT_ELBOW) && lm.ContainsKey(RIGHT_WRIST))
        {
            SetBoneRotation(rightLowerArm, lm[RIGHT_ELBOW], lm[RIGHT_WRIST]);
        }

        // ô��
        if (lm.ContainsKey(LEFT_HIP) && lm.ContainsKey(RIGHT_HIP) && lm.ContainsKey(LEFT_SHOULDER) && lm.ContainsKey(RIGHT_SHOULDER))
        {
            Vector3 hipCenter = (lm[LEFT_HIP] + lm[RIGHT_HIP]) / 2f;
            Vector3 shoulderCenter = (lm[LEFT_SHOULDER] + lm[RIGHT_SHOULDER]) / 2f;
            SetBoneRotation(spine, hipCenter, shoulderCenter);
        }

        // ��� �Ӹ�
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
