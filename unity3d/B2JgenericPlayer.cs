using UnityEngine;

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using MiniJSON;

namespace B2J {

	public class B2JgenericPlayer : MonoBehaviour {
		
		public B2Jserver B2Jserver;
		protected Dictionary< string, B2Jmap > maps;
		protected Dictionary< string, B2Jblender > blenderByModel; // preprocessing of the mocap blend happens here
		protected List< B2Jblender > blenderList;
		protected Dictionary< string, B2Jplayhead > playheadDict;
		protected List< B2Jplayhead > playheadList;
		
		protected Dictionary < Transform, Matrix4x4 > world2local;
		protected Dictionary < string, Transform > armature;
		protected Dictionary< Transform, Quaternion > initialQuaternions;
		protected Dictionary< Transform, Vector3 > initialTranslations;
		protected Dictionary< Transform, Vector3 > initialScales;
		
		protected Dictionary< Transform, Quaternion > updatedQuaternions;
		protected Dictionary< Transform, Vector3 > updatedTranslations;
		protected Dictionary< Transform, Vector3 > updatedScales;
		
		protected Dictionary< Transform, Quaternion > allQuaternions;
		protected Dictionary< Transform, Vector3 > allTranslations;
		protected Dictionary< Transform, Vector3 > allScales;
		
		protected Dictionary< Transform, float > weights;
		
		protected bool interpolate;
		protected bool rotationNormalise;
		protected bool translationNormalise;
		protected bool scaleNormalise;
		protected B2Jloop defaultLoop;

		protected bool verbose;
		private bool forceSync;

		public B2JgenericPlayer() {
			
			B2Jserver = null;
			maps = new Dictionary< string, B2Jmap >();
			blenderByModel = new Dictionary< string, B2Jblender >();
			blenderList = new List< B2Jblender >();
			playheadDict = new Dictionary< string, B2Jplayhead >();
			playheadList = new List< B2Jplayhead > ();
			
			// making a copy of the current object rotations and orientations
			world2local = new Dictionary < Transform, Matrix4x4 >();
			armature = new Dictionary < string, Transform > ();
			
			initialQuaternions = new Dictionary< Transform, Quaternion > ();
			initialTranslations = new Dictionary< Transform, Vector3 > ();
			initialScales = new Dictionary< Transform, Vector3 > ();
			
			allQuaternions = new Dictionary< Transform, Quaternion > ();
			allTranslations = new Dictionary< Transform, Vector3 > ();
			allScales = new Dictionary< Transform, Vector3 > ();
			
			updatedQuaternions = new Dictionary< Transform, Quaternion >();
			updatedTranslations = new Dictionary< Transform, Vector3 >();
			updatedScales = new Dictionary< Transform, Vector3 >();
			
			weights = new Dictionary< Transform, float > ();
			
			interpolate = true;
			rotationNormalise = true;
			translationNormalise = true;
			scaleNormalise = true;

			defaultLoop = B2Jloop.B2JLOOP_NORMAL;

			verbose = true;
			forceSync = true;

		}

		public void Quiet() {
			verbose = false;
		}
		
		public void Verbose() {
			verbose = true;
		}

		protected void init() {
			
			Transform[] all_transforms = GetComponentsInChildren<Transform>();
			foreach( Transform t in all_transforms ) {
				
				armature.Add( t.name, t );
				world2local.Add( t, t.worldToLocalMatrix );
				
				initialQuaternions.Add( t, new Quaternion( t.localRotation.x, t.localRotation.y, t.localRotation.z, t.localRotation.w ) );
				initialTranslations.Add( t, new Vector3( t.localPosition.x, t.localPosition.y, t.localPosition.z ) );
				initialScales.Add( t, new Vector3( t.localScale.x, t.localScale.y, t.localScale.z ) );
				
				allQuaternions.Add( t, new Quaternion( t.localRotation.x, t.localRotation.y, t.localRotation.z, t.localRotation.w ) );
				allTranslations.Add( t, new Vector3( t.localPosition.x, t.localPosition.y, t.localPosition.z ) );
				allScales.Add( t, new Vector3( t.localScale.x, t.localScale.y, t.localScale.z ) );
				
				weights.Add( t, 1 );
				
			}
			
		}
		
		public void LoadMapping( TextAsset asset ) {
			B2Jmap map = new B2Jmap();
			if ( map.load( asset, this ) ) {
				if ( maps.ContainsKey( map.model ) && verbose ) {
					Debug.LogError( "A map with the same model as already been loaded! It will be overwritten by the current one: " + map.name );
				}
				maps.Add( map.model, map );
				// creating related blender
				B2Jblender mapblend = new B2Jblender( 
                     map,
				     world2local,
                     initialQuaternions,
                     initialTranslations,
                     initialScales
                     );

				blenderList.Add( mapblend  );
				blenderByModel.Add( map.model, mapblend );
				forceSync = true;
			}
		}
		
