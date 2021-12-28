using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Experimental.Animations;
using System.Reflection;
    
namespace Myy
{

public class AnimToMMD : MonoBehaviour
{


    Animator animator;

    public string animationStateName;
    public AnimationClip clipOfThisState;

    /* FIXME : Make a structure. Display it correctly */
    public Transform[] bonesTransforms;
    public string[] bonesMMDnames;
    int nElementsToDump;
    int gameFrame = 0;

    float clipFrameRate = 0;
    float clipLength = 0;

    Myy.VMD vmd = new VMD();

    public string vmdFilePath = "";
    public string vmdModelName = "miku";


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

    Quaternion[] baseRotations;
    Quaternion[] baseLocalRotations;
    Vector3[] basePositions;

    void Start()
    {

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

        animator = GetComponent<Animator>();

        clipFrameRate = clipOfThisState.frameRate;
        clipLength    = clipOfThisState.length;
        vmd.VMDName   = vmdModelName;
    }


    float elapsedTime = 0;

    /*void ResetBones()
    {
        for (int tIndex = 0; tIndex < nElementsToDump; tIndex++)
        {
            Transform t = bonesTransforms[tIndex];
            t.localPosition = basePositions[tIndex];
            t.rotation = baseRotations[tIndex];
        }
    }*/

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
            vmd.AddBoneFrame(bonesMMDnames[tIndex], gameFrame,
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



   /* After that are code samples I kept from here and here.
    * I don't know if they'll be useful at some point, though.
    */

    public struct GetGoalPositionAndRotationJob : IAnimationJob
    {
        public Vector3 leftFootGoalFromPosePosition;
        public Quaternion leftFootGoalFromPoseRotation;
        public Vector3 rightFootGoalFromPosePosition;
        public Quaternion rightFootGoalFromPoseRotation;
        public Vector3 leftHandGoalFromPosePosition;
        public Quaternion leftHandGoalFromPoseRotation;
        public Vector3 rightHandGoalFromPosePosition;
        public Quaternion rightHandGoalFromPoseRotation;
    
        public Vector3 bodyPosition;
        public Quaternion bodyRotation;
    
        public void ProcessRootMotion(AnimationStream stream) {}
        public void ProcessAnimation(AnimationStream stream)
        {
            var humanStream = stream.AsHuman();
    
            leftFootGoalFromPosePosition = humanStream.GetGoalPositionFromPose(AvatarIKGoal.LeftFoot);
            leftFootGoalFromPoseRotation = humanStream.GetGoalRotationFromPose(AvatarIKGoal.LeftFoot);
            rightFootGoalFromPosePosition = humanStream.GetGoalPositionFromPose(AvatarIKGoal.RightFoot);
            rightFootGoalFromPoseRotation = humanStream.GetGoalRotationFromPose(AvatarIKGoal.RightFoot);
            leftHandGoalFromPosePosition = humanStream.GetGoalPositionFromPose(AvatarIKGoal.LeftHand);
            leftHandGoalFromPoseRotation = humanStream.GetGoalRotationFromPose(AvatarIKGoal.LeftHand);
            rightHandGoalFromPosePosition = humanStream.GetGoalPositionFromPose(AvatarIKGoal.RightHand);
            rightHandGoalFromPoseRotation = humanStream.GetGoalRotationFromPose(AvatarIKGoal.RightHand);
    
            bodyPosition = humanStream.bodyPosition;
            bodyRotation = humanStream.bodyRotation;
        }
    }
    
    
    AnimationClip animationClip;
    
    void BakeIKGoal()
    {
        var animator = GetComponent<Animator>();
    
        var graph = PlayableGraph.Create();
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        var clipPlayable = AnimationClipPlayable.Create(graph, animationClip);
    
        var job = new GetGoalPositionAndRotationJob();
        var scriptPlayable = AnimationScriptPlayable.Create(graph, job, 1);
    
        scriptPlayable.ConnectInput(0, clipPlayable, 0, 1.0f);
    
        var output = AnimationPlayableOutput.Create(graph, "output", animator);
        output.SetSourcePlayable(scriptPlayable);
    
        var frameRate = animationClip.frameRate;
        var step = 1.0f / frameRate;
        var length = animationClip.length;
    
        for (float time = 0.0f; time < length; time += step)
        {
            graph.Evaluate(time == 0.0f ? 0.0f : step);
    
            job = scriptPlayable.GetJobData<GetGoalPositionAndRotationJob>();
    
                // IK goal are in avatar body local space
            Quaternion invRootQ = Quaternion.Inverse(job.bodyRotation);
            Vector3 goalPosition = (invRootQ * (job.leftFootGoalFromPosePosition - job.bodyPosition)) / animator.humanScale;
            Quaternion goalRotation = invRootQ * job.leftFootGoalFromPoseRotation;
            Debug.Log($"Time : {time} - position : {goalPosition} - rotation {goalRotation}");
        }
    
        graph.Destroy();
    
    }


	static readonly MethodInfo GetPreRotationMethod  = typeof(Avatar).GetMethod("GetPreRotation", BindingFlags.NonPublic | BindingFlags.Instance);
	static readonly MethodInfo GetPostRotationMethod = typeof(Avatar).GetMethod("GetPostRotation", BindingFlags.NonPublic | BindingFlags.Instance);
	static readonly MethodInfo GetLimitSignMethod    = typeof(Avatar).GetMethod("GetLimitSign", BindingFlags.NonPublic | BindingFlags.Instance);

    Quaternion GetPreRotation(Avatar avatar, HumanBodyBones bone)
    {
        return (Quaternion)GetPreRotationMethod.Invoke(avatar, new object[] { bone });
    }

    Quaternion GetPostRotation(Avatar avatar, HumanBodyBones bone)
    {
        return (Quaternion)GetPostRotationMethod.Invoke(avatar, new object[] { bone });
    }

	// Following code is by lox9973, from:
	// https://gitlab.com/lox9973/shadermotion/-/blob/master/Script/Common/HumanAxes.cs

	static Vector3 GetLimitSignFull(Avatar avatar, HumanBodyBones humanBone) {
		var sign = Vector3.zero;
		for(var b = humanBone; (int)b >= 0; ) {
			var s = (Vector3)GetLimitSignMethod.Invoke(avatar, new object[]{b});
			for(int i=0; i<3; i++)
            {
				if(HumanTrait.MuscleFromBone((int)b, i) < 0)
                {
					s[i] = 0;
                }
				else if(sign[i] == 0)
                {
                    sign[i] = s[i];
                }
            }

			if(s.x*s.y*s.z != 0) {
				for(int i=0; i<3 && (sign.x*sign.y*sign.z) != (s.x*s.y*s.z); i++)
                {
					if(HumanTrait.MuscleFromBone((int)humanBone, i) < 0)
                    {
                        sign[i] *= -1;
                    }

                }

				return sign;
			}

            /* Get the right parent ? */
			b = b == HumanBodyBones.LeftShoulder  ? HumanBodyBones.LeftUpperArm :
				b == HumanBodyBones.RightShoulder ? HumanBodyBones.RightUpperArm :
				(HumanBodyBones)HumanTrait.GetParentBone((int)b);
		}
		return new Vector3(1, 1, 1);
	}

}

}