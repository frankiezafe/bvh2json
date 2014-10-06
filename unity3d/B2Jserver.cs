using UnityEngine;

using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

using B2J;

namespace B2J {

	public class B2Jserver: MonoBehaviour {
		
		private List<string> _loadedpath;
		private List<string> _loadingpath;
		private List<B2Jrecord> _records;

		private bool newRecord;

		private bool verbose;

		public B2Jserver() {
			_loadedpath = new List<string> ();
			_loadingpath = new List<string> ();
			_records = new List<B2Jrecord> ();
			newRecord = false;
			verbose = true;
		}

		public void Quiet() {
			verbose = false;
		}

		public void Verbose() {
			verbose = true;
		}

		public void Start() {}

		public void Update() {}
		
		public void OnApplicationQuit() {}

		public void OnDestroy() {}

		public void Load( string path ) {
			if ( _loadedpath.Contains ( path ) ) {
				if ( verbose )
					Debug.Log ( "'" + path + "' already loaded" );
				return;
			}
			AddNewRecord( B2Jparser.Instance.load ( path ), path );
		}
		
		public void AddNewRecord( B2Jrecord rec, string path ) {
			if ( rec != null ) {
				_loadedpath.Add( path );
				_records.Add( rec );
				newRecord = true;
				if ( verbose )
					Debug.Log ( "new record added: " + rec.name + ", " + _records.Count + " record(s) loaded" );
			}
		}

		public bool SyncPlayheads( List< B2Jplayhead > phs, Dictionary< string, B2Jplayhead > dict, B2Jloop loop ) {

			bool modified = false;

			if ( !newRecord && phs.Count == _records.Count )
				return modified;

			// is there playheads not registered anymore?
			foreach ( B2Jplayhead ph in phs ) {
				if ( ! _records.Contains( ph.Record ) ) {
					phs.Remove( ph );
					dict.Remove( ph.Name );
					modified = true;
				}
			}

			// new records may have been loaded, creating a new playhead if required
			foreach (B2Jrecord rec in _records) {
				bool found = false;
				foreach ( B2Jplayhead ph in phs ) {
					if ( ph.Record == rec ) {
						found = true;
						break;
					}
				}
				if ( !found ) {
					B2Jplayhead ph = createNewPlayhead( rec, phs, loop );
					dict.Add( ph.Name, ph );
					modified = true;
				}
			}

			return modified;

			newRecord = false;
		
		}

		private B2Jplayhead createNewPlayhead( B2Jrecord rec, List< B2Jplayhead > phs, B2Jloop loop ) {
		
			B2Jplayhead ph = new B2Jplayhead ( rec, loop );
			phs.Add ( ph );
			return ph;
		
		}

		
		public void PrintRecord( B2Jrecord br ) {
			
			if (br == null) {
				Debug.Log ("BVH2JSON: record is empty" );
			} else {
				Debug.Log ("BVH2JSON************");
				Debug.Log ( br.name );
				for ( int i = 0; i < br.groups.Count; i++ ) {
					Debug.Log ( "group[" + i + "] = " + br.groups[ i ].name + " (" + br.groups[ i ].use_millis + "," + br.groups[ i ].use_keys + ")" );
				}
				for ( int i = 0; i < br.bones.Count; i++ ) {
					Debug.Log ( "bone[" + i + "] = " + br.bones[ i ].name + " (" + br.bones[ i ].positions_enabled + "," +  br.bones[ i ].rotations_enabled  + "," +  br.bones[ i ].scales_enabled + ")" );
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
