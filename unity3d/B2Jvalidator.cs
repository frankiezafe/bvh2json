using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using B2J;

public class B2Jvalidator : B2JgenericPlayer {

	public TextAsset Map_numediart;

	private LineRenderer liner;
	private Dictionary < string, Transform > bones;

	// Use this for initialization
	void Start () {;
		
		bones = new Dictionary < string, Transform > ();
		Transform[] all_transforms = GetComponentsInChildren<Transform>();
		foreach( Transform transform in all_transforms ) {
			bones.Add( transform.name, transform );
		}

		LineRenderer[] all_linerenders = GetComponentsInChildren<LineRenderer>();
		liner = all_linerenders[ 0 ];

		init();
		
		loadMapping( Map_numediart ); // mapping for model "bvh_numediart"

		if (B2J_server != null) {
			B2J_server.load( "bvh2json/data/bending_left_hip" );
		}

	}
	
	// Update is called once per frame
	void Update () {

		sync();
		render();

		B2Jmap m = B2J_maps["bvh_numediart"];
		foreach ( KeyValuePair< int, Transform > pair in m.transformById ) {
		}

		liner.SetPosition ( 1, transform.position );
		liner.SetPosition ( 2, transform.position + bones[ "hips" ].localPosition );

	}
}
