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
//			B2J_server.load( "bvh2json/data/capoiera" );
		}
		sync();
		armature = new Dictionary < string, Transform > ();
		world2local = new Dictionary < Transform, Matrix4x4 > ();
		Transform[] all_transforms = GetComponentsInChildren<Transform>();
		foreach( Transform transform in all_transforms ) {
			armature.Add( transform.name, transform );
			world2local.Add( transform, transform.worldToLocalMatrix );
		}
//		B2Jplayhead ph = getPlayhead( "capoiera" );
		B2Jplayhead ph = getPlayhead( "thomas_se_leve_02" );
		B2Jrecord rec = ph.Record;
		B2Jmap m = B2J_maps["bvh_numediart"];

//		ph.Loop = B2Jloop.B2JLOOPPALINDROME;

	}

	void Update() {

		sync();
		B2Jplayhead ph = getPlayhead( "thomas_se_leve_02" );
		if ( ph != null ) {
			ph.Speed = 2.0f;
		}
		render();

		Quaternion corr = Quaternion.identity;
		corr.eulerAngles = new Vector3 ( -90, 0, 0 );

		Matrix4x4 loc = new Matrix4x4();
		loc.SetTRS( Vector3.zero, corr, Vector3.one );
		Matrix4x4 loci = loc.inverse;

		// and applying on the model
		foreach ( KeyValuePair< Transform, Quaternion > pair in updatedRots ) {

			Transform t = pair.Key;
			Quaternion locValue = Quaternion.identity;
			Matrix4x4 mat = new Matrix4x4();
			mat.SetTRS( Vector3.zero, pair.Value, Vector3.one );

			Matrix4x4 tmat = world2local[ t ];
			mat = tmat * mat * tmat.inverse;

			t.localRotation = localRotations[t] * Quaternion.LookRotation( mat.GetColumn(2), mat.GetColumn(1) );

			// thierry way
//			Matrix4x4 tmat = world2local[ t ];
//			mat = tmat.inverse * mat * tmat;
//			t.localRotation = Quaternion.LookRotation( mat.GetColumn(2), mat.GetColumn(1) ) ;
			// françois way
//			mat = loci * mat * loc;
//			t.localRotation = Quaternion.LookRotation( mat.GetColumn(2), mat.GetColumn(1) ) * localRotations[ t ];
		}

	}

}
