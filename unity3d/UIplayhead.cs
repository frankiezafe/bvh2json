using UnityEngine;
using System.Collections;

using B2J;

public class UIplayhead : MonoBehaviour {

	public B2Jplayhead playhead;

	public string Model;
	public string Name;

	[ Range( 0.0f, 1.0f ) ]
	public float weight;
	private float lastWeight;

	[ Range( 0.0f, 1.0f ) ]
	public float HeadReadOnly;
	[ Range( 0.0f, 1.0f ) ]
	public float WeightReadOnly;

	// Use this for initialization
	void Start () {
	
		weight = playhead.getWeight();
		Name = playhead.getName ();
		Model = playhead.getModel ();

	}
	
	// Update is called once per frame
	void Update () {
	
		if ( weight != lastWeight ) {
			playhead.setWeight( weight );
			lastWeight = weight;
		}

		HeadReadOnly = playhead.getPercent();
		WeightReadOnly = playhead.getWeight();

	}
}
