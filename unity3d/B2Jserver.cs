using UnityEngine;

using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

using B2J;

namespace B2J {

	public class B2Jserver: MonoBehaviour {
		
		private Dictionary< string, B2Jrecord > loaded;
		private List<string> loading;
		private List<B2Jrecord> records;

		private bool newRecord;

		private bool verbose;

		public B2Jserver() {
			loaded = new Dictionary< string, B2Jrecord > ();
			loading = new List<string> ();
			records = new List<B2Jrecord> ();
			newRecord = false;
			verbose = false;
		}

		public void setQuiet() {
			verbose = false;
		}

		public void setVerbose() {
			verbose = true;
		}

		public void Start() {}

		public void Update() {}
		
		public void OnApplicationQuit() {}

		public void OnDestroy() {}

		public B2Jrecord createOpenniRecord( string name ) {
				
			if ( loaded.ContainsKey( name ) ) {
				Debug.LogError ( " a live record '" + name + "' already exists!" );
				return null;
			}

			B2Jrecord liverec = new B2Jrecord ();
			liverec.type = "live";
			liverec.version = 0.0f;
			liverec.desc = "auto-generated record, prepared for collecting live stream from OpenNI";
			liverec.name = name;
			liverec.model = "openni";
			liverec.origin = "auto-generated";
			liverec.groups = new List<B2Jgroup>();

			// building bones hierarchy
			B2Jbone Head = new B2Jbone ();
			B2Jbone Neck = new B2Jbone ();
			B2Jbone LeftShoulder = new B2Jbone ();
			B2Jbone LeftElbow = new B2Jbone ();
			B2Jbone LeftHand = new B2Jbone ();
			B2Jbone RightShoulder = new B2Jbone ();
			B2Jbone RightElbow = new B2Jbone ();
			B2Jbone RightHand = new B2Jbone ();
			B2Jbone Torso = new B2Jbone ();
			B2Jbone LeftHip = new B2Jbone ();
			B2Jbone LeftKnee = new B2Jbone ();
			B2Jbone LeftFoot = new B2Jbone ();
			B2Jbone RightHip = new B2Jbone ();
			B2Jbone RightKnee = new B2Jbone ();
			B2Jbone RightFoot = new B2Jbone ();
			B2Jbone Hip = new B2Jbone (); // GENERATED BONE!

			Head.init();
			Head.name = "Head";
			Head.parent = Neck;

			Neck.init();
			Neck.name = "Neck";
			Neck.children.Add( Head );
			Neck.parent = Torso;

			LeftShoulder.init();
			LeftShoulder.name = "LeftShoulder";
			LeftShoulder.children.Add( LeftElbow );
			LeftShoulder.parent = Torso;

			LeftElbow.init();
			LeftElbow.name = "LeftElbow";
			LeftElbow.children.Add( LeftHand );
			LeftElbow.parent = LeftShoulder;
			
			LeftHand.init();
			LeftHand.name = "LeftHand";
			LeftHand.parent = LeftElbow;

			RightShoulder.init();
			RightShoulder.name = "RightShoulder";
			RightShoulder.children.Add( RightElbow );
			RightShoulder.parent = Torso;
			
			RightElbow.init();
			RightElbow.name = "RightElbow";
			RightElbow.children.Add( RightHand );
			RightElbow.parent = RightShoulder;
			
			RightHand.init();
			RightHand.name = "RightHand";
			RightHand.parent = RightElbow;
			
			Torso.init();
			Torso.name = "Torso";
			Torso.children.Add( Neck );
			Torso.children.Add( LeftShoulder );
			Torso.children.Add( RightShoulder );
			Torso.parent = Hip;

			LeftHip.init();
			LeftHip.name = "LeftHip";
			LeftHip.children.Add( LeftKnee );
			LeftHip.parent = Hip;

			LeftKnee.init();
			LeftKnee.name = "LeftKnee";
			LeftKnee.children.Add( LeftFoot );
			LeftKnee.parent = LeftHip;

			LeftFoot.init();
			LeftFoot.name = "LeftFoot";
			LeftFoot.parent = LeftKnee;
			
			RightHip.init();
			RightHip.name = "RightHip";
			RightHip.children.Add( RightKnee );
			RightHip.parent = Hip;
			
			RightKnee.init();
			RightKnee.name = "RightKnee";
			RightKnee.children.Add( RightFoot );
			RightKnee.parent = RightHip;
			
			RightFoot.init();
			RightFoot.name = "RightFoot";
			RightFoot.parent = RightKnee;

			Hip.init();
			Hip.name = "Hip";
			Hip.children.Add( Torso );
			Hip.children.Add( LeftHip );
			Hip.children.Add( RightHip );

			
			liverec.bones = new List<B2Jbone>();
			liverec.bones.Add( Head );
			liverec.bones.Add( Neck );
			liverec.bones.Add( LeftShoulder );
			liverec.bones.Add( LeftElbow );
			liverec.bones.Add( LeftHand );
			liverec.bones.Add( RightShoulder );
			liverec.bones.Add( RightElbow );
			liverec.bones.Add( RightHand );
			liverec.bones.Add( Torso );
			liverec.bones.Add( LeftHip );
			liverec.bones.Add( LeftKnee );
			liverec.bones.Add( LeftFoot );
			liverec.bones.Add( RightHip );
			liverec.bones.Add( RightKnee );
			liverec.bones.Add( RightFoot );
			liverec.bones.Add( Hip );

			// generating the only key
			B2Jkey key = new B2Jkey ();
			key.kID = 0;
			key.timestamp = 0;

			foreach( B2Jbone b in liverec.bones ) {
				key.positions.Add( Vector3.zero );
				key.rotations.Add( Quaternion.identity );
				key.eulers.Add( Vector3.zero );
				key.scales.Add( Vector3.one );
			}
			
			liverec.keys = new List<B2Jkey>();
			liverec.keys.Add( key );
			liverec.keyCount = liverec.keys.Count;

			loaded.Add ( name, liverec );
			records.Add (liverec);
			return liverec;

		}

