using UnityEngine;
using System.Collections;

using B2J;

public class B2Jplayer : MonoBehaviour {

	// Use this for initialization
	void Start () {
		B2Jserver.Instance.load( "bvh2json/data/test" );
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
