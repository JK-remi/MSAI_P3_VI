using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
/// <sumemary>
/// Cache various values which will be reused during the runtime.
/// </summary>
public class CalibrationData
{
    public string parentn, childn;
    public eLandmark lmParent, lmChild; // for doing a lookup at runtime
    [System.NonSerialized]
    public Transform parent, child;
    [SerializeField]
    public Vector3 initialDir;
    [SerializeField]
    public Quaternion initialRotation;
    [SerializeField]
    public Quaternion targetRotation;

    public void Tick(Quaternion newTarget, float speed)
    {
        parent.rotation = newTarget;
        parent.rotation = Quaternion.Lerp(parent.localRotation, targetRotation, Time.deltaTime * speed);
    }

    public CalibrationData(Transform tParent, Transform tChild, eLandmark lmParent, eLandmark lmChild)
    {
        initialRotation = tParent.rotation;
        
        this.parent = tParent;
        this.child = tChild;
        this.lmParent = lmParent;
        this.lmChild = lmChild;

        initialDir = (tChild.position - tParent.position).normalized;

        parentn = GetPath(parent);
        childn = GetPath(child);
    }

    public void SetInitDir(Vector3 parentPos, Vector3 childPos) 
    {
        initialDir = (childPos - parentPos).normalized;
    }

    public CalibrationData ReconstructReferences()
    {
        SetFromPath(parentn, out parent);
        SetFromPath(childn, out child);
        return this;
    }
    private void SetFromPath(string path, out Transform target)
    {
        if(path != null&&path != "")
        {
            target = GameObject.Find(path).transform;
            return;
        }
        target = null;
    }
    private string GetPath(Transform child)
    {
        List<Transform> chain = new List<Transform>();
        while (child != null)
        {
            chain.Add(child);
            child = child.parent;
        }
        chain.Reverse();

        string s = "";
        foreach (Transform t in chain)
        {
            s += t.name + "/";
        }
        return s;
    }

}