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

		init();

		loadMapping( Map_numediart ); // mapping for model "bvh_numediart"

		if (B2J_server != null) {
			B2J_server.load( "bvh2json/data/thomas_se_leve_02" );
		}

	}

	void Update() {

		sync();
		B2Jplayhead ph = getPlayhead( "thomas_se_leve_02" );
		if ( ph != null ) {
			ph.Speed = 0.5f;
		}
		apply();

	}

}
