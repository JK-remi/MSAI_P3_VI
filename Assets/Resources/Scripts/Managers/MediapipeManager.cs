using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MediapipeManager : MonoBehaviour
{
    private static MediapipeManager _instance = null;
    public static MediapipeManager Instance
    {
        get
        {
            _instance = GameObject.FindObjectOfType<MediapipeManager>();
            if (_instance == null)
            {
                GameObject container = new GameObject("MediapipeManager");
                _instance = container.AddComponent<MediapipeManager>();
            }

            return _instance;
        }
    }

    private const int BODY_PORT = 5052;
    private const int HAND_PORT = 5053;
    private const int FACE_PORT = 5054;

    public UdpReceiver bodyUdpRecv;
    public UdpReceiver handUdpRecv;
    public UdpReceiver faceUdpRecv;

    public Dictionary<eLandmark, Vector3> bodyLandmarks = new Dictionary<eLandmark, Vector3>();

    public object handLockObj = new object();
    public Vector3[] leftHandLandmarks;
    public Vector3[] rightHandLandmarks;
    public bool leftHandDetected;
    public bool rightHandDetected;

    public readonly object faceLockObj = new object();
    public Dictionary<string, float> newBlendShapeWeights = new Dictionary<string, float>();
    public bool newExpressionData = false;

    [Header("Temp")]
    public GameObject sphere;
    public List<GameObject> handLandmarks = new List<GameObject>(40);

    public GameObject curCharacter;
    private MediapipeHandMapper handMap;

    private void Awake()
    {
        ActivateUdpReceiver(BODY_PORT, HandleReceivedData_Body, out bodyUdpRecv);
        ActivateUdpReceiver(HAND_PORT, HandleReceivedData_Hand, out handUdpRecv);
        ActivateUdpReceiver(FACE_PORT, HandleReceivedData_Face, out faceUdpRecv);

        if(curCharacter != null)
        {
            ActivateCharacter(curCharacter);
            curCharacter.GetComponent<Animator>().enabled = false;
        }

        for(int i=0; i<21*2; i++)
        {
            GameObject go = Instantiate(sphere, this.transform);
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (i < 21)
            {
                go.name = "LEFT_" + i;
                mr.material.color = Color.red;
            }
            else
            {
                go.name = "RIGHT_" + i;
                mr.material.color = Color.blue;
            }

            handLandmarks.Add(go);
        }
    }

    private void Update()
    {
        if (handLandmarks.Count == 42)
        {
            // wrist (hand root)
            handLandmarks[0].transform.position = handMap.leftRootBone.position;
            // thumb
            handLandmarks[1].transform.position = GetHandBonePos(handLandmarks[0].transform.position, 1, 0, GetBoneLength(handMap.leftThumbBones, 0, handMap.leftRootBone), true);
            handLandmarks[2].transform.position = GetHandBonePos(handLandmarks[1].transform.position, 2, 1, GetBoneLength(handMap.leftThumbBones, 1, 0), true);
            handLandmarks[3].transform.position = GetHandBonePos(handLandmarks[2].transform.position, 3, 2, GetBoneLength(handMap.leftThumbBones, 2, 1), true);
            handLandmarks[4].transform.position = GetHandBonePos(handLandmarks[3].transform.position, 4, 3, GetBoneLength(handMap.leftThumbBones, 2, 1, 0.7f), true);
            // index
            handLandmarks[5].transform.position = GetHandBonePos(handLandmarks[0].transform.position, 5, 0, GetBoneLength(handMap.leftIndexBones, 0, handMap.leftRootBone), true);
            handLandmarks[6].transform.position = GetHandBonePos(handLandmarks[5].transform.position, 6, 5, GetBoneLength(handMap.leftIndexBones, 1, 0), true);
            handLandmarks[7].transform.position = GetHandBonePos(handLandmarks[6].transform.position, 7, 6, GetBoneLength(handMap.leftIndexBones, 2, 1), true);
            handLandmarks[8].transform.position = GetHandBonePos(handLandmarks[7].transform.position, 8, 7, GetBoneLength(handMap.leftIndexBones, 2, 1, 0.7f), true);
            // middle
            handLandmarks[9].transform.position = GetHandBonePos(handLandmarks[0].transform.position, 9, 0, GetBoneLength(handMap.leftMiddleBones, 0, handMap.leftRootBone), true);
            handLandmarks[10].transform.position = GetHandBonePos(handLandmarks[9].transform.position, 10, 9, GetBoneLength(handMap.leftMiddleBones, 1, 0), true);
            handLandmarks[11].transform.position = GetHandBonePos(handLandmarks[10].transform.position, 11, 10, GetBoneLength(handMap.leftMiddleBones, 2, 1), true);
            handLandmarks[12].transform.position = GetHandBonePos(handLandmarks[11].transform.position, 12, 11, GetBoneLength(handMap.leftMiddleBones, 2, 1, 0.7f), true);
            // ring
            handLandmarks[13].transform.position = GetHandBonePos(handLandmarks[0].transform.position, 13, 0, GetBoneLength(handMap.leftRingBones, 0, handMap.leftRootBone), true);
            handLandmarks[14].transform.position = GetHandBonePos(handLandmarks[13].transform.position, 14, 13, GetBoneLength(handMap.leftRingBones, 1, 0), true);
            handLandmarks[15].transform.position = GetHandBonePos(handLandmarks[14].transform.position, 15, 14, GetBoneLength(handMap.leftRingBones, 2, 1), true);
            handLandmarks[16].transform.position = GetHandBonePos(handLandmarks[15].transform.position, 16, 15, GetBoneLength(handMap.leftRingBones, 2, 1, 0.7f), true);
            // little
            handLandmarks[17].transform.position = GetHandBonePos(handLandmarks[0].transform.position, 17, 0, GetBoneLength(handMap.leftLittleBones, 0, handMap.leftRootBone), true);
            handLandmarks[18].transform.position = GetHandBonePos(handLandmarks[17].transform.position, 18, 17, GetBoneLength(handMap.leftLittleBones, 1, 0), true);
            handLandmarks[19].transform.position = GetHandBonePos(handLandmarks[18].transform.position, 19, 18, GetBoneLength(handMap.leftLittleBones, 2, 1), true);
            handLandmarks[20].transform.position = GetHandBonePos(handLandmarks[19].transform.position, 20, 19, GetBoneLength(handMap.leftLittleBones, 2, 1, 0.7f), true);

            // wrist (hand root)
            handLandmarks[21].transform.position = handMap.rightRootBone.position;
            // thumb
            handLandmarks[22].transform.position = GetHandBonePos(handLandmarks[21].transform.position, 1, 0, GetBoneLength(handMap.rightThumbBones, 0, handMap.rightRootBone), false);
            handLandmarks[23].transform.position = GetHandBonePos(handLandmarks[22].transform.position, 2, 1, GetBoneLength(handMap.rightThumbBones, 1, 0), false);
            handLandmarks[24].transform.position = GetHandBonePos(handLandmarks[23].transform.position, 3, 2, GetBoneLength(handMap.rightThumbBones, 2, 1), false);
            handLandmarks[25].transform.position = GetHandBonePos(handLandmarks[24].transform.position, 4, 3, GetBoneLength(handMap.rightThumbBones, 2, 1, 0.7f), false);
            // index
            handLandmarks[26].transform.position = GetHandBonePos(handLandmarks[21].transform.position, 5, 0, GetBoneLength(handMap.rightIndexBones, 0, handMap.rightRootBone), false);
            handLandmarks[27].transform.position = GetHandBonePos(handLandmarks[26].transform.position, 6, 5, GetBoneLength(handMap.rightIndexBones, 1, 0), false);
            handLandmarks[28].transform.position = GetHandBonePos(handLandmarks[27].transform.position, 7, 6, GetBoneLength(handMap.rightIndexBones, 2, 1), false);
            handLandmarks[29].transform.position = GetHandBonePos(handLandmarks[28].transform.position, 8, 7, GetBoneLength(handMap.rightIndexBones, 2, 1, 0.7f), false);
            // middle
            handLandmarks[30].transform.position = GetHandBonePos(handLandmarks[21].transform.position, 9, 0, GetBoneLength(handMap.rightMiddleBones, 0, handMap.rightRootBone), false);
            handLandmarks[31].transform.position = GetHandBonePos(handLandmarks[30].transform.position, 10, 9, GetBoneLength(handMap.rightMiddleBones, 1, 0), false);
            handLandmarks[32].transform.position = GetHandBonePos(handLandmarks[31].transform.position, 11, 10, GetBoneLength(handMap.rightMiddleBones, 2, 1), false);
            handLandmarks[33].transform.position = GetHandBonePos(handLandmarks[32].transform.position, 12, 11, GetBoneLength(handMap.rightMiddleBones, 2, 1, 0.7f), false);
            // ring
            handLandmarks[34].transform.position = GetHandBonePos(handLandmarks[21].transform.position, 13, 0, GetBoneLength(handMap.rightRingBones, 0, handMap.rightRootBone), false);
            handLandmarks[35].transform.position = GetHandBonePos(handLandmarks[34].transform.position, 14, 13, GetBoneLength(handMap.rightRingBones, 1, 0), false);
            handLandmarks[36].transform.position = GetHandBonePos(handLandmarks[35].transform.position, 15, 14, GetBoneLength(handMap.rightRingBones, 2, 1), false);
            handLandmarks[37].transform.position = GetHandBonePos(handLandmarks[36].transform.position, 16, 15, GetBoneLength(handMap.rightRingBones, 2, 1, 0.7f), false);
            // little
            handLandmarks[38].transform.position = GetHandBonePos(handLandmarks[21].transform.position, 17, 0, GetBoneLength(handMap.rightLittleBones, 0, handMap.rightRootBone), false);
            handLandmarks[39].transform.position = GetHandBonePos(handLandmarks[38].transform.position, 18, 17, GetBoneLength(handMap.rightLittleBones, 1, 0), false);
            handLandmarks[40].transform.position = GetHandBonePos(handLandmarks[39].transform.position, 19, 18, GetBoneLength(handMap.rightLittleBones, 2, 1), false);
            handLandmarks[41].transform.position = GetHandBonePos(handLandmarks[40].transform.position, 20, 19, GetBoneLength(handMap.rightLittleBones, 2, 1, 0.7f), false);

            if(handMap != null)
            {
                handMap.leftRootBone.rotation = UpdateWristRotation(true);
                handMap.rightRootBone.rotation = UpdateWristRotation(false);
            }
        }
    }

    public Quaternion UpdateWristRotation(bool isLeft)
    {
        int rightIdx = isLeft ? 0 : 21;

        Vector3 wristTransform = handLandmarks[0 + rightIdx].transform.position;
        Vector3 indexFinger = handLandmarks[5 + rightIdx].transform.position;
        Vector3 middleFinger = handLandmarks[9 + rightIdx].transform.position;

        Vector3 vectorToMiddle = middleFinger - wristTransform;
        Vector3 vectorToIndex = indexFinger - wristTransform;
        //to get ortho vector of middle finger from index finger
        Vector3.OrthoNormalize(ref vectorToMiddle, ref vectorToIndex);

        //vector normal to wrist
        Vector3 normalVector = Vector3.Cross(vectorToIndex, vectorToMiddle);
        if(isLeft == false) normalVector = Vector3.Cross(vectorToMiddle, vectorToIndex);

        Quaternion additionalRotation = Quaternion.Euler(180f, 0f, 0f);
        //wristTransform.rotation = Quaternion.LookRotation(vectorToIndex, normalVector) * additionalRotation;
        return Quaternion.LookRotation(vectorToIndex, normalVector) * additionalRotation;
    }

    private float GetBoneLength(List<Transform> bones, int head, int tail, float offset = 1f)
    {
        if (bones.Count <= tail) return 0f;

        return GetBoneLength(bones, head, bones[tail]) * offset;
    }

    private float GetBoneLength(List<Transform> bones, int head, Transform tail)
    {
        if (bones.Count <= head) return 0f;

        return (bones[head].position - tail.position).magnitude;
    }

    private Vector3 GetHandBonePos(Vector3 origin, int head, int tail, float length, bool isLeft)
    {
        if (isLeft && (leftHandLandmarks.Length <= head || leftHandLandmarks.Length <= tail)) return Vector3.zero;
        if (!isLeft && (rightHandLandmarks.Length <= head || rightHandLandmarks.Length <= tail)) return Vector3.zero;

        Vector3 vel = Vector3.zero;
        if(isLeft)
        {
            vel = (leftHandLandmarks[head] - leftHandLandmarks[tail]).normalized;
        }
        else
        {
            vel = (rightHandLandmarks[head] - rightHandLandmarks[tail]).normalized;
        }

        return origin + (vel * length);
    }

    private void ActivateUdpReceiver(int port, UdpReceiver.DataReceivedHandler handler, out UdpReceiver receiver)
    {
        receiver = new UdpReceiver(port);
        receiver.OnDataReceived += handler;
        receiver.Start();
    }

    void HandleReceivedData_Body(string data)
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
                lock (bodyLandmarks)
                {
                    bodyLandmarks.Clear(); // 이전 데이터를 지웁니다.
                    for (int i = 0; i < receivedLandmarks.Count; i++)
                    {
                        List<float> kvp = receivedLandmarks[i];

                        // 좌표 변환 (Y축 반전)
                        float x = kvp[0];
                        float y = -kvp[1];
                        float z = kvp[2];
                        Vector3 position = new Vector3(x, y, z);

                        eLandmark landmarkKey = (eLandmark)i;
                        bodyLandmarks[landmarkKey] = position;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error parsing data: " + ex.Message);
        }
    }
    void HandleReceivedData_Hand(string data)
    {
        try
        {
            var parsedData = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
            lock (handLockObj)
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
                                float y = -point[1];
                                float z = point[2];
                                landmarks[i] = new Vector3(x, y, z);
                            }

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
    void HandleReceivedData_Face(string data)
    {
        try
        {
            Dictionary<string, object> receivedData = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);

            Dictionary<string, float> receivedWeights = new Dictionary<string, float>();

            if (receivedData.ContainsKey("BlendShapeWeights"))
            {
                var blendShapeWeightsJson = receivedData["BlendShapeWeights"].ToString();
                Dictionary<string, float> blendShapeWeights = JsonConvert.DeserializeObject<Dictionary<string, float>>(blendShapeWeightsJson);

                foreach (var kvp in blendShapeWeights)
                {
                    receivedWeights[kvp.Key] = kvp.Value;
                }
            }

            if (receivedData.ContainsKey("blendshapes"))
            {
                var blendshapesJson = receivedData["blendshapes"].ToString();
                Dictionary<string, float> blendshapes = JsonConvert.DeserializeObject<Dictionary<string, float>>(blendshapesJson);

                foreach (var kvp in blendshapes)
                {
                    // 입 모양에 필요한 키만 추출
                    if (kvp.Key == "eyeBlinkLeft" || kvp.Key == "eyeBlinkRight" ||
                        kvp.Key == "mouthShapeA" || kvp.Key == "mouthShapeI" ||
                        kvp.Key == "mouthShapeU" || kvp.Key == "mouthShapeE" ||
                        kvp.Key == "mouthShapeO")
                    {
                        receivedWeights[kvp.Key] = kvp.Value;
                    }
                }
            }

            lock (faceLockObj)
            {
                newBlendShapeWeights = receivedWeights;
                newExpressionData = true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("블렌드 쉐이프 가중치 데이터 파싱 중 오류 발생: " + ex.Message);
        }
    }

    public void ActivateCharacter(GameObject go)
    {
        UnityChanPoseController body = go.GetComponent<UnityChanPoseController>();
        if (body) body.Activate(bodyUdpRecv);
        handMap = go.GetComponent<MediapipeHandMapper>();
        if (handMap) handMap.Activate(handUdpRecv);
        ExpressionController face = go.GetComponent<ExpressionController>();
        if (face) face.Activate(faceUdpRecv);

        curCharacter = go;
    }

    public void DeActivateCharacter(GameObject go)
    {
        UnityChanPoseController body = go.GetComponent<UnityChanPoseController>();
        if (body) body.Deactivate();
        if (handMap) handMap.Deactivate();
        ExpressionController face = go.GetComponent<ExpressionController>();
        if (face) face.Deactivate();
    }

    void OnApplicationQuit()
    {
        if (bodyUdpRecv != null) bodyUdpRecv.Stop();
        if (handUdpRecv != null) handUdpRecv.Stop();
        if (faceUdpRecv != null) faceUdpRecv.Stop();
    }
}
