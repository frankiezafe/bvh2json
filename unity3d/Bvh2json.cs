using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using MiniJSON;

namespace Bvh2json {

	#region data objects definition

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
		public int bID;
		public string name;
		public B2Jbone parent;
		public List<B2Jbone> children;
		public List<Vector3> positions;
		public List<Quaternion> rotations;
		public List<Vector3> scales;
	}

	public class B2Jkey {
		public int kID;
		public float timestamp;
	}

	public class B2Jrecord {
		public string type;
		public float version;
		public string desc;
		public string name;
		public string model;
		public string origin;
		public int keyCount;
		public List<B2Jkey> keys;
		public List<B2Jgroup> groups;
		public List<B2Jbone> bones;
		public List<B2Jbone> b_positions;
		public List<B2Jbone> b_rotations;
		public List<B2Jbone> b_scales;
	}

	#endregion

	#region reader definition
	public class Bvh2jsonReader {



		public Bvh2jsonReader( string path, B2Jrecord rec ) {

			TextAsset bvhj = Resources.Load( path ) as TextAsset;
			if ( bvhj == null) {
				Debug.Log ( "Bvh2jsonReader::" + path + " not loaded" );
				return;
			} else {
				Debug.Log ( "Bvh2jsonReader::" + path + " successfully loaded" );
			}

			IDictionary data = Json.Deserialize (bvhj.ToString ());
			if ( data == null) {
				Debug.Log ( "Failed to parse " + path );
				return;
			}

			rec = new B2Jrecord();
			rec.type = data[ "type" ];
			rec.version = float.Parse( ""+data[ "version" ] );
			rec.desc = data[ "desc" ];
			rec.name = data[ "name" ];
			rec.model = data[ "model" ];
			rec.origin = data[ "origin" ];
			rec.keyCount = int.TryParse( data[ "keys" ] );

		}

		// private float gettry( object out  )

	}
	#endregion

	public class Bvh2json : MonoBehaviour {

		private List<B2Jrecord> records;

		void Start () {

			B2Jrecord newrec = null;
			Bvh2jsonReader br = new Bvh2jsonReader ( "bvh2json/test", newrec );

		}

		void Update () {}

	}

}
