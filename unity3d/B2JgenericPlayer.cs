using UnityEngine;

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using MiniJSON;

namespace B2J {

	public class B2JgenericPlayer : MonoBehaviour {
		
		public B2Jserver B2Jserver;
		protected Dictionary< string, B2Jmap > _b2jMaps;
		protected Dictionary< string, B2Jplayhead > _b2jPlayheads;
		protected List< B2Jplayhead > _b2jPlayheadList;
		
		protected Dictionary < Transform, Matrix4x4 > _world2local;
		protected Dictionary < string, Transform > _armature;
		protected Dictionary< Transform, Quaternion > _initialQuaternions;
		protected Dictionary< Transform, Vector3 > _initialTranslations;
		protected Dictionary< Transform, Vector3 > _initialScales;
		
		protected Dictionary< Transform, Quaternion > _updatedQuaternions;
		protected Dictionary< Transform, Vector3 > _updatedTranslations;
		protected Dictionary< Transform, Vector3 > _updatedScales;
		
		protected Dictionary< Transform, Quaternion > _allQuaternions;
		protected Dictionary< Transform, Vector3 > _allTranslations;
		protected Dictionary< Transform, Vector3 > _allScales;
		
		protected Dictionary< Transform, float > _weights;
		
		protected bool _interpolate;
		protected bool _normaliseRotationWeight;
		protected bool _normaliseTranslationWeight;
		protected bool _normaliseScaleWeight;
		protected B2Jloop _defaultLoop;
		
		public B2JgenericPlayer() {
			
			B2Jserver = null;
			_b2jMaps = new Dictionary< string, B2Jmap >();
			_b2jPlayheads = new Dictionary< string, B2Jplayhead >();
			_b2jPlayheadList = new List< B2Jplayhead > ();
			
			// making a copy of the current object rotations and orientations
			_world2local = new Dictionary < Transform, Matrix4x4 >();
			_armature = new Dictionary < string, Transform > ();
			
			_initialQuaternions = new Dictionary< Transform, Quaternion > ();
			_initialTranslations = new Dictionary< Transform, Vector3 > ();
			_initialScales = new Dictionary< Transform, Vector3 > ();
			
			_allQuaternions = new Dictionary< Transform, Quaternion > ();
			_allTranslations = new Dictionary< Transform, Vector3 > ();
			_allScales = new Dictionary< Transform, Vector3 > ();
			
			_updatedQuaternions = new Dictionary< Transform, Quaternion >();
			_updatedTranslations = new Dictionary< Transform, Vector3 >();
			_updatedScales = new Dictionary< Transform, Vector3 >();
			
			_weights = new Dictionary< Transform, float > ();
			
			_interpolate = true;
			_normaliseRotationWeight = true;
			_defaultLoop = B2Jloop.B2JLOOP_NORMAL;
			
		}
		
		protected void init() {
			
			Transform[] all_transforms = GetComponentsInChildren<Transform>();
			foreach( Transform t in all_transforms ) {
				
				_armature.Add( t.name, t );
				_world2local.Add( t, t.worldToLocalMatrix );
				
				_initialQuaternions.Add( t, new Quaternion( t.localRotation.x, t.localRotation.y, t.localRotation.z, t.localRotation.w ) );
				_initialTranslations.Add( t, new Vector3( t.localPosition.x, t.localPosition.y, t.localPosition.z ) );
				_initialScales.Add( t, new Vector3( t.localScale.x, t.localScale.y, t.localScale.z ) );
				
				_allQuaternions.Add( t, new Quaternion( t.localRotation.x, t.localRotation.y, t.localRotation.z, t.localRotation.w ) );
				_allTranslations.Add( t, new Vector3( t.localPosition.x, t.localPosition.y, t.localPosition.z ) );
				_allScales.Add( t, new Vector3( t.localScale.x, t.localScale.y, t.localScale.z ) );
				
				_weights.Add( t, 1 );
				
			}
			
		}
		
		public void loadMapping( TextAsset asset ) {
			B2Jmap map = new B2Jmap();
			if ( map.load( asset, this ) ) {
				if ( _b2jMaps.ContainsKey( map.model ) ) {
					Debug.Log( "A map with the same model as already been loaded! It will be overwritten by the current one: " + map.name );
				}
				_b2jMaps.Add( map.model, map );
			}
		}
		
		public B2Jplayhead getPlayhead( string name ) {
			foreach( B2Jplayhead ph in _b2jPlayheadList )
				if ( ph.Name == name )
					return ph;
			return null;
		}
		
		protected void sync() {
			Synchronise();
		}
		
		private void Synchronise() {
			if ( B2Jserver != null ) {
				B2Jserver.syncPlayheads( _b2jPlayheadList, _b2jPlayheads, _defaultLoop );
				// all playheads are now ok
				foreach( B2Jplayhead ph in _b2jPlayheadList ) {
					ph.update( _interpolate );
				}
			}
		}
		
