using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using B2J;

public class B2Jplayer : B2JgenericPlayer {

	public TextAsset Map_numediart;
	public TextAsset Map_tester;

	// Use this for initialization
	void Start () {


		Matrix4x4 mat = new Matrix4x4 ();
		mat.m00 = 0;
		mat.m01 = 0;
		mat.m02 = 0;
		mat.m03 = 0;
		Debug.Log ( mat.ToString() );

		interpolate = false;

		init();

		loadMapping( Map_numediart ); // mapping for model "bvh_numediart"
		loadMapping( Map_tester ); // mapping for model "tester"

		if (B2J_server != null) {
			B2J_server.load( "bvh2json/data/bending_left_hip" );
//			B2J_server.load( "bvh2json/data/thomas_se_leve_02" );
//			B2J_server.load( "bvh2json/data/reallybasic" );
		}

	}

	void Update() {

		sync();
		B2Jplayhead ph = getPlayhead( "thomas_se_leve_02" );
		if ( ph != null ) {
			ph.Speed = 0.5f;
		}
		render();

		
		// and applying on the model
		foreach ( KeyValuePair< Transform, Quaternion > pair in updatedRots ) {

			Matrix4x4 mat = new Matrix4x4();
			mat.SetTRS( Vector3.zero, pair.Value, Vector3.one );
		
			Matrix4x4 loc = new Matrix4x4();
			loc.SetTRS( Vector3.zero, localRotations[ pair.Key ], Vector3.one );
			Matrix4x4 loci = loc.inverse;

			mat = loci * mat * loc;

//			pair.Key.localRotation = Quaternion.LookRotation( mat.GetColumn( 2 ), mat.GetColumn( 1 ) );
			pair.Key.localRotation = localRotations[ pair.Key ] * pair.Value;
//			pair.Key.localRotation = pair.Value;

		}
//		foreach ( KeyValuePair< Transform, Vector3 > pair in updatedPos ) {
//			pair.Key.localPosition = localTranslations[ pair.Key ] + pair.Value;
//		}

	}

}
