using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using B2J;

public class B2Jplayer : B2JgenericPlayer {

	public TextAsset Map_numediart;

	// Use this for initialization
	void Start () {

		loadMapping( Map_numediart ); // mapping for model "bvh_numediart"

		if (B2J_server != null) {
			B2J_server.load( "bvh2json/data/thomas_se_leve_02" );
		}

	}

	void Update() {

		sync();

		B2Jplayhead ph = getPlayhead( "thomas_se_leve_02" );
		Debug.Log( ph.Model + ", " + ph.Speed + ", [" + ph.CueIn + ", " + ph.CueOut +"] - time: " + ph.CurrentTime );


//		sync();
//		B2Jplayhead testp = getPlayhead( "ariaII_02" );
//		if (testp != null)
//			testp.Speed = 0.01f;
////		testp = getPlayhead( "hips" );
////		if (testp != null)
////			testp.Speed = 3.0f;
//
//		foreach ( B2Jplayhead ph in b2jPlayheads ) {
//			if ( ph.Active ) {
//				foreach( KeyValuePair< Transform, Quaternion > kv in ph.Retriever.rotations ) {
//
//					Vector3 initeulers = b2jMaps[ ph.Retriever.model ].initialRotation[ kv.Key ].eulerAngles;
//					Vector3 neweulers = kv.Value.eulerAngles;
//
////					kv.Key.rotation = Quaternion.Euler(
////						initq.eulerAngles.x,
////						initq.eulerAngles.y,
////						initq.eulerAngles.z );
//
////					kv.Key.rotation = Quaternion.Euler(
////						initeulers.x + neweulers.x,
////						initeulers.y + neweulers.y,
////						initeulers.z + neweulers.z );
//
//					kv.Key.localRotation = Quaternion.Euler(
//						initeulers.x + neweulers.x,
//						initeulers.y + neweulers.y,
//						initeulers.z + neweulers.z );
//
////					kv.Key.localRotation = kv.Value;
//
//				}
//				foreach( KeyValuePair< Transform, Vector3 > kv in ph.Retriever.positions ) {
////					kv.Key.localPosition = new Vector3( kv.Value.x * 0.001f, kv.Value.y * 0.001f, kv.Value.z * 0.001f );
////					Debug.Log ( ph.ToString() + " / " + kv.Value.x + ", " + kv.Value.y + ", " + kv.Value.z );
//				}
//			}
//		}
		
	}

}