		public B2Jplayhead GetPlayhead( string name ) {
			foreach( B2Jplayhead ph in playheadList )
				if ( ph.Name == name )
					return ph;
			return null;
		}
		
		protected void Process() {

			synchronise();
			
			// all playheads are now ok
			foreach( B2Jplayhead ph in playheadList ) {
				ph.update( interpolate );
			}

		}
		
		private void synchronise() {

			if ( B2Jserver != null ) {

				bool smthchanged = B2Jserver.SyncPlayheads( playheadList, playheadDict, defaultLoop );

				if ( smthchanged || forceSync ) {

					// first, checking if there some playheads have been destroyed
					foreach( B2Jblender mb in blenderList ) {

						foreach( B2Jplayhead mb_ph in mb.playheads ) {

							bool found = false;

							foreach ( B2Jplayhead ph in playheadList ) {
								if ( ph == mb_ph ) {
									found = true;
									break;
								}
							}

							if ( !found ) {

								mb.playheads.Remove( mb_ph );

							}

						}

					}

					// then checking new ones
					foreach ( B2Jplayhead ph in playheadList ) {
					
						if ( blenderByModel.ContainsKey( ph.Model ) ) {

							B2Jblender mb = blenderByModel[ ph.Model ];

							bool found = false;

							foreach( B2Jplayhead mb_ph in mb.playheads ) {
							
								if ( ph == mb_ph ) {
									found = true;
									break;
								}
							
							}

							if ( !found ) {

								mb.playheads.Add( ph );
								if ( verbose )
									Debug.Log ( "new map blend added " + ph.Model + " >> " + ph.Name );

							}

						} else {

							Debug.LogError( "the player have no map for this model! '" + ph.Model + "'" );

						}
					
					}
					
					if ( verbose )
						Debug.Log ( "One or several playheads have been added or removed from the list!" );
					// map blend have to be checked!

					forceSync = false;

				}

			}

		}
		
		protected void Render() {
			
			// updating all the blenders
			float blenderWeight = 0;
			foreach (B2Jblender bb in blenderList) {
				bb.update( rotationNormalise, translationNormalise, scaleNormalise );
				blenderWeight += bb.getWeight();
			}

			// storing all updated transforms in a temporary dict
			if ( !rotationNormalise )
				updatedQuaternions.Clear();

			if ( !translationNormalise )
				updatedTranslations.Clear();

			if ( !scaleNormalise )
				updatedScales.Clear();

			// for the moment, just one blender is considered
			foreach (B2Jblender bb in blenderList) {
			
				float bw = 1;
				if ( rotationNormalise || translationNormalise || scaleNormalise ) {
					if ( blenderWeight > 1 ) {
						bw = bb.getWeight() / blenderWeight;
					} else {
						bw = bb.getWeight();
					}
				}

				Dictionary< Transform, Quaternion > qts = bb.getQuaternions();
				foreach( KeyValuePair< Transform, Quaternion > pair in qts ) {
					if ( !updatedQuaternions.ContainsKey( pair.Key ) ) {
						updatedQuaternions.Add( pair.Key, Quaternion.identity );
					}
					if ( rotationNormalise ) {
						updatedQuaternions[ pair.Key ] = Quaternion.Slerp( updatedQuaternions[ pair.Key ], pair.Value, bw );
					} else {
						updatedQuaternions[ pair.Key ] *= pair.Value;
					}
				}

				Dictionary< Transform, Vector3 > tls = bb.getTranslations();
				foreach( KeyValuePair< Transform, Vector3 > pair in tls ) {
					if ( !updatedTranslations.ContainsKey( pair.Key ) ) {
						updatedTranslations.Add( pair.Key, Vector3.zero );
					}
					if ( translationNormalise ) {
						updatedTranslations[ pair.Key ] = B2Jutils.VectorSlerp( updatedTranslations[ pair.Key ], pair.Value, bw );
					} else {
						updatedTranslations[ pair.Key ] += pair.Value;
					}
				}
				
				Dictionary< Transform, Vector3 > scs = bb.getScales();
				foreach( KeyValuePair< Transform, Vector3 > pair in scs ) {
					if ( !updatedScales.ContainsKey( pair.Key ) ) {
						updatedScales.Add( pair.Key, pair.Value );
					}
					if ( scaleNormalise ) {
						updatedScales[ pair.Key ] = B2Jutils.VectorSlerp( updatedScales[ pair.Key ], pair.Value, bw );
					} else {
						updatedScales[ pair.Key ] += pair.Value;
					}
				}

			}

		}
		
	}

}
