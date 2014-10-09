using UnityEngine;

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using MiniJSON;

namespace B2J {
	
	// this object is used to holds the result
	// of the fusion of all the records of the same MODEL (and thus B2Jmap)
	public class B2Jblender {
		
		public List< B2Jplayhead > playheads;

		private Dictionary< Transform, Quaternion > quaternions;
		private Dictionary< Transform, Vector3 > translations;
		private Dictionary< Transform, Vector3 > scales;

		private Dictionary< Transform, Quaternion > newQuaternions;
		private Dictionary< Transform, Vector3 > newTranslations;
		private Dictionary< Transform, Vector3 > newScales;

		// these objects are created and managed by B2JgenericPlayhead!
		private Dictionary < Transform, Matrix4x4 > world2local;
		private Dictionary< Transform, Quaternion > initialQuaternions;
		private Dictionary< Transform, Vector3 > initialTranslations;
		private Dictionary< Transform, Vector3 > initialScales;

		private B2Jmap map;
		private float weight;

		// used when map is B2JsmoothMethod.B2JSMOOTH_ACCUMULATION_OF_DIFFERENCE
		private float smooth_speed;

		private B2Jmask mask;

		public B2Jblender(
			B2Jmap map,
			Dictionary < Transform, Matrix4x4 > _world2local,
			Dictionary< Transform, Quaternion > _initialQuaternions,
			Dictionary< Transform, Vector3 > _initialTranslations,
			Dictionary< Transform, Vector3 > _initialScales
			) {
			
			playheads = new List< B2Jplayhead > ();

			quaternions = new Dictionary<Transform, Quaternion> ();
			translations = new Dictionary< Transform, Vector3 > ();
			scales = new Dictionary< Transform, Vector3 > ();

			newQuaternions = new Dictionary<Transform, Quaternion> ();
			newTranslations = new Dictionary< Transform, Vector3 > ();
			newScales = new Dictionary< Transform, Vector3 > ();

			this.world2local = _world2local;
			this.initialQuaternions = _initialQuaternions;
			this.initialTranslations = _initialTranslations;
			this.initialScales = _initialScales;

			setMap( map );

			weight = 1;
			smooth_speed = 1.0f;

			mask = new B2Jmask( map.uniqueTransforms );

			// blender is loaded ready to go!!

		}
		
		public B2Jmask getMask() {
			return mask;
		}

		public B2Jmap getMap() {
			return map;
		}

		public Dictionary< Transform, Quaternion > getQuaternions() {
			return quaternions;
		}

		public Dictionary< Transform, Vector3 > getTranslations() {
			return translations;
		}

		public Dictionary< Transform, Vector3 > getScales() {
			return scales;
		}
		
		public string getName() {
			return map.name;
		}

		public float getWeight() {
			return weight;
		}

		public void setWeight( float w ) {
			weight = w;
		}

		public float getSmoothSpeed() {
			return smooth_speed;
		}

		public void setSmoothSpeed( float s ) {
			smooth_speed = s;
		}

		public void setSmoothMethod( B2JsmoothMethod m ) {
			map.smooth_mehod = m;
		}

		private void setMap( B2Jmap map ) {

			this.map = map;
			foreach ( Transform t in map.uniqueTransforms ) {

				if ( map.enable_rotations ) {
					quaternions.Add( t, Quaternion.identity );
				}

				if ( map.enable_translations ) {
					translations.Add( t, Vector3.zero );
				}

				if ( map.enable_scales ) {
					scales.Add( t, Vector3.one );
				}

			}

		}

		private void reset() {

			if ( map == null )
				return;

			foreach ( Transform t in map.uniqueTransforms ) {
				if ( map.enable_rotations ) {
					quaternions[ t ] = Quaternion.identity;
				}
				if ( map.enable_translations ) {
					translations[ t ] = Vector3.zero;
				}
				if ( map.enable_scales ) {
					scales[ t ] = Vector3.one;
				}
			}

			newQuaternions.Clear ();
			newTranslations.Clear ();
			newScales.Clear ();

		}

