using UnityEngine;

using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

using B2J;

public class B2Jserver: MonoBehaviour {
	
	private List<string> loadedpath;
	private List<string> loadingpath;
	private List<B2Jrecord> records;
	private int tcounter;

	public B2Jserver() {
		loadedpath = new List<string> ();
		loadingpath = new List<string> ();
		records = new List<B2Jrecord> ();
	}

	public void Start() {}

	public void Update() {}
	
	public void OnApplicationQuit() {}

	public void OnDestroy() {}

	public void load( string path ) {
		if ( loadedpath.Contains ( path ) ) {
			Debug.Log ( "'" + path + "' already loaded" );
			return;
		}
		addNewRecord( B2Jparser.Instance.load ( path ), path );
	}
	
	public void addNewRecord( B2Jrecord rec, string path ) {
		if ( rec != null ) {
			loadedpath.Add( path );
			records.Add( rec );
			Debug.Log ( "new record added: " + rec.name + ", " + records.Count + " record(s) loaded" );
		}
	}

	public void syncPlayheads( List< B2Jplayhead > phs ) {
	
		// is there playheads not registered anymore?
		foreach ( B2Jplayhead ph in phs ) {
			if ( ! records.Contains( ph.Record ) ) {
				phs.Remove( ph );
			}
		}

		foreach (B2Jrecord rec in records) {
			bool found = false;
			foreach ( B2Jplayhead ph in phs ) {
				if ( ph.Record == rec ) {
					found = true;
					break;
				}
			}
			if ( !found ) {
				createNewPlayhead( rec, phs );
			}
		}
	
	}

	private void createNewPlayhead( B2Jrecord rec, List< B2Jplayhead > phs ) {
	
		B2Jplayhead ph = new B2Jplayhead ( rec, B2Jloop.B2JLOOPNORMAL );
		phs.Add ( ph );
	
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