		protected void render() {
			
			//			Debug.LogError ( "build smooth method on this basis: " +
			//								"each map define its own smooth, meaning smooth must be rendererd using _allQuaternions during main loop. ");
			
			
			float totalWeight = 0;
			
			// reseting all weights
			foreach ( KeyValuePair< string, Transform > pair in _armature ) {
				_weights[ pair.Value ] = 1.0f;
			}
			
			// retrieval of total weight per bone
			// a transform can be influenced by several playheads
			// and in each MAP, a transform can be influenced by several b2j bones
			if ( _normaliseRotationWeight ) {
				foreach( B2Jplayhead ph in _b2jPlayheadList ) {
					B2Jmap map = _b2jMaps[ ph.Model ];
					// no map found, no need to go further!
					if ( map == null ) {
						continue;
					}
					foreach( Transform t in map.uniqueTransforms ) {
						_weights[ t ] += ph.Weight;
					}
				}
			} else {
				totalWeight = 1;
			}
			
			// storing all updated transforms in a temporary dict
			_updatedQuaternions.Clear();
			_updatedTranslations.Clear();
			_updatedScales.Clear();
			
			//Debug.LogError( "FINISH THIS!" );
			//			// first: collecting new orientations, translations & scales per map
			//			foreach( B2Jmap map in _b2jMaps ) {
			//
			//				float lWeight = 0;
			//				// rendering the weight of the plays heads related to this map
			//				if ( _normaliseRotationWeight ) {
			//					foreach( B2Jplayhead ph in _b2jPlayheadList ) {
			//						B2Jmap bm = _b2jMaps[ ph.Model ];
			//						if ( map == null || bm != map ) {
			//							continue;
			//						} else if ( bm == map ) {
			//							lWeight += ph.Weight;
			//						}
			//					}
			//					if ( lWeight > 1 ) {
			//						lWeight = 1 / lWeight;
			//					} else if ( lWeight < 1 ) {
			//						lWeight = 1;
			//					}
			//				} else {
			//					lWeight = 1;
			//				}
			//				// retrieval of data, second loop on playheads
			//				foreach( B2Jplayhead ph in _b2jPlayheadList ) {
			//					
			//				}
			//				
			//
			//			}
			
			foreach( B2Jplayhead ph in _b2jPlayheadList ) {
				
				if ( ph.Weight == 0 ) {
					continue;
				}
				// searching the map for this model
				B2Jmap map = _b2jMaps[ ph.Model ];
				// no map found, no need to go further!
				if ( map == null ) {
					continue;
				}
				
				float smooth = map.smooth;
				
				// no need to go over all bones, just the ones of the mapping
				foreach ( KeyValuePair< int, B2JtransformList > pair in map.transformListById ) {
					
					int bid = pair.Key;
					B2JtransformList tlist = pair.Value;
					
					for ( int i = 0; i < tlist.transforms.Count; i++ ) {
						
						Transform t = tlist.transforms[ i ];
						float locw = tlist.weights[ i ];
						if ( _normaliseRotationWeight ) {
							totalWeight = _weights[ t ];
							if ( totalWeight == 0 ) {
								continue;
							} else if ( totalWeight < 1 ) {
								totalWeight = 1;
							} else {
								totalWeight = 1 / totalWeight;
							}
						}
						
						float ratio = locw * ph.Weight * totalWeight; // calcul du weight absolu
						
						if ( map.enable_rotations ) {
							if ( !_updatedQuaternions.ContainsKey( t ) ) {
								// _updatedQuaternions.Add( t, Quaternion.identity );
								Quaternion qbase = new Quaternion(
									_initialQuaternions[t].x,
									_initialQuaternions[t].y,
									_initialQuaternions[t].z,
									_initialQuaternions[t].w ); 
								_updatedQuaternions.Add( t, qbase );
							}
							Quaternion newrot = ph.Rotations[ bid ];
							// depending on the record model, quaternion is processed differently
							if ( ph.Model == "bvh_numediart" ) {
								Matrix4x4 mat = new Matrix4x4();
								mat.SetTRS( Vector3.zero, newrot, Vector3.one );
								Matrix4x4 tmat = _world2local[ t ];
								mat = tmat* mat * tmat.inverse;
								newrot = Quaternion.LookRotation( mat.GetColumn(2), mat.GetColumn(1) ) ;
							} else {
								Debug.LogError( "enable_rotations :: UNKNOWN B2J MODEL!!! : " + ph.Model );
							}
							if ( _normaliseRotationWeight ) {
								_updatedQuaternions[ t ] = Quaternion.Slerp(
									_updatedQuaternions[ t ],
									_initialQuaternions[t] * newrot,
									ratio
									);
							} else {
								// accumulation of rotations, until everything explodes...
								Quaternion tmp = Quaternion.Slerp( Quaternion.identity, newrot, ratio );
								_updatedQuaternions[ t ] *= tmp;
							}
						}
						
						if ( map.enable_translations ) {
							
							if ( !_updatedTranslations.ContainsKey( t ) ) {
								_updatedTranslations.Add( t, Vector3.zero );
							}
							
							Vector3 newpos = ph.Positions[ bid ];
							if ( ph.Model == "bvh_numediart" ) {
								newpos *= 0.01f;
							} else {
								Debug.LogError( "enable_translations :: UNKNOWN B2J MODEL!!! : " + ph.Model );
							}
							
							if ( _normaliseTranslationWeight ) {
								newpos *= ratio;
							} else {
								_updatedTranslations[ t ] += newpos;
							}
							
							_updatedTranslations[ t ] += newpos;
							
						}
						
					}
					
				}
				
			}
			
		}
		
	}

}
