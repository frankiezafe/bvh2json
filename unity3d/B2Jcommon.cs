using UnityEngine;

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using MiniJSON;

namespace B2J {
	
	// USEFUL METHODS
	public class B2Jutils {
		
		public static Vector3 vectorSlerp( Vector3 orig, Vector3 dest, float ratio ) {
			return new Vector3(
				( dest.x - orig.x ) * ratio,
				( dest.y - orig.y ) * ratio,
				( dest.z - orig.z ) * ratio
				);
		}

		public static Quaternion copy( Quaternion src ) {
			return new Quaternion( src.x, src.y, src.z, src.w );
		}

		public static Vector3 copy( Vector3 src ) {
			return new Vector3 (src.x, src.y, src.z);
		}

		public static void copy( B2JmaskConfig src, B2JmaskConfig dest ) {
			dest.name = src.name;
			dest.description = dest.description;
			dest.version = src.version;
			dest.weights.Clear ();
			foreach( KeyValuePair< Transform, float > pair in src.weights )
				dest.weights.Add( pair.Key, pair.Value );

		}
		
	}

	// REQUEST SPECIFIC

	public enum B2JrequestType {
		B2JREQ_TEXTASSET = 0,
		B2JREQ_STREAM = 1
	}

	public class B2Jrequest {
		public B2JrequestType type;
		public string name;
	}

	// RECORDS SPECIFIC

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
		public bool rotations_enabled;
		public bool translations_enabled;
		public bool scales_enabled;
		public void init() {
			rotation_order = "XYZ";
			parent = null;
			children = new List<B2Jbone>();
			head = Vector3.zero;
			tail = Vector3.zero;
			rest = Vector3.zero;
			rotations_enabled = true;
			translations_enabled = true;
			scales_enabled = true;
		}
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

	public class B2Jhierarchy {
		public string name;
		public Vector3 head;
		public Vector3 tail;
		public List< B2Jhierarchy > children;
	}

	public enum B2Jloop {
		B2JLOOP_NONE = 0,
		B2JLOOP_NORMAL = 1,
		B2JLOOP_PALINDROME = 2,
		B2JLOOP_STREAM = 3
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

	// MASK SPECIFIC

	public class B2JmaskConfig {
		public string name;
		public string description;
		public float version;
		public Dictionary< Transform, float > weights;
		public B2JmaskConfig() {
			name = "empty";
			description = "empty configuration";
			version = 0;
			weights = new Dictionary< Transform, float > ();
		}
		public void clear() {
			weights.Clear ();
		}
	}

	public class B2JmaskConfigLoader {

		public static B2JmaskConfig load( string path, Transform[] transforms ) {
			TextAsset asset = Resources.Load( path ) as TextAsset;
			if ( asset == null) {
				Debug.LogError ( "B2JmaskConfigLoader:: not loaded" );
				return null;
			}
			IDictionary data = ( IDictionary ) Json.Deserialize ( asset.ToString() );
			if ( data == null) {
				Debug.LogError ( "Failed to parse " + asset.name );
				return null;
			}
			if ( System.String.Compare ( (string) data ["type"], "mask") != 0) {
				Debug.LogError ( "B2J masks must have a type 'mask'" );
				return null;
			}
			B2JmaskConfig conf = new B2JmaskConfig ();
			conf.name = (string) data[ "name" ];
			conf.description = (string) data[ "desc" ];
			conf.version = float.Parse( "" + data[ "version" ] );
			IList values = ( IList ) data[ "mask" ];
			for( int i = 0; i < values.Count; i++ ) {
				IDictionary< string, object > value = ( Dictionary< string, object > ) values[ i ];
				foreach( KeyValuePair< string, object > v in value ) {
					foreach( Transform t in transforms ) {
						if ( t.name == v.Key ) {
							conf.weights.Add( t, float.Parse( v.Value.ToString() ) );
						}
					}
				}
			}
			return conf;
		}

	}

	// MAP SPECIFIC

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