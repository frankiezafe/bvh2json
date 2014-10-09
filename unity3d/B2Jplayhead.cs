using UnityEngine;

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using MiniJSON;

namespace B2J {

	public class B2Jplayhead {
		
		private B2Jloop _loop;
		private bool _active = true;
		private float _time;
		private float _speed; // multiplier of time in millis
		private B2Jrecord _record;
		private float _weight;
		private float _mult;
		private List< Vector3 > _positions; // same length as record.bones
		private List< Quaternion > _rotations; // same length as record.bones
		private List< Vector3 > _eulers; // same length as record.bones
		private List< Vector3 > _scales; // same length as record.bones
		
		private float _cueIn;
		private float _cueOut;
		private float _percent;

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
			
			_loop = loop;
			_record = rec;
			_positions = new List<Vector3> ();
			_rotations = new List<Quaternion> ();
			_eulers = new List<Vector3> ();
			_scales = new List<Vector3> ();
			foreach (B2Jbone b in rec.bones) {
				_positions.Add( Vector3.zero );
				_rotations.Add(  Quaternion.identity );
				_eulers.Add( Vector3.zero );
				_scales.Add( Vector3.one );
			}
			_cueIn = _record.keys[ 0 ].timestamp;
			_cueOut = _record.keys[ _record.keys.Count - 1 ].timestamp;
			_time = _cueIn;
			_percent = 0;
			_speed = 1;
			_weight = 1;
			_mult = 1;

			mask = null;
			
		}

		public B2Jmask getMask() {
			return mask;
		}

		public B2Jloop getLoop() {
			return _loop;
		}

		public void setLoop( B2Jloop l ) {
			_loop = l;
		}

		public string getName()  {
			return _record.name;
		}

		public string getModel()  {
			return _record.model;
		}

		public float getSpeed()  {
			return _speed;
		}

		public void setSpeed( float s )  {
			_speed = s;
		}
		
		public string getInfo() {
			return "" + _time + " [ " + _cueIn + ", " + _cueOut + "], " + _record.name + " / " + _active;
		}

		public B2Jrecord getRecord() {
			return _record;
		}

		public bool isActive() {
			if ( !_active )
				return false;
			return true;
		}
		
		public void setWeight( float w ) {

			if ( w < 0 || w > 1 ) {
				Debug.LogError( "weight must be in [ 0,1 ]!" );
				return;
			}
			_weight = w;
		}

		public float getWeight() {
			return _weight;
		}

		public float getMultiplier() {
			return _mult;
		}

		public void setMultiplier( float m ) {
			_mult = m;
		}
		
		public float getPercent() {
			return _percent;
		}

		public void setPercent( float p ) {
			_percent = p;
			_time = _cueIn + ( _cueOut - _cueIn ) * _percent;
		}
		
		public float getCurrentTime() {
			return _time;
		}
		
		public float getCueIn() {
			return _cueIn;
		}

		public void setCueIn( float ci ) {
			_cueIn = ci;
		}
		
		public float getCueOut() {
			return _cueOut;
		}

		public void setCueOut( float co ) {
			_cueOut = co;
		}
		
		public List< Quaternion > getRotations() {
			return _rotations;
		}
		
		public List< Vector3 > getPositions() {
			return _positions;
		}
		
		public List< Vector3 > getScales() {
			return _scales;
		}
		
		public List< Vector3 > getEulers() {
			return _eulers;
		}
		
		public string getRotationOrder( int bID ) {
			return _record.bones [bID].rotation_order;
		}
		
		public void update( bool interpolation ) {
			
			if ( !_active ) {
				// not ready to use, create mapping or reactivate...
				return;
			}
			
			if ( _time > _cueOut ) {
				
				_time = _cueOut;
				
				if ( _loop == B2Jloop.B2JLOOP_NONE ) {
					_active = false;
					return;
				} else if ( _loop == B2Jloop.B2JLOOP_NORMAL ) {
					// go back to beginning
					_time = _cueIn;
				} else if ( _loop == B2Jloop.B2JLOOP_PALINDROME ) {
					// go back to beginning
					_speed *= -1;
				}
				
			} else if ( _time < _cueIn ) {
				_time = _cueIn;
				if ( _loop == B2Jloop.B2JLOOP_PALINDROME ) {
					_speed *= -1;
				}
			}
			
			_percent = (_time - _cueIn) / (_cueOut - _cueIn);
			_time += Time.deltaTime * 1000 * _speed;
			
			if ( _weight == 0 ) {
				return;
			}
			
			renderFrame( interpolation );
			
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
					if ( above == null )
						above = k;
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
						e1 = above.eulers[ i ];
						_eulers[ i ] = new Vector3( e1.x, e1.y, e1.z );
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
						e1 = below.eulers[ i ];
						e2 = above.eulers[ i ];
						_eulers[ i ] = new Vector3( 
						                           e1.x * belowpc + e2.x * abovepc,
						                           e1.y * belowpc + e2.y * abovepc,
						                           e1.z * belowpc + e2.z * abovepc
						                           );
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

}