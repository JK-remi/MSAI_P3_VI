using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading;

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

    private UdpClient client;
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

        client = new UdpClient(port);
        client.BeginReceive(new System.AsyncCallback(ReceiveData), null);
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

        animator.enabled = false; // disable animator to stop interference.
    }

    void ReceiveData(System.IAsyncResult result)
    {
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
        byte[] data = client.EndReceive(result, ref anyIP);
        string json = Encoding.UTF8.GetString(data);

        Dictionary<string, List<float[]>> receivedDatas = JsonConvert.DeserializeObject<Dictionary<string, List<float[]>>>(json);
        List<float[]> receivedLandmarks = null;
        if (receivedDatas.ContainsKey("pose"))
        {
            receivedLandmarks = receivedDatas["pose"];
        }

        lock (landmarks)
        {
            for (int i=0; i< receivedLandmarks.Count; i++)
            {
                float[] kvp = receivedLandmarks[i];

                // 좌표 변환 (Y축 반전)
                float x = kvp[0];
                float y = -kvp[1];
                float z = kvp[2];
                Vector3 position = new Vector3(x, y, z);
                if(landmarks.ContainsKey((eLandmark)i) == false)
                {
                    landmarks.Add((eLandmark)i,position);
                }
                else
                {
                    landmarks[(eLandmark)i] = position;
                }
            }
        }

        client.BeginReceive(new System.AsyncCallback(ReceiveData), null);
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
        // 각 사지에 대해 방향 벡터를 계산하고 해당 본의 회전을 설정합니다.
        // Compute the new rotations for each limbs of the avatar using the calibration datas we created before.
        foreach(var i in parentCalibrationData)
        {
            Vector3 curDir = GetCurDirection(i.Value.lmChild, i.Value.lmParent);
            Quaternion deltaRotTracked = Quaternion.FromToRotation(i.Value.initialDir, curDir);
            i.Value.parent.rotation = deltaRotTracked * i.Value.initialRotation;
        }

        // Deal with spine chain as a special case.
        if (parentCalibrationData.Count > 0)
        {
            Vector3 hipCenter = (landmarks[eLandmark.LEFT_HIP] + landmarks[eLandmark.RIGHT_HIP]) / 2f;
            Vector3 shoulderCenter = (landmarks[eLandmark.LEFT_SHOULDER] + landmarks[eLandmark.RIGHT_SHOULDER]) / 2f;
            Vector3 hipTwistDir = GetCurDirection(hipsTwist.lmChild, hipsTwist.lmParent);

            Vector3 hd = GetCurDirection(landmarks[head.lmChild], shoulderCenter);
            // Some are partial rotations which we can stack together to specify how much we should rotate.
            Quaternion headr = Quaternion.FromToRotation(head.initialDir, hd);
            Quaternion twist = Quaternion.FromToRotation(hipsTwist.initialDir,
                Vector3.Slerp(hipsTwist.initialDir, hipTwistDir, .25f));
            Quaternion updown = Quaternion.FromToRotation(spineUpDown.initialDir,
                Vector3.Slerp(spineUpDown.initialDir, GetCurDirection(shoulderCenter, hipCenter), .25f));

            // Compute the final rotations.
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

    Vector3 GetCurDirection(Vector3 vChild, Vector3 vParent)
    {
        return (vChild - vParent).normalized;
    }

    Vector3 GetCurDirection(eLandmark lmChild, eLandmark lmParent)
    {
        return GetCurDirection(landmarks[lmChild], landmarks[lmParent]);
    }

    void OnApplicationQuit()
    {
        if (client != null)
            client.Close();
    }
}
