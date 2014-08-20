# this script is based on "import_bvh.py" standard script by Campbell Barton
# ##### BEGIN GPL LICENSE BLOCK #####
#
#  This program is free software; you can redistribute it and/or
#  modify it under the terms of the GNU General Public License
#  as published by the Free Software Foundation; either version 2
#  of the License, or (at your option) any later version.
#
#  This program is distributed in the hope that it will be useful,
#  but WITHOUT ANY WARRANTY; without even the implied warranty of
#  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
#  GNU General Public License for more details.
#
#  You should have received a copy of the GNU General Public License
#  along with this program; if not, write to the Free Software Foundation,
#  Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.
#
# ##### END GPL LICENSE BLOCK #####
# <pep8 compliant>
# Script copyright (C) Campbell Barton

# adaptation: frankiezafe.org

import bpy
import os,re
import math
from math import radians
from mathutils import Vector, Euler, Matrix, Quaternion
from collections import OrderedDict
import locale
import json

JSON_MODEL = "bvh_numediart"
JSON_TYPE = "data"
JSON_VERSION = "0.1"
JSON_DESC = "generated with bvh2json.py - frankiezafe - july 2014"
JSON_COMPRESS = False
JSON_OPTIMISE = True

bvhpath = bpy.path.abspath( '//bvhs' )
bvhlist = []

def loadBvhs( path, subfolder = '' ):
	global bvhlist
	files = os.listdir( path + "/" + subfolder )
	for f in files:
		fp = path
		fsub = ''
		if subfolder != '':
			fp += "/" + subfolder
			fsub += subfolder + "/"
		fp += "/"+ f
		fsub += f
		print( fp )
		if os.path.isdir( fp ):
			loadBvhs( path, fsub )
		elif f.endswith(".bvh"):
			bvhlist.append( [ f[:-4], JSON_MODEL, fp ] )

loadBvhs( bvhpath )

# exception for realy basic, who has model "tester"
for i in range( 0, len( bvhlist ) ):
	if bvhlist[ i ][ 0 ] == "reallybasic":
		bvhlist[ i ][ 1 ] = "tester"
	subpart = bvhlist[ i ][ 2 ][ len( bvhpath ) : ]
	subpart = subpart.replace( '/', '_' )
	print( subpart, bvhlist[ i ][ 0 ], bvhlist[ i ][ 1 ] )

