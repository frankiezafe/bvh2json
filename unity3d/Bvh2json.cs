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

//	public class B2Jmapping {
//		public Dictionary< UnityEngine.Transform, string> transform2Bones = new Dictionary<Transform, string> ();
//		public Dictionary< UnityEngine.Transform, Quaternion> initialRotation = new Dictionary<Transform, Quaternion> ();
//	}

	public class B2Jretriever {
		public string model;
		public Dictionary< UnityEngine.Transform, int> ids;
		public Dictionary< UnityEngine.Transform, Vector3> positions;
		public Dictionary< UnityEngine.Transform, Quaternion> rotations;
		public Dictionary< UnityEngine.Transform, Vector3> scales;

	}

	public enum B2Jloop {
		B2JLOOPNONE = 0,
		B2JLOOPNORMAL = 1,
		B2JLOOPPALINDROME = 2
	}

	public class B2Jplayhead {

		private B2Jloop _loop;
		private bool _active = true;
		private float _time;
		private float _speed; // multiplier of time in millis
		private B2Jrecord _record;
		private float _weight;
		private List<Vector3> positions; // same length as record.bones
		private List<Quaternion> rotations; // same length as record.bones
		private List<Vector3> scales; // same length as record.bones

		private float _cueIn;
		private float _cueOut;

		protected B2Jretriever _retriever;

		// working values
		Vector3 p1;
		Quaternion q1;
		Vector3 s1;
		Vector3 p2;
		Quaternion q2;
		Vector3 s2;
		Vector3 Rp;
		Quaternion Rq;
		Vector3 Rs;

		public B2Jplayhead( B2Jrecord rec, B2Jloop loop ) {

			_loop = loop;
			_record = rec;
			_retriever = null;
			positions = new List<Vector3> ();
			rotations = new List<Quaternion> ();
			scales = new List<Vector3> ();
			foreach (B2Jbone b in rec.bones) {
				positions.Add( new Vector3() );
				rotations.Add(  Quaternion.identity );
				scales.Add( new Vector3() );
			}
			_cueIn = _record.keys[ 0 ].timestamp;
			_cueOut = _record.keys[ _record.keys.Count - 1 ].timestamp;
			_time = _cueIn;
			_speed = 1;
			_weight = 1;

		}

		public string Name {
			get {
				return _record.name;
			}
		}

		public float Speed {
			get {
				return _speed;
			}
			set {
				_speed = value;
			}
		}

		public B2Jretriever Retriever {
			get{
				return _retriever;
			}
		}

		public string Info {
			get {
				return "" + _time + " [ " + _cueIn + ", " + _cueOut + "], " + _record.name + " / " + _active;
			}
		}

		public B2Jrecord Record {
			get{
				return _record;
			}
		}

		public bool Active {
			get{
				if ( _retriever == null || !_active || _weight == 0 )
					return false;
				return true;
			}
		}

		public float Weight {
			set {
				if ( value < 0 || value > 1 ) {
					Debug.LogError( "weight must be in [ 0,1 ]!" );
					return;
				}
				_weight = value;
			}
			get {
				return _weight;
			}
		}

//		public void setMap( Dictionary< string, B2Jmapping > mapmodel ) {
//
//			B2Jmapping mm = mapmodel [_record.model];
//			if ( mm != null ) {
//
//				_retriever = new B2Jretriever();
//				_retriever.model = _record.model;
//				_retriever.ids = new Dictionary< UnityEngine.Transform, int>();
//				_retriever.positions = new Dictionary< UnityEngine.Transform, Vector3>();
//				_retriever.rotations = new Dictionary< UnityEngine.Transform, Quaternion>();
//				_retriever.scales = new Dictionary< UnityEngine.Transform, Vector3>();
//
//				foreach( KeyValuePair< UnityEngine.Transform, string > kv in mm.transform2Bones ) {
//					string bname = kv.Value;
//					int bID = 0;
//					foreach( B2Jbone b in _record.bones ) {
//						if ( b.name == bname ) {
//							_retriever.ids.Add( kv.Key, bID );
//							_retriever.positions.Add( kv.Key, positions[ bID ] );
//							_retriever.rotations.Add( kv.Key, rotations[ bID ] );
//							_retriever.scales.Add( kv.Key, scales[ bID ] );
//							break;
//						}
//						bID++;
//					}
//				}
//
//			}
//
//		}

		public void update() {

			if ( !_active || _speed == 0 ) {
				// not ready to use, create mapping or reactivate...
				return;
			}
			
			if ( _time > _cueOut ) {
				
				_time = _cueOut;
				
				if ( _loop == B2Jloop.B2JLOOPNONE ) {
					_active = false;
					return;
				} else if ( _loop == B2Jloop.B2JLOOPNORMAL ) {
					// go back to beginning
					_time = _cueIn;
				} else if ( _loop == B2Jloop.B2JLOOPPALINDROME ) {
					// go back to beginning
					_speed *= -1;
				}
				
			} else if ( _time < _cueIn ) {
				_time = _cueIn;
				if ( _loop == B2Jloop.B2JLOOPNORMAL ) {
					_speed *= -1;
				}
			}
			
			if ( _weight == 0 || _retriever == null ) {
				return;
			}

			renderFrame();

			// updating the retriever
			foreach ( KeyValuePair< UnityEngine.Transform, int > kv in _retriever.ids ) {
				_retriever.positions[ kv.Key ] = positions[ kv.Value ];
				_retriever.rotations[ kv.Key ] = rotations[ kv.Value ];
				_retriever.scales[ kv.Key ] = scales[ kv.Value ];
			}

			_time += Time.deltaTime * 1000 * _speed;

		}
		
		private void renderFrame() {

			// seeking frames
			B2Jkey below = null;
			B2Jkey above = null;

			if ( _speed > 0 ) {
				foreach( B2Jkey k in _record.keys ) {
					below = above;
					above = k;
					if ( above.timestamp >= _time )
						break;
				}
			} else {
				foreach( B2Jkey k in _record.keys ) {
					above = below;
					below = k;
					if ( below.timestamp >= _time )
						break;
				}
			}

			if ( above == null ) {
				Debug.LogError ( "Impossible to find frames at this timecode!!!: " + _time );
			}

			if ( above.timestamp == _time || below == null ) {
				// cool, it's easy ( but rare )
				for( int i = 0; i < _record.bones.Count; i++ ) {
					if ( _record.bones[ i ].positions_enabled ) {
						p1 = above.positions[ i ];
						positions[ i ] = new Vector3( p1.x, p1.y, p1.z );
					}
					if ( _record.bones[ i ].rotations_enabled ) {
						q1 = above.rotations[ i ];
						rotations[ i ] = new Quaternion( q1.x, q1.y, q1.z, q1.w );
					}
					if ( _record.bones[ i ].scales_enabled ) {
						s1 = above.scales[ i ];
						scales[ i ] = new Vector3( s1.x, s1.y, s1.z );
					}
				}

			} else {

				// ... less funny, have to smooth values...
				float gap = above.timestamp - below.timestamp;
				float abovepc = ( ( _time - below.timestamp ) / gap );
				float belowpc = 1 - abovepc;

				for( int i = 0; i < _record.bones.Count; i++ ) {

					if ( _record.bones[ i ].positions_enabled ) {
						p1 = below.positions[ i ];
						p2 = above.positions[ i ];
						positions[ i ] = new Vector3( 
						       p1.x * belowpc + p2.x * abovepc,
						       p1.y * belowpc + p2.y * abovepc,
						       p1.z * belowpc + p2.z * abovepc
						       );
					}

					if ( _record.bones[ i ].rotations_enabled ) {
						q1 = below.rotations[ i ];
						q2 = above.rotations[ i ];
						rotations[ i ] = Quaternion.Slerp( q1, q2, abovepc );
					}

					if ( _record.bones[ i ].scales_enabled ) {
						s1 = below.scales[ i ];
						s2 = above.scales[ i ];
						scales[ i ] = new Vector3( 
						       s1.x * belowpc + s2.x * abovepc,
						       s1.y * belowpc + s2.y * abovepc,
						       s1.z * belowpc + s2.z * abovepc
						       );
					}

				}

			}

		}

	}

	public class B2JgenericPlayer : MonoBehaviour {
		
		public B2Jserver B2J_server;
		protected Dictionary< string, B2Jmap > B2J_maps;
		protected List< B2Jplayhead > B2J_playheads;

		public B2JgenericPlayer() {
			B2J_server = null;
			B2J_maps = new Dictionary< string, B2Jmap >();
			B2J_playheads = new List< B2Jplayhead > ();
		}

		public void loadMapping( TextAsset asset ) {
			B2Jmap map = new B2Jmap();
			if ( map.load( asset, this ) ) {
				if ( B2J_maps.ContainsKey( map.model ) ) {
					Debug.Log( "A map with the same model as already been loaded! It will be overwritten by the current one: " + map.name );
				}
				B2J_maps.Add( map.model, map );
			}
		}

		public B2Jplayhead getPlayhead( string name ) {

			foreach( B2Jplayhead ph in B2J_playheads )
				if ( ph.Name == name )
					return ph;

			return null;

		}

		protected void sync() {
			Synchronise ();
		}

		private void Synchronise() {

			if (B2J_server != null) {

				B2J_server.syncPlayheads( B2J_playheads );
				// all playheads are now ok
				foreach( B2Jplayhead ph in B2J_playheads ) {

//					if ( ph.Retriever == null )
//						ph.setMap( b2jMaps );

					ph.update();

				}

			}

		}

		
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
		private List<int> idsFullList;
		
		private B2Jparser() {}

		public B2Jrecord load( string path ) {
			
			TextAsset bvhj = Resources.Load( path ) as TextAsset;
			if ( bvhj == null) {
				Debug.Log ( "Bvh2jsonReader::" + path + " not found" );
				return null;
			} else {
				Debug.Log ( "Bvh2jsonReader::" + path + " successfully loaded" );
			}
			
			IDictionary data = ( IDictionary ) Json.Deserialize ( bvhj.ToString() );
			if ( data == null) {
				Debug.Log ( "Failed to parse " + path );
				return null;
			}

			idsFullList = new List<int> ();

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
			B2Jkey prevk = null;
			for (int i = 0; i < dataks.Count; i++) {
				// waiting for the first key to be id 0
				B2Jkey newk = parseKey( (IDictionary) dataks[ i ], bones, prevk );
				if ( newk != null ) {
					output.Add( newk );
				}
				prevk = newk;
			}

			return output;
		
		}

		private B2Jkey parseKey( IDictionary keydata, List<B2Jbone> bones, B2Jkey previouskey ) {
		
			B2Jkey newkey = new B2Jkey ();
			newkey.kID = int.Parse ( "" + keydata ["id"] );
			newkey.timestamp = float.Parse ( "" + keydata ["time"]);

			// positions list
			if (summary_p.Count > 0) {
				newkey.positions = new List<Vector3> ();
				for (int i = 0; i < bones.Count; i++) {
					if ( previouskey == null )
						newkey.positions.Add( new Vector3( 0,0,0 ) );
					else
						newkey.positions.Add( new Vector3( previouskey.positions[ i ].x, previouskey.positions[ i ].y, previouskey.positions[ i ].z ) );
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
					if ( previouskey == null )
						newkey.rotations.Add ( Quaternion.identity );
					else
						newkey.rotations.Add( previouskey.rotations[ i ] );
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
					newkey.rotations[ qIds [i] ] = new Quaternion ( qValues [i * 4], qValues [i * 4 + 1], qValues [i * 4 + 2], qValues [i * 4 + 3] );
				}
			} else {
				newkey.rotations = null;
			}
			
			// scales list
			if (summary_s.Count > 0) {
				newkey.scales = new List<Vector3> ();
				for (int i = 0; i < bones.Count; i++) {
					if ( previouskey == null )
						newkey.scales.Add ( new Vector3 (1, 1, 1) );
					else
						newkey.scales.Add( new Vector3( previouskey.scales[ i ].x, previouskey.scales[ i ].y, previouskey.scales[ i ].z ) );
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
			IList rests = ( IList ) data[ "rest" ];
			for ( int i = 0; i < dbs.Count; i++ ) {
				string bname = "" + dbs[ i ];
				B2Jbone newb = new B2Jbone();
				newb.name = bname;
				newb.children = new List<B2Jbone>();
				newb.parent = null;
				newb.rest = new Vector3( float.Parse( "" + rests[ ( i * 3 ) ] ), float.Parse( "" + rests[ ( i * 3 ) + 1 ] ), float.Parse( "" + rests[ ( i * 3 ) + 2 ] ) );
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
				// filling list of all bones ids
				idsFullList.Add( i );
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
			if (_list.Count == 0) {
				return output;
			}
			if ( string.Compare( "" + _list[0], "all" ) == 0 ) {
				output = new List<int>( idsFullList );
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

	public class B2Jmap {

		public string model;
		public string name;
		public string description;
		public float version;
		private Dictionary< string, Transform > transformByName;
		private Dictionary< int, Transform > transformById;

		public B2Jmap() {

			transformByName = new Dictionary< string, Transform >();
			transformById = new Dictionary< int, Transform >();

		}

		// pass the text assets containung the mapping and the game object (an avatar...) where the bones are
		public bool load( TextAsset bvhj, B2JgenericPlayer obj ) { 

			if ( bvhj == null) {
				Debug.Log ( "B2Jmap:: not loaded" );
				return false;
			} else {
				Debug.Log ( "B2Jmap:: '" + bvhj.name + "' successfully loaded" );
			}

			IDictionary data = ( IDictionary ) Json.Deserialize ( bvhj.ToString() );
			if ( data == null) {
				Debug.Log ( "Failed to parse " + bvhj.name );
				return false;
			}

			if (System.String.Compare ( (string) data ["type"], "mapping") != 0) {
				Debug.Log ( "B2J maps must have a type 'maaping'" );
				return false;
			}

			model = (string) data[ "model" ];
			name = (string) data[ "name" ];
			description = (string) data[ "desc" ];
			version = float.Parse( "" + data[ "version" ] );

			IList bvh_bones = ( IList ) data[ "local" ];
			IList transform_names = ( IList ) data[ "foreign" ];
			if ( bvh_bones.Count != transform_names.Count ) {
				Debug.Log ( "local count and foreign doesn't match! local: " + bvh_bones.Count +", foreign: "+ transform_names.Count );
				return false;
			}

			// validation of foreigns
			Transform[] all_transforms = obj.GetComponentsInChildren<Transform>();
			for ( int i = 0; i < transform_names.Count; i++ ) {
				foreach( Transform transform in all_transforms ) {
					if ( System.String.Compare( transform.name, (string) transform_names[ i ] ) == 0 ) {
						transformByName.Add( (string) bvh_bones[ i ], transform );
						transformById.Add( i, transform );
						break;
					}
				}
			}
			return true;

		}

		public Transform getTransformByName( string bvh_bone_name ) {
			if ( !transformByName.ContainsKey( bvh_bone_name ) ) {
				return null;
			}
			return transformByName[ bvh_bone_name ];
		}
		
		public Transform getTransformById( int bvh_bone_id ) {
			if ( !transformById.ContainsKey( bvh_bone_id ) ) {
				return null;
			}
			return transformById[ bvh_bone_id ];
		}

	
	}

}
