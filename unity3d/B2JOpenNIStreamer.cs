using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace B2J {

	public enum BoneMask
	{
		ROOT = 0,
		PLAYER_CENTER,
		HIP_CENTER,
		HIP_LEFT,
		HIP_RIGHT,
		SPINE,
		SHOULDER_CENTER,
		SHOULDER_LEFT,
		SHOULDER_RIGHT,
		HEAD,
		ELBOW_LEFT,
		WRIST_LEFT,
		HAND_LEFT,
		ELBOW_RIGHT,
		WRIST_RIGHT,
		HAND_RIGHT,
		KNEE_LEFT,
		ANKLE_LEFT,
		FOOT_LEFT,
		KNEE_RIGHT,
		ANKLE_RIGHT,
		FOOT_RIGHT,
		COUNT
	}

	public class B2JOpenNIStreamer : MonoBehaviour {

		public B2Jserver B2Jserver;

		private B2Jrecord visitorStream;
		private B2Jrecord dancerStream;

		private int frameCount;
		[ SerializeField ] KINECT_DATA kd;
		
		public int current;
		public List< Quaternion > bonesVisitor;

		// Use this for initialization
		void Start () {

			// kd.SequenceTable[0].PoseTable[0];

			if ( B2Jserver != null ) {
				visitorStream = B2Jserver.createOpenniRecord( "openni_visitor" );
				dancerStream = B2Jserver.createOpenniRecord( "openni_dancer" );
			}

			frameCount = 0;

		}

		public Quaternion retrieveRotation( Matrix4x4 mat ) {
			return Quaternion.LookRotation( mat.GetColumn(2), mat.GetColumn(1) ) ;
		}

		public Vector3 retrieveTranslation( Matrix4x4 mat ) {
			return mat.GetColumn (3);
		}

		// Update is called once per frame
		void Update () {
		
			current = frameCount;

				// updating data in visitor record!
//				Head
//				Neck
//				LeftShoulder
//				LeftElbow
//				LeftHand
//				RightShoulder
//				RightElbow
//				RightHand 
//				Torso
//				LeftHip
//				LeftKnee
//				LeftFoot
//				RightHip
//				RightKnee
//				RightFoot
//				Hip -- GENERATED BONE!
			
			B2Jkey key = visitorStream.keys[ 0 ];

			bonesVisitor.Clear ();
			foreach ( Quaternion q in key.rotations ) {
				bonesVisitor.Add( q );
			}

			for ( int i = 0; i < visitorStream.bones.Count; i++ ) {
				B2Jbone b = visitorStream.bones[ i ];
				if ( b.name == "Head" ) {
					key.rotations[ i ] = retrieveRotation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.HEAD ] );
					key.positions[ i ] = retrieveTranslation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.HEAD ] );
				} else if ( b.name == "Neck" ) {
					key.rotations[ i ] = retrieveRotation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.SHOULDER_CENTER ] );
					key.positions[ i ] = retrieveTranslation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.SHOULDER_CENTER ] );
				} else if ( b.name == "LeftShoulder" ) {
					key.rotations[ i ] = retrieveRotation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.SHOULDER_LEFT ] );
					key.positions[ i ] = retrieveTranslation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.SHOULDER_LEFT ] );
				} else if ( b.name == "LeftElbow" ) {
					key.rotations[ i ] = retrieveRotation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.ELBOW_LEFT ] );
					key.positions[ i ] = retrieveTranslation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.ELBOW_LEFT ] );
				} else if ( b.name == "LeftHand" ) {
					key.rotations[ i ] = retrieveRotation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.HAND_LEFT ] );
					key.positions[ i ] = retrieveTranslation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.HAND_LEFT ] );
				} else if ( b.name == "RightShoulder" ) {
					key.rotations[ i ] = retrieveRotation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.SHOULDER_RIGHT ] );
					key.positions[ i ] = retrieveTranslation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.SHOULDER_RIGHT ] );
				} else if ( b.name == "RightElbow" ) {
					key.rotations[ i ] = retrieveRotation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.ELBOW_RIGHT ] );
					key.positions[ i ] = retrieveTranslation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.ELBOW_RIGHT ] );
				} else if ( b.name == "RightHand" ) {
					key.rotations[ i ] = retrieveRotation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.HAND_RIGHT ] );
					key.positions[ i ] = retrieveTranslation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.HAND_RIGHT ] );
				} else if ( b.name == "Torso" ) {
					key.rotations[ i ] = retrieveRotation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.SPINE ] );
					key.positions[ i ] = retrieveTranslation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.SPINE ] );
				} else if ( b.name == "LeftHip" ) {
					key.rotations[ i ] = retrieveRotation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.HIP_LEFT ] );
					key.positions[ i ] = retrieveTranslation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.HIP_LEFT ] );
				} else if ( b.name == "LeftKnee" ) {
					key.rotations[ i ] = retrieveRotation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.KNEE_LEFT ] );
					key.positions[ i ] = retrieveTranslation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.KNEE_LEFT ] );
				} else if ( b.name == "LeftFoot" ) {
					key.rotations[ i ] = retrieveRotation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.FOOT_LEFT ] );
					key.positions[ i ] = retrieveTranslation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.FOOT_LEFT ] );
				} else if ( b.name == "RightHip" ) {
					key.rotations[ i ] = retrieveRotation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.HIP_RIGHT ] );
					key.positions[ i ] = retrieveTranslation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.HIP_RIGHT ] );
				} else if ( b.name == "RightKnee" ) {
					key.rotations[ i ] = retrieveRotation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.KNEE_RIGHT ] );
					key.positions[ i ] = retrieveTranslation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.KNEE_RIGHT ] );
				} else if ( b.name == "RightFoot" ) {
					key.rotations[ i ] = retrieveRotation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.FOOT_RIGHT ] );
					key.positions[ i ] = retrieveTranslation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.FOOT_RIGHT ] );
				} else if ( b.name == "Hip" ) {
					// rotation ????????????
					Vector3 v1 = retrieveTranslation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.HIP_LEFT ] );
					Vector3 v2 = retrieveTranslation( kd.SequenceTable[ frameCount ].PoseTable[ (int) BoneMask.HIP_RIGHT ] );
					key.positions[ i ] = B2Jutils.vectorSlerp( v1, v2, 0.5f );
				}

			}

			frameCount ++;
			if ( frameCount >= kd.SequenceTable.Count ) {
				frameCount = 0;
			}
		}
	}

}