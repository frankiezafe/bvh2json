import bpy
from math import radians
from mathutils import Vector, Euler, Matrix, Quaternion
from collections import OrderedDict
import locale

JSON_TYPE = "data"
JSON_VERSION = "0.0"
JSON_DESC = "generated with bvh2json.py - frankiezafe - july 2014"
JSON_MODEL = "bvh_numediart"
JSON_COMPRESS = False
JSON_OPTIMISE = True

bvhlist = [ 
		# [ "miko_ariaII02", "//bvhs/ariaII_02.bvh" ],
		[ "test", "//bvhs/test.bvh" ],
]

class BvhNode(object):
	__slots__ = (
		'name',  # bvh joint name
		'parent',  # BVH_Node type or None for no parent
		'children',  # a list of children of this type.
		'rest_head_world',  # worldspace rest location for the head of this node
		'rest_head_local',  # localspace rest location for the head of this node
		'rest_tail_world',  # worldspace rest location for the tail of this node
		'rest_tail_local',  # worldspace rest location for the tail of this node
		'channels',  # list of 6 ints, -1 for an unused channel, otherwise an index for the BVH motion data lines, loc triple then rot triple
		'rot_order',  # a triple of indices as to the order rotation is applied. [0,1,2] is x/y/z - [None, None, None] if no rotation.
		'rot_order_str',  # same as above but a string 'XYZ' format.
		'anim_data',  # a list one tuple's one for each frame. (locx, locy, locz, rotx, roty, rotz), euler rotation ALWAYS stored xyz order, even when native used.
		'has_loc',  # Convenience function, bool, same as (channels[0]!=-1 or channels[1]!=-1 or channels[2]!=-1)
		'has_rot',  # Convenience function, bool, same as (channels[3]!=-1 or channels[4]!=-1 or channels[5]!=-1)
		'index',  # index from the file, not strictly needed but nice to maintain order
		'temp',  # use this for whatever you want
		)
	_eul_order_lookup = {
		(None, None, None): 'XYZ',  # XXX Dummy one, no rotation anyway!
		(0, 1, 2): 'XYZ',
		(0, 2, 1): 'XZY',
		(1, 0, 2): 'YXZ',
		(1, 2, 0): 'YZX',
		(2, 0, 1): 'ZXY',
		(2, 1, 0): 'ZYX',
		}
	
	def __init__(self, name, rest_head_world, rest_head_local, parent, channels, rot_order, index):
		self.name = name
		self.rest_head_world = rest_head_world
		self.rest_head_local = rest_head_local
		self.rest_tail_world = None
		self.rest_tail_local = None
		self.parent = parent
		self.channels = channels
		self.rot_order = tuple(rot_order)
		self.rot_order_str = BvhNode._eul_order_lookup[self.rot_order]
		self.index = index
		# convenience functions
		self.has_loc = channels[0] != -1 or channels[1] != -1 or channels[2] != -1
		self.has_rot = channels[3] != -1 or channels[4] != -1 or channels[5] != -1
		self.children = []
		# list of 6 length tuples: (lx,ly,lz, rx,ry,rz)
		# even if the channels aren't used they will just be zero
		self.anim_data = [(0, 0, 0, 0, 0, 0)]
	
	def __repr__(self):
		return ('Render Bvh Node name:"%s", rest_loc:(%.3f,%.3f,%.3f), rest_tail:(%.3f,%.3f,%.3f)' %
				(self.name,
				 self.rest_head_world.x, self.rest_head_world.y, self.rest_head_world.z,
				 self.rest_head_world.x, self.rest_head_world.y, self.rest_head_world.z))

PLAY_LOOP_NORMAL =	  0
PLAY_LOOP_PALINDROME =  1
PLAY_LOOP_NONE =		2

