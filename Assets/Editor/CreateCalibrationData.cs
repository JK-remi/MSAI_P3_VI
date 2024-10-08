using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UI;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;


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
                Calibrate();
        }
    }

    void Calibrate()
    {
        PersistentCalibrationData calibrationData = PersistentCalibrationData.CreateData(animator.name + GUID.Generate().ToString());

        parentCalibrationData.Clear();
        //Dictionary<HumanBodyBones, CalibrationData> parentCalibrationData = new Dictionary<HumanBodyBones, CalibrationData>();

        // Manually setting calibration data for the spine chain as we want really specific control over that.
        calibrationData.spineUpDown = new CalibrationData(animator.GetBoneTransform(HumanBodyBones.Spine), animator.GetBoneTransform(HumanBodyBones.Neck), eLandmark.NONE, eLandmark.NONE);
        //server.GetVirtualHip(), server.GetVirtualNeck());
        calibrationData.hipsTwist = new CalibrationData(animator.GetBoneTransform(HumanBodyBones.Hips), animator.GetBoneTransform(HumanBodyBones.Hips), eLandmark.RIGHT_HIP, eLandmark.LEFT_HIP);
        calibrationData.chest = new CalibrationData(animator.GetBoneTransform(HumanBodyBones.Chest), animator.GetBoneTransform(HumanBodyBones.Chest), eLandmark.RIGHT_HIP, eLandmark.LEFT_HIP);
        calibrationData.head = new CalibrationData(animator.GetBoneTransform(HumanBodyBones.Neck), animator.GetBoneTransform(HumanBodyBones.Head), eLandmark.NONE, eLandmark.NOSE);
        //server.GetVirtualNeck(), eLandmark.NOSE);

        // Adding calibration data automatically for the rest of the bones.
        AddCalibration(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, eLandmark.RIGHT_SHOULDER, eLandmark.RIGHT_ELBOW);
        AddCalibration(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, eLandmark.RIGHT_ELBOW, eLandmark.RIGHT_WRIST);

        AddCalibration(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, eLandmark.RIGHT_HIP, eLandmark.RIGHT_KNEE);
        AddCalibration(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, eLandmark.RIGHT_KNEE, eLandmark.RIGHT_ANKLE);

        AddCalibration(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, eLandmark.LEFT_SHOULDER, eLandmark.LEFT_ELBOW);
        AddCalibration(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, eLandmark.LEFT_ELBOW, eLandmark.LEFT_WRIST);

        AddCalibration(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, eLandmark.LEFT_HIP, eLandmark.LEFT_KNEE);
        AddCalibration(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, eLandmark.LEFT_KNEE, eLandmark.LEFT_ANKLE);

        if (bFootTracking)
        {
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
