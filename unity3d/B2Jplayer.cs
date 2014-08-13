using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using B2J;

public class B2Jplayer : B2JgenericPlayer {

	public TextAsset Map_numediart;

	// Use this for initialization
	void Start () {

		loadMapping( Map_numediart ); // mapping for model "bvh_numediart"

		if (B2J_server != null) {
			B2J_server.load( "bvh2json/data/thomas_se_leve_02" );
		}

//		// creating mapping infos for model 'bvh_numediart'
//		B2Jmapping mm = new B2Jmapping();
//		Transform[] allChildren = GetComponentsInChildren<Transform>();
//		foreach( Transform child in allChildren ) {
//			if ( child.name == "hips" ) {
//				mm.transform2Bones.Add( child, "Hips" );
//				mm.initialRotation.Add( child, child.localRotation );
//			} 
////			else 
//			if ( child.name == "head" ) {
//				mm.transform2Bones.Add( child, "Head" );
//				mm.initialRotation.Add( child, child.localRotation );
//			} 
////			else  
//			if ( child.name == "spine" ) {
//				mm.transform2Bones.Add( child, "Spine" );
//				mm.initialRotation.Add( child, child.localRotation );
//			} 
//			if ( child.name == "chest" ) {
//				mm.transform2Bones.Add( child, "Spine1" );
//				mm.initialRotation.Add( child, child.localRotation );
//			} 
////			else 
////			if ( child.name == "upper_arm_L" ) {
////				mm.transform2Bones.Add( child, "LeftArm" );
////				mm.initialRotation.Add( child, child.localRotation );
////			} 
////			else 
////			if ( child.name == "foot_L" ) {
////				mm.transform2Bones.Add( child, "LeftFoot" );
////				mm.initialRotation.Add( child, child.localRotation );
////			} 
////			else 
////			if ( child.name == "forearm_L" ) {
////				mm.transform2Bones.Add( child, "LeftForeArm" );
////				mm.initialRotation.Add( child, child.localRotation );
////			} 
////			else 
////			if ( child.name == "hand_L" ) {
////				mm.transform2Bones.Add( child, "LeftHand" );
////				mm.initialRotation.Add( child, child.localRotation );
////			} 
////			else 
////			if ( child.name == "shin_L" ) {
////				mm.transform2Bones.Add( child, "LeftLeg" );
////				mm.initialRotation.Add( child, child.localRotation );
////			} 
////			else 
//			if ( child.name == "shoulder_L" ) {
//				mm.transform2Bones.Add( child, "LeftShoulder" );
//				mm.initialRotation.Add( child, child.localRotation );
//			} 
//			if ( child.name == "shoulder_R" ) {
//				mm.transform2Bones.Add( child, "RightShoulder" );
//				mm.initialRotation.Add( child, child.localRotation );
//			} 
////			else 
////			if ( child.name == "thigh_L" ) {
////				mm.transform2Bones.Add( child, "LeftUpLeg" );
////				mm.initialRotation.Add( child, child.localRotation );
////			}  
////			if ( child.name == "shin_L" ) {
////				mm.transform2Bones.Add( child, "LeftLeg" );
////				mm.initialRotation.Add( child, child.localRotation );
////			}   
////			if ( child.name == "foot_L" ) {
////				mm.transform2Bones.Add( child, "LeftFoot" );
////				mm.initialRotation.Add( child, child.localRotation );
////			}  
////			if ( child.name == "thigh_R" ) {
////				mm.transform2Bones.Add( child, "RightUpLeg" );
////				mm.initialRotation.Add( child, child.localRotation );
////			}  
////			if ( child.name == "shin_R" ) {
////				mm.transform2Bones.Add( child, "RightLeg" );
////				mm.initialRotation.Add( child, child.localRotation );
////			}   
////			if ( child.name == "foot_R" ) {
////				mm.transform2Bones.Add( child, "RightFoot" );
////				mm.initialRotation.Add( child, child.localRotation );
////			} 
////			else 
//			if ( child.name == "neck" ) {
//				mm.transform2Bones.Add( child, "Neck" );
//				mm.initialRotation.Add( child, child.localRotation );
//			}
//		}
//		b2jMaps.Add( "bvh_numediart", mm );
//
//		mm = new B2Jmapping();
//		foreach( Transform child in allChildren ) {
//			if ( child.name == "forearm_L" ) {
//				mm.transform2Bones.Add( child, "Hips" );
//				mm.initialRotation.Add( child, child.localRotation );
//			} else if ( child.name == "forearm_R" ) {
//				mm.transform2Bones.Add( child, "Hips" );
//				mm.initialRotation.Add( child, child.localRotation );
//			}
//		}
//		b2jMaps.Add( "tester", mm );
//
//		if (server != null) {
//			server.load ("bvh2json/data/ariaII_02");
//		}

	}

	void Update () {

		sync();

//		sync();
//		B2Jplayhead testp = getPlayhead( "ariaII_02" );
//		if (testp != null)
//			testp.Speed = 0.01f;
////		testp = getPlayhead( "hips" );
////		if (testp != null)
////			testp.Speed = 3.0f;
//
//		foreach ( B2Jplayhead ph in b2jPlayheads ) {
//			if ( ph.Active ) {
//				foreach( KeyValuePair< Transform, Quaternion > kv in ph.Retriever.rotations ) {
//
//					Vector3 initeulers = b2jMaps[ ph.Retriever.model ].initialRotation[ kv.Key ].eulerAngles;
//					Vector3 neweulers = kv.Value.eulerAngles;
//
////					kv.Key.rotation = Quaternion.Euler(
////						initq.eulerAngles.x,
////						initq.eulerAngles.y,
////						initq.eulerAngles.z );
//
////					kv.Key.rotation = Quaternion.Euler(
////						initeulers.x + neweulers.x,
////						initeulers.y + neweulers.y,
////						initeulers.z + neweulers.z );
//
//					kv.Key.localRotation = Quaternion.Euler(
//						initeulers.x + neweulers.x,
//						initeulers.y + neweulers.y,
//						initeulers.z + neweulers.z );
//
////					kv.Key.localRotation = kv.Value;
//
//				}
//				foreach( KeyValuePair< Transform, Vector3 > kv in ph.Retriever.positions ) {
////					kv.Key.localPosition = new Vector3( kv.Value.x * 0.001f, kv.Value.y * 0.001f, kv.Value.z * 0.001f );
////					Debug.Log ( ph.ToString() + " / " + kv.Value.x + ", " + kv.Value.y + ", " + kv.Value.z );
//				}
//			}
//		}
		
	}

}
