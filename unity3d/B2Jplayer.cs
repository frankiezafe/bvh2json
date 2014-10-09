using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using B2J;

public class B2Jplayer : B2JgenericPlayer {

	public TextAsset Map_numediart;
	public TextAsset Map_numediart_other;
	private int correctionCount;

	public bool normalise_rotations;
	private bool last_normalise_rotations;

	public bool normalise_translations;
	private bool last_normalise_translations;

	public bool normalise_scales;
	private bool last_normalise_scales;

	public bool interpolation;
	private bool last_interpolation;

	public bool mask_upper_only;
	public bool mask_lower_only;
	private int last_use_mask;

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

		normalise_translations = translationNormalise;
		last_normalise_translations = normalise_translations;
		
		normalise_scales = scaleNormalise;
		last_normalise_scales = normalise_scales;

		interpolation = interpolate;
		last_interpolation = interpolation;

		mask_upper_only = false;
		mask_lower_only = false;
		last_use_mask = -1;

//		setVerbose();

		initPlayer( GetComponentsInChildren<Transform>() );

		loadMapping( Map_numediart ); // mapping for model "bvh_numediart"
		loadMapping( Map_numediart_other ); // mapping for model "bvh_numediart"

		loadMask ( "bvh2json/data/tanuki_upperbody_mask" );
		loadMask ( "bvh2json/data/tanuki_lowerbody_mask" );

		loadAsset( "bvh2json/data/thomas_se_leve_02" );
		loadAsset( "bvh2json/data/capoiera" );
		loadAsset( "bvh2json/data/ariaII_02" );

		loadLive( "kinect_visitor" );
		loadLive( "kinect_dancer" );

		// after this, everything should be ready,
		// blender and assets loaded 
		process();

		setWeight ( "bvh_numediart_other", 0 );
		setWeight ( "miko_ariaII_02", 1 );
		setWeight ( "thomas_se_leve_02", 1 );

		percent = 0;
		lastPercent = percent;

		speed = 1;
		lastSpeed = speed;

		foreach ( B2Jplayhead ph in playheadList ) {
			ph.setPercent( percent );
			ph.setSpeed( speed );
		}

		foreach ( B2Jblender bb in blenderList ) {
			B2JblenderUI ui = gameObject.AddComponent<B2JblenderUI>();
			ui.blender = bb;
			foreach( B2Jplayhead ph in bb.playheads ) {
				B2JplayheadUI uip = gameObject.AddComponent<B2JplayheadUI>();
				uip.Mocap = ph.getName();
				uip.playhead = ph;
			}
		}

	}

	void Update() {

		process();

		if ( mask_upper_only && last_use_mask != 0 ) {
			applyMaskOnBlender( "bvh_numediart", "tanuki-upperbody" );
			applyMaskOnBlender( "bvh_numediart_other", "tanuki-lowerbody" );
			mask_lower_only = false;
			last_use_mask = 0;
		} else if ( mask_lower_only && last_use_mask != 1 ) {
			applyMaskOnBlender( "bvh_numediart", "tanuki-lowerbody" );
			applyMaskOnBlender( "bvh_numediart_other", "tanuki-upperbody" );
			mask_upper_only = false;
			last_use_mask = 1;
		} else if ( !mask_upper_only && !mask_lower_only && last_use_mask != -1 ) {
			resetMaskOnBlender( "bvh_numediart" );
			resetMaskOnBlender( "bvh_numediart_other" );
			last_use_mask = -1;
		}

		if ( normalise_rotations != last_normalise_rotations ) {
			rotationNormalise = normalise_rotations;
			last_normalise_rotations = normalise_rotations;
		}

		if ( normalise_translations != last_normalise_translations ) {
			translationNormalise = normalise_translations;
			last_normalise_translations = normalise_translations;
		}
		
		if ( normalise_scales != last_normalise_scales ) {
			scaleNormalise = normalise_scales;
			last_normalise_scales = normalise_scales;
		}

		if ( interpolation != last_interpolation ) {
			interpolate = interpolation;
			last_interpolation = interpolation;
		}

		if ( lastPercent != percent ) {
			foreach ( B2Jplayhead ph in playheadList ) {
				ph.setPercent( percent );
			}
			lastPercent = percent;
		}
		 
		if ( lastSpeed != speed ) {
			foreach ( B2Jplayhead ph in playheadList ) {
				ph.setSpeed( speed );
			}
			lastSpeed = speed;
		}

		render();

		// and applying on the model
		foreach ( KeyValuePair< Transform, Quaternion > pair in quaternions ) {

			Transform t = pair.Key;
			Quaternion q = pair.Value;
			t.localRotation = q;

		}

	}

}
