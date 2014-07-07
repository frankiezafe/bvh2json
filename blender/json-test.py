# https://docs.python.org/2/library/json.html
# http://stackoverflow.com/questions/15525837/which-is-the-best-way-to-compress-json-to-store-in-a-memory-based-store-like-red
# http://bsonspec.org/

import json

mocap = open( '../bvhs/test.json', 'r' ).read()

print( type( mocap ) )

d = json.loads( mocap )
for i in d:
	print( i, d[ i ] )
