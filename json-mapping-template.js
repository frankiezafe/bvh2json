{
	
	"type": "mapping",
	"version": 0.0,
	"desc": "template of mapping file in json, july 2014",
	"name": "mapping-template", // free text
	"model": "bvh_numediart", // ULTRA important for mapping!!! each file with same model will have the same bones names & hierarchy
	"local": [ "hips", "spine", "chest", "neck", "shoulderL" ], // list of bones, MUST be identical to the mocap file!
	"foreign": [ "b_Hips", "b_Spine01", "b_Spine02", "b_Neck", "b_shoulder.l" ], // list of bones of the avatar to animate
	"relation": {
		"hips": [
			[ "b_Hips", 1 ] // avatar bone and weight		
		],
		"spine": [
			[ "b_Spine01", 0.7 ]	
		],
		"chest": [
			[ "b_Spine01", 0.3 ],
			[ "b_Spine02", 0.7 ]
		],
		"neck": [
			[ "b_Neck", 1 ]
		],
		"shoulderL": [
			[ "b_shoulder.l", 1 ]
		]
	}

}
