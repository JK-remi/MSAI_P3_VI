using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


public class CreateCalibrationData : EditorWindow
{
    public Animator animator;
    public bool bFootTracking = false;
    private Dictionary<HumanBodyBones, CalibrationData> parentCalibrationData = new Dictionary<HumanBodyBones, CalibrationData>();

    [MenuItem("Eruza/CreateCalibrationData")]
    static void Init()
    {
        CreateCalibrationData wnd = GetWindow<CreateCalibrationData>();
        wnd.Show();
        wnd.titleContent = new GUIContent("CreateCalibrationData");
    }

    public void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Model: ");
        animator = EditorGUILayout.ObjectField(animator, typeof(Animator), true) as Animator;
        EditorGUILayout.EndHorizontal();

        bFootTracking = EditorGUILayout.Toggle("FootTracking: ", bFootTracking);

        if (GUILayout.Button("Create"))
        {
            if (animator != null)
            {
                Calibrate();
                HandMapping();
                Expression();
            }
        }
    }

    void Expression()
    {
        ExpressionController exp = animator.GetComponent<ExpressionController>();
        if(exp == null)
        {
            exp = animator.AddComponent<ExpressionController>();
        }

        Transform face = animator.transform.Find("Face");
        if (face == null) return;

        exp.faceMeshRenderer = face.GetComponent<SkinnedMeshRenderer>();    
    }

    void HandMapping()
    {
        MediapipeHandMapper hand = animator.GetComponent<MediapipeHandMapper>();
        if(hand == null)
        {
            hand = animator.AddComponent<MediapipeHandMapper>();
        }

        hand.leftRootBone = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        hand.leftThumbBones.Add(animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal));
        hand.leftThumbBones.Add(animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate));
        hand.leftThumbBones.Add(animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal));

        hand.leftIndexBones.Add(animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal));
        hand.leftIndexBones.Add(animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate));
        hand.leftIndexBones.Add(animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal));

        hand.leftMiddleBones.Add(animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal));
        hand.leftMiddleBones.Add(animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate));
        hand.leftMiddleBones.Add(animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal));

        hand.leftRingBones.Add(animator.GetBoneTransform(HumanBodyBones.LeftRingProximal));
        hand.leftRingBones.Add(animator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate));
        hand.leftRingBones.Add(animator.GetBoneTransform(HumanBodyBones.LeftRingDistal));

        hand.leftLittleBones.Add(animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal));
        hand.leftLittleBones.Add(animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate));
        hand.leftLittleBones.Add(animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal));

        hand.rightRootBone = animator.GetBoneTransform(HumanBodyBones.RightHand);
        hand.rightThumbBones.Add(animator.GetBoneTransform(HumanBodyBones.RightThumbProximal));
        hand.rightThumbBones.Add(animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate));
        hand.rightThumbBones.Add(animator.GetBoneTransform(HumanBodyBones.RightThumbDistal));

        hand.rightIndexBones.Add(animator.GetBoneTransform(HumanBodyBones.RightIndexProximal));
        hand.rightIndexBones.Add(animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate));
        hand.rightIndexBones.Add(animator.GetBoneTransform(HumanBodyBones.RightIndexDistal));

        hand.rightMiddleBones.Add(animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal));
        hand.rightMiddleBones.Add(animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate));
        hand.rightMiddleBones.Add(animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal));

        hand.rightRingBones.Add(animator.GetBoneTransform(HumanBodyBones.RightRingProximal));
        hand.rightRingBones.Add(animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate));
        hand.rightRingBones.Add(animator.GetBoneTransform(HumanBodyBones.RightRingDistal));

        hand.rightLittleBones.Add(animator.GetBoneTransform(HumanBodyBones.RightThumbProximal));
        hand.rightLittleBones.Add(animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate));
        hand.rightLittleBones.Add(animator.GetBoneTransform(HumanBodyBones.RightThumbDistal));
    }

    void Calibrate()
    {
        PersistentCalibrationData calibrationData = PersistentCalibrationData.CreateData(animator.name + "_calibration");
        UnityChanPoseController poseController = animator.GetComponent<UnityChanPoseController>(); 
        if(poseController == null)
        {
            poseController = animator.AddComponent<UnityChanPoseController>();
        }
        poseController.calibrationData = calibrationData;

        parentCalibrationData.Clear();
        //Dictionary<HumanBodyBones, CalibrationData> parentCalibrationData = new Dictionary<HumanBodyBones, CalibrationData>();

        // Manually setting calibration data for the spine chain as we want really specific control over that.
        calibrationData.spineUpDown = new CalibrationData(animator.GetBoneTransform(HumanBodyBones.Spine), animator.GetBoneTransform(HumanBodyBones.Neck), eLandmark.NONE, eLandmark.NONE);
        //server.GetVirtualHip(), server.GetVirtualNeck());
        calibrationData.hipsTwist = new CalibrationData(animator.GetBoneTransform(HumanBodyBones.Hips), animator.GetBoneTransform(HumanBodyBones.Hips), eLandmark.RIGHT_HIP, eLandmark.LEFT_HIP);
        calibrationData.chest = new CalibrationData(animator.GetBoneTransform(HumanBodyBones.Chest), animator.GetBoneTransform(HumanBodyBones.Chest), eLandmark.RIGHT_HIP, eLandmark.LEFT_HIP);
        calibrationData.head = new CalibrationData(animator.GetBoneTransform(HumanBodyBones.Neck), animator.GetBoneTransform(HumanBodyBones.Head), eLandmark.NONE, eLandmark.NOSE);
        calibrationData.head.initialDir = new Vector3(0.07303416f, 0.4054966f, -0.9111742f);
        //server.GetVirtualNeck(), eLandmark.NOSE);

        // Adding calibration data automatically for the rest of the bones.
        AddCalibration(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, eLandmark.RIGHT_SHOULDER, eLandmark.RIGHT_ELBOW);
        AddCalibration(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, eLandmark.RIGHT_ELBOW, eLandmark.RIGHT_WRIST);

        AddCalibration(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, eLandmark.LEFT_SHOULDER, eLandmark.LEFT_ELBOW);
        AddCalibration(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, eLandmark.LEFT_ELBOW, eLandmark.LEFT_WRIST);

        if (bFootTracking)
        {
            AddCalibration(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, eLandmark.RIGHT_HIP, eLandmark.RIGHT_KNEE);
            AddCalibration(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, eLandmark.RIGHT_KNEE, eLandmark.RIGHT_ANKLE);

            AddCalibration(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, eLandmark.LEFT_HIP, eLandmark.LEFT_KNEE);
            AddCalibration(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, eLandmark.LEFT_KNEE, eLandmark.LEFT_ANKLE);

            AddCalibration(HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes, eLandmark.LEFT_ANKLE, eLandmark.LEFT_FOOT_INDEX);
            AddCalibration(HumanBodyBones.RightFoot, HumanBodyBones.RightToes, eLandmark.RIGHT_ANKLE, eLandmark.RIGHT_FOOT_INDEX);
        }

        List<PersistentCalibrationData.CalibrationEntry> calibrations = new List<PersistentCalibrationData.CalibrationEntry>();
        foreach (KeyValuePair<HumanBodyBones, CalibrationData> k in parentCalibrationData)
        {
            calibrations.Add(new PersistentCalibrationData.CalibrationEntry() { bone = k.Key, data = k.Value });
        }
        calibrationData.parentCalibrationData = calibrations.ToArray();

        calibrationData.Dirty();
    }

    private void AddCalibration(HumanBodyBones parent, HumanBodyBones child, eLandmark trackParent, eLandmark trackChild)
    {
        parentCalibrationData.Add(parent,
            new CalibrationData(animator.GetBoneTransform(parent), animator.GetBoneTransform(child), trackParent, trackChild));
    }


}
