using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Myy;
public class TestRotations : MonoBehaviour
{

    public Transform[] transforms;

    Quaternion[] globals;
    Quaternion[] locals;
    Vector3[] vectors;

    int nElementsToDump = 0;

    Quaternion GetRotationDeltaSelf(Quaternion baseRotation, Quaternion currentRotation)
    {
        return Quaternion.Inverse(baseRotation) * currentRotation;
    }

    /// <summary>Get the Global rotation difference between two rotations.</summary>
    /// <param name="baseRotation">The initial rotation.</param>
    /// <param name="currentRotation">The current rotation.</param>
    /// <returns>A quaternion representation the rotation to apply to baseRotation,
    /// in order to get to currentRotation, assuming that rotations are global.</returns>

    Quaternion GetRotationDeltaWorld(Quaternion baseRotation, Quaternion currentRotation)
    {
        return currentRotation * Quaternion.Inverse(baseRotation);
    }

    // Start is called before the first frame update
    void Start()
    {
        nElementsToDump = transforms.Length;
        globals = new Quaternion[nElementsToDump];
        locals  = new Quaternion[nElementsToDump];
        vectors = new Vector3[nElementsToDump];

        for (int i = 0; i < nElementsToDump; i++)
        {
            globals[i] = transforms[i].rotation;
            locals[i]  = transforms[i].localRotation;
            vectors[i] = transforms[i].localPosition;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.T))
        {
            for (int i = 0; i < nElementsToDump; i++)
            {

                Transform t = transforms[i];
                Debug.Log(t.name);
                Debug.Log(VMD.StringProp(GetRotationDeltaSelf(globals[i], t.rotation)));
                Debug.Log(VMD.StringProp(GetRotationDeltaSelf(t.rotation, globals[i])));
                Debug.Log(VMD.StringProp(GetRotationDeltaSelf(globals[i], t.localRotation)));
                Debug.Log(VMD.StringProp(GetRotationDeltaSelf(t.rotation, globals[i])));
                Debug.Log(VMD.StringProp(GetRotationDeltaSelf(locals[i], t.rotation)));
                Debug.Log(VMD.StringProp(GetRotationDeltaSelf(t.rotation, locals[i])));
                Debug.Log(VMD.StringProp(GetRotationDeltaSelf(locals[i], t.localRotation)));
                Debug.Log(VMD.StringProp(GetRotationDeltaSelf(t.rotation, locals[i])));               
                Debug.Log(VMD.StringProp(GetRotationDeltaWorld(globals[i], t.rotation)));
                Debug.Log(VMD.StringProp(GetRotationDeltaWorld(t.rotation, globals[i])));
                Debug.Log(VMD.StringProp(GetRotationDeltaWorld(globals[i], t.localRotation)));
                Debug.Log(VMD.StringProp(GetRotationDeltaWorld(t.rotation, globals[i])));
                Debug.Log(VMD.StringProp(GetRotationDeltaWorld(locals[i], t.rotation)));
                Debug.Log(VMD.StringProp(GetRotationDeltaWorld(t.rotation, locals[i])));
                Debug.Log(VMD.StringProp(GetRotationDeltaWorld(locals[i], t.localRotation)));
                Debug.Log(VMD.StringProp(GetRotationDeltaWorld(t.rotation, locals[i])));       
            }
        }
    }
}