class BvhPlayer():
	
	# send the player to the manager to receive the data
	def __init__( self, manager ): # manager > BvhManager!
		
		self.manager = manager
		self.bvhname = 0
		self.speed = 0
		self.way = 1
		self.loop = PLAY_LOOP_PALINDROME
		self.head = 0 # form 0 to 1, bvh independant
		self.prev_head = 0
		self.influence = []
		self.influencetarget = []
		for i in range( 0, len( self.manager.data ) ):
			self.influence.append( 0 )
			self.influencetarget.append( 1 )
		self.min = 0
		self.max = 1
		
		self.bones = 0 # AdvancedBone list
	
	def setInfluence( self, id, value, default = 0 ):
		for i in range( 0, len( self.manager.data ) ):
			if i == id:
				self.influencetarget[ i ] = value
			else:
				self.influencetarget[ i ] = default
	
	def update( self ):
		
		# influence adaptation
		for i in range( 0, len( self.manager.data ) ):
			if self.influencetarget[ i ] != self.influence[ i ]:
				self.influence[ i ] += ( self.influencetarget[ i ] - self.influence[ i ] ) * 0.1
			if self.influencetarget[ i ] - self.influence[ i ] < 0.001:
				self.influence[ i ] = self.influencetarget[ i ]
		# head management
		self.prev_head = self.head
		self.head += self.speed * self.way
		if self.head < self.min:
			if self.loop == PLAY_LOOP_NORMAL:
				self.head = self.max
			elif self.loop == PLAY_LOOP_PALINDROME:
				self.head = self.min
				self.way *= -1
			elif self.loop == PLAY_LOOP_NONE:
				self.head = self.min
		elif self.head > self.max:
			if self.loop == PLAY_LOOP_NORMAL:
				self.head = self.min
			elif self.loop == PLAY_LOOP_PALINDROME:
				self.head = self.max
				self.way *= -1
			elif self.loop == PLAY_LOOP_NONE:
				self.head = self.max
		
		self.manager.collectdata( self )

class BvhData():
	# parsed data container
	def __init__( self, name="NO NAME" ):
		self.name = name
		self.nodes = 0
		self.frametime = 0
		self.maxframe = 0

