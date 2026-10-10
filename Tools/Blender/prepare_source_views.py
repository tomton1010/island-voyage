import bpy
import sys
from pathlib import Path

here=Path(__file__).resolve().parent;sys.path.insert(0,str(here))
from modeling import prepare_viewport
files=sorted((here.parents[1]/'ArtSource'/'Blender').glob('*.blend'))
for file in files:
    bpy.ops.wm.open_mainfile(filepath=str(file));bpy.context.view_layer.update();prepare_viewport()
    bpy.ops.wm.save_as_mainfile(filepath=str(file))
print(f'BLENDER_SOURCE_VIEWS_PASSED: {len(files)} editable sources framed with vertex paint',flush=True)
