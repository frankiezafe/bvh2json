{
	
	"type": "mapping",
	"version": 0.0,
	"desc": "template of mapping file in json, july 2014",
	"name": "mapping-template", // free text
	"model": "bvh_numediart", // ULTRA important for mapping!!! each file with same model will have the same bones names & hierarchy
	"enable_rotations": 1,
	"enable_translations": 0,
	"enable_scales": 0,
	"smooth_method": "ACCUMULATION_OF_DIFFERENCE",
	"list": [
		"Head",
		"Hips",
		"LeftArm",
		"LeftFoot",
		"LeftFootHeel",
		"LeftForeArm",
		"LeftHand",
		"LeftLeg",
		"LeftShoulder",
		"LeftUpLeg",
		"Neck",
		"RightArm",
		"RightFoot",
		"RightFootHeel",
		"RightForeArm",
		"RightHand",
		"RightLeg",
		"RightShoulder",
		"RightUpLeg",
		"Spine",
		"Spine1"
	],
	"relations": [
		{"Head": [ { "bone": "head_bone", "weight": 1 } ] },
		{"Hips": [ { "bone": "pelvis_bone", "weight": 1 } ] },
		{"LeftArm": [ { "bone": "arm_l_bone", "weight": 1 } ] },
		{"LeftFoot": [ { "bone": "ankle_l_bone", "weight": 0.5 } ] },
		{"LeftFootHeel": [ { "bone": "foot_l_bone", "weight": 1 } ] },
		{"LeftForeArm": [ { "bone": "elbow_l_bone", "weight": 1 } ] },
		{"LeftHand": [ { "bone": "hand_l_bone", "weight": 1 } ] },
		{"LeftLeg": [ { "bone": "knee_l_bone", "weight": 1 } ] },
		{"LeftShoulder": [ { "bone": "collar_l_bone", "weight": 1 } ] },
		{"LeftUpLeg": [ { "bone": "leg_l_bone", "weight": 1 } ] },
		{"Neck": [ { "bone": "neck_bone", "weight": 1 } ] },
		{"RightArm": [ { "bone": "arm_r_bone", "weight": 1 } ] },
		{"RightFoot": [ { "bone": "ankle_r_bone", "weight": 0.5 } ] },
		{"RightFootHeel": [ { "bone": "foot_r_bone", "weight": 1 } ] },
		{"RightForeArm": [ { "bone": "elbow_r_bone", "weight": 1 } ] },
		{"RightHand": [ { "bone": "hand_r_bone", "weight": 1 } ] },
		{"RightLeg": [ { "bone": "knee_r_bone", "weight": 1 } ] },
		{"RightShoulder": [ { "bone": "collar_r_bone", "weight": 1 } ] },
		{"RightUpLeg": [ { "bone": "leg_r_bone", "weight": 1 } ] },
		{"Spine": [ { "bone": "spine_01_bone", "weight": 0.2 } ] },
		{"Spine1": [ { "bone": "spine_01_bone", "weight": 0.8 }, { "bone": "spine_02_bone", "weight": 0.4 } ] }
	]

}
