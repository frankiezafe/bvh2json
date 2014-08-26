using UnityEngine;
using System.Collections;

using B2J;

public class B2JplayheadUI : MonoBehaviour {

	public B2Jplayhead playhead;
	
	public string Mocap;

	[ Range( 0.0f, 1.0f ) ]
	public float weight;
	private float lastWeight;

	[ Range( 0.0f, 1.0f ) ]
	public float HeadReadOnly;
	[ Range( 0.0f, 1.0f ) ]
	public float WeightReadOnly;

	// Use this for initialization
	void Start () {
	
		weight = playhead.Weight;

	}
	
	// Update is called once per frame
	void Update () {
	
		if ( weight != lastWeight ) {
			playhead.Weight = weight;
			lastWeight = weight;
		}

		HeadReadOnly = playhead.Percent;
		WeightReadOnly = playhead.Weight;

	}
}