class BvhConverter():
	
	def __init__( self ):
		
		self.BVH2BLENDER = Matrix()
		b2b = self.BVH2BLENDER
		b2b[0][0],b2b[0][1],b2b[0][2]=1,0,0
		b2b[1][0],b2b[1][1],b2b[1][2]=0,0,1
		b2b[2][0],b2b[2][1],b2b[2][2]=0,-1,0
		self.BVH2BLENDERi = b2b.inverted()
		
		self.data = []
	
	def exists( self ):
		return True
	
	def sortedNodes( self, bvh_nodes ):
		bvh_nodes_list = list(bvh_nodes.values())
		bvh_nodes_list.sort(key=lambda bvh_node: bvh_node.index)
		return bvh_nodes_list
	
	def loadFile( self, file_path, rotate_mode='XYZ', global_scale = 1.0 ):
		
		opath = file_path
		file_path = bpy.path.abspath( file_path )
		file = open( file_path, 'rU' )
		file_lines = file.readlines()
		if len(file_lines) == 1:
			file_lines = file_lines[0].split('\r')
		file_lines = [ll for ll in [l.split() for l in file_lines] if ll]
		if file_lines[0][0].lower() == 'hierarchy':
			pass
		else:
			raise 'ERROR: This is not a BVH file'
		bvh_nodes = {None: None}
		bvh_nodes_serial = [None]
		bvh_frame_time = None
		channelIndex = -1
		lineIdx = 0  # An index for the file.
		while lineIdx < len(file_lines) - 1:
			if file_lines[lineIdx][0].lower() == 'root' or file_lines[lineIdx][0].lower() == 'joint':
				if len(file_lines[lineIdx]) > 2:
					file_lines[lineIdx][1] = '_'.join(file_lines[lineIdx][1:])
					file_lines[lineIdx] = file_lines[lineIdx][:2]
				name = file_lines[lineIdx][1]
				lineIdx += 2  # Increment to the next line (Offset)
				rest_head_local = Vector((float(file_lines[lineIdx][1]), float(file_lines[lineIdx][2]), float(file_lines[lineIdx][3]))) * global_scale
				lineIdx += 1
				my_channel = [-1, -1, -1, -1, -1, -1]
				my_rot_order = [None, None, None]
				rot_count = 0
				for channel in file_lines[lineIdx][2:]:
					channel = channel.lower()
					channelIndex += 1  # So the index points to the right channel
					if channel == 'xposition':
						my_channel[0] = channelIndex
					elif channel == 'yposition':
						my_channel[1] = channelIndex
					elif channel == 'zposition':
						my_channel[2] = channelIndex
					elif channel == 'xrotation':
						my_channel[3] = channelIndex
						my_rot_order[rot_count] = 0
						rot_count += 1
					elif channel == 'yrotation':
						my_channel[4] = channelIndex
						my_rot_order[rot_count] = 1
						rot_count += 1
					elif channel == 'zrotation':
						my_channel[5] = channelIndex
						my_rot_order[rot_count] = 2
						rot_count += 1
				channels = file_lines[lineIdx][2:]
				my_parent = bvh_nodes_serial[-1]  # account for none
				if my_parent is None:
					rest_head_world = Vector(rest_head_local)
				else:
					rest_head_world = my_parent.rest_head_world + rest_head_local
				bvh_node = bvh_nodes[name] = BvhNode(name, rest_head_world, rest_head_local, my_parent, my_channel, my_rot_order, len(bvh_nodes) - 1)
				bvh_nodes_serial.append(bvh_node)

			if file_lines[lineIdx][0].lower() == 'end' and file_lines[lineIdx][1].lower() == 'site':  # There is sometimes a name after 'End Site' but we will ignore it.
				lineIdx += 2  # Increment to the next line (Offset)
				rest_tail = Vector((float(file_lines[lineIdx][1]), float(file_lines[lineIdx][2]), float(file_lines[lineIdx][3]))) * global_scale
				bvh_nodes_serial[-1].rest_tail_world = bvh_nodes_serial[-1].rest_head_world + rest_tail
				bvh_nodes_serial[-1].rest_tail_local = bvh_nodes_serial[-1].rest_head_local + rest_tail
				bvh_nodes_serial.append(None)
			if len(file_lines[lineIdx]) == 1 and file_lines[lineIdx][0] == '}':  # == ['}']
				bvh_nodes_serial.pop()  # Remove the last item
			if len(file_lines[lineIdx]) == 1 and file_lines[lineIdx][0].lower() == 'motion':
				lineIdx += 2  # Read frame rate.
				if (len(file_lines[lineIdx]) == 3 and
					file_lines[lineIdx][0].lower() == 'frame' and
					file_lines[lineIdx][1].lower() == 'time:'):
					bvh_frame_time = float(file_lines[lineIdx][2])
				lineIdx += 1  # Set the cursor to the first frame
				break
			lineIdx += 1
		del bvh_nodes[None]
		del bvh_nodes_serial

		bvh_nodes_list = self.sortedNodes( bvh_nodes )

		while lineIdx < len(file_lines):
			line = file_lines[lineIdx]
			for bvh_node in bvh_nodes_list:
				lx = ly = lz = rx = ry = rz = 0.0
				channels = bvh_node.channels
				anim_data = bvh_node.anim_data
				if channels[0] != -1:
					lx = global_scale * float(line[channels[0]])
				if channels[1] != -1:
					ly = global_scale * float(line[channels[1]])
				if channels[2] != -1:
					lz = global_scale * float(line[channels[2]])
				if channels[3] != -1 or channels[4] != -1 or channels[5] != -1:
					rx = radians(float(line[channels[3]]))
					ry = radians(float(line[channels[4]]))
					rz = radians(float(line[channels[5]]))
				anim_data.append((lx, ly, lz, rx, ry, rz))
			lineIdx += 1

		for bvh_node in bvh_nodes_list:
			bvh_node_parent = bvh_node.parent
			if bvh_node_parent:
				bvh_node_parent.children.append(bvh_node)
		
		for bvh_node in bvh_nodes_list:
			if not bvh_node.rest_tail_world:
				if len(bvh_node.children) == 0:
					bvh_node.rest_tail_world = Vector(bvh_node.rest_head_world)
					bvh_node.rest_tail_local = Vector(bvh_node.rest_head_local)
				elif len(bvh_node.children) == 1:
					bvh_node.rest_tail_world = Vector(bvh_node.children[0].rest_head_world)
					bvh_node.rest_tail_local = bvh_node.rest_head_local + bvh_node.children[0].rest_head_local
				else:
					rest_tail_world = Vector((0.0, 0.0, 0.0))
					rest_tail_local = Vector((0.0, 0.0, 0.0))
					for bvh_node_child in bvh_node.children:
						rest_tail_world += bvh_node_child.rest_head_world
						rest_tail_local += bvh_node_child.rest_head_local
					bvh_node.rest_tail_world = rest_tail_world * (1.0 / len(bvh_node.children))
					bvh_node.rest_tail_local = rest_tail_local * (1.0 / len(bvh_node.children))

			if (bvh_node.rest_tail_local - bvh_node.rest_head_local).length <= 0.001 * global_scale:
				print("\tzero length node found:", bvh_node.name)
				bvh_node.rest_tail_local.y = bvh_node.rest_tail_local.y + global_scale / 10
				bvh_node.rest_tail_world.y = bvh_node.rest_tail_world.y + global_scale / 10
		
		return bvh_nodes, bvh_frame_time
	
	def saveJsonData( self, fpath, data ):
		
		# sorting nodes by name
		data.nodes = OrderedDict( sorted( data.nodes.items() ) )
		# print( data.nodes.keys() )		
		
		jpath = fpath[ 0:-4 ]+".json"
		jpath = bpy.path.abspath( jpath )
		json = open( jpath, 'w' )
		
		endl = ""
		tab = ""
		if not JSON_COMPRESS:
			endl = "\n"
			tab = "\t"
		
		# printing header
		json.write("{%s" % ( endl ) )
		json.write("\"type\":\"%s\",%s" % ( JSON_TYPE, endl ) )
		json.write("\"version\":%s,%s" % ( JSON_VERSION, endl ) )
		json.write("\"model\":\"%s\",%s" % ( JSON_MODEL, endl ) )
		json.write("\"desc\":\"%s\",%s" % ( JSON_DESC, endl ) )
		json.write("\"name\":\"%s\",%s" % ( data.name, endl ) )
		json.write("\"origin\":\"%s\",%s" % ( fpath, endl ) )
		json.write("\"keys\":%i,%s" % ( data.maxframe, endl ) )
		json.write("\"groups\":[%s%s{ \"name\":\"default\", \"in\":-1, \"out\":-1, \"kin\":%i, \"kout\":%i, },%s],%s" % ( endl, tab, 0, data.maxframe, endl, endl ) )
		json.write("\"list\":[%s" % ( endl ) )
		for i in data.nodes:
			json.write( "%s\"%s\",%s" % ( tab, data.nodes[ i ].name, endl ) )
		json.write("],%s" % ( endl ) )
		
		json.write("\"hierarchy\":[%s" % ( endl ) )
		roots = self.seekOrigins( data.nodes )
		for r in roots:
			self.hierarchyPrint( json, r, 0 )
		json.write("],%s" % ( endl ) )
		
		# json.write("\"frametime\":\"%f\",%s" % ( data.frametime * 1000, endl ) )
		
		# big stuff now!
		self.dataframes = {}
		self.dataframes[ "summary" ] = self.newFrameData()
		self.dataframes[ "data" ] = [] # list of frameData
		self.previousDFrame = 0
		self.collectFrames( data )
		print( "SUMMARY", self.dataframes[ "summary" ] )
		
		# writting summary
		json.write("\"summary\":{%s" % ( endl ) )
		json.write("%s\"positions\":[" % ( tab ) )
		for n in self.dataframes[ "summary" ][ "positionIds" ]:
			if type( n ) == type(str()):
				json.write("\"%s\"," % ( n ) )
			else:
				json.write("%i," % ( n ) )
		json.write("],%s" % ( endl ) )
		json.write("%s\"quaternions\":[" % ( tab ) )
		for n in self.dataframes[ "summary" ][ "quaternionIds" ]:
			if type( n ) == type(str()):
				json.write("\"%s\"," % ( n ) )
			else:
				json.write("%i," % ( n ) )
		json.write("],%s" % ( endl ) )
		json.write("%s\"scales\":[" % ( tab ) )
		for n in self.dataframes[ "summary" ][ "scaleIds" ]:
			json.write("%s," % ( n ) )
		json.write("],%s" % ( endl ) )
		json.write("},%s" % ( endl ) )

		json.write("\"data\":[%s" % ( endl ) )
		for i in range( len( self.dataframes[ "data" ] ) ):
			self.framePrint( json, data, i )
		json.write("],%s" % ( endl ) )
		
		json.write("}")
		json.close()
		
		del self.dataframes
		del self.previousDFrame
	
	def compareQuats( self, q1, q2 ):
		if q1 is None or q2 is None:
			return False
		if q1.x == q2.x and q1.y == q2.y and q1.z == q2.z and q1.w == q2.w:
			return True
		return False
	
	def sortDict( self, d ):
		return OrderedDict( sorted( d.items(), key=lambda x: x[1] ) )
	
	def newFrameData( self ):
		fData = {}
		fData[ "positionIds" ] = []
		fData[ "positionData" ] = []
		fData[ "quaternionIds" ] = []
		fData[ "quaternionData" ] = []
		fData[ "scaleIds" ] = []
		fData[ "scaleData" ] = []
		return fData
	
	def collectFrames( self, data ):
		
		if not JSON_OPTIMISE:
			self.dataframes[ "summary" ][ "positionIds" ].append( "all" )
			self.dataframes[ "summary" ][ "quaternionIds" ].append( "all" )
			# NO SCALING
		
		for frame in range( int( data.maxframe ) ):
		
			allpositions = {}
			allquaterions = {}
			
			frameData = self.newFrameData()
			
			for n in data.nodes:
			
				node = data.nodes[ n ]
				d = node.anim_data[ int( frame ) ]
				allpositions[ n ] = Vector( ( d[ 0 ], d[ 1 ], d[ 2 ] ) )
				allquaterions[ n ] = self.getQuaternion( node, frame )
				
			if JSON_OPTIMISE:
			
				i = 0
				pchanged = {}
				qchanged = {}
				emptyq = Euler( ( 0,0,0 ), "XYZ" ).to_quaternion()
				emptyv = Vector( ( 0,0,0 ) )
				
				for n in data.nodes:
					if self.previousDFrame is not 0:
						if self.previousDFrame[ "positions" ][ n ] != allpositions[ n ]:
							pchanged[ n ] = i
						if not self.compareQuats( self.previousDFrame[ "quaternions" ][ n ], allquaterions[ n ] ):
							qchanged[ n ] = i
					elif self.previousDFrame is 0:
						if allpositions[ n ] != emptyv:
							pchanged[ n ] = i
						if not self.compareQuats( allquaterions[ n ], emptyq ):
							qchanged[ n ] = i
					else:
						pchanged[ n ] = i
						qchanged[ n ] = i
					i += 1
				
				pchanged = self.sortDict( pchanged )
				qchanged = self.sortDict( qchanged )
				
				# adding new bones that have changed in the summary
				for n in pchanged:
					found = False
					for nId in self.dataframes[ "summary" ][ "positionIds" ]:
						if nId == pchanged[ n ]:
							found = True
							break
					if not found:
						self.dataframes[ "summary" ][ "positionIds" ].append( pchanged[ n ] )
				for n in qchanged:
					found = False
					for nId in self.dataframes[ "summary" ][ "quaternionIds" ]:
						if nId == qchanged[ n ]:
							found = True
							break
					if not found:
						self.dataframes[ "summary" ][ "quaternionIds" ].append( qchanged[ n ] )			
			
				if len( pchanged ) == len( data.nodes ):
					frameData[ "positionIds" ].append( "all" )
					for n in pchanged:
						frameData[ "positionData" ].append( allpositions[ n ] )
				elif len( pchanged ) != 0:
					for n in pchanged:
						frameData[ "positionIds" ].append( pchanged[ n ] )
					for n in pchanged:
						frameData[ "positionData" ].append( allpositions[ n ] )
				
				if len( qchanged ) == len( data.nodes ):
					frameData[ "quaternionIds" ].append( "all" )
					for n in qchanged:
						frameData[ "quaternionData" ].append( allquaterions[ n ] )
				elif len( qchanged ) != 0:
					for n in qchanged:
						frameData[ "quaternionIds" ].append( qchanged[ n ] )
					for n in qchanged:
						frameData[ "quaternionData" ].append( allquaterions[ n ] )

			else:
				frameData[ "positionIds" ].append( "all" )
				for n in allpositions:
					frameData[ "positionData" ].append( allpositions[ n ] )
				frameData[ "quaternionIds" ].append( "all" )
				for n in allquaterions:
					frameData[ "quaternionData" ].append( allquaterions[ n ] )
				
			if JSON_OPTIMISE:
				self.previousDFrame = {}
				self.previousDFrame[ "positions" ] = dict( allpositions )
				self.previousDFrame[ "quaternions" ] = dict( allquaterions )
		
			self.dataframes[ "data" ].append( dict( frameData ) )
		
		self.dataframes[ "summary" ][ "positionIds" ].sort()
		self.dataframes[ "summary" ][ "quaternionIds" ].sort()
		
	def framePrint( self, json, data, frame ):
		
		fData = self.dataframes[ "data" ][ frame ]
		
		if JSON_OPTIMISE:
			# skipping blank frames
			if len( fData[ "positionIds" ] ) == 0 and len( fData[ "quaternionIds" ] ) == 0 and len( fData[ "scaleIds" ] ) == 0:
				return
		
		endl = ""
		tab = ""
		tab2 = ""
		if not JSON_COMPRESS:
			endl = "\n"
			tab = "\t"
			tab2 = "\t\t"
		
		time = frame * data.frametime * 1000.0
		
		json.write("%s{%s" % ( tab, endl ) )
		
		json.write("%s\"key\":%f,%s" % ( tab2, time, endl ) )
		json.write("%s\"positions\":{%s" % ( tab2, endl ) )
		json.write("%s%s\"bones\":[" % ( tab2,tab ))
		for n in fData[ "positionIds" ]:
			if type( n ) == type(str()):
				json.write("\"%s\"," % ( n ) )
			else:
				json.write("%i," % ( n ) )
		json.write("],%s" % ( endl ) )
		json.write("%s%s\"values\":[" % ( tab2,tab ) )
		for v in fData[ "positionData" ]:
			json.write("%f,%f,%f," % ( v.x, v.y,v.z ) )
		json.write("],%s" % ( endl ) )
		json.write("%s},%s" % ( tab2, endl ) )
		
		json.write("%s\"quaternions\":{%s" % ( tab2, endl ) )
		json.write("%s%s\"bones\":[" % ( tab2,tab ))
		for n in fData[ "quaternionIds" ]:
			if type( n ) == type(str()):
				json.write("\"%s\"," % ( n ) )
			else:
				json.write("%i," % ( n ) )
		json.write("],%s" % ( endl ) )
		json.write("%s%s\"values\":[" % ( tab2,tab ) )
		for q in fData[ "quaternionData" ]:
			json.write("%f,%f,%f,%f," % ( q.x, q.y, q.z, q.w )  )
		json.write("],%s" % ( endl ) )
		json.write("%s},%s" % ( tab2, endl ) )
		
		json.write("%s\"scales\":{%s" % ( tab2, endl ) )
		json.write("%s%s\"bones\":[],%s" % ( tab2,tab,endl ) )
		json.write("%s%s\"values\":[],%s" % ( tab2,tab,endl ) )
		json.write("%s},%s" % ( tab2, endl ) )
		
		json.write("%s},%s" % ( tab, endl ) )
		
		return
		
		
		
		
		
		
		
		
		
		allpositions = {}
		allquaterions = {}
		
		for n in data.nodes:
			node = data.nodes[ n ]
			d = node.anim_data[ int( frame ) ]
			allpositions[ n ] = Vector( ( d[ 0 ], d[ 1 ], d[ 2 ] ) )
			allquaterions[ n ] = self.getQuaternion( node, frame )
		
		if JSON_OPTIMISE:
			
			i = 0
			pchanged = {}
			qchanged = {}
			emptyq = Euler( ( 0,0,0 ), "XYZ" ).to_quaternion()
			emptyv = Vector( ( 0,0,0 ) )
			
			for n in data.nodes:
				if self.previousDFrame is not 0:
					if self.previousDFrame[ "positions" ][ n ] != allpositions[ n ]:
						pchanged[ n ] = i
					if not self.compareQuats( self.previousDFrame[ "quaternions" ][ n ], allquaterions[ n ] ):
						qchanged[ n ] = i
				elif self.previousDFrame is 0:
					if allpositions[ n ] != emptyv:
						pchanged[ n ] = i
					if not self.compareQuats( allquaterions[ n ], emptyq ):
						qchanged[ n ] = i
				else:
					pchanged[ n ] = i
					qchanged[ n ] = i
				i += 1
			
			pchanged = self.sortDict( pchanged )
			qchanged = self.sortDict( qchanged )
			
			json.write("%s\"positions\":{%s" % ( tab, endl ) )
			if len( pchanged ) == 0:
				json.write("%s%s\"bones\":[],%s" % ( tab,tab,endl ) )
				json.write("%s%s\"values\":[],%s" % ( tab,tab,endl ) )
			elif len( pchanged ) == len( data.nodes ):
				json.write("%s%s\"bones\":[\"all\"],%s" % ( tab,tab,endl ) )
				json.write("%s%s\"values\":[" % ( tab,tab ) )
				for n in pchanged:
					d = allpositions[ n ]
					json.write("%f,%f,%f," % ( d[ 0 ], d[ 1 ], d[ 2 ] ) )
				json.write("],%s" % ( endl ) )
			else:
				json.write("%s%s\"bones\":[" % ( tab,tab ))
				for n in pchanged:
					json.write( "%i," % ( pchanged[ n ] ) )
				json.write("],%s" % ( endl ) )
				json.write("%s%s\"values\":[" % ( tab,tab ) )
				for n in pchanged:
					d = allpositions[ n ]
					json.write("%f,%f,%f," % ( d[ 0 ], d[ 1 ], d[ 2 ] ) )
				json.write("],%s" % ( endl ) )
			json.write("%s},%s" % ( tab, endl ) )
			
			json.write("%s\"quaternions\":{%s" % ( tab, endl ) )
			if len( qchanged ) == 0:
				json.write("%s%s\"bones\":[],%s" % ( tab,tab,endl ) )
				json.write("%s%s\"values\":[],%s" % ( tab,tab,endl ) )
			elif len( qchanged ) == len( data.nodes ):
				json.write("%s%s\"bones\":[\"all\"],%s" % ( tab,tab,endl ) )
				json.write("%s%s\"values\":[" % ( tab,tab ) )
				for n in qchanged:
					q = allquaterions[ n ]
					json.write("%f,%f,%f,%f," % ( q.x, q.y, q.z, q.w ) )
				json.write("],%s" % ( endl ) )
			else:
				json.write("%s%s\"bones\":[" % ( tab,tab ))
				for n in qchanged:
					json.write( "%i," % ( qchanged[ n ] ) )
				json.write("],%s" % ( endl ) )
				json.write("%s%s\"values\":[" % ( tab,tab ) )
				for n in qchanged:
					q = allquaterions[ n ]
					json.write("%f,%f,%f,%f," % ( q.x, q.y, q.z, q.w ) )
				json.write("],%s" % ( endl ) )
				
			json.write("%s},%s" % ( tab, endl ) )
			
			# print( "frame:", frame, len( pchanged ), len( qchanged ) )
			
		else:
			json.write("%s\"positions\":{%s" % ( tab, endl ) )
			json.write("%s%s\"bones\":[\"all\"],%s" % ( tab,tab,endl ) )
			json.write("%s%s\"values\":[" % ( tab,tab ) )
			for n in data.nodes:
				d = allpositions[ n ]
				json.write("%f,%f,%f," % ( d[ 0 ], d[ 1 ], d[ 2 ] ) )
			json.write("%s]%s" % ( tab, endl ) )
			json.write("%s},%s" % ( tab, endl ) )
			
			json.write("%s\"quaternions\":{%s" % ( tab, endl ) )
			json.write("%s%s\"bones\":[\"all\"],%s" % ( tab,tab,endl ) )
			json.write("%s%s\"values\":[" % ( tab,tab ) )
			for n in data.nodes:
				node = data.nodes[ n ]
				q = allquaterions[ n ]
				json.write("%f,%f,%f,%f," % ( q.x, q.y, q.z, q.w ) )
			json.write("%s]%s" % ( tab, endl ) )
			json.write("%s},%s" % (  tab, endl ) )
		
		json.write("%s\"scales\":{%s" % ( tab, endl ) )
		json.write("%s%s\"bones\":[],%s" % ( tab,tab,endl ) )
		json.write("%s%s\"values\":[],%s" % ( tab,tab,endl ) )
		json.write("%s},%s" % ( tab, endl ) )
		
		if JSON_OPTIMISE:
			self.previousDFrame = {}
			self.previousDFrame[ "positions" ] = dict( allpositions )
			self.previousDFrame[ "quaternions" ] = dict( allquaterions )
		
		json.write("%s}%s" % ( tab, endl ) )	
	
	def hierarchyPrint( self, json, node, lvl ):
		
		if node is None:
			return
			
		lvl += 1
		endl = ""
		tab = ""
		if not JSON_COMPRESS:
			endl = "\n"
			for i in range( lvl ):
				tab += "\t"
				
		json.write("%s{%s%s\"bone\":\"%s\",%s" % ( tab, endl, tab, node.name, endl ) )
		json.write("%s\"children\":[%s" % ( tab,endl ) )
		
		for child in node.children:
			self.hierarchyPrint( json, child, lvl )
		
		json.write("%s],},%s" % ( tab,endl ) )

	def seekOrigins( self, nodes ):
		
		out = []
		for n in nodes:
			if nodes[ n ].parent == None:
				out.append( nodes[ n ] )
		return out
	
	def loadBvhData( self, holder, path ):
		holder.nodes, holder.frametime = self.loadFile( path )
		for i in holder.nodes:
			holder.maxframe = float ( len( holder.nodes[ i ].anim_data ) )
			break
	
	def load( self, bvhlist ):
		for i in range( 0, len( bvhlist ) ):
		
			tmpdata = BvhData( bvhlist[ i ][ 0 ] )
			self.loadBvhData( tmpdata, bvhlist[ i ][ 1 ] )	
			
			print( "bvh_frame_time:", tmpdata.frametime )
			
			self.saveJsonData( bvhlist[ i ][ 1 ], tmpdata )
			
			print( "bvh \"{}\" loaded, remains {}/{}".format( self.data[ i ].name, ( len( bvhlist ) - ( i + 1 ) ), len( bvhlist ) ) )

	def getQuaternion( self, bvhnode, frame ):
	
		roto = bvhnode.rot_order[0] * 100 + bvhnode.rot_order[1] * 10 + bvhnode.rot_order[2]
		ero = 'ZYX'
		if roto == 201:
			ero = 'YXZ'
		elif roto == 210:
			ero = 'XYZ'
		elif roto == 21:
			ero = 'YZXZ'
		elif roto == 102:
			ero = 'ZXY'
			
		data = bvhnode.anim_data[ int( frame ) ]
		q = Euler( ( data[ 3 ], data[ 4 ], data[ 5 ]  ), ero ).to_quaternion()
		return q
			
	def interpolate( self, frame, bvhnode, strength = 1 ):
	
		roto = bvhnode.rot_order[0] * 100 + bvhnode.rot_order[1] * 10 + bvhnode.rot_order[2]
		# revert axis order, because it works when you do so...
		ero = 'ZYX'
		if roto == 201:
			ero = 'YXZ'
		elif roto == 210:
			ero = 'XYZ'
		elif roto == 21:
			ero = 'YZXZ'
		elif roto == 102:
			ero = 'ZXY'
		fbelow = int( frame )
		if fbelow >= len( bvhnode.anim_data ):
			fbelow = len( bvhnode.anim_data ) - 1
		fabove = fbelow + 1
		if fabove >= len( bvhnode.anim_data ):
			fabove = fbelow
			
		if frame == int( frame ) or fbelow == fabove:
			data = bvhnode.anim_data[ int( frame ) ]
			eul = Euler( ( data[ 3 ], data[ 4 ], data[ 5 ]  ), ero )
			return eul.to_quaternion().to_matrix().to_4x4()
		
		else:
			fbelow = int( frame )
			fabove = fbelow + 1
			ratio = frame - fbelow # between 0 and 1, obviously
			oitar = 1 - ratio
			dataB = bvhnode.anim_data[ fbelow ]
			dataA = bvhnode.anim_data[ fabove ]
			
			q = Euler( ( dataB[ 3 ] * strength, dataB[ 4 ] * strength, dataB[ 5 ] * strength ), ero ).to_quaternion()
			q.slerp( 
				Euler( ( dataA[ 3 ] * strength, dataA[ 4 ] * strength, dataA[ 5 ] * strength ), ero ).to_quaternion(),
				ratio )
			
			# q = q.slerp( eul.to_quaternion(), strength )
			# qB = Euler( ( dataB[ 3 ] * strength, dataB[ 4 ] * strength, dataB[ 5 ] * strength ), ero ).to_quaternion()
			# q = Euler( ( dataA[ 3 ] * strength, dataA[ 4 ] * strength, dataA[ 5 ] * strength ), ero ).to_quaternion()
			# q = q.slerp( qB, oitar )
			# q = q.slerp( q, strength )
			# return q
			return q.to_matrix().to_4x4()

	def collectdata( self, player ): # adapts player's eulers
		
		for bone in player.bones:
			euler = player.bones[ bone ].euler
			map = player.bones[ bone ].map
			T = player.bones[ bone ].rest
			Ti = T.inverted()
			mat = Matrix().to_4x4()
			for i in range( 0, len( self.data ) ): # loop trhough all BVHs
				influ = player.influence[ i ]
				if influ == 0:
					continue
				tmpdata = self.data[ i ]
				for nodename in map:
					b = tmpdata.nodes[ nodename ]
					# chargement des infos BVH
					newM = self.interpolate( player.head * tmpdata.maxframe, b, influ )
					mat = mat * newM
			# conversion système axes IGS > blender
			mat = ( self.BVH2BLENDERi * mat * self.BVH2BLENDER )
			# matrice globale vers matrice locale
			mat = ( Ti * mat * T )
			eu = mat.to_euler()
			euler.x = eu.x
			euler.y = eu.y
			euler.z = eu.z
			
	def newPlayer( self ):
		return BvhPlayer( self )

BvhConverter().load( bvhlist )
