using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using B2J;

public class B2Jplayer : B2JgenericPlayer {

	public TextAsset Map_numediart;
	private int correctionCount;

	public bool normalise_weights;
	private bool last_normalise_weights;

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

		_defaultLoop = B2Jloop.B2JLOOPPALINDROME;
		_interpolate = true;
		_normaliseWeight = false;

		normalise_weights = _normaliseWeight;
		last_normalise_weights = normalise_weights;

		interpolation = _interpolate;
		last_interpolation = interpolation;

		init();

		loadMapping( Map_numediart ); // mapping for model "bvh_numediart"

		if (B2Jserver != null) {
			B2Jserver.load( "bvh2json/data/thomas_se_leve_02" );
			B2Jserver.load( "bvh2json/data/tensions_01" );
			B2Jserver.load( "bvh2json/data/capoiera" );
		}

		sync();

		percent = 0;
		lastPercent = percent;

		speed = 1;
		lastSpeed = speed;

		foreach ( B2Jplayhead ph in _b2jPlayheadList ) {
			ph.Percent = percent;
			ph.Speed = speed;
		}

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

		if ( normalise_weights != last_normalise_weights ) {
			_normaliseWeight = normalise_weights;
			last_normalise_weights = normalise_weights;
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

		render();

		// and applying on the model
		foreach ( KeyValuePair< Transform, Quaternion > pair in _updatedQuaternions ) {

			Transform t = pair.Key;
			Quaternion locValue = Quaternion.identity;
			Matrix4x4 mat = new Matrix4x4();
			mat.SetTRS( Vector3.zero, pair.Value, Vector3.one );

			Matrix4x4 tmat = _world2local[ t ];
			mat = tmat* mat * tmat.inverse;

			t.localRotation = _initialQuaternions[t] * Quaternion.LookRotation( mat.GetColumn(2), mat.GetColumn(1) ) ;

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
