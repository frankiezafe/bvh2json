using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using B2J;

public class B2Jplayer : B2JgenericPlayer {

	public TextAsset Map_numediart;

	private Dictionary < Transform, Matrix4x4 > world2local;
	private Dictionary < string, Transform > armature;
	private int correctionCount;

	// Use this for initialization
	void Start () {

		interpolate = false;
		init();
		loadMapping( Map_numediart ); // mapping for model "bvh_numediart"
		if (B2J_server != null) {
			B2J_server.load( "bvh2json/data/thomas_se_leve_02" );
		}
		sync();
		armature = new Dictionary < string, Transform > ();
		world2local = new Dictionary < Transform, Matrix4x4 > ();
		Transform[] all_transforms = GetComponentsInChildren<Transform>();
		foreach( Transform transform in all_transforms ) {
			armature.Add( transform.name, transform );
			world2local.Add( transform, transform.worldToLocalMatrix );
		}
		B2Jplayhead ph = getPlayhead( "thomas_se_leve_02" );
		B2Jrecord rec = ph.Record;
		B2Jmap m = B2J_maps["bvh_numediart"];

		ph.Loop = B2Jloop.B2JLOOPPALINDROME;
		ph.Multiplier = 2;

//		correction = new Dictionary<Transform, Quaternion> ();
//		foreach ( B2Jbone b in rec.bones ) {
//			if ( b.parent == null ) {
//				Debug.Log ( "starting from: " + b.name );
//				DefaultPose( b, m, rec );
//			}
//		}

//		foreach ( KeyValuePair< int, Transform > pair in m.transformById ) {
//			B2Jbone b = rec.bones[ pair.Key ];
//			Transform t = pair.Value;
//
//			Vector3 delta = b.rest;
//			Vector3 rotated_delta = t.worldToLocalMatrix.MultiplyPoint3x4( delta );
//
//			Quaternion aa = Quaternion.FromToRotation( t.localPosition, rotated_delta );
//
//			t.localRotation = aa;
//		}


	}

//	void DefaultPose( B2Jbone b, B2Jmap m, B2Jrecord rec ) {
//
//		// find related transform
//		Transform t = null;
//		foreach ( KeyValuePair< int, Transform > pair in m.transformById ) {
//			if ( rec.bones[ pair.Key ] == b ) {
//				t = pair.Value;
//				break;
//			}
//		}
//
//		// if the bone is mapped
//		if ( t != null 
//		    && t.parent != null
//		    //&& b.children.Count != 0
//		    && b.parent != null
//		    ) {
//
//			Debug.Log ( "DefaultPose [" + correctionCount + "]: " + b.name + " & " + t.name );
////
////			Matrix4x4 T = Matrix4x4.TRS( Vector3.zero, t.localRotation, Vector3.one );
////			Matrix4x4 Ti = T.inverse;
////
////			Quaternion aa = Quaternion.FromToRotation( b.parent.rest, b.rest );
////			Matrix4x4 mat = Matrix4x4.TRS( Vector3.zero, aa, Vector3.one );
////
////			mat = Ti * mat * T;
////
////			t.localRotation = Quaternion.LookRotation( mat.GetColumn(2), mat.GetColumn(1) );
//
//
////			Vector3 delta = b.rest;
////			Vector3 rotated_delta = t.parent.worldToLocalMatrix.MultiplyPoint3x4( b.rest );
////			Vector4 temp = t.worldToLocalMatrix.GetColumn( 3 );
////			rotated_delta.x -= temp.x;
////			rotated_delta.y -= temp.y;
////			rotated_delta.z -= temp.z;
////			Quaternion aa = Quaternion.FromToRotation( t.localPosition, rotated_delta );
////
////			t.localRotation = aa;
//
////			Quaternion aa = Quaternion.FromToRotation( t.localPosition, rotated_delta );
////			Quaternion mo = Quaternion.FromToRotation( b.parent == null ? Vector3.up : b.parent.rest, b.rest );
////			Quaternion mo = Quaternion.FromToRotation( b.rest, b.children[ b.children.Count - 1 ].rest );
////			Quaternion un = Quaternion.FromToRotation( 
////			                                          t.parent == null ? t.position : t.position - t.parent.position, 
////			                                          t == null ? Vector3.up : t.position 
////			                                          );
////			Quaternion diff = un * Quaternion.Inverse( mo );
////			
////			t.localRotation = t.localRotation * diff;
//
//
////			Vector3 res = b.rest;
////			res = t.worldToLocalMatrix.MultiplyPoint3x4( res );
////			Vector4 temp = t.worldToLocalMatrix.GetColumn( 3 );
//////			res.x -= temp.x;
//////			res.y -= temp.y;
//////			res.z -= temp.z;
////
////			Quaternion rot = Quaternion.FromToRotation( res, t.localPosition );
////			t.localRotation = rot ;
////
////			Quaternion mo = Quaternion.FromToRotation( b.rest, b.parent == null ? Vector3.up : b.parent.rest );
////			Quaternion un = Quaternion.FromToRotation( 
////			       t.parent.parent == null ? t.parent.localPosition : t.parent.position - t.parent.parent.position, 
////			       t.parent == null ? Vector3.up : t.parent.localPosition 
////			       );
////			Quaternion diff = un * Quaternion.Inverse( mo );
////			t.parent.rotation =  t.parent.rotation * Quaternion.Inverse( diff );
////
////			//Debug.Log( t.localPosition );
////			correction.Add( t, Quaternion.identity );
//
//			correctionCount++;
//
//		}
//
//		// looping over childrens
//		foreach (B2Jbone child in b.children) {
//			DefaultPose( child, m, rec );
//		}
//
//	}

	void Update() {

		sync();
		B2Jplayhead ph = getPlayhead( "thomas_se_leve_02" );
//		if ( ph != null ) {
//			ph.Speed = 0.5f;
//		}
		render();

//		B2Jmap m = B2J_maps["bvh_numediart"];
//		foreach ( KeyValuePair< int, Transform > pair in m.transformById ) {
//			Quaternion q = ph.Rotations[ pair.Key ];
//			Transform t = pair.Value;
//			//			t.localRotation = defaultRot[ t ] * q;
//			t.localRotation = q;
//		}

		Quaternion corr = Quaternion.identity;
		corr.eulerAngles = new Vector3 ( -90, 0, 0 );

		Matrix4x4 loc = new Matrix4x4();
		loc.SetTRS( Vector3.zero, corr, Vector3.one );
		Matrix4x4 loci = loc.inverse;

		// and applying on the model
		foreach ( KeyValuePair< Transform, Quaternion > pair in updatedRots ) {
			Transform t = pair.Key;
//			Debug.Log( this.name + " bone: " + t.name + " mat: " + t.worldToLocalMatrix.ToString() );
			Matrix4x4 mat = new Matrix4x4();
			mat.SetTRS( Vector3.zero, pair.Value, Vector3.one );
			// thierry way
//			Matrix4x4 tmat = world2local[ t ];
//			mat = tmat.inverse * mat * tmat;
//			t.localRotation = Quaternion.LookRotation( mat.GetColumn(2), mat.GetColumn(1) ) ;
			// françois way
			mat = loci * mat * loc;
			t.localRotation = Quaternion.LookRotation( mat.GetColumn(2), mat.GetColumn(1) ) * localRotations[ t ];
		}

	}

}
