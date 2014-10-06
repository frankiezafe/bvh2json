using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using B2J;

public class TanukiController : B2JgenericPlayer {

	public TextAsset Map_numediart;
	private int correctionCount;
	
	public bool normaliseRotation;
	private bool last_normaliseRotation;
	
	public bool interpolation;
	private bool last_interpolation;
	
	[ Range( 0.0f, 1.0f ) ]
	public float percent;
	private float lastPercent;
	
	[ Range( 0.0f, 3.0f ) ]
	public float speed;
	private float lastSpeed;

	void Start () {

		// setting the B2J player
		_defaultLoop = B2Jloop.B2JLOOP_PALINDROME;
		_interpolate = true;
		_normaliseRotationWeight = false;

		// loading all transforms
		init();

		// loading mappings
		loadMapping( Map_numediart ); // mapping for model "bvh_numediart"

		// populating server
		if (B2Jserver != null) {
			B2Jserver.load( "bvh2json/data/thomas_se_leve_02" );
			//			B2Jserver.load( "bvh2json/data/tensions_01" );
			B2Jserver.load( "bvh2json/data/capoiera" );
		}

		// generation of all playheads
		sync();

		// UI specific
		percent = 0;
		lastPercent = percent;
		speed = 1;
		lastSpeed = speed;

		normaliseRotation = _normaliseRotationWeight;
		last_normaliseRotation = normaliseRotation;
		interpolation = _interpolate;
		last_interpolation = interpolation;

		for( int i = 0; i < _b2jPlayheadList.Count; i++ ) {
			if ( i == 0 ) {
				_b2jPlayheadList[ i ].Weight = 1;
			} else {
				_b2jPlayheadList[ i ].Weight = 0;
			}
			B2JplayheadUI ui = gameObject.AddComponent<B2JplayheadUI>();
			ui.Mocap = _b2jPlayheadList[ i ].Name;
			ui.playhead = _b2jPlayheadList[ i ];
		}

	}
	
	void Update() {
		
		sync();

		// adaptions before rendering
		if ( normaliseRotation != last_normaliseRotation ) {
			_normaliseRotationWeight = normaliseRotation;
			last_normaliseRotation = normaliseRotation;
		}
		if ( interpolation != last_interpolation ) {
			_interpolate = interpolation;
			last_interpolation = interpolation;
		}
		if ( lastPercent != percent ) {
			foreach ( B2Jplayhead ph in _b2jPlayheadList ) {
				ph.Percent = percent;
			}
			lastPercent = percent;
		}
		if ( lastSpeed != speed ) {
			foreach ( B2Jplayhead ph in _b2jPlayheadList ) {
				ph.Speed = speed;
			}
			lastSpeed = speed;
		}

		// quaternions rendering
		render();
		
		// and applying on the model
		foreach ( KeyValuePair< Transform, Quaternion > pair in _updatedQuaternions ) {

			Transform t = pair.Key;
			Quaternion q = pair.Value;
			t.localRotation = q;

//			Quaternion locValue = Quaternion.identity;
//			Matrix4x4 mat = new Matrix4x4();
//			mat.SetTRS( Vector3.zero, pair.Value, Vector3.one );
//			Matrix4x4 tmat = _world2local[ t ];
//			mat = tmat* mat * tmat.inverse;			
//			t.localRotation = _initialQuaternions[t] * Quaternion.LookRotation( mat.GetColumn(2), mat.GetColumn(1) ) ;
		
		}

		// specific to mask
		_armature [ "mask_root" ].position = _armature [ "head_bone" ].position;
		_armature [ "mask_root" ].rotation = _armature [ "head_bone" ].rotation;
		
	}
}
