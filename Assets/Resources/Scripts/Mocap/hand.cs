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

    private Thread receiveThread;
    private UdpClient client;
    public int port = 5052; // Python 스크립트와 일치하는 포트 번호
    private object lockObject = new object(); // 스레드 안전성을 위한 락 오브젝트

    void Start()
    {
        leftHandLandmarks = new Vector3[21]; // 왼손에 대한 21개의 랜드마크
        rightHandLandmarks = new Vector3[21]; // 오른손에 대한 21개의 랜드마크
        initialLeftRootRotation = leftRootBone.rotation;
        initialRightRootRotation = rightRootBone.rotation;
        leftHandDetected = false;
        rightHandDetected = false;

        // UDP 리스너 스레드 시작
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

                // JSON 데이터 파싱
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
            // JSON 데이터를 딕셔너리로 역직렬화
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
                            // 좌표 변환이 필요한 경우 여기에서 적용
                            float x = point[0];
                            float y = 1 - point[1]; // Y축 반전 (필요 시)
                            float z = -point[2];
                            leftHandLandmarks[i] = new Vector3(x, y, z);
                        }
                        leftHandDetected = true; // 왼손 감지됨
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
                            // 좌표 변환이 필요한 경우 여기에서 적용
                            float x = point[0];
                            float y = 1 - point[1]; // Y축 반전 (필요 시)
                            float z = -point[2];
                            rightHandLandmarks[i] = new Vector3(x, y, z);
                        }
                        rightHandDetected = true; // 오른손 감지됨
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
                // 왼손이 감지되지 않은 경우 루트 본을 초기 회전 상태로 유지
                leftRootBone.rotation = initialLeftRootRotation;
            }

            if (rightHandDetected)
            {
                UpdateHand(rightHandLandmarks, rightRootBone, initialRightRootRotation, rightIndexBones, rightMiddleBones, rightRingBones, rightLittleBones, rightThumbBones, rightOtherBones);
            }
            else
            {
                // 오른손이 감지되지 않은 경우 루트 본을 초기 회전 상태로 유지
                rightRootBone.rotation = initialRightRootRotation;
            }
        }
    }

    void UpdateHand(Vector3[] handLandmarks, Transform rootBone, Quaternion initialRootRotation, Transform[] indexBones, Transform[] middleBones, Transform[] ringBones, Transform[] littleBones, Transform[] thumbBones, Transform[] otherBones)
    {
        // 루트 본 회전을 랜드마크를 기반으로 업데이트
        UpdateRootBoneRotation(handLandmarks, rootBone);

        // 손 뼈대를 추적 데이터에 따라 업데이트
        MapFinger(handLandmarks, thumbBones, new int[] { 1, 2, 3, 4 }); // 엄지손가락
        MapFinger(handLandmarks, indexBones, new int[] { 5, 6, 7, 8 }); // 검지손가락
        MapFinger(handLandmarks, middleBones, new int[] { 9, 10, 11, 12 }); // 중지
        MapFinger(handLandmarks, ringBones, new int[] { 13, 14, 15, 16 }); // 약지
        MapFinger(handLandmarks, littleBones, new int[] { 17, 18, 19, 20 }); // 새끼손가락

        // 기타 랜드마크를 해당 뼈대에 매핑
        MapOtherBones(handLandmarks, otherBones);
    }

    void UpdateRootBoneRotation(Vector3[] handLandmarks, Transform rootBone)
    {
        // 랜드마크 0(손목)과 9(중지 기저부)를 사용하여 루트 본의 회전 계산
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
        // 기타 뼈대 매핑, 예를 들어 손목
        // otherBones[0]이 랜드마크 0(손목)에 해당한다고 가정
        if (otherBones.Length > 0)
        {
            otherBones[0].position = handLandmarks[0];
        }
    }
}

