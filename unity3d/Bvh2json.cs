using UnityEngine;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using MiniJSON;

namespace B2J {

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
		// index in positions, rotations & scales is related to their bone
		// meaning: if seeking the rotation of the bone named "A"
		// first thing to know is its index in the B2Jrecord.bones
		// if A.rotations_enabled is false, the Quaternion is default
		// this implies also that rotations.Count == B2Jrecord.bones.Count
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
		// empty keys are null
		// except for the first one (index & time = 0)
		// keys[ 0 ] always contains all the positions, rotations & scales
		public List<B2Jkey> keys; 
		public List<B2Jbone> bones;

	}

	public class B2Jhierarchy {
		public string name;
		public List< B2Jhierarchy > children;
	}

	#endregion

	#region reader definition
	public sealed class B2Jparser {

		static readonly B2Jparser _instance = new B2Jparser();

		public static B2Jparser Instance {
			get {
				return _instance;
			}
		}
		
		private List< B2Jhierarchy > tmphierarchies; // used to decompress hierarchy
		private bool summary_p_all;
		private List<int> summary_p; // contains the list of bones positions
		private bool summary_q_all;
		private List<int> summary_q;
		private bool summary_s_all;
		private List<int> summary_s;
		
		private B2Jparser() {}

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
			parseSummary( (IDictionary) data["summary"] );

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
			rec.keys = parseKeys( data, rec.bones );

			tmphierarchies.Clear ();
			summary_p.Clear ();
			summary_q.Clear ();
			summary_s.Clear ();
			tmphierarchies = null;
			summary_p = null;
			summary_q = null;
			summary_s = null;

			return rec;
			
		}

		private List<B2Jkey> parseKeys( IDictionary data, List<B2Jbone> bones ) {

			List<B2Jkey> output = new List<B2Jkey> ();
			IList dataks = (IList) data[ "data" ];
			for (int i = 0; i < dataks.Count; i++) {
				// waiting for the first key to be id 0
				B2Jkey newk = parseKey( (IDictionary) dataks[ i ], bones );
				if ( newk != null ) {
					output.Add( newk );
				}
			}

			return output;
		
		}

		private B2Jkey parseKey( IDictionary keydata, List<B2Jbone> bones ) {
		
			B2Jkey newkey = new B2Jkey ();
			newkey.kID = int.Parse ( "" + keydata ["id"] );
			newkey.timestamp = float.Parse ( "" + keydata ["time"]);

			// positions list
			if (summary_p.Count > 0) {
				newkey.positions = new List<Vector3> ();
				for (int i = 0; i < bones.Count; i++) {
					newkey.positions.Add( new Vector3( 0,0,0 ) );
					/*
					if ( newkey.kID == 0 && bones[ i ].positions_enabled ) {
						newkey.positions.Add( new Vector3( 0,0,0 ) );
					} else {
						newkey.positions.Add( null );
					}
					*/
				}
				List<int> pIds = convertListOfIndex ((IList)((IDictionary) keydata ["positions"]) ["bones"] );
				List<float> pValues = convertListOfFloat ((IList)((IDictionary) keydata ["positions"]) ["values"] );
				for (int i = 0; i < pIds.Count; i++) {
					newkey.positions[ pIds[ i ] ] = new Vector3( pValues[ i * 3 ], pValues[ i * 3 + 1 ], pValues[ i * 3 + 2 ] );
				}
			} else {
				newkey.positions = null;
			}

			// rotations list
			if (summary_q.Count > 0) {
				newkey.rotations = new List<Quaternion> ();
				for (int i = 0; i < bones.Count; i++) {
					newkey.rotations.Add ( new Quaternion () );
					/*
					if (newkey.kID == 0 && bones [i].rotations_enabled) {
						newkey.rotations.Add ( new Quaternion () );
					} else {
						newkey.rotations.Add ( null );
					}
					*/
				}
				List<int> qIds = convertListOfIndex ((IList)((IDictionary)keydata ["quaternions"]) ["bones"]);
				List<float> qValues = convertListOfFloat ((IList)((IDictionary)keydata ["quaternions"]) ["values"]);
				for (int i = 0; i < qIds.Count; i++) {
					newkey.rotations [ qIds [i] ] = new Quaternion (qValues [i * 4], qValues [i * 4 + 1], qValues [i * 4 + 2], qValues [i * 4 + 3]);
				}
			} else {
				newkey.rotations = null;
			}
			
			// scales list
			if (summary_s.Count > 0) {
				newkey.scales = new List<Vector3> ();
				for (int i = 0; i < bones.Count; i++) {
					newkey.scales.Add (new Vector3 (1, 1, 1));
					/*
					if (newkey.kID == 0 && bones [i].scales_enabled) {
						newkey.scales.Add (new Vector3 (1, 1, 1));
					} else {
						newkey.scales.Add ( null );
					}
					*/
				}
				List<int> sIds = convertListOfIndex ((IList)((IDictionary)keydata ["scales"]) ["bones"]);
				List<float> sValues = convertListOfFloat ((IList)((IDictionary)keydata ["scales"]) ["values"]);
				for (int i = 0; i < sIds.Count; i++) {
					newkey.scales [ sIds [i] ] = new Vector3( sValues[ i * 3 ], sValues[ i * 3 + 1 ], sValues[ i * 3 + 2 ] );
				}
			} else {
				newkey.scales = null;
			}


			return newkey;
		
		}
		
		// local method, works with tmphierarchies
		// only available during B2Jrecord.load
		private void parseHierarchy( IList hierarchy, List< B2Jhierarchy > holder ) {
			for (int i = 0; i < hierarchy.Count; i++) {
				IDictionary tmph = ( IDictionary ) hierarchy[ i ];
				B2Jhierarchy newh = new B2Jhierarchy();
				newh.name = "" + tmph[ "bone" ];
				newh.children = new List<B2Jhierarchy>();
				parseHierarchy( ( IList ) tmph[ "children" ], newh.children );
				holder.Add( newh );
			}
		}

		// local method, works with summary_p, etc
		// only available during B2Jrecord.load
		private void parseSummary( IDictionary summary ) {

			// parsing summary first, will make our life easier
			// when parsing bones
			
			summary_p_all = false;
			summary_p = convertListOfIndex( ( IList ) summary[ "positions" ] );
			if ( summary_p.Count > 0 && summary_p[ 0 ] == -1 ) {
				summary_p.Clear();
				summary_p_all = true;
			}
			
			summary_q_all = false;
			summary_q = convertListOfIndex( ( IList ) summary[ "quaternions" ] );
			if ( summary_q.Count > 0 && summary_q[ 0 ] == -1 ) {
				summary_q.Clear();
				summary_q_all = true;
			}
			
			summary_s_all = false;
			summary_s = convertListOfIndex( ( IList ) summary[ "scales" ] );
			if ( summary_s.Count > 0 && summary_s[ 0 ] == -1 ) {
				summary_s.Clear();
				summary_s_all = true;
			}

		}

		// very important!
		// at the end of theis method, summaries will be adapted if they are flagged "all"
		// this will make the work on the key level a bit faster
		// !!! => this must be done BEFORE parsing keys 
		private List<B2Jbone> parseBones( IDictionary data ) {

			// basic list of bones
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

			// rebuilding hierarchy
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

			// adapting summaries lists
			if ( summary_p_all || summary_q_all || summary_s_all ) {
				if ( summary_p_all ) {
					summary_p.Clear();
				}
				if ( summary_q_all ) {
					summary_q.Clear();
				}
				if ( summary_s_all ) {
					summary_s.Clear();
				}
				for ( int i = 0; i < output.Count; i++ ) {
					if ( summary_p_all ) {
						summary_p.Add( i );
					}
					if ( summary_q_all ) {
						summary_q.Add( i );
					}
					if ( summary_s_all ) {
						summary_s.Add( i );
					}
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

		private int getIndexInSummary( IList<int> summary, int id ) {

			for( int i = 0; i < summary.Count; i++ ) {
				if ( id == summary[ i ] ) {
					return i;
				}
			}
			return -1;
			
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

		public List<float> convertListOfFloat( IList _list ) {
			
			List<float> output = new List<float>();
			for (int i = 0; i < _list.Count; i++) {
				output.Add ( float.Parse ( "" + _list [i] ) );
			}
			return output;
			
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

	}
	#endregion

	#region server definition
	public class B2Jserver {

		static readonly B2Jserver _instance = new B2Jserver();
		
		public static B2Jserver Instance {
			get {
				return _instance;
			}
		}

		private List<string> loadedpath;
		private List<string> loadingpath;
		private List<B2Jrecord> records;

		private B2Jserver() {

			loadedpath = new List<string> ();
			loadingpath = new List<string> ();
			records = new List<B2Jrecord> ();

		}

		public void load( string path ) {

			if ( loadedpath.Contains ( path ) ) {
				Debug.Log ( "'" + path + "' already loaded" );
				return;
			}

			loadedpath.Add( path );
			addNewRecord( B2Jparser.Instance.load ( path ) );

		}

		/*
		private IEnumerator loader() {
		
			if ( loadingpath == null ) {
				Debug.Log ( "FUCK!" );
				yield return true;
			}

			while ( loadingpath.Count > 0 ) {
				string path = loadingpath[ 0 ];
				loadingpath.Remove( path );
				addNewRecord( B2Jparser.Instance.load ( path ) );
			}

			yield return true;

		}
		*/

		public void addNewRecord( B2Jrecord rec ) {

			if ( rec != null ) {
				records.Add( rec );
				Debug.Log ( "new record added: " + rec.name + ", " + records.Count + " record(s) loaded" );
			}

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
						Debug.Log ( "\t\tparent:" + br.bones[ i ].parent.name );
					}
					foreach( B2Jbone child in br.bones[ i ].children ) {
						Debug.Log ( "\t\tchild:" + child.name );
					}

				}
				for ( int i = 0; i < br.keys.Count; i++ ) {
					Debug.Log ( "key[" + i + "] = " + br.keys[ i ].kID + " / " + br.keys[ i ].timestamp );
				}
				Debug.Log ("BVH2JSON************");
			}
		}

	}
	#endregion

}
