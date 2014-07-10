class B2JBone {

  String name;
  PVector rest;
  B2JBone parent;
  ArrayList< B2JBone > children;
  
  public B2JBone() {
    name = "";
    rest = new PVector();
    parent = null;
    children = new ArrayList< B2JBone >();
  }

}
