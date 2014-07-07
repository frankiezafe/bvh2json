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
		public string name;
		public B2Jbone parent;
		public List<B2Jbone> children;
		public bool positions_enabled;
		public bool rotations_enabled;
		public bool scales_enabled;
	}

	public class B2Jkey {
		public int kID;
		public float timestamp;
		public List<Vector3> positions;
		public List<Quaternion> rotations;
		public List<Vector3> scales;
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
		public List<B2Jkey> keys;
		public List<B2Jbone> bones;

	}

	public class B2Jhierarchy {
		public string name;
		public List< B2Jhierarchy > children;
	}

	#endregion

	#region reader definition
	public sealed class Bvh2jsonReader {

		static readonly Bvh2jsonReader _instance = new Bvh2jsonReader();

		public static Bvh2jsonReader Instance {
			get {
				return _instance;
			}
		}

		private List< B2Jhierarchy > tmphierarchies; // used to decompress hierarchy

		private Bvh2jsonReader() {}

		public B2Jrecord load( string path ) {
			
			TextAsset bvhj = Resources.Load( path ) as TextAsset;
			if ( bvhj == null) {
				Debug.Log ( "Bvh2jsonReader::" + path + " not loaded" );
				return null;
			} else {
				Debug.Log ( "Bvh2jsonReader::" + path + " successfully loaded" );
			}
			
			IDictionary data = ( IDictionary ) Json.Deserialize ( bvhj.ToString() );
			if ( data == null) {
				Debug.Log ( "Failed to parse " + path );
				return null;
			}

			tmphierarchies = new List< B2Jhierarchy > ();
			parseHierarchy ( (IList) data["hierarchy"], tmphierarchies );

			B2Jrecord rec = new B2Jrecord();
			rec.type = "" + data[ "type" ];
			rec.version = float.Parse( ""+data[ "version" ] );
			rec.desc = "" + data[ "desc" ];
			rec.name = "" + data[ "name" ];
			rec.model = "" + data[ "model" ];
			rec.origin = "" + data[ "origin" ];
			rec.keyCount = int.Parse( "" + data[ "keys" ] );

			rec.groups = parseGroups( data );
			rec.bones = parseBones( data );

			return rec;
			
		}

		public B2Jbone getBoneByName( List<B2Jbone> bones, string bname ) {
			foreach (B2Jbone bone in bones) {
				if ( bone.name == bname ) {
					return bone;
				}
			}
			return null;
		}
		
		public B2Jbone getBoneById( List<B2Jbone> bones, int bId ) {
			if (bId < 0 || bId >= bones.Count) {
				return null;
			}
			return bones[ bId ];
		}

		private void parseHierarchy( IList data, List< B2Jhierarchy > holder ) {
			for (int i = 0; i < data.Count; i++) {
				IDictionary tmph = ( IDictionary ) data[ i ];
				B2Jhierarchy newh = new B2Jhierarchy();
				newh.name = "" + tmph[ "bone" ];
				newh.children = new List<B2Jhierarchy>();
				parseHierarchy( ( IList ) tmph[ "children" ], newh.children );
				holder.Add( newh );
			}
		}

		private B2Jhierarchy findInHierarchy( List< B2Jhierarchy > hs, string name ) {

			foreach ( B2Jhierarchy h in hs ) {

				if ( h.name == name ) {
					return h;
				}

				B2Jhierarchy tmph = findInHierarchy( h.children, name );
				if ( tmph != null ) {
					return tmph;
				}

			}

			return null;

		}

		private B2Jhierarchy findInHierarchyChilds( List< B2Jhierarchy > hs, string name ) {

			B2Jhierarchy tmph;
			foreach ( B2Jhierarchy h in hs ) {
				tmph = findInHierarchyChilds( h.children, name );
				if ( tmph != null ) {
					return tmph;
				}
				tmph = findInHierarchy( h.children, name );
				if ( tmph != null ) {
					return h;
				}
			}

			return null;
			
		}

		private List<B2Jbone> parseBones( IDictionary data ) {
		
			// parsing summary first, will make our life easier
			// when parsing bones

			bool summary_p_all = false;
			List<int> summary_p = convertListOfIndex( ( IList ) (( IDictionary ) data[ "summary" ])[ "positions" ] );
			if ( summary_p.Count > 0 && summary_p[ 0 ] == -1 ) {
				summary_p.Clear();
				summary_p_all = true;
			}

			bool summary_q_all = false;
			List<int> summary_q = convertListOfIndex( ( IList ) (( IDictionary ) data[ "summary" ])[ "quaternions" ] );
			if ( summary_q.Count > 0 && summary_q[ 0 ] == -1 ) {
				summary_q.Clear();
				summary_q_all = true;
			}

			bool summary_s_all = false;
			List<int> summary_s = convertListOfIndex( ( IList ) (( IDictionary ) data[ "summary" ])[ "scales" ] );
			if ( summary_s.Count > 0 && summary_s[ 0 ] == -1 ) {
				summary_s.Clear();
				summary_s_all = true;
			}

			List<B2Jbone> output = new List<B2Jbone> ();
			IList dbs = ( IList ) data[ "list" ];
			for ( int i = 0; i < dbs.Count; i++ ) {
				string bname = "" + dbs[ i ];
				B2Jbone newb = new B2Jbone();
				newb.name = bname;
				newb.children = new List<B2Jbone>();
				newb.parent = null;
				newb.positions_enabled = false;
				newb.rotations_enabled = false;
				newb.scales_enabled = false;
				if ( summary_p.Contains( i ) || summary_p_all ) {
					newb.positions_enabled = true;
				}
				if ( summary_q.Contains( i ) || summary_q_all ) {
					newb.rotations_enabled = true;
				}
				if ( summary_s.Contains( i ) || summary_s_all ) {
					newb.scales_enabled = true;
				}
				output.Add( newb );
			}

			B2Jbone tmpb;
			B2Jhierarchy h;
			foreach ( B2Jbone bone in output ) {
				h = findInHierarchy( tmphierarchies, bone.name );
				if ( h != null ) {
					foreach( B2Jhierarchy hc in h.children ) {
						tmpb = getBoneByName( output, hc.name );
						if ( tmpb != null ) {
							bone.children.Add( tmpb );
						}
					}
				}
				h = findInHierarchyChilds( tmphierarchies, bone.name );
				if ( h != null ) {
					tmpb = getBoneByName( output, h.name );
					bone.parent = tmpb;
				}
			}

			return output;

		}

		private List<B2Jgroup> parseGroups( IDictionary data ) {

			List<B2Jgroup> output = new List<B2Jgroup> ();

			IList dgps = ( IList ) data[ "groups" ];
			for ( int i = 0; i < dgps.Count; i++ ) {
				IDictionary gp = ( IDictionary ) dgps[ i ];
				B2Jgroup newgp = new B2Jgroup();
				newgp.name = "" + gp["name"];
				newgp.millisIn = float.Parse( "" + gp["in"] );
				newgp.millisOut = float.Parse( "" + gp["out"] );
				newgp.keyIn = int.Parse( "" + gp["kin"] );
				newgp.keyOut = int.Parse( "" + gp["kout"] );
				newgp.use_millis = false;
				newgp.use_keys = false;
				if ( newgp.millisIn != -1 ) {
					newgp.use_millis = true;
				}
				if ( newgp.keyIn != -1 ) {
					newgp.use_keys = true;
				}
				output.Add( newgp );
			}
			return output;

		}

		public List<int> convertListOfIndex( IList _list ) {

			List<int> output = new List<int>();
			if (_list.Count == 1 && _list [0] == "all") {
				output.Add (-1);
			} else {
				for (int i = 0; i < _list.Count; i++) {
					output.Add ( int.Parse ( "" + _list [i] ) );
				}
			}
			return output;

		}

	}
	#endregion

	public class Bvh2json : MonoBehaviour {

		private List<B2Jrecord> records;

		public void Start () {

			B2Jrecord newrec = Bvh2jsonReader.Instance.load ( "bvh2json/ariaII_02" );
			printRecord ( newrec );

		}

		public void printRecord( B2Jrecord br ) {

			if (br == null) {
				Debug.Log ("BVH2JSON: record is empty" );
			} else {
				Debug.Log ("BVH2JSON************");
				Debug.Log ( br.name );
				for ( int i = 0; i < br.groups.Count; i++ ) {
					Debug.Log ( "group[" + i + "] = " + br.groups[ i ].name + " (" + br.groups[ i ].use_millis + "," + br.groups[ i ].use_keys + ")" );
				}
				for ( int i = 0; i < br.bones.Count; i++ ) {
					Debug.Log ( "bone[" + i + "] = " + br.bones[ i ].name + " (" + br.bones[ i ].positions_enabled + "," +  br.bones[ i ].rotations_enabled  + "," +  br.bones[ i ].scales_enabled + ")" );
					if ( br.bones[ i ].parent != null ) {
						Debug.Log ( "--parent:" + br.bones[ i ].parent.name );
					}
					foreach( B2Jbone child in br.bones[ i ].children ) {
						Debug.Log ( "--child:" + child.name );
					}

				}
				Debug.Log ("BVH2JSON************");
			}
		}

		public void Update () {}

	}

}