		public void update( bool rotNormalise, bool transNormalise, bool scaleNormalise ) {

			if ( 
			    map == null || 
			    initialQuaternions == null || 
			    initialTranslations == null || 
			    initialScales == null
			    ) {
				Debug.LogError( "This blender is not correctly set!" );
			}

			if ( mask != null ) {
				mask.update();
			}

			reset ();

			// collecting the total playheads weights

			float totalWeight = 0;

			if ( rotNormalise || transNormalise || scaleNormalise ) {
				foreach (B2Jplayhead ph in playheads)
					totalWeight += ph.getWeight();
				if ( totalWeight < 1 ) {
					totalWeight = 1;
				} else {
					totalWeight = 1 / totalWeight;
				}
			} else {
				totalWeight = 1;
			}

			foreach( B2Jplayhead ph in playheads ) {

				// no need to go over all bones, just the ones of the mapping
				foreach ( KeyValuePair< int, B2JtransformList > pair in map.transformListById ) {
					
					int bid = pair.Key;
					B2JtransformList tlist = pair.Value;
					
					for ( int i = 0; i < tlist.transforms.Count; i++ ) {
						
						Transform t = tlist.transforms[ i ];
						float locw = tlist.weights[ i ];
						
						float ratio = locw * ph.getWeight() * totalWeight; // calcul du weight absolu
						
						if ( map.enable_rotations ) {
							if ( !newQuaternions.ContainsKey( t ) ) {
								// _updatedQuaternions.Add( t, Quaternion.identity );
								Quaternion qbase = new Quaternion(
									initialQuaternions[t].x,
									initialQuaternions[t].y,
									initialQuaternions[t].z,
									initialQuaternions[t].w ); 
								newQuaternions.Add( t, qbase );
							}
							Quaternion newrot = ph.getRotations()[ bid ];
							// depending on the record model, quaternion is processed differently
							if ( ph.getModel() == "bvh_numediart" ) {
								Matrix4x4 mat = new Matrix4x4();
								mat.SetTRS( Vector3.zero, newrot, Vector3.one );
								Matrix4x4 tmat = world2local[ t ];
								mat = tmat* mat * tmat.inverse;
								newrot = Quaternion.LookRotation( mat.GetColumn(2), mat.GetColumn(1) ) ;
							} else {
								Debug.LogError( "enable_rotations :: UNKNOWN B2J MODEL!!! : " + ph.getModel() );
							}
							if ( rotNormalise ) {
								newQuaternions[ t ] = Quaternion.Slerp(
									newQuaternions[ t ],
									initialQuaternions[t] * newrot,
									ratio
									);
							} else {
								// accumulation of rotations, until everything explodes...
								Quaternion tmp = Quaternion.Slerp( Quaternion.identity, newrot, ratio );
								newQuaternions[ t ] *= tmp;
							}
						}
						
						if ( map.enable_translations ) {
							
							if ( !newTranslations.ContainsKey( t ) ) {
								newTranslations.Add( t, Vector3.zero );
							}
							
							Vector3 newpos = ph.getPositions()[ bid ];
							if ( ph.getModel() == "bvh_numediart" ) {
								newpos *= 0.01f;
							} else {
								Debug.LogError( "enable_translations :: UNKNOWN B2J MODEL!!! : " + ph.getModel() );
							}
							
							if ( transNormalise ) {
								newpos *= ratio;
							}
							newTranslations[ t ] += newpos;
							
						}

						if ( map.enable_scales ) {
							
							if ( !newScales.ContainsKey( t ) ) {
								newScales.Add( t, Vector3.one );
							}
							
							Vector3 newscale = ph.getScales()[ bid ];

							if ( scaleNormalise ) {
								newscale *= ratio;
							}
							newTranslations[ t ] += newscale;
							
						}
					
					}

				}
				
			}

			// new values have been sorted in new* distionnaries
			// applying smooth depending on map settings

			if ( map.smooth_mehod == B2JsmoothMethod.B2JSMOOTH_ACCUMULATION_OF_DIFFERENCE && smooth_speed < 1 ) {
			
				foreach ( KeyValuePair< Transform, Quaternion > pair in newQuaternions ) {
					Quaternion q = quaternions[ pair.Key ];
					quaternions[ pair.Key ] = Quaternion.Slerp( q, pair.Value, smooth_speed );
				}
				foreach ( KeyValuePair< Transform, Vector3 > pair in newTranslations ) {
					Vector3 v = translations[ pair.Key ];
					translations[ pair.Key ] = B2Jutils.vectorSlerp( v, pair.Value, smooth_speed );
				}
				foreach ( KeyValuePair< Transform, Vector3 > pair in newScales ) {
					Vector3 v = scales[ pair.Key ];
					scales[ pair.Key ] = B2Jutils.vectorSlerp( v, pair.Value, smooth_speed );
				}
			
			// by default: simple copy of new values in dictionary
			} else {

				foreach ( KeyValuePair< Transform, Quaternion > pair in newQuaternions )
					quaternions[ pair.Key ] = pair.Value;
				foreach ( KeyValuePair< Transform, Vector3 > pair in newTranslations )
					translations[ pair.Key ] = pair.Value;
				foreach ( KeyValuePair< Transform, Vector3 > pair in newScales )
					scales[ pair.Key ] = pair.Value;
				
			}

		
		}
		
		


		
	}

}