		private void load( string path ) {
			if ( loaded.ContainsKey ( path ) ) {
				if ( verbose )
					Debug.Log ( "'" + path + "' already loaded" );
				return;
			}
			addNewRecord( B2Jparser.Instance.load ( path ), path );
		}
		
		private void addNewRecord( B2Jrecord rec, string path ) {
			if ( rec != null ) {
				loaded.Add( path, rec );
				records.Add( rec );
				newRecord = true;
				if ( verbose )
					Debug.Log ( "new record added: " + rec.name + ", " + records.Count + " record(s) loaded" );
			}
		}

		public bool syncPlayheads( List< B2Jrequest > syncRequests, List< B2Jplayhead > phs, Dictionary< string, B2Jplayhead > dict, B2Jloop loop ) {

			bool modified = false;

			if ( !newRecord && syncRequests.Count == 0 && phs.Count == records.Count )
				return modified;

			// is there playheads not registered anymore?
			List< B2Jplayhead > tmpphs = new List< B2Jplayhead > ( phs );
			foreach ( B2Jplayhead ph in tmpphs ) {
				if ( ! records.Contains( ph.getRecord() ) ) {
					phs.Remove( ph );
					dict.Remove( ph.getName() );
					modified = true;
					if ( verbose )
						Debug.Log ( "Removing playhead for record: " + ph.getName() );
				}
			}

			// is there sync requests?
			if ( syncRequests.Count > 0 ) {
				List< B2Jrequest > tmpreqs = new List< B2Jrequest > (syncRequests);

				foreach( B2Jrequest req in tmpreqs ) {

					if ( req.type == B2JrequestType.B2JREQ_TEXTASSET ) {

						load( req.name );
						if ( loaded.ContainsKey ( req.name ) ) {
							B2Jplayhead ph = createNewPlayhead( loaded[ req.name ], phs, loop );
							dict.Add( ph.getName(), ph );
							modified = true;
						} else {
							Debug.LogError( "Impossible to load the record '" + req.name +"'" );
						}
						syncRequests.Remove( req );

					} else if ( req.type == B2JrequestType.B2JREQ_STREAM ) {

						if ( loaded.ContainsKey ( req.name ) ) {
							B2Jplayhead ph = createNewPlayhead( loaded[ req.name ], phs, B2Jloop.B2JLOOP_STREAM );
							dict.Add( ph.getName(), ph );
							modified = true;
						} else {
							Debug.LogError( "Impossible to load the record '" + req.name +"'" );
						}

//						Debug.Log( "Implement connection to kinect records, special kind... '" + req.name + "'" );
						syncRequests.Remove( req );

					} else {

						Debug.LogError( "Unknown request type! '" + req.name +"'" );
						syncRequests.Remove( req );

					}
				}
			}
			
			newRecord = false;

			return modified;
		
		}

		private B2Jplayhead createNewPlayhead( B2Jrecord rec, List< B2Jplayhead > phs, B2Jloop loop ) {
		
			B2Jplayhead ph = new B2Jplayhead ( rec, loop );
			phs.Add ( ph );
			return ph;
		
		}

		
		public void printRecord( B2Jrecord br ) {
			
			if (br == null) {
				Debug.Log ("BVH2JSON: record is empty" );
			} else {
				Debug.Log ("BVH2JSON************");
				Debug.Log ( br.name );
				for ( int i = 0; i < br.groups.Count; i++ ) {
					Debug.Log ( "group[" + i + "] = " + br.groups[ i ].name + " (" + br.groups[ i ].use_millis + "," + br.groups[ i ].use_keys + ")" );
				}
				for ( int i = 0; i < br.bones.Count; i++ ) {
					Debug.Log ( "bone[" + i + "] = " + br.bones[ i ].name + " (" + br.bones[ i ].translations_enabled + "," +  br.bones[ i ].rotations_enabled  + "," +  br.bones[ i ].scales_enabled + ")" );
					if ( br.bones[ i ].parent != null ) {
						Debug.Log ( "\t\tparent:" + br.bones[ i ].parent.name );
					}
					foreach( B2Jbone child in br.bones[ i ].children ) {
						Debug.Log ( "\t\tchild:" + child.name );
					}
					
				}
				for ( int i = 0; i < br.keys.Count; i++ ) {
					Debug.Log ( "key[" + i + "] = " + br.keys[ i ].kID + " / " + br.keys[ i ].timestamp );
				}
				Debug.Log ("BVH2JSON************");
			}
		}
		
	}

}
