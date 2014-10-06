using UnityEngine;

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using MiniJSON;

namespace B2J {

	// SERVER SIDE OBJECTS

	public class B2Jgroup {
		public string name;
		public float millisIn;
		public float millisOut;
		public int keyIn;
		public int keyOut;
		public bool use_millis;
		public bool use_keys;
	}

	public class B2Jbone {
		public string name;
		public string rotation_order;
		public B2Jbone parent;
		public List<B2Jbone> children;
		public Vector3 head;
		public Vector3 tail;
		public Vector3 rest;
		public bool positions_enabled;
		public bool rotations_enabled;
		public bool scales_enabled;
	}

	public class B2Jkey {
		public int kID;
		public float timestamp;
		// index in positions, rotations & scales is related to their bone
		// meaning: if seeking the rotation of the bone named "A"
		// first thing to know is its index in the B2Jrecord.bones
		// if A.rotations_enabled is false, the Quaternion is default
		// this implies also that rotations.Count == B2Jrecord.bones.Count
		public List<Vector3> positions;
		public List<Quaternion> rotations;
		public List<Vector3> eulers;
		public List<Vector3> scales;
		public B2Jkey() {
			positions = new List<Vector3> ();
			rotations = new List<Quaternion> ();
			eulers = new List<Vector3> ();
			scales = new List<Vector3> ();
		}
	}

	public class B2Jrecord {
		public string type;
		public float version;
		public string desc;
		public string name;
		public string model;
		public string origin;
		public int keyCount;
		public List<B2Jgroup> groups;
		// empty keys are null
		// except for the first one (index & time = 0)
		// keys[ 0 ] always contains all the positions, rotations & scales
		public List<B2Jkey> keys; 
		public List<B2Jbone> bones;
		
	}

	public class B2Jhierarchy {
		public string name;
		public Vector3 head;
		public Vector3 tail;
		public List< B2Jhierarchy > children;
	}

	public enum B2Jloop {
		B2JLOOP_NONE = 0,
		B2JLOOP_NORMAL = 1,
		B2JLOOP_PALINDROME = 2
	}

	// MAP OBJECTS

	public class B2JtransformList {
		public List< Transform > transforms;
		public List< float > weights;
		public B2JtransformList() {
			transforms = new List < Transform > ();
			weights = new List < float > ();
		}
	}
	
	public enum B2JsmoothMethod {
		B2JSMOOTH_NONE = 0,
		B2JSMOOTH_ACCUMULATION_OF_DIFFERENCE = 1
	}
	
	public class B2JmapLocalValues {
		public Dictionary< Transform, Quaternion > quaternions;
		public Dictionary< Transform, Vector3 > translations;
		public Dictionary< Transform, Vector3 > scales;
		public B2JmapLocalValues() {
			quaternions = new Dictionary< Transform, Quaternion >();
			translations = new Dictionary< Transform, Vector3 >();
			scales = new Dictionary< Transform, Vector3 >();
		}
	}

}