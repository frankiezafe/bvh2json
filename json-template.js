{
	
	// this file does not store scales
	"type": "data",
	"version": 0.0,
	"desc": "template of mocap file in json, july 2014",
	"name": "template", // free text
	"model": "bvh_numediart", // ULTRA important for mapping!!! each file with same model will have the same bones names & hierarchy
	"origin": "file.bvh", // original file
	"keys": 200, // number of keys in the file
	"empty_keys": 0, // number of blank keys in the original file
	"groups": [ // groups of keys, used as sequences
		{ "name": "start", "in": 0, "out": 20, "kin": -1, "kout": -1 }, // in & out: time in millis
		{ "name": "walk", "in": -1, "out": -1, "kin": 6, "kout": 25 }, // kin & kout: key index
		{ "name": "run", "in": -1, "out": -1, "kin": 50, "kout": 96 },
	]
	"list": [ "hips", "spine", "chest", "neck", "shoulderL" ], // index of quaternions in "data[n].quaternions"
	"hierarchy": [
		{ // hierarchy of bones of the list above. all bones must be listed
		"bone":  "hips", "children": [
			{ 
			"bone": "spine", "children": [
				{ 
				"bone": "chest", "children": [
					{ 
					"bone": "neck", "children": [ ]
					},
					{ 
					"bone": "shoulderL", "children": [ ]
					},
				]
				},
			]
			},
		]
		},
	],
	
	"summary": {
		"positions": [], // index of modified bones
		"quaternions": [], // index of modified bones
		"scales": [], // index of modified bones
	},
	
	"data": [ // per key data
		{
			"id": 0,
			"time": 0, // time in millis
			"positions":  {
				"bones": [ "all" ], // key contains all positions values
				"values": [ 0,0,0, 0,0,0, 0,0,0, 0,0,0, 0,0,0, ]
			}, // X Y Z order
			"scales":  {
				"bones": [ "all" ], // key contains all scale values
				"values": [ 1,1,1, 1,1,1, 1,1,1, 1,1,1, 1,1,1, ]
			}, // X Y Z order
			"quaternions": {
				"bones": [ "all" ], // key contains all quaternions values
				"values" : [ 0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, ], // W X Y Z order!
			}
		},
		{
			"id": 1,
			"time": 15, // time in millis
			"positions":  {
				"bones": [],
				"values": []
			}, // X Y Z order
			"scales":  {
				"bones": [],
				"values": []
			}, // X Y Z order
			"quaternions": {
				"bones": [ 0, 1 ], // key contains quaternions for bones 0 & 1 -> "hips" & "spine"
				"values" : [ 0,0,0,0, 0,0,0,0, ], // W X Y Z order!
			}
		},
	],
	
}