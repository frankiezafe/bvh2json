class B2JParser {

  JSONObject b2jdata;
  ArrayList< B2JBone > bones;
  ArrayList< B2JBone > roots;
  
  public B2JParser( String path ) {
    
    b2jdata = loadJSONObject( path );
    
    // bones retrieval
    JSONArray bnames = b2jdata.getJSONArray("list");
    JSONArray brest = b2jdata.getJSONArray("rest");
    bones = new ArrayList< B2JBone >();
    roots = new ArrayList< B2JBone >();
    for ( int i = 0; i < bnames.size(); i++ ) {
      B2JBone b = new B2JBone();
      b.name = bnames.getString( i );
      b.rest.set( brest.getFloat( i * 3 ), brest.getFloat( ( i * 3 ) + 1 ), brest.getFloat( ( i * 3 ) + 2 ) );
      println( b.name + " / " + b.rest );
      bones.add( b );
    }
    // bones hierarchy
    recursiveHieararchy( b2jdata.getJSONArray("hierarchy"), null );
    for (  B2JBone b : bones ) {
      if ( b.parent == null ) {
        roots.add( b );
      }
    }
  }
  
  public B2JBone getBoneByName( String name ) {
    
    for (  B2JBone b : bones ) {
      if ( b.name.equals( name ) )
       return b; 
    }
    return null;
    
  }
  
  private void recursiveHieararchy( JSONArray h, B2JBone parent ) {
    for ( int i = 0; i < h.size(); i++ ) {
      JSONObject bh = h.getJSONObject( i );
      B2JBone b = getBoneByName( bh.getString( "bone" ) );
      if ( b != null ) {
        b.parent = parent;
        if ( parent != null ) {
          parent.children.add( b );
        }
        recursiveHieararchy( bh.getJSONArray("children"), b );
      }
    }
  }
  
  public void draw() {
    pushStyle();
    strokeWeight( 1 );
    stroke( 255,0,255 );
    for (  B2JBone b : roots ) {
      recursiveBoneDraw( b, 0,0,0 );
    }
    popStyle();
  }
  
  private void recursiveBoneDraw( B2JBone bone, float x, float y, float z ) {
    
    line( x,y,z, x + bone.rest.x, y + bone.rest.y, z + bone.rest.z );
    for ( B2JBone b : bone.children ) {
      recursiveBoneDraw( b, x + bone.rest.x, y + bone.rest.y, z + bone.rest.z );
    }
    
  } 

}
