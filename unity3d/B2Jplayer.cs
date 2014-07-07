using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using B2J;

public class B2Jplayer : MonoBehaviour {

	public GameObject offsetNode;

	public Transform Hips;
	public Transform Spine;
	public Transform Neck;
	public Transform Head;
	public Transform LeftShoulder;
	public Transform LeftUpperArm;
	public Transform LeftElbow; 
	public Transform LeftHand;
	public Transform RightShoulder;
	public Transform RightUpperArm;
	public Transform RightElbow;
	public Transform RightHand;
	public Transform LeftThigh;
	public Transform LeftKnee;
	public Transform LeftFoot;
	public Transform LeftToes;
	public Transform RightThigh;
	public Transform RightKnee;
	public Transform RightFoot;
	public Transform RightToes;

	private Transform[] bones;
	private Quaternion[] initialRotations;
	private Quaternion[] initialLocalRotations;
	private Vector3[] initialDirections;
	private Quaternion originalRotation;

	private Vector3 hipsUp;
	private Vector3 hipsRight;
	private Vector3 chestRight;

	// Use this for initialization
	void Start () {

		bones = new Transform[ 25 ];
		initialRotations = new Quaternion[ bones.Length ];
		initialLocalRotations = new Quaternion[bones.Length];
		initialDirections = new Vector3[bones.Length];

		MapBones();
		GetInitialDirections();
		GetInitialRotations();

//		B2Jserver.Instance.load( "bvh2json/data/ariaII_02" );
//		B2Jserver.Instance.load( "bvh2json/data/clavaeolina_01" );
//		B2Jserver.Instance.load( "bvh2json/data/test" );
		B2Jserver.Instance.load( "bvh2json/data/test" );

	}

	void MapBones() {

		if(Hips != null)
			bones[1] = Hips;
		if(Spine != null)
			bones[2] = Spine;
		if(Neck != null)
			bones[3] = Neck;
		if(Head != null)
			bones[4] = Head;
		
		if(LeftShoulder != null)
			bones[5] = LeftShoulder;
		if(LeftUpperArm != null)
			bones[6] = LeftUpperArm;
		if(LeftElbow != null)
			bones[7] = LeftElbow;
		if(LeftHand != null)
			bones[8] = LeftHand;
		if(RightShoulder != null)
			bones[11] = RightShoulder;
		if(RightUpperArm != null)
			bones[12] = RightUpperArm;
		if(RightElbow != null)
			bones[13] = RightElbow;
		if(RightHand != null)
			bones[14] = RightHand;
		
		// Hips 2
		if(Hips != null)
			bones[16] = Hips;
		
		if(LeftThigh != null)
			bones[17] = LeftThigh;
		if(LeftKnee != null)
			bones[18] = LeftKnee;
		if(LeftFoot != null)
			bones[19] = LeftFoot;
		if(LeftToes != null)
			bones[20] = LeftToes;
		
		if(RightThigh != null)
			bones[21] = RightThigh;
		if(RightKnee != null)
			bones[22] = RightKnee;
		if(RightFoot != null)
			bones[23] = RightFoot;
		if(RightToes!= null)
			bones[24] = RightToes;
	}

	void GetInitialDirections() {
		int[] intermediateBone = { 1, 2, 3, 5, 6, 7, 11, 12, 13, 17, 18, 19, 21, 22, 23};
		for (int i = 0; i < bones.Length; i++)
		{
			if( Array.IndexOf(intermediateBone, i) >= 0 )
			{
				// intermediary joint
				if(bones[i] && bones[i + 1])
				{
					initialDirections[i] = bones[i + 1].position - bones[i].position;
					initialDirections[i] = bones[i].InverseTransformDirection(initialDirections[i]);
				}
				else
				{
					initialDirections[i] = Vector3.zero;
				}
			}
			else
			{
				// end joint
				initialDirections[i] = Vector3.zero;
			}
		}
		
		if(Hips && LeftThigh && RightThigh)
		{
			hipsUp = ((RightThigh.position + LeftThigh.position) / 2.0f) - Hips.position;
			hipsUp = Hips.InverseTransformDirection(hipsUp);
			hipsRight = RightThigh.position - LeftThigh.position;
			hipsRight = Hips.InverseTransformDirection(hipsRight);
			Vector3.OrthoNormalize(ref hipsUp, ref hipsRight);
		}
		
		if(Spine && LeftUpperArm && RightUpperArm)
		{
			chestRight = RightUpperArm.position - LeftUpperArm.position;
			chestRight = Spine.InverseTransformDirection(chestRight);
			chestRight -= Vector3.Project(chestRight, initialDirections[2]);
		}
	}
	
	void GetInitialRotations()
	{
		if (offsetNode != null) {
			originalRotation = offsetNode.transform.rotation;
			offsetNode.transform.rotation = Quaternion.Euler (Vector3.zero);
		}

		for (int i = 0; i < bones.Length; i++) {
			if (bones [i] != null) {
				initialRotations [i] = bones [i].rotation;
				initialLocalRotations [i] = bones [i].localRotation;
			}
		}
	
	}
	
	// Update is called once per frame
	void Update () {

		float r = UnityEngine.Random.value;
		RotateBone( 4, 0, 0, r * 20 );

	}

	void RotateBone( int boneIndex, float rX, float rY, float rZ ) {
		
		Transform boneTransform = bones[boneIndex];
		if (boneTransform == null)
			return;

		boneTransform.rotation = Quaternion.Euler(
			initialRotations[boneIndex].eulerAngles.x + rX,
			initialRotations[boneIndex].eulerAngles.y + rY, 
			initialRotations[boneIndex].eulerAngles.z + rZ );
		
	}

}
