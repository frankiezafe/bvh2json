
B2JParser parser;

void setup() {
  
  size( 800, 600, P3D );
  parser = new B2JParser( "test.json" );
  
}

void draw() {
  
  background( 10 );
  
  fill( 255 );
  int i = 0;
  for (  B2JBone b : parser.bones ) {
    String s = i + ": " + b.name;
//    if ( b.parent != null ) {
//      s += " - parent: " + b.parent.name;
//    }
//    s += " - children: " + b.children.size();
    text( s, 10, 15 + i * 15 );
    i++;
  }
  
  pushMatrix();
  translate( width * 0.5f, height * 0.5f, 0 );
  rotateX( ( mouseY - height * 0.5f ) / 100 );
  rotateY( ( mouseX - width * 0.5f ) / 100 );
  scale( 2,2,2 );
  parser.draw();
  popMatrix();
  
}
