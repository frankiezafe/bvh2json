using UnityEngine;
using System.Collections;

using B2J;

public class B2JblenderUI : MonoBehaviour {

	public B2Jblender blender;
	
	public string Model;

	[ Range( 0.0f, 1.0f ) ]
	public float weight;
	private float last_weight;

	[ Range( 0.0f, 1.0f ) ]
	public float smooth_strength;
	private float last_smooth_strength;

	public bool enable_smooth;
	private bool last_enable_smooth;

	// Use this for initialization
	void Start () {
	
		Model = blender.getName();
		weight = blender.getWeight ();
		last_weight = weight;
		smooth_strength = blender.getSmoothStrength ();
		last_smooth_strength = smooth_strength;
		enable_smooth = true;
		last_enable_smooth = enable_smooth;

	}
	
	// Update is called once per frame
	void Update () {

		if ( last_enable_smooth != enable_smooth ) {
			if ( enable_smooth ) {
				blender.setSmoothMethod( B2JsmoothMethod.B2JSMOOTH_ACCUMULATION_OF_DIFFERENCE );
			} else {
				blender.setSmoothMethod( B2JsmoothMethod.B2JSMOOTH_NONE );
			}
			last_enable_smooth = enable_smooth;
		}

		if (last_weight != weight) {
			blender.setWeight( weight );
			last_weight = weight;
		}

		if (last_smooth_strength != smooth_strength) {
			blender.setSmoothStrength( smooth_strength );
			last_smooth_strength = smooth_strength;
		}

	}
}
