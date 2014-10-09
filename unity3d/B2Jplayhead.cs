using UnityEngine;

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using MiniJSON;

namespace B2J {

	public class B2Jplayhead {
		
		private B2Jloop loop;
		private bool active = true;
		private float time;
		private float speed; // multiplier of time in millis
		private B2Jrecord record;
		private float weight;
		private float mult;
		private List< Vector3 > positions; // same length as record.bones
		private List< Quaternion > rotations; // same length as record.bones
		private List< Vector3 > eulers; // same length as record.bones
		private List< Vector3 > scales; // same length as record.bones
		
		private float cueIn;
		private float cueOut;
		private float percent;

		private B2Jmask mask;

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
		Vector3 e1;
		Vector3 e2;
		
		public B2Jplayhead( B2Jrecord rec, B2Jloop loop ) {
			
			this.loop = loop;
			record = rec;
			positions = new List<Vector3> ();
			rotations = new List<Quaternion> ();
			eulers = new List<Vector3> ();
			scales = new List<Vector3> ();
			foreach (B2Jbone b in rec.bones) {
				positions.Add( Vector3.zero );
				rotations.Add(  Quaternion.identity );
				eulers.Add( Vector3.zero );
				scales.Add( Vector3.one );
			}
			cueIn = record.keys[ 0 ].timestamp;
			cueOut = record.keys[ record.keys.Count - 1 ].timestamp;
			time = cueIn;
			percent = 0;
			speed = 1;
			weight = 0;
			mult = 1;

			mask = null;
			
		}

		public B2Jmask getMask() {
			return mask;
		}

		public B2Jloop getLoop() {
			return loop;
		}

		public void setLoop( B2Jloop l ) {
			loop = l;
		}

		public string getName()  {
			return record.name;
		}

		public string getModel()  {
			return record.model;
		}

		public float getSpeed()  {
			return speed;
		}

		public void setSpeed( float s )  {
			speed = s;
		}
		
		public string getInfo() {
			return "" + time + " [ " + cueIn + ", " + cueOut + "], " + record.name + " / " + active;
		}

		public B2Jrecord getRecord() {
			return record;
		}

		public bool isActive() {
			if ( !active )
				return false;
			return true;
		}
		
		public void setWeight( float w ) {
			if ( w < 0 || w > 1 ) {
				Debug.LogError( "weight must be in [ 0,1 ]!" );
				return;
			}
			weight = w;
		}

		public float getWeight() {
			return weight;
		}

		public float getMultiplier() {
			return mult;
		}

		public void setMultiplier( float m ) {
			mult = m;
		}
		
		public float getPercent() {
			return percent;
		}

		public void setPercent( float p ) {
			percent = p;
			time = cueIn + ( cueOut - cueIn ) * percent;
		}
		
		public float getCurrentTime() {
			return time;
		}
		
		public float getCueIn() {
			return cueIn;
		}

		public void setCueIn( float ci ) {
			cueIn = ci;
		}
		
		public float getCueOut() {
			return cueOut;
		}

		public void setCueOut( float co ) {
			cueOut = co;
		}
		
		public List< Quaternion > getRotations() {
			return rotations;
		}
		
		public List< Vector3 > getPositions() {
			return positions;
		}
		
		public List< Vector3 > getScales() {
			return scales;
		}
		
		public List< Vector3 > getEulers() {
			return eulers;
		}
		
		public string getRotationOrder( int bID ) {
			return record.bones [bID].rotation_order;
		}
		
		public void update( bool interpolation ) {

			// the playhead is NOT moving in a live stream!
			if ( loop == B2Jloop.B2JLOOP_STREAM ) {
				active = true;
				renderFrame( interpolation );
				return;
			}

			if ( !active ) {
				// not ready to use, create mapping or reactivate...
				return;
			}
			
			if ( time > cueOut ) {
				
				time = cueOut;
				
				if ( loop == B2Jloop.B2JLOOP_NONE ) {
					active = false;
					return;
				} else if ( loop == B2Jloop.B2JLOOP_NORMAL ) {
					// go back to beginning
					time = cueIn;
				} else if ( loop == B2Jloop.B2JLOOP_PALINDROME ) {
					// go back to beginning
					speed *= -1;
				}
				
			} else if ( time < cueIn ) {
				time = cueIn;
				if ( loop == B2Jloop.B2JLOOP_PALINDROME ) {
					speed *= -1;
				}
			}
			
			percent = (time - cueIn) / (cueOut - cueIn);
			time += Time.deltaTime * 1000 * speed;
			
			if ( weight == 0 ) {
				return;
			}
			
			renderFrame( interpolation );
			
		}
		
		private void renderFrame( bool interpolation ) {
			
			// seeking frames
			B2Jkey below = null;
			B2Jkey above = null;
			
			if ( speed > 0 ) {
				foreach( B2Jkey k in record.keys ) {
					below = above;
					above = k;
					if ( above.timestamp >= time )
						break;
				}
			} else {
				foreach( B2Jkey k in record.keys ) {
					above = below;
					if ( above == null )
						above = k;
					below = k;
					if ( below.timestamp >= time )
						break;
				}
			}
			
			if ( above == null ) {
				Debug.LogError ( "Impossible to find frames at this timecode!!!: " + time );
			}
			
			if ( above.timestamp == time || below == null || !interpolation ) {
				// cool, it's easy ( but rare )
				for( int i = 0; i < record.bones.Count; i++ ) {
					if ( record.bones[ i ].translations_enabled ) {
						p1 = above.positions[ i ];
						positions[ i ] = new Vector3( p1.x, p1.y, p1.z );
					}
					if ( record.bones[ i ].rotations_enabled ) {
						q1 = above.rotations[ i ];
						rotations[ i ] = new Quaternion( q1.x, q1.y, q1.z, q1.w );
						e1 = above.eulers[ i ];
						eulers[ i ] = new Vector3( e1.x, e1.y, e1.z );
					}
					if ( record.bones[ i ].scales_enabled ) {
						s1 = above.scales[ i ];
						scales[ i ] = new Vector3( s1.x, s1.y, s1.z );
					}
				}
				
			} else {
				
				// ... less funny, have to smooth values...
				float gap = above.timestamp - below.timestamp;
				float abovepc = ( ( time - below.timestamp ) / gap );
				float belowpc = 1 - abovepc;
				
				for( int i = 0; i < record.bones.Count; i++ ) {
					
					if ( record.bones[ i ].translations_enabled ) {
						p1 = below.positions[ i ];
						p2 = above.positions[ i ];
						positions[ i ] = new Vector3( 
						                              p1.x * belowpc + p2.x * abovepc,
						                              p1.y * belowpc + p2.y * abovepc,
						                              p1.z * belowpc + p2.z * abovepc
						                              );
					}
					
					if ( record.bones[ i ].rotations_enabled ) {
						q1 = below.rotations[ i ];
						q2 = above.rotations[ i ];
						rotations[ i ] = Quaternion.Slerp( q1, q2, abovepc );
						e1 = below.eulers[ i ];
						e2 = above.eulers[ i ];
						eulers[ i ] = new Vector3( 
						                           e1.x * belowpc + e2.x * abovepc,
						                           e1.y * belowpc + e2.y * abovepc,
						                           e1.z * belowpc + e2.z * abovepc
						                           );
					}
					
					if ( record.bones[ i ].scales_enabled ) {
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

}