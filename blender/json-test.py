import json

mocap = open( 'test.json', 'r' ).read()

print( type( mocap ) )

d = json.loads( '["foo", {"bar":["baz", null, 1.0, 2]}]' )
for i in range( len( d ) ):
	print( i, d[ i ] )

