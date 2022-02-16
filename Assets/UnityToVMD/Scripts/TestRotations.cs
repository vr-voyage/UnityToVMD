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

        Quaternion q = Quaternion.Euler(0, 0, 0);
        Quaternion r = Quaternion.Euler(60, 25, 10);
        Quaternion qCancelled = q * Quaternion.Inverse(r);
        Debug.Log(VMD.StringProp(q));
        Debug.Log(VMD.StringProp(qCancelled));
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.T))
        {
            for (int i = 1; i < nElementsToDump; i++)
            {
                Transform t = transforms[i];
                Quaternion q = t.rotation * Quaternion.Inverse(globals[i]);
                Quaternion p = transforms[i-1].rotation * Quaternion.Inverse(globals[i-1]);
                q = q * Quaternion.Inverse(p);
                Debug.Log(VMD.StringProp(VMD.MMDRotation(q)));
            }
        }


    }
}
