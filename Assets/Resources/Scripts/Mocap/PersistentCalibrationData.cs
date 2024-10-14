using UnityEngine;

public class PersistentCalibrationData : ScriptableObject
{
    [SerializeField]
    public CalibrationEntry[] parentCalibrationData;
    [SerializeField]
    public CalibrationData spineUpDown, hipsTwist, chest, head;

#if UNITY_EDITOR
    public static PersistentCalibrationData CreateData(string fileName, string folder = "Assets/Resources/CalibrationData/")
    {
        PersistentCalibrationData p = ScriptableObject.CreateInstance<PersistentCalibrationData>();
        string path = folder + fileName + ".asset";
        UnityEditor.AssetDatabase.CreateAsset(p, path);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
        UnityEditor.EditorUtility.FocusProjectWindow();
        UnityEditor.Selection.activeObject = p;
        return p;
    }
#endif

    public void Dirty()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    [System.Serializable]
    public class CalibrationEntry
    {
        public HumanBodyBones bone;
        public CalibrationData data;
        public CalibrationEntry Reconstruct()
        {
            data.ReconstructReferences();
            return this;
        }
    }
}