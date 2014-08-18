using UnityEngine;

using System;
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
		public string rotation_order;
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
		private List< Vector3 > _positions; // same length as record.bones
		private List< Quaternion > _rotations; // same length as record.bones
		private List< Vector3 > _scales; // same length as record.bones

		private float _cueIn;
		private float _cueOut;

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
			_positions = new List<Vector3> ();
			_rotations = new List<Quaternion> ();
			_scales = new List<Vector3> ();
			foreach (B2Jbone b in rec.bones) {
				_positions.Add( new Vector3() );
				_rotations.Add(  Quaternion.identity );
				_scales.Add( new Vector3() );
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
		
		
		public string Model {
			get {
				return _record.model;
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
				if ( !_active || _weight == 0 )
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

		public float CurrentTime {
			get {
				return _time;
			}
			set {
				_time = value;
			}
		}

		public float CueIn {
			get {
				return _cueIn;
			}
			set {
				_cueIn = value;
			}
		}
		
		public float CueOut {
			get {
				return _cueOut;
			}
			set {
				_cueOut = value;
			}
		}

		public List< Quaternion > Rotations {
			get {
				return _rotations;
			}
		}
		
		public List< Vector3 > Positions {
			get {
				return _positions;
			}
		}
		
		public List< Vector3 > Scales {
			get {
				return _scales;
			}
		}

		public void update( bool interpolation ) {

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
			
			if ( _weight == 0 ) {
				return;
			}

			renderFrame( interpolation );

			_time += Time.deltaTime * 1000 * _speed;

		}
		
		private void renderFrame( bool interpolation ) {

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

			if ( above.timestamp == _time || below == null || !interpolation ) {
				// cool, it's easy ( but rare )
				for( int i = 0; i < _record.bones.Count; i++ ) {
					if ( _record.bones[ i ].positions_enabled ) {
						p1 = above.positions[ i ];
						_positions[ i ] = new Vector3( p1.x, p1.y, p1.z );
					}
					if ( _record.bones[ i ].rotations_enabled ) {
						q1 = above.rotations[ i ];
						_rotations[ i ] = new Quaternion( q1.x, q1.y, q1.z, q1.w );
					}
					if ( _record.bones[ i ].scales_enabled ) {
						s1 = above.scales[ i ];
						_scales[ i ] = new Vector3( s1.x, s1.y, s1.z );
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
						_positions[ i ] = new Vector3( 
						       p1.x * belowpc + p2.x * abovepc,
						       p1.y * belowpc + p2.y * abovepc,
						       p1.z * belowpc + p2.z * abovepc
						       );
					}

					if ( _record.bones[ i ].rotations_enabled ) {
						q1 = below.rotations[ i ];
						q2 = above.rotations[ i ];
						_rotations[ i ] = Quaternion.Slerp( q1, q2, abovepc );
					}

					if ( _record.bones[ i ].scales_enabled ) {
						s1 = below.scales[ i ];
						s2 = above.scales[ i ];
						_scales[ i ] = new Vector3( 
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

		protected Dictionary< Transform, Quaternion > localRotations;
		protected Dictionary< Transform, Vector3 > localTranslations;
		protected Dictionary< Transform, Vector3 > localScales;

		protected bool interpolate;

		public B2JgenericPlayer() {

			B2J_server = null;
			B2J_maps = new Dictionary< string, B2Jmap >();
			B2J_playheads = new List< B2Jplayhead > ();

			// making a copy of the current object rotations and orientations
			localRotations = new Dictionary< Transform, Quaternion > ();
			localTranslations = new Dictionary< Transform, Vector3 > ();
			localScales = new Dictionary< Transform, Vector3 > ();

			interpolate = true;

		}

		protected void init() {
			Transform[] all_transforms = GetComponentsInChildren<Transform>();
			foreach( Transform t in all_transforms ) {
				localRotations.Add( t, new Quaternion( t.localRotation.x, t.localRotation.y, t.localRotation.z, t.localRotation.w ) );
				localTranslations.Add( t, new Vector3( t.localPosition.x, t.localPosition.y, t.localPosition.z ) );
				localScales.Add( t, new Vector3( t.localScale.x, t.localScale.y, t.localScale.z ) );
			}
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
			Synchronise();
		}

		private void Synchronise() {
			if ( B2J_server != null ) {
				B2J_server.syncPlayheads( B2J_playheads );
				// all playheads are now ok
				foreach( B2Jplayhead ph in B2J_playheads ) {
					ph.update( interpolate );
				}
			}
		}

		protected void apply() {

			float totalWeight = 0;
			foreach( B2Jplayhead ph in B2J_playheads ) {
				totalWeight += ph.Weight;
			}
			if ( totalWeight == 0 ) {
				return;
			}
			// storing all updated transforms in a temporary dict
			Dictionary< Transform, Quaternion > updatedRots = new Dictionary<Transform, Quaternion>();
			foreach( B2Jplayhead ph in B2J_playheads ) {
				if ( ph.Weight == 0 ) {
					continue;
				}
				// searching the map for this model
				B2Jmap map = B2J_maps[ ph.Model ];
				// no map found, no need to go further!
				if ( map == null ) {
					continue;
				}
				// no need to go over all bones, just the ones of the mapping
				foreach ( KeyValuePair< int, Transform > pair in map.transformById ) {
					int bid = pair.Key;
					Transform t = pair.Value;
					if ( !updatedRots.ContainsKey( t ) ) {
						updatedRots.Add( t, Quaternion.identity );
					}
					Quaternion newrot = ph.Rotations[ bid ];
					updatedRots[ t ] = Quaternion.Slerp( updatedRots[ t ], newrot, ph.Weight );
				}
			}

			// and applying on the model
			foreach ( KeyValuePair< Transform, Quaternion > pair in updatedRots ) {

				pair.Key.localRotation = localRotations[ pair.Key ] * pair.Value;
//				pair.Key.localRotation = pair.Value;
				
//				Transform t = pair.Key;
//				Quaternion test = Quaternion.identity;
//				test = Quaternion.Slerp( test, pair.Value, 0.5f );
//				Quaternion newq = localRotations[ t ] * pair.Value;
//				t.rotation = newq;
//				t.localRotation = pair.Value;
//				t.localRotation = localRotations[ t ];

//				Matrix4x4 restmat = new Matrix4x4();
//				restmat.SetTRS( Vector3.zero, localRotations[ t ], Vector3.one );
//				Matrix4x4 restmatI = restmat.inverse;
//				Matrix4x4 newmat = new Matrix4x4();
//				newmat.SetTRS( Vector3.zero, pair.Value, Vector3.one );
//				newmat = restmatI * newmat * restmat;
//				t.localRotation = Quaternion.LookRotation( newmat.GetColumn(2), newmat.GetColumn(1) );

				/*
					// BVH SPACE
					Quaternion q = new Quaternion ( qValues [i * 4], qValues [i * 4 + 1], qValues [i * 4 + 2], qValues [i * 4 + 3] );
					Matrix4x4 BVH2UNITY = Matrix4x4.identity;

					BVH2UNITY.m00 = 1;
					BVH2UNITY.m01 = 0;
					BVH2UNITY.m02 = 0;

					BVH2UNITY.m10 = 0;
					BVH2UNITY.m11 = 1;
					BVH2UNITY.m12 = 0;

					BVH2UNITY.m20 = 0;
					BVH2UNITY.m21 = 0;
					BVH2UNITY.m22 = -1;

					Matrix4x4 BVH2UNITYi = BVH2UNITY.inverse;
					Matrix4x4 conv = new Matrix4x4();
					conv.SetTRS( Vector3.zero, q, Vector3.one );
					conv = BVH2UNITYi * conv * BVH2UNITY;
					q = Quaternion.LookRotation( conv.GetColumn(2), conv.GetColumn(1) );

					newkey.rotations[ qIds [i] ] = q;
					*/


			}

		}
		
	}

	public class B2Jmap {
		
		public string model;
		public string name;
		public string description;
		public float version;
		public Dictionary< string, Transform > transformByName;
		public Dictionary< int, Transform > transformById;
		
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
		private bool summary_euler_all;
		private List<int> summary_euler;
		private bool summary_s_all;
		private List<int> summary_s;
		private List<int> idsFullList;
		private List<string> summary_rotation_order;
		
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
			summary_rotation_order = new List<string> ();

			parseHierarchy ( (IList) data["hierarchy"], tmphierarchies );
			parseSummary( (IDictionary) data["summary"] );
			parseRotationOrder ((IList) data ["rotation_order"]);

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
			summary_euler.Clear ();
			summary_s.Clear ();
			tmphierarchies = null;
			summary_p = null;
			summary_euler = null;
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
			if (summary_euler.Count > 0) {
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

				List<int> eulIds = convertListOfIndex ( (IList)( ( IDictionary)keydata ["eulers"] ) ["bones"] );
				List<float> eulValues = convertListOfFloat ( (IList)( ( IDictionary)keydata ["eulers"] ) ["values"] );

				for (int i = 0; i < eulIds.Count; i++ ) {

					Quaternion q = Quaternion.identity;
					Vector3 eulers = new Vector3( eulValues [i * 3], eulValues [i * 3 + 1], eulValues [i * 3 + 2] );
					eulers.z *= -1;
//					string roto = summary_rotation_order[ eulIds[i] ];
//					Debug.Log( eulIds[i] +" rot order: " + roto );
					q.eulerAngles = eulers;
					newkey.rotations[ eulIds[i] ] = q;

//					newkey.rotations[ qIds [i] ] = new Quaternion ( qValues [i * 4], qValues [i * 4 + 1], qValues [i * 4 + 2], qValues [i * 4 + 3] );
//					Vector3 euls = newkey.rotations[ qIds [i] ].eulerAngles;
//					Debug.Log ( qIds [i] + "eulers = " + euls.x +", " + euls.y +", " + euls.z );

					// conversion form right 2 left handed
//					Quaternion q = new Quaternion ( qValues [i * 4], qValues [i * 4 + 1], qValues [i * 4 + 2], qValues [i * 4 + 3] );
//					Matrix4x4 mat = new Matrix4x4();
//					mat.SetTRS( Vector3.zero, q, Vector3.one );
//
//					Matrix4x4 matR2L = Matrix4x4.identity;
//					matR2L.m11 = -1;
//					matR2L.m22 = -1;
//					Matrix4x4 matR2Li = matR2L.inverse;
//
//					mat = mat * matR2L;
//					newkey.rotations[ qIds [i] ] = Quaternion.LookRotation( mat.GetColumn(2), mat.GetColumn(1) );
//
//					// correction
//					Matrix4x4 matR2L = Matrix4x4.identity;
//					matR2L.m11 = -1;
//					Matrix4x4 matR2Li = matR2L.inverse;
//
//					matR2L = matR2Li * mat * matR2L;
//
//					newkey.rotations[ qIds [i] ] = Quaternion.LookRotation( matR2L.GetColumn(2), matR2L.GetColumn(1) );

//					Matrix4x4 matR2L = Matrix4x4.identity;

// ref: http://stackoverflow.com/questions/1263072/changing-a-matrix-from-right-handed-to-left-handed-coordinate-system/1264880#1264880
/*

{ m00, m01, m02, m03 }
{ m10, m11, m12, m13 }
{ m20, m21, m22, m23 }
{ m30, m31, m32, m33 }

{ rx, ry, rz, 0 }  
{ ux, uy, uz, 0 }  
{ lx, ly, lz, 0 }  
{ px, py, pz, 1 }

To change it from left to right or right to left, flip it like this:

{ rx, rz, ry, 0 }  
{ lx, lz, ly, 0 }  
{ ux, uz, uy, 0 }  
{ px, pz, py, 1 }

*/
//					matR2L.m00 = mat.m00;
//					matR2L.m01 = mat.m02;
//					matR2L.m02 = mat.m01;
//					matR2L.m10 = mat.m20;
//					matR2L.m11 = mat.m22;
//					matR2L.m12 = mat.m21;
//					matR2L.m20 = mat.m10;
//					matR2L.m21 = mat.m12;
//					matR2L.m22 = mat.m11;

// ref: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/
//					float qx, qy, qz, qw;
//
//					float tr = matR2L.m00 + matR2L.m11 + matR2L.m22;
//					if (tr > 0) { 
//						float S = (float) Math.Sqrt( tr + 1.0 ) * 2; // S=4*qw 
//						qw = 0.25f * S;
//						qx = (matR2L.m21 - matR2L.m12) / S;
//						qy = (matR2L.m02 - matR2L.m20) / S; 
//						qz = (matR2L.m10 - matR2L.m01) / S; 
//					} else if ((matR2L.m00 > matR2L.m11)&(matR2L.m00 > matR2L.m22)) { 
//						float S = (float) Math.Sqrt( 1.0 + matR2L.m00 - matR2L.m11 - matR2L.m22 ) * 2; // S=4*qx 
//						qw = (matR2L.m21 - matR2L.m12) / S;
//						qx = 0.25f * S;
//						qy = (matR2L.m01 + matR2L.m10) / S; 
//						qz = (matR2L.m02 + matR2L.m20) / S; 
//					} else if (matR2L.m11 > matR2L.m22) { 
//						float S = (float) Math.Sqrt( 1.0 + matR2L.m11 - matR2L.m00 - matR2L.m22 ) * 2; // S=4*qy
//						qw = (matR2L.m02 - matR2L.m20) / S;
//						qx = (matR2L.m01 + matR2L.m10) / S; 
//						qy = 0.25f * S;
//						qz = (matR2L.m12 + matR2L.m21) / S; 
//					} else { 
//						float S = (float) Math.Sqrt( 1.0 + matR2L.m22 - matR2L.m00 - matR2L.m11 ) * 2; // S=4*qz
//						qw = (matR2L.m10 - matR2L.m01) / S;
//						qx = (matR2L.m02 + matR2L.m20) / S;
//						qy = (matR2L.m12 + matR2L.m21) / S;
//						qz = 0.25f * S;
//					}
//					newkey.rotations[ qIds [i] ] = new Quaternion( qx, qy, qz, qw );

//					newkey.rotations[ qIds [i] ] = Quaternion.LookRotation( mat.GetColumn(2), mat.GetColumn(1) );

//					newkey.rotations[ qIds [i] ] = Quaternion.LookRotation( matR2L.GetColumn(2), matR2L.GetColumn(1) );

					// RIGHT 2 LEFT HANDED
//					Quaternion lq = new Quaternion ( (float) Math.Sqrt(2) * 0.5f, (float) Math.Sqrt(2) * 0.5f, 0, 0 );
//					Quaternion rq = new Quaternion ( qValues [i * 4], qValues [i * 4 + 1], qValues [i * 4 + 2], qValues [i * 4 + 3] );

					// BVH SPACE
//					Quaternion q = new Quaternion ( qValues [i * 4], qValues [i * 4 + 1], qValues [i * 4 + 2], qValues [i * 4 + 3] );
//					Matrix4x4 BVH2UNITY = Matrix4x4.identity;
//
//					BVH2UNITY.m00 = 1;
//					BVH2UNITY.m01 = 0;
//					BVH2UNITY.m02 = 0;
//
//					BVH2UNITY.m10 = 0;
//					BVH2UNITY.m11 = 1;
//					BVH2UNITY.m12 = 0;
//
//					BVH2UNITY.m20 = 0;
//					BVH2UNITY.m21 = 0;
//					BVH2UNITY.m22 = 1;
//
//					Matrix4x4 BVH2UNITYi = BVH2UNITY.inverse;
//					Matrix4x4 conv = new Matrix4x4();
//					conv.SetTRS( Vector3.zero, q, Vector3.one );
//					conv = BVH2UNITYi * conv * BVH2UNITY;
//					q = Quaternion.LookRotation( conv.GetColumn(2), conv.GetColumn(1) );

//					newkey.rotations[ qIds [i] ] = q;

//					newkey.rotations[ qIds [i] ] = rq * lq;

//					Quaternion tmp = new Quaternion ( qValues [i * 4], qValues [i * 4 + 1], qValues [i * 4 + 2], qValues [i * 4 + 3] );
//					Vector3 eq = tmp.eulerAngles;
//					eq.y = (float) Math.Atan2( Math.Cos( eq.y / 180.0 * Math.PI ), Math.Sin( eq.y / 180.0 * Math.PI ) );
//					eq.y *= -1;
//					Quaternion newq = new Quaternion();
//					newq.eulerAngles = eq;
//					newkey.rotations[ qIds [i] ] = newq;

//					Quaternion tmp = new Quaternion( qValues [i * 4], qValues [i * 4 + 1], qValues [i * 4 + 2], qValues [i * 4 + 3] );
//					Vector3 eq = tmp.eulerAngles;
//					eq.y *= -1;
//					eq.z *= -1;
//					Quaternion newq = new Quaternion();
//					newq.eulerAngles = eq;
//					newkey.rotations[ qIds [i] ] = newq;

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
			
			summary_euler_all = false;
			summary_euler = convertListOfIndex( ( IList ) summary[ "eulers" ] );
			if ( summary_euler.Count > 0 && summary_euler[ 0 ] == -1 ) {
				summary_euler.Clear();
				summary_euler_all = true;
			}
			
			summary_s_all = false;
			summary_s = convertListOfIndex( ( IList ) summary[ "scales" ] );
			if ( summary_s.Count > 0 && summary_s[ 0 ] == -1 ) {
				summary_s.Clear();
				summary_s_all = true;
			}

		}

		private void parseRotationOrder( IList torOrder ) {

			for (int i = 0; i < torOrder.Count; i++) {
				summary_rotation_order.Add( torOrder[ i ].ToString() );
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
				newb.rotation_order = summary_rotation_order[ i ];
				newb.children = new List<B2Jbone>();
				newb.parent = null;
				newb.rest = new Vector3( float.Parse( "" + rests[ ( i * 3 ) ] ), float.Parse( "" + rests[ ( i * 3 ) + 1 ] ), float.Parse( "" + rests[ ( i * 3 ) + 2 ] ) );
				newb.positions_enabled = false;
				newb.rotations_enabled = false;
				newb.scales_enabled = false;
				if ( summary_p.Contains( i ) || summary_p_all ) {
					newb.positions_enabled = true;
				}
				if ( summary_euler.Contains( i ) || summary_euler_all ) {
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
			if ( summary_p_all || summary_euler_all || summary_s_all ) {
				if ( summary_p_all ) {
					summary_p.Clear();
				}
				if ( summary_euler_all ) {
					summary_euler.Clear();
				}
				if ( summary_s_all ) {
					summary_s.Clear();
				}
				for ( int i = 0; i < output.Count; i++ ) {
					if ( summary_p_all ) {
						summary_p.Add( i );
					}
					if ( summary_euler_all ) {
						summary_euler.Add( i );
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

}
