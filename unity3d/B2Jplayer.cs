using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using B2J;

public class B2Jplayer : B2JgenericPlayer {

	public TextAsset Map_numediart;
	private int correctionCount;

	public bool normalise_rotations;
	private bool last_normalise_rotations;

	public bool interpolation;
	private bool last_interpolation;

	[ Range( 0.0f, 1.0f ) ]
	public float percent;
	private float lastPercent;

	[ Range( 0.0f, 3.0f ) ]
	public float speed;
	private float lastSpeed;

	// Use this for initialization
	void Start () {

		defaultLoop = B2Jloop.B2JLOOP_PALINDROME;
		interpolate = true;

		normalise_rotations = rotationNormalise;
		last_normalise_rotations = normalise_rotations;

		interpolation = interpolate;
		last_interpolation = interpolation;

		InitPlayer();

		LoadMapping( Map_numediart ); // mapping for model "bvh_numediart"

		if (B2Jserver != null) {
			B2Jserver.Load( "bvh2json/data/thomas_se_leve_02" );
//			B2Jserver.load( "bvh2json/data/tensions_01" );
			B2Jserver.Load( "bvh2json/data/capoiera" );
		}

		Process();

		percent = 0;
		lastPercent = percent;

		speed = 1;
		lastSpeed = speed;

		foreach ( B2Jplayhead ph in playheadList ) {
			ph.Percent = percent;
			ph.Speed = speed;
		}

		foreach ( B2Jblender bb in blenderList ) {
			B2JblenderUI ui = gameObject.AddComponent<B2JblenderUI>();
			ui.blender = bb;
		}

		for( int i = 0; i < playheadList.Count; i++ ) {
			if ( i == 0 ) {
				playheadList[ i ].Weight = 1;
			} else {
				playheadList[ i ].Weight = 0;
			}
			B2JplayheadUI ui = gameObject.AddComponent<B2JplayheadUI>();
			ui.Mocap = playheadList[ i ].Name;
			ui.playhead = playheadList[ i ];
		}

	}

	void Update() {

		Process();

		if ( normalise_rotations != last_normalise_rotations ) {
			rotationNormalise = normalise_rotations;
			last_normalise_rotations = normalise_rotations;
		}

		if ( interpolation != last_interpolation ) {
			interpolate = interpolation;
			last_interpolation = interpolation;
		}

		if ( lastPercent != percent ) {
			foreach ( B2Jplayhead ph in playheadList ) {
				ph.Percent = percent;
			}
			lastPercent = percent;
		}
		 
		if ( lastSpeed != speed ) {
			foreach ( B2Jplayhead ph in playheadList ) {
				ph.Speed = speed;
			}
			lastSpeed = speed;
		}

		Render();

		// and applying on the model
//		foreach ( KeyValuePair< Transform, Quaternion > pair in updatedQuaternions ) {
		foreach ( KeyValuePair< Transform, Quaternion > pair in quaternions ) {

			Transform t = pair.Key;
			Quaternion q = pair.Value;
			t.localRotation = q;
			
//			Transform t = pair.Key;
//			Quaternion locValue = Quaternion.identity;
//			Matrix4x4 mat = new Matrix4x4();
//			mat.SetTRS( Vector3.zero, pair.Value, Vector3.one );
//			Matrix4x4 tmat = _world2local[ t ];
//			mat = tmat* mat * tmat.inverse;
//			t.localRotation = _initialQuaternions[t] * Quaternion.LookRotation( mat.GetColumn(2), mat.GetColumn(1) ) ;
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
