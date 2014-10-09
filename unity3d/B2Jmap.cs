using UnityEngine;

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using MiniJSON;

namespace B2J {

	public class B2Jmap {
		
		public string model;
		public string name;
		public string description;
		public float version;
		public List< Transform > uniqueTransforms; // list of all the transforms concerned by this map
		public Dictionary< string, B2JtransformList > transformListByName; // relation between mocap and transform(s) + weight, by mocap bone name
		public Dictionary< int, B2JtransformList > transformListById; // relation between mocap and transform(s) + weight, by mocap bone id
		public bool enable_rotations = true;
		public bool enable_translations = true;
		public bool enable_scales = true;
		
		public B2JmapLocalValues locals;
		public B2JsmoothMethod smooth_mehod;
		
		public B2Jmap() {

			uniqueTransforms = new List< Transform > ();
			transformListByName = new Dictionary< string, B2JtransformList > ();
			transformListById = new Dictionary< int, B2JtransformList > ();
			locals = new B2JmapLocalValues ();
			smooth_mehod = B2JsmoothMethod.B2JSMOOTH_NONE;

		}
		
		// pass the text assets containung the mapping and the game object (an avatar...) where the bones are
		public bool load( TextAsset bvhj, B2JgenericPlayer obj ) { 
			
			if ( bvhj == null) {
				Debug.LogError ( "B2Jmap:: not loaded" );
				return false;
			} else {
//				Debug.Log ( "B2Jmap:: '" + bvhj.name + "' successfully loaded" );
			}
			IDictionary data = ( IDictionary ) Json.Deserialize ( bvhj.ToString() );
			if ( data == null) {
				Debug.LogError ( "Failed to parse " + bvhj.name );
				return false;
			}
			if ( System.String.Compare ( (string) data ["type"], "mapping") != 0) {
				Debug.LogError ( "B2J maps must have a type 'mapping'" );
				return false;
			}
			model = (string) data[ "model" ];
			name = (string) data[ "name" ];
			description = (string) data[ "desc" ];
			version = float.Parse( "" + data[ "version" ] );
			
			int er = int.Parse( "" + data[ "enable_rotations" ] );
			if ( er == 0 ) {
				enable_rotations = false;
			}
			int et = int.Parse( "" + data[ "enable_translations" ] );
			if ( et == 0 ) {
				enable_translations = false;
			}
			int es = int.Parse( "" + data[ "enable_scales" ] );
			if ( es == 0 ) {
				enable_scales = false;
			}

			smooth_mehod = B2JsmoothMethod.B2JSMOOTH_NONE;
			string tmpsm = "" + data[ "smooth_method" ];
			if ( tmpsm == "ACCUMULATION_OF_DIFFERENCE" ) {
				smooth_mehod = B2JsmoothMethod.B2JSMOOTH_ACCUMULATION_OF_DIFFERENCE;
			}
			
			IList bvh_bones = ( IList ) data[ "list" ];
			Transform[] all_transforms = obj.GetComponentsInChildren < Transform > ();
			if ( data.Contains( "relations" ) ) {
				IList relations = ( IList ) data[ "relations" ];
				for( int i = 0; i < relations.Count; i++ ) {
					IDictionary< string, object > relation = ( Dictionary< string, object > ) relations[ i ];
					foreach( KeyValuePair< string, object > rel in relation ) {
						string b2jname = rel.Key;
						int b2jid = -1;
						for ( int j = 0; j < bvh_bones.Count; j++ ) {
							if ( System.String.Compare( b2jname, bvh_bones[ j ].ToString() ) == 0 ) {
								b2jid = j;
								break;
							}
						}
						if ( b2jid == -1 ) {
							Debug.LogError ( "Unknown relation key: " + b2jname + ", verify 'local' list." );
						}
						if ( rel.Value == null ) {
							Debug.LogError ( "NULL value for key: " + b2jname );
							continue;
						}
						IList relmaps = ( IList ) rel.Value;
						B2JtransformList ml = new B2JtransformList();
						bool store = false;
						for ( int j = 0; j < relmaps.Count; j++ ) {
							IDictionary rmap = ( IDictionary ) relmaps[ j ];
							foreach( Transform transform in all_transforms ) {
								if ( System.String.Compare( transform.name, rmap["bone"].ToString() ) == 0 ) {
									if ( !uniqueTransforms.Contains( transform ) ) {
										uniqueTransforms.Add ( transform );
									}
									ml.transforms.Add( transform );
									ml.weights.Add( float.Parse( rmap["weight"].ToString() ) );
									store = true;
									break;
								}
							}
						}
						if ( store ) {
							transformListByName.Add( b2jname, ml );
							transformListById.Add( b2jid, ml );
						} else {
							Debug.LogError ( "No relations for key: " + b2jname );
							continue;
						}
					}
				}
			} else {
				return false;
			}
			
			return true;
			
		}
		
	}

}
