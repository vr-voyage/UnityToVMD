using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Experimental.Animations;
using UnityEngine.Playables;
    
namespace Myy
{

public class AnimToVMD : MonoBehaviour
{

    [Serializable]
    public struct BoneName
    {
        public string mecanim;
        public string mmd;
    }

    [Serializable]
    public struct ConversionDatabase
    {
        public string version;
        public string type;
        public BoneName[] bones_names;
    }

    Animator animator;

    public string animationStateName;
    public AnimationClip clipOfThisState;

    public TextAsset conversionDatabaseJson;
    public Transform[] bonesTransforms;
    public string[] bonesMMDnames;
    int nElementsToDump;
    int gameFrame = 0;

    float clipFrameRate = 0;
    float clipLength = 0;

    Myy.VMD vmd = new VMD();

    public string vmdFilePath = "";
    public string vmdModelName = "miku";

    Quaternion[] baseRotations;
    Quaternion[] baseLocalRotations;
    Vector3[] basePositions;

    private void GenerateConversions()
    {
        
        ConversionDatabase conversionDatabase =
            JsonUtility.FromJson<ConversionDatabase>(conversionDatabaseJson.text);
        
        int nBones = conversionDatabase.bones_names.Length;
        List<Transform> modelBones = new List<Transform>(nBones);
        List<String> modelBonesMMDNames = new List<String>(nBones);
        foreach (BoneName bone_name in conversionDatabase.bones_names)
        {
            if (Enum.TryParse<HumanBodyBones>(bone_name.mecanim, out HumanBodyBones boneID))
            {
                Transform boneTransform = animator.GetBoneTransform(boneID);
                if (boneTransform == null)
                {
                    continue;
                }

                modelBones.Add(boneTransform);
                modelBonesMMDNames.Add(bone_name.mmd);
            }
            else
            {
                Debug.LogError($"Invalid bone name : {bone_name.mecanim}");
            }
        }

        bonesTransforms = modelBones.ToArray();
        bonesMMDnames = modelBonesMMDNames.ToArray();
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        GenerateConversions();

        nElementsToDump = Mathf.Min(bonesTransforms.Length, bonesMMDnames.Length);

        baseRotations = new Quaternion[nElementsToDump];
        baseLocalRotations = new Quaternion[nElementsToDump];
        basePositions = new Vector3[nElementsToDump];
        for (int i = 0; i < nElementsToDump; i++)
        {
            baseRotations[i]      = bonesTransforms[i].rotation;
            baseLocalRotations[i] = bonesTransforms[i].localRotation;
            basePositions[i]      = bonesTransforms[i].localPosition;
        }

        

        clipFrameRate = clipOfThisState.frameRate;
        clipLength    = clipOfThisState.length;
        vmd.VMDName   = vmdModelName;
    }


    float elapsedTime = 0;

    /* The idea is pretty dumb here :
     * Play an animation at different intervals, on the character.
     * Record the bones positions and rotations at that moment.
     * Write them to a VMD file once done playing.
     */
    void Update()
    {
        if (Time.deltaTime > 0.01) return;
        animator.Play(animationStateName, 0, elapsedTime);
        for (int tIndex = 0; tIndex < nElementsToDump; tIndex++)
        {
            Transform t = bonesTransforms[tIndex];
             vmd.AddBoneFrame(
                bonesMMDnames[tIndex], gameFrame,
                t.localPosition - basePositions[tIndex],
                GetRotationDeltaWorld(baseRotations[tIndex], t.rotation));
            /* Cancel the current parent rotation, to get the right
                * child rotation.
                */
            t.localRotation = baseLocalRotations[tIndex];
        }

        gameFrame++;
        if (elapsedTime > clipOfThisState.length)
        {
            vmd.Write(vmdFilePath);
            gameObject.SetActive(false);
        }
        elapsedTime += Time.deltaTime;

    }

    /// <summary>Get the Local rotation difference between two rotations</summary>
    /// <param name="baseRotation">The initial rotation.</param>
    /// <param name="currentRotation">The current rotation.</param>
    /// <returns>A quaternion representation the rotation to apply to baseRotation,
    /// in order to get to currentRotation, assuming that rotations are local.</returns>

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

}

}