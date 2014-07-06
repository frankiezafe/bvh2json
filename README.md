Bvh2json
========

custom motion capture format based on json

The idea is to use 2 files:
- 1 data file, holding all the infos about timing, positions, rotations and scaling
- 1 / many mapping file, describing the relation between the bones in the data and the avatar in the 3D engine

check json-template.js & json-mapping-template.js for details

Unity 3D
========
JSonFX : https://github.com/jsonfx/jsonfx

MiniJSON: https://gist.github.com/darktable/1411710

TODO:
=====
- [done] add a "summary" in data : shows bones that are updated on position, rotation & scale
- [IMPORTANT] reformat output, no ending comma! > json python lib can't syand them
- add "offset" field from bvh bones description
- reader in blender
- reader in unity
