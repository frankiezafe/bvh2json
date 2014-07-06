# https://docs.python.org/2/library/json.html
# http://stackoverflow.com/questions/15525837/which-is-the-best-way-to-compress-json-to-store-in-a-memory-based-store-like-red
# http://bsonspec.org/

import json

mocap = open( 'test.json', 'r' ).read()

print( type( mocap ) )

d = json.loads( '["foo", {"bar":["baz", null, 1.0, 2]}]' )
for i in range( len( d ) ):
	print( i, d[ i ] )

