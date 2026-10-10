"""Check that the installed Blender can model, save, export FBX, and render."""
import json
from pathlib import Path
import sys
import bpy
from mathutils import Vector

output = Path(sys.argv[sys.argv.index('--') + 1]).resolve()
output.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, location=(0, 0, 0.65))
model = bpy.context.object
model.name = 'Environment validation — modeling and export'
model.scale = (1.4, 0.65, 0.65)
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
material = bpy.data.materials.new('Ocean teal')
material.diffuse_color = (0.04, 0.35, 0.42, 1)
material.use_nodes = True
material.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value = material.diffuse_color
material.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = 0.38
model.data.materials.append(material)
for polygon in model.data.polygons:
    polygon.use_smooth = True
bpy.ops.mesh.primitive_plane_add(size=200)
ground = bpy.context.object
ground.name = 'Ground'
ground_material = bpy.data.materials.new('Warm sand')
ground_material.diffuse_color = (0.6, 0.51, 0.36, 1)
ground.data.materials.append(ground_material)
bpy.ops.object.light_add(type='AREA', location=(2, -3, 6))
light = bpy.context.object
light.data.energy = 850
light.data.shape = 'DISK'
light.data.size = 5
bpy.ops.object.camera_add(location=(4, -6, 3.5))
camera = bpy.context.object
camera.rotation_euler = (Vector((0, 0, 0.5)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
scene = bpy.context.scene
scene.camera = camera
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 16
scene.render.resolution_x = 640
scene.render.resolution_y = 360
scene.render.resolution_percentage = 100
scene.world = bpy.data.worlds.new('World')
scene.world.use_nodes = True
scene.world.node_tree.nodes.get('Background').inputs['Color'].default_value = (0.25, 0.3, 0.4, 1)
scene.world.node_tree.nodes.get('Background').inputs['Strength'].default_value = 0.4
scene.render.image_settings.file_format = 'PNG'
scene.render.filepath = str(output / 'Environment.png')
bpy.ops.wm.save_as_mainfile(filepath=str(output / 'Environment.blend'))
bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
bpy.context.view_layer.objects.active = model
bpy.ops.export_scene.fbx(filepath=str(output / 'Environment.fbx'), use_selection=True, object_types={'MESH'}, add_leaf_bones=False, axis_forward='-Z', axis_up='Y')
bpy.ops.render.render(write_still=True)
for name in ['Environment.blend', 'Environment.fbx', 'Environment.png']:
    assert (output / name).stat().st_size > 1024, f'Missing or invalid output: {name}'
bpy.ops.wm.open_mainfile(filepath=str(output / 'Environment.blend'))
assert 'Environment validation — modeling and export' in bpy.data.objects
report = {'version': bpy.app.version_string, 'modeling': 'passed', 'blend_save_and_reopen': 'passed', 'fbx_export': 'passed', 'cpu_cycles_render': 'passed', 'artifacts': str(output)}
(output / 'Validation.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print('BLENDER_ENVIRONMENT_PASSED: ' + json.dumps(report, ensure_ascii=False))
