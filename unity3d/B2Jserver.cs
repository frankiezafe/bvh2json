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

	private object m_Handle = new object();
	private static volatile Boolean m_thread_stop;
	Thread m_thread;
	private int tcounter;

	public B2Jserver() {
		
		loadedpath = new List<string> ();
		loadingpath = new List<string> ();
		records = new List<B2Jrecord> ();

//		m_thread = new Thread( new System.Threading.ThreadStart( Background ) );
//		m_thread.Start();
//		m_thread_stop = false;

	}

	public void Start() {}

	public void Update() {

//		lock (m_Handle) {
//			if ( loadingpath.Count == 0 && !m_thread_stop ) {
//				m_thread_stop = true;
//			} else if ( loadingpath.Count >= 0 && m_thread_stop ) {
//				m_thread_stop = false;
//			}
//		}

	}
	
	public void OnApplicationQuit() {
//		m_thread.Abort();
	}

	public void OnDestroy() {
//		m_thread.Abort();
	}

	private void Background() {

		while( !m_thread_stop ) {
			Debug.Log ( "Background called " + tcounter );
			string currentp = "";
			lock (m_Handle) {
				if( loadingpath.Count > 0 ) {
					currentp = loadingpath[0];
					loadingpath.Remove (currentp);
					Debug.Log ("starting to load >> " + currentp);
				}
			}
			Thread.Sleep( 1000 );
			tcounter++;
		}

	}

	public void load( string path ) {
		
		if ( loadedpath.Contains ( path ) ) {
			Debug.Log ( "'" + path + "' already loaded" );
			return;
		}

//		lock (m_Handle) {
//			Debug.Log ( "'" + path + "' added in loading path" );
//			loadingpath.Add( path );
//		}

		loadedpath.Add( path );

		addNewRecord( B2Jparser.Instance.load ( path ) );

	}
	
	public void addNewRecord( B2Jrecord rec ) {
		if ( rec != null ) {
			lock (m_Handle) {
				records.Add( rec );
			}
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
