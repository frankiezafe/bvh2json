using UnityEngine;

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using MiniJSON;

namespace B2J {

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
		
		private B2Jrecord tmp_rec;
		
		public static Quaternion renderQuaternion( Vector3 eulers, string roto ) {
			
			Quaternion q = Quaternion.identity;
			Quaternion qx = Quaternion.AngleAxis( eulers.x, Vector3.right );
			Quaternion qy = Quaternion.AngleAxis( eulers.y, Vector3.up );
			Quaternion qz = Quaternion.AngleAxis( eulers.z, Vector3.forward );
			
			if ( roto == "ZXY" )
				q = qz * qx * qy;
			else if ( roto == "ZYX" )
				q = qz * qy * qx;
			else if ( roto == "YZX" )
				q = qy * qz * qx;
			else if ( roto == "YXZ" )
				q = qy * qx * qz;
			else if ( roto == "XZY" )
				q = qy * qz * qx;
			else
				q = qx * qy * qz;
			
			return q;
			
		}
		
		private B2Jparser() {}
		
		public B2Jrecord load( string path ) {
			
			TextAsset bvhj = Resources.Load( path ) as TextAsset;
			if ( bvhj == null) {
				Debug.LogError ( "Bvh2jsonReader::" + path + " not found" );
				return null;
			} else {
//				Debug.Log ( "Bvh2jsonReader::" + path + " successfully loaded" );
			}
			
			IDictionary data = ( IDictionary ) Json.Deserialize ( bvhj.ToString() );
			if ( data == null) {
				Debug.LogError ( "Failed to parse " + path );
				return null;
			}
			
			idsFullList = new List<int> ();
			tmphierarchies = new List< B2Jhierarchy > ();
			summary_rotation_order = new List<string> ();
			
			parseHierarchy ( (IList) data["hierarchy"], tmphierarchies );
			parseSummary( (IDictionary) data["summary"] );
			parseRotationOrder ((IList) data ["rotation_order"]);
			
			tmp_rec = new B2Jrecord();
			tmp_rec.type = "" + data[ "type" ];
			tmp_rec.version = float.Parse( ""+data[ "version" ] );
			tmp_rec.desc = "" + data[ "desc" ];
			tmp_rec.name = "" + data[ "name" ];
			tmp_rec.model = "" + data[ "model" ];
			tmp_rec.origin = "" + data[ "origin" ];
			tmp_rec.keyCount = int.Parse( "" + data[ "keys" ] );
			
			tmp_rec.groups = parseGroups( data );
			tmp_rec.bones = parseBones( data );
			tmp_rec.keys = parseKeys( data );
			
			tmphierarchies.Clear ();
			summary_p.Clear ();
			summary_euler.Clear ();
			summary_s.Clear ();
			tmphierarchies = null;
			summary_p = null;
			summary_euler = null;
			summary_s = null;
			
			return tmp_rec;
			
		}
		
		private List<B2Jkey> parseKeys( IDictionary data ) {
			
			List<B2Jkey> output = new List<B2Jkey> ();
			IList dataks = (IList) data[ "data" ];
			B2Jkey prevk = null;
			for (int i = 0; i < dataks.Count; i++) {
				// waiting for the first key to be id 0
				B2Jkey newk = parseKey( (IDictionary) dataks[ i ], prevk );
				if ( newk != null ) {
					output.Add( newk );
				}
				prevk = newk;
			}
			
			return output;
			
		}
		
		private B2Jkey parseKey( IDictionary keydata, B2Jkey previouskey ) {
			
			B2Jkey newkey = new B2Jkey ();
			newkey.kID = int.Parse ( "" + keydata ["id"] );
			newkey.timestamp = float.Parse ( "" + keydata ["time"]);
			
			// positions list
			if (summary_p.Count > 0) {
				for (int i = 0; i < tmp_rec.bones.Count; i++) {
					if ( previouskey == null )
						newkey.positions.Add( new Vector3( 0,0,0 ) );
					else
						newkey.positions.Add( new Vector3( previouskey.positions[ i ].x, previouskey.positions[ i ].y, previouskey.positions[ i ].z ) );
				}
				List<int> pIds = convertListOfIndex ((IList)((IDictionary) keydata ["positions"]) ["bones"] );
				List<float> pValues = convertListOfFloat ((IList)((IDictionary) keydata ["positions"]) ["values"] );
				for (int i = 0; i < pIds.Count; i++) {
					newkey.positions[ pIds[ i ] ] = new Vector3( -pValues[ i * 3 ], pValues[ i * 3 + 1 ], pValues[ i * 3 + 2 ] );
				}
			} else {
				newkey.positions = null;
			}
			
			// rotations list
			if (summary_euler.Count > 0) {
				for (int i = 0; i < tmp_rec.bones.Count; i++) {
					if ( previouskey == null ) {
						newkey.rotations.Add ( Quaternion.identity );
						newkey.eulers.Add( Vector3.zero );
					} else {
						newkey.rotations.Add( previouskey.rotations[ i ] );
						newkey.eulers.Add( previouskey.eulers[ i ] );
					}
				}
				
				List<int> eulIds = convertListOfIndex ( (IList)( ( IDictionary)keydata ["eulers"] ) ["bones"] );
				List<float> eulValues = convertListOfFloat ( (IList)( ( IDictionary)keydata ["eulers"] ) ["values"] );
				
				for (int i = 0; i < eulIds.Count; i++ ) {

					newkey.eulers[ eulIds[i] ] = new Vector3( eulValues [i * 3], -eulValues [i * 3 + 1], -eulValues [i * 3 + 2] );
					Quaternion q = B2Jparser.renderQuaternion( newkey.eulers[ eulIds[i] ], summary_rotation_order[ eulIds[i] ] );
					newkey.rotations[ eulIds[i] ] = q;
					
				}
				
			} else {
				newkey.rotations = null;
				newkey.eulers = null;
			}
			
			// scales list
			if (summary_s.Count > 0) {
				newkey.scales = new List<Vector3> ();
				for (int i = 0; i < tmp_rec.bones.Count; i++) {
					if ( previouskey == null )
						newkey.scales.Add ( new Vector3 (1, 1, 1) );
					else
						newkey.scales.Add( new Vector3( previouskey.scales[ i ].x, previouskey.scales[ i ].y, previouskey.scales[ i ].z ) );
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
				List<float> vs;
				vs = convertListOfFloat( (IList) tmph[ "head" ] );
				newh.head = new Vector3( -vs[ 0 ], vs[ 1 ], vs[ 2 ] );
				vs = convertListOfFloat( (IList) tmph[ "tail" ] );
				newh.tail = new Vector3( -vs[ 0 ], vs[ 1 ], vs[ 2 ] );
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
			for ( int i = 0; i < dbs.Count; i++ ) {
				string bname = "" + dbs[ i ];
				B2Jbone newb = new B2Jbone();
				newb.name = bname;
				newb.rotation_order = summary_rotation_order[ i ];
				newb.children = new List<B2Jbone>();
				newb.parent = null;
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
					bone.head = h.head;
					bone.tail = h.tail;
					bone.rest = bone.tail - bone.head;
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

}