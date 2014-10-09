using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using B2J;

public class B2Jvalidator : B2JgenericPlayer {

	public TextAsset Map_numediart;

	private LineRenderer liner;
	private B2Jplayhead ph;
	private B2Jrecord rec;
	private B2Jmap m;

	private Dictionary < Transform, Quaternion > defaultRot;

	// Use this for initialization
	void Start () {;

		LineRenderer[] all_linerenders = GetComponentsInChildren<LineRenderer>();
		liner = all_linerenders[ 0 ];

		setQuiet();

		initPlayer();

		loadMapping( Map_numediart ); // mapping for model "bvh_numediart"
		loadAsset( "bvh2json/data/thomas_se_leve_02" );

		process();

		ph = getPlayhead( "thomas_se_leve_02" );
		rec = ph.getRecord();
		m = maps["bvh_numediart"];
		// z axis should point in the bone direction
		defaultRot = new Dictionary < Transform, Quaternion >();
		foreach ( KeyValuePair< int, B2JtransformList > pair in m.transformListById ) {
			B2Jbone b = rec.bones[ pair.Key ];
			B2JtransformList ml = pair.Value;
			for ( int i = 0; i < ml.transforms.Count; i++ ) {
				Transform t = ml.transforms[ i ];
				t.localPosition += b.head * 0.01f;
				if ( t.name == "heel_l" || t.name == "heel_r" || t.name == "hand_l" || t.name == "hand_r" ) {
					string endofsite = t.name + "_end";
					armature[ endofsite ].localPosition += b.rest * 0.01f;
				}
			}
		}

	}
	
	// Update is called once per frame
	void Update () {

		process();
		render();

		foreach ( KeyValuePair< int, B2JtransformList > pair in m.transformListById ) {
			B2Jbone b = rec.bones[ pair.Key ];
			B2JtransformList ml = pair.Value;
			for ( int i = 0; i < ml.transforms.Count; i++ ) {
				Quaternion q = ph.getRotations()[ pair.Key ];
				Transform t = armature[ ml.transforms[ i ].name ];
				t.localRotation = q;
			}
		}

		int lp = 0;
		liner.SetPosition ( lp, transform.position ); lp++;
		liner.SetPosition ( lp, armature ["hips"].position ); lp++;
		liner.SetPosition ( lp, armature ["spine_1"].position ); lp++;
		liner.SetPosition ( lp, armature ["spine_2"].position ); lp++;
		liner.SetPosition ( lp, armature ["neck"].position ); lp++;
		liner.SetPosition ( lp, armature ["head"].position ); lp++;
		liner.SetPosition ( lp, armature ["spine_2"].position ); lp++;
		liner.SetPosition ( lp, armature ["collar_l"].position ); lp++;
		liner.SetPosition ( lp, armature ["arm_l"].position ); lp++;
		liner.SetPosition ( lp, armature ["elbow_l"].position ); lp++;
		liner.SetPosition ( lp, armature ["hand_l"].position ); lp++;
		liner.SetPosition ( lp, armature ["hand_l_end"].position ); lp++;
		liner.SetPosition ( lp, armature ["hand_r_end"].position ); lp++;
		liner.SetPosition ( lp, armature ["hand_r"].position ); lp++;
		liner.SetPosition ( lp, armature ["elbow_r"].position ); lp++;
		liner.SetPosition ( lp, armature ["arm_r"].position ); lp++;
		liner.SetPosition ( lp, armature ["collar_r"].position ); lp++;
		liner.SetPosition ( lp, armature ["spine_2"].position ); lp++;
		liner.SetPosition ( lp, armature ["hips"].position ); lp++;
		liner.SetPosition ( lp, armature ["leg_l"].position ); lp++;
		liner.SetPosition ( lp, armature ["knee_l"].position ); lp++;
		liner.SetPosition ( lp, armature ["foot_l"].position ); lp++;
		liner.SetPosition ( lp, armature ["heel_l"].position ); lp++;
		liner.SetPosition ( lp, armature ["heel_l_end"].position ); lp++;
		liner.SetPosition ( lp, armature ["heel_r_end"].position ); lp++;
		liner.SetPosition ( lp, armature ["heel_r"].position ); lp++;
		liner.SetPosition ( lp, armature ["foot_r"].position ); lp++;
		liner.SetPosition ( lp, armature ["knee_r"].position ); lp++;
		liner.SetPosition ( lp, armature ["leg_r"].position ); lp++;
		liner.SetPosition ( lp, armature ["hips"].position );

	}
}
