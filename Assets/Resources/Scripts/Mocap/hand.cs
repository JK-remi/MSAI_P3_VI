using System.Collections;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading;

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

    private Thread receiveThread;
    private UdpClient client;
    public int port = 5052; // Python ��ũ��Ʈ�� ��ġ�ϴ� ��Ʈ ��ȣ
    private object lockObject = new object(); // ������ �������� ���� �� ������Ʈ

    void Start()
    {
        leftHandLandmarks = new Vector3[21]; // �޼տ� ���� 21���� ���帶ũ
        rightHandLandmarks = new Vector3[21]; // �����տ� ���� 21���� ���帶ũ
        initialLeftRootRotation = leftRootBone.rotation;
        initialRightRootRotation = rightRootBone.rotation;
        leftHandDetected = false;
        rightHandDetected = false;

        // UDP ������ ������ ����
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    void OnApplicationQuit()
    {
        if (receiveThread != null)
            receiveThread.Abort();
        if (client != null)
            client.Close();
    }

    void ReceiveData()
    {
        client = new UdpClient(port);
        while (true)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);

                string json = Encoding.UTF8.GetString(data);

                // JSON ������ �Ľ�
                ParseLandmarks(json);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.ToString());
            }
        }
    }

    void ParseLandmarks(string json)
    {
        try
        {
            // JSON �����͸� ��ųʸ��� ������ȭ
            Dictionary<string, List<float[]>> data = JsonConvert.DeserializeObject<Dictionary<string, List<float[]>>>(json);

            lock (lockObject)
            {
                leftHandDetected = false;
                rightHandDetected = false;

                if (data.ContainsKey("left_hand"))
                {
                    List<float[]> leftHandData = data["left_hand"];
                    if (leftHandData.Count == 21)
                    {
                        for (int i = 0; i < 21; i++)
                        {
                            float[] point = leftHandData[i];
                            // ��ǥ ��ȯ�� �ʿ��� ��� ���⿡�� ����
                            float x = point[0];
                            float y = 1 - point[1]; // Y�� ���� (�ʿ� ��)
                            float z = -point[2];
                            leftHandLandmarks[i] = new Vector3(x, y, z);
                        }
                        leftHandDetected = true; // �޼� ������
                    }
                }

                if (data.ContainsKey("right_hand"))
                {
                    List<float[]> rightHandData = data["right_hand"];
                    if (rightHandData.Count == 21)
                    {
                        for (int i = 0; i < 21; i++)
                        {
                            float[] point = rightHandData[i];
                            // ��ǥ ��ȯ�� �ʿ��� ��� ���⿡�� ����
                            float x = point[0];
                            float y = 1 - point[1]; // Y�� ���� (�ʿ� ��)
                            float z = -point[2];
                            rightHandLandmarks[i] = new Vector3(x, y, z);
                        }
                        rightHandDetected = true; // ������ ������
                    }
                }
            }
        }
        catch (System.Exception ex)
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
                UpdateHand(leftHandLandmarks, leftRootBone, initialLeftRootRotation, leftIndexBones, leftMiddleBones, leftRingBones, leftLittleBones, leftThumbBones, leftOtherBones);
            }
            else
            {
                // �޼��� �������� ���� ��� ��Ʈ ���� �ʱ� ȸ�� ���·� ����
                leftRootBone.rotation = initialLeftRootRotation;
            }

            if (rightHandDetected)
            {
                UpdateHand(rightHandLandmarks, rightRootBone, initialRightRootRotation, rightIndexBones, rightMiddleBones, rightRingBones, rightLittleBones, rightThumbBones, rightOtherBones);
            }
            else
            {
                // �������� �������� ���� ��� ��Ʈ ���� �ʱ� ȸ�� ���·� ����
                rightRootBone.rotation = initialRightRootRotation;
            }
        }
    }

    void UpdateHand(Vector3[] handLandmarks, Transform rootBone, Quaternion initialRootRotation, Transform[] indexBones, Transform[] middleBones, Transform[] ringBones, Transform[] littleBones, Transform[] thumbBones, Transform[] otherBones)
    {
        // ��Ʈ �� ȸ���� ���帶ũ�� ������� ������Ʈ
        UpdateRootBoneRotation(handLandmarks, rootBone);

        // �� ���븦 ���� �����Ϳ� ���� ������Ʈ
        MapFinger(handLandmarks, thumbBones, new int[] { 1, 2, 3, 4 }); // �����հ���
        MapFinger(handLandmarks, indexBones, new int[] { 5, 6, 7, 8 }); // �����հ���
        MapFinger(handLandmarks, middleBones, new int[] { 9, 10, 11, 12 }); // ����
        MapFinger(handLandmarks, ringBones, new int[] { 13, 14, 15, 16 }); // ����
        MapFinger(handLandmarks, littleBones, new int[] { 17, 18, 19, 20 }); // �����հ���

        // ��Ÿ ���帶ũ�� �ش� ���뿡 ����
        MapOtherBones(handLandmarks, otherBones);
    }

    void UpdateRootBoneRotation(Vector3[] handLandmarks, Transform rootBone)
    {
        // ���帶ũ 0(�ո�)�� 9(���� ������)�� ����Ͽ� ��Ʈ ���� ȸ�� ���
        Vector3 wristPosition = handLandmarks[0];
        Vector3 middleBasePosition = handLandmarks[9];

        Vector3 direction = middleBasePosition - wristPosition;
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        rootBone.rotation = targetRotation;
    }

    void MapFinger(Vector3[] handLandmarks, Transform[] fingerBones, int[] landmarkIndices)
    {
        for (int i = 0; i < fingerBones.Length; i++)
        {
            Vector3 from = handLandmarks[landmarkIndices[i]];
            Vector3 to = handLandmarks[landmarkIndices[i + 1]];

            Vector3 direction = to - from;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, direction.normalized);

            fingerBones[i].rotation = rotation;
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