bvhlist = [[ "thomas_se_leve_02", JSON_MODEL, "//bvhs/thomas/se_lever/02.bvh" ]]
# bvhlist = [[ "reallybasic", "tester", "//bvhs/reallybasic.bvh" ]]
# bvhlist = [[ "rising_left_hip", JSON_MODEL, "//bvhs/bending_left_hip.bvh" ]]

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
		self.model = ""
	
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
		jsonf = open( jpath, 'w' )
		
		# collecting data
		self.dataframes = {}
		self.dataframes[ "framescount" ] = 0
		self.dataframes[ "summary" ] = self.newFrameData()
		self.dataframes[ "data" ] = [] # list of frameData
		self.previousDFrame = 0
		self.collectFrames( data )
		
		jsonData = {}
		jsonData[ "type" ] = JSON_TYPE
		jsonData[ "version" ] = JSON_VERSION
		jsonData[ "model" ] = self.model
		jsonData[ "desc" ] = JSON_DESC
		jsonData[ "name" ] = data.name
		jsonData[ "origin" ] = fpath
		jsonData[ "keys" ] = data.maxframe
		if self.dataframes[ "framescount" ] != data.maxframe:
			jsonData[ "empty_keys" ] = ( data.maxframe - self.dataframes[ "framescount" ])
		jsonData[ "groups" ] = []
		jsonData[ "groups" ].append( { "name":"default", "in":-1, "out":-1, "kin":0, "kout":data.maxframe } )
		jsonData[ "list" ] = []
		for i in data.nodes:
			jsonData[ "list" ].append( data.nodes[ i ].name )
		'''		
		jsonData[ "rest" ] = []
		for i in data.nodes:
			jsonData[ "rest" ].append( data.nodes[ i ].rest_head_local.x )
			jsonData[ "rest" ].append( data.nodes[ i ].rest_head_local.y )
			jsonData[ "rest" ].append( data.nodes[ i ].rest_head_local.z )
		'''
		jsonData[ "rotation_order" ] = []
		for i in data.nodes:
			jsonData[ "rotation_order" ].append( data.nodes[ i ].rot_order_str )
		
		jsonData[ "hierarchy" ] = []
		roots = self.seekOrigins( data.nodes )
		for r in roots:
			jsonData[ "hierarchy" ].append( self.hierarchyData( r ) )
		
		jsonData[ "summary" ] = {}
		jsonData[ "summary" ][ "positions" ] = list( self.dataframes[ "summary" ][ "positions" ]["bones"] )
		jsonData[ "summary" ][ "eulers" ] = list( self.dataframes[ "summary" ][ "eulers" ]["bones"] )
		# jsonData[ "summary" ][ "quaternions" ] = list( self.dataframes[ "summary" ][ "quaternions" ]["bones"] )
		jsonData[ "summary" ][ "scales" ] = list( self.dataframes[ "summary" ][ "scales" ]["bones"] )
		
		jsonData[ "data" ] = []
		for fd in self.dataframes[ "data" ]:
			if "EMPTY" not in fd:
				jsonData[ "data" ].append( fd )
		
		jstr = ""
		if not JSON_COMPRESS:
			jstr = json.dumps( jsonData, indent=2, sort_keys=True, separators=(',',':') )
		else:
			jstr = json.dumps( jsonData, sort_keys=True, separators=(',',':') )
		jsonf.write( jstr )
		jsonf.close()
		
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
		fData[ "time" ] = 0
		fData[ "id" ] = 0
		fData[ "positions" ] = {}
		fData[ "positions" ]["bones"] = []
		fData[ "positions" ]["values"] = []
		fData[ "eulers" ] = {}
		fData[ "eulers" ]["bones"] = []
		fData[ "eulers" ]["values"] = []
		# fData[ "quaternions" ] = {}
		# fData[ "quaternions" ]["bones"] = []
		# fData[ "quaternions" ]["values"] = []
		fData[ "scales" ] = {}
		fData[ "scales" ]["bones"] = []
		fData[ "scales" ]["values"] = []
		return fData
	
	def collectFrames( self, data ):
		
		self.dataframes[ "framescount" ] = data.maxframe
		
		if not JSON_OPTIMISE:
			self.dataframes[ "summary" ][ "positions" ]["bones"].append( "all" )
			self.dataframes[ "summary" ][ "eulers" ]["bones"].append( "all" )
			# self.dataframes[ "summary" ][ "quaternions" ]["bones"].append( "all" )
			# NO SCALING
		
		for frame in range( int( data.maxframe ) ):
		
			allpositions = {}
			alleulers = {}
			# allquaterions = {}
			
			frameData = self.newFrameData()
			frameData[ "time" ] = frame * data.frametime * 1000.0
			frameData[ "id" ] = frame
			
			for n in data.nodes:
			
				node = data.nodes[ n ]
				d = node.anim_data[ int( frame ) ]
				allpositions[ n ] = Vector( ( d[ 0 ], d[ 1 ], d[ 2 ] ) )
				alleulers[ n ] = Vector( ( d[ 3 ] * 180 / math.pi, d[ 4 ] * 180 / math.pi, d[ 5 ] * 180 / math.pi ) )
				# allquaterions[ n ] = self.getQuaternion( node, frame )
				
			if JSON_OPTIMISE:
			
				i = 0
				pchanged = {}
				# qchanged = {}
				echanged = {}
				# emptyq = Euler( ( 0,0,0 ), "XYZ" ).to_quaternion()
				# emptyv = Vector( ( 0,0,0 ) )
				
				for n in data.nodes:
					if self.previousDFrame is not 0:
						if self.previousDFrame[ "positions" ][ n ] != allpositions[ n ]:
							pchanged[ n ] = i
						if self.previousDFrame[ "eulers" ][ n ] != alleulers[ n ]:
							echanged[ n ] = i
						'''						
						if not self.compareQuats( self.previousDFrame[ "quaternions" ][ n ], allquaterions[ n ] ):
							qchanged[ n ] = i
						'''						
						'''
						elif self.previousDFrame is 0:
							if allpositions[ n ] != emptyv:
								pchanged[ n ] = i
							if not self.compareQuats( allquaterions[ n ], emptyq ):
								qchanged[ n ] = i
						'''
					else:
						pchanged[ n ] = i
						echanged[ n ] = i
						# qchanged[ n ] = i
					i += 1
				
				pchanged = self.sortDict( pchanged )
				echanged = self.sortDict( echanged )
				# qchanged = self.sortDict( qchanged )
				
				# adding new bones that have changed in the summary
				for n in pchanged:
					found = False
					for nId in self.dataframes[ "summary" ][ "positions" ]["bones"]:
						if nId == pchanged[ n ]:
							found = True
							break
					if not found:
						self.dataframes[ "summary" ][ "positions" ]["bones"].append( pchanged[ n ] )

				for n in echanged:
					found = False
					for nId in self.dataframes[ "summary" ][ "eulers" ]["bones"]:
						if nId == echanged[ n ]:
							found = True
							break
					if not found:
						self.dataframes[ "summary" ][ "eulers" ]["bones"].append( echanged[ n ] )
				'''
				for n in qchanged:
					found = False
					for nId in self.dataframes[ "summary" ][ "quaternions" ]["bones"]:
						if nId == qchanged[ n ]:
							found = True
							break
					if not found:
						self.dataframes[ "summary" ][ "quaternions" ]["bones"].append( qchanged[ n ] )			
				'''

				if len( pchanged ) == len( data.nodes ):
					frameData[ "positions" ]["bones"].append( "all" )
					for n in pchanged:
						frameData[ "positions" ]["values"].append( allpositions[ n ].x )
						frameData[ "positions" ]["values"].append( allpositions[ n ].y )
						frameData[ "positions" ]["values"].append( allpositions[ n ].z )
				elif len( pchanged ) != 0:
					for n in pchanged:
						frameData[ "positions" ]["bones"].append( pchanged[ n ] )
					for n in pchanged:
						frameData[ "positions" ]["values"].append( allpositions[ n ].x )
						frameData[ "positions" ]["values"].append( allpositions[ n ].y )
						frameData[ "positions" ]["values"].append( allpositions[ n ].z )
				
				if len( echanged ) == len( data.nodes ):
					frameData[ "eulers" ]["bones"].append( "all" )
					for n in echanged:
						frameData[ "eulers" ]["values"].append( alleulers[ n ].x )
						frameData[ "eulers" ]["values"].append( alleulers[ n ].y )
						frameData[ "eulers" ]["values"].append( alleulers[ n ].z )
				elif len( echanged ) != 0:
					for n in echanged:
						frameData[ "eulers" ]["bones"].append( echanged[ n ] )
					for n in echanged:
						frameData[ "eulers" ]["values"].append( alleulers[ n ].x )
						frameData[ "eulers" ]["values"].append( alleulers[ n ].y )
						frameData[ "eulers" ]["values"].append( alleulers[ n ].z )

				'''
				if len( qchanged ) == len( data.nodes ):
					frameData[ "quaternions" ]["bones"].append( "all" )
					for n in qchanged:
						frameData[ "quaternions" ]["values"].append( allquaterions[ n ].x )
						frameData[ "quaternions" ]["values"].append( allquaterions[ n ].y )
						frameData[ "quaternions" ]["values"].append( allquaterions[ n ].z )
						frameData[ "quaternions" ]["values"].append( allquaterions[ n ].w ) 
				elif len( qchanged ) != 0:
					for n in qchanged:
						frameData[ "quaternions" ]["bones"].append( qchanged[ n ] )
					for n in qchanged:
						frameData[ "quaternions" ]["values"].append( allquaterions[ n ].x )
						frameData[ "quaternions" ]["values"].append( allquaterions[ n ].y )
						frameData[ "quaternions" ]["values"].append( allquaterions[ n ].z )
						frameData[ "quaternions" ]["values"].append( allquaterions[ n ].w ) 
				'''

			else:

				frameData[ "positions" ]["bones"].append( "all" )
				for n in allpositions:
					frameData[ "positions" ]["values"].append( allpositions[ n ].x )
					frameData[ "positions" ]["values"].append( allpositions[ n ].y )
					frameData[ "positions" ]["values"].append( allpositions[ n ].z )
				frameData[ "eulers" ]["bones"].append( "all" )
				for n in alleulers:
					frameData[ "eulers" ]["values"].append( alleulers[ n ].x )
					frameData[ "eulers" ]["values"].append( alleulers[ n ].y )
					frameData[ "eulers" ]["values"].append( alleulers[ n ].z )
				'''
				frameData[ "quaternions" ]["bones"].append( "all" )
				for n in allquaterions:
					frameData[ "quaternions" ]["values"].append( allquaterions[ n ].x )
					frameData[ "quaternions" ]["values"].append( allquaterions[ n ].y )
					frameData[ "quaternions" ]["values"].append( allquaterions[ n ].z )
					frameData[ "quaternions" ]["values"].append( allquaterions[ n ].w ) 
				'''
				
			if JSON_OPTIMISE:
				self.previousDFrame = {}
				self.previousDFrame[ "positions" ] = dict( allpositions )
				self.previousDFrame[ "eulers" ] = dict( alleulers )
				# self.previousDFrame[ "quaternions" ] = dict( allquaterions )
		
			self.dataframes[ "data" ].append( dict( frameData ) )
		
		self.dataframes[ "summary" ][ "positions" ]["bones"].sort()
		self.dataframes[ "summary" ][ "eulers" ]["bones"].sort()
		# self.dataframes[ "summary" ][ "quaternions" ]["bones"].sort()
		
		if JSON_OPTIMISE:
			self.dataframes[ "framescount" ] = 0
			for d in self.dataframes[ "data" ]:
				# if len( d[ "positions" ]["bones"] ) != 0 or len( d[ "eulers" ]["bones"] ) != 0  or len( d[ "quaternions" ]["bones"] ) != 0 or len( d[ "scales" ]["bones"] ) != 0:
				if len( d[ "positions" ]["bones"] ) != 0 or len( d[ "eulers" ]["bones"] ) != 0 or len( d[ "scales" ]["bones"] ) != 0:
					self.dataframes[ "framescount" ] += 1
				else:
					d["EMPTY"] = True
		
	def hierarchyData( self, node ):
		if node is None:
			return
		d = {}
		d["bone"] = node.name
		d["head"] = [ node.rest_head_local.x, node.rest_head_local.y, node.rest_head_local.z ]
		d["tail"] = [ node.rest_tail_local.x, node.rest_tail_local.y, node.rest_tail_local.z ]
		d["children"] = []
		for child in node.children:
			d["children"].append( self.hierarchyData( child ) )
		return d
			
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
		
			print( "bvh \"%s\" loading, %i of %i" % ( bvhlist[ i ][ 2 ],  ( i + 1 ), ( len( bvhlist ) ) ) )
			tmpdata = BvhData( bvhlist[ i ][ 0 ] )
			self.model = bvhlist[ i ][ 1 ]
			self.loadBvhData( tmpdata, bvhlist[ i ][ 2 ] )
			# print( "bvh_frame_time:", tmpdata.frametime )
			self.saveJsonData( bvhlist[ i ][ 2 ], tmpdata )
		
		print( "parsing of %i bvhs finished" % len( bvhlist ) )

	def getQuaternion( self, bvhnode, frame ):
		data = bvhnode.anim_data[ int( frame ) ]
		xrot = data[ 3 ]
		yrot = data[ 4 ]
		zrot = data[ 5 ]
		# unity3D is left handed => angles must be adapted
		# xrot = -math.atan2( math.sin( xrot ), -math.cos( xrot ) )
		# yrot = math.atan2( -math.sin( yrot ), math.cos( yrot ) )
		# yrot *= math.atan2( -math.sin( yrot ), math.cos( yrot ) )
		# yrot *= -1		
		# zrot *= -1
		# print( ( xrot * 180 / math.pi ), ( yrot * 180 / math.pi ), ( zrot * 180 / math.pi ) )
		q = Euler( ( xrot, yrot, zrot ), bvhnode.rot_order_str ).to_quaternion()
		'''
		if ( data[ 3 ] != 0 or data[ 4 ] != 0 or data[ 5 ] != 0 ):
			euls = q.to_euler( bvhnode.rot_order_str )
			print( "data:", round( data[ 3 ] / math.pi * 180 ), round( data[ 4 ] / math.pi * 180 ), round( data[ 5 ] / math.pi * 180 ) )
			print( "eulers:", round( euls.x / math.pi * 180 ), round(euls.y / math.pi * 180 ), round(euls.z / math.pi * 180 ) )
		'''
		return q

BvhConverter().load( bvhlist )
