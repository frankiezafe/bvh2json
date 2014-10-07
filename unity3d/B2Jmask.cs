using UnityEngine;

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using MiniJSON;


namespace B2J {

	public class B2Jmask {

		private List< Transform > transforms;
		private B2JmaskConfig current;
		private B2JmaskConfig target;
		private float transitionSpeed;

		public B2Jmask( List< Transform > transformList ) {
			transforms = new List< Transform > ( transformList );
			current = new B2JmaskConfig ();
			foreach( Transform t in transforms ) {
				current.weights.Add( t, 1 );
			}
			target = null;
			transitionSpeed = 0.1f;
		}

		public Dictionary< Transform, float > getWeights() {
			return current.weights;
		}

		public void reset() {
			target = new B2JmaskConfig ();
			foreach( KeyValuePair< Transform, float > pair in current.weights ) {
				target.weights.Add( pair.Key, 1 );
			}
		}

		public void goTo( B2JmaskConfig conf ) {
			if ( conf == null ) {
				target = null;
				return;
			}
			if ( target == null ) {
				target = new B2JmaskConfig ();
			}
			B2Jutils.copy( conf, target );
			// validation of the target
			// only known keys are copied
			foreach( KeyValuePair< Transform, float > pair in conf.weights ) {
				if ( !transforms.Contains( pair.Key ) ) {
					target.weights.Remove( pair.Key );
				}
			}
		}

		// Update is called once per frame
		public void update () {

			if ( target == null ) {
				return;
			}

			foreach( Transform t in transforms ) {
				if ( target.weights.ContainsKey( t ) ) {
					current.weights[ t ] += ( target.weights[ t ] - current.weights[ t ] ) * transitionSpeed;
					// if very close to target, removing the key
					if ( Math.Abs( target.weights[ t ] - current.weights[ t ] ) < 0.001f ) {
						target.weights.Remove( t );
					}
				}
				// current.weights.Add( t, 1 );
			}
		}
	}

}