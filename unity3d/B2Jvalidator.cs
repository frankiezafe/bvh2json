using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using B2J;

public class B2Jvalidator : B2JgenericPlayer {

	public TextAsset Map_numediart;

	private LineRenderer liner;
	private Dictionary < string, Transform > bones;

	private B2Jplayhead ph;
	private B2Jrecord rec;
	private B2Jmap m;

	private Dictionary < Transform, Quaternion > defaultRot;

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
			B2J_server.load( "bvh2json/data/thomas_se_leve_02" );
		}

		sync();

		ph = B2J_playheads [0];
		rec = ph.Record;
		m = B2J_maps["bvh_numediart"];
		// z axis should point in the bone direction
		defaultRot = new Dictionary < Transform, Quaternion >();

		foreach ( KeyValuePair< int, Transform > pair in m.transformById ) {

			B2Jbone b = rec.bones[ pair.Key ];
			Transform t = bones[ pair.Value.name ];

			if ( b.parent == null ) {

				t.localPosition += b.rest * 0.015f;
				defaultRot.Add( t, Quaternion.FromToRotation( Vector3.one, b.rest ) );

			} else {

				Vector3 plp = b.parent.rest;
				Vector3 clp = new Vector3( 0, 0, 1 );
				if ( b.children.Count > 0 ) {
					clp = b.children[ b.children.Count - 1 ].rest;
				}

				float l = b.rest.magnitude * 0.015f;
				Vector3 lp = new Vector3( l, 0, 0 );
				Quaternion q = Quaternion.FromToRotation( plp, clp );
				defaultRot.Add( t, q );
				t.localPosition = b.rest * 0.015f;
				t.localRotation = q;

			}

//			t.localPosition += b.rest * 0.015f;

		}

	}
	
	// Update is called once per frame
	void Update () {

		sync();
		render();

		foreach ( KeyValuePair< int, Transform > pair in m.transformById ) {
			Quaternion q = ph.Rotations[ pair.Key ];
			Transform t = bones[ pair.Value.name ];
//			t.localRotation = defaultRot[ t ] * q;
			t.localRotation = q;
		}

		Vector3 accumh = transform.position;
		int lp = 0;
		liner.SetPosition ( lp, transform.position ); lp++;
		liner.SetPosition ( lp, bones ["hips"].position ); lp++;
		liner.SetPosition ( lp, bones ["spine_1"].position ); lp++;
		liner.SetPosition ( lp, bones ["spine_2"].position ); lp++;
		liner.SetPosition ( lp, bones ["neck"].position ); lp++;
		liner.SetPosition ( lp, bones ["head"].position ); lp++;
		liner.SetPosition ( lp, bones ["spine_2"].position ); lp++;
		liner.SetPosition ( lp, bones ["collar_l"].position ); lp++;
		liner.SetPosition ( lp, bones ["arm_l"].position ); lp++;
		liner.SetPosition ( lp, bones ["elbow_l"].position ); lp++;
		liner.SetPosition ( lp, bones ["hand_l"].position ); lp++;
		liner.SetPosition ( lp, bones ["hand_r"].position ); lp++;
		liner.SetPosition ( lp, bones ["elbow_r"].position ); lp++;
		liner.SetPosition ( lp, bones ["arm_r"].position ); lp++;
		liner.SetPosition ( lp, bones ["collar_r"].position ); lp++;
		liner.SetPosition ( lp, bones ["spine_2"].position ); lp++;
		liner.SetPosition ( lp, bones ["hips"].position ); lp++;
		liner.SetPosition ( lp, bones ["leg_l"].position ); lp++;
		liner.SetPosition ( lp, bones ["knee_l"].position ); lp++;
		liner.SetPosition ( lp, bones ["foot_l"].position ); lp++;
		liner.SetPosition ( lp, bones ["foot_r"].position ); lp++;
		liner.SetPosition ( lp, bones ["knee_r"].position ); lp++;
		liner.SetPosition ( lp, bones ["leg_r"].position ); lp++;
		liner.SetPosition ( lp, bones ["hips"].position );


	}
}
