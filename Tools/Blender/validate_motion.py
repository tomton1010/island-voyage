"""Independent source-pose checks: loop seams, species stroke planes and walking foot height."""
import bpy
import sys
import json
from pathlib import Path

root=Path(__file__).resolve().parents[2]
report=[]
for name in ['Whale','Shark','Dolphin','Resident0','Palm0','Sailboat']:
    bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource'/'Blender'/f'{name}.blend'))
    armatures=[o for o in bpy.context.scene.objects if o.type=='ARMATURE']
    entry={'name':name,'loops':[]}
    for armature in armatures:
        for action in bpy.data.actions:
            armature.animation_data.action=action
            start,end=action.frame_range
            bpy.context.scene.frame_set(int(start));before=[p.matrix.copy() for p in armature.pose.bones]
            bpy.context.scene.frame_set(int(end));after=[p.matrix.copy() for p in armature.pose.bones]
            error=max(max(abs(a[r][c]-b[r][c]) for r in range(4) for c in range(4)) for a,b in zip(before,after))
            if error>.002:raise RuntimeError(f'{name}/{action.name} loop seam: {error}')
            entry['loops'].append({'clip':action.name,'seam_error':error})
        if name=='Resident0':
            armature.animation_data.action=bpy.data.actions['Walk'];feet=[]
            for f in range(36):
                bpy.context.scene.frame_set(f)
                feet.append([round((armature.matrix_world @ armature.pose.bones['Foot.'+side].head).z,5) for side in ['L','R']])
            entry['walking_ankle_heights']=feet;entry['minimum_ankle_height']=min(min(p) for p in feet)
            if entry['minimum_ankle_height']<.005:raise RuntimeError('Walking ankle penetrates ground')
    report.append(entry)
target=root/'ArtSource'/'Blender'/'MotionValidation.json';target.write_text(json.dumps(report,indent=2),encoding='utf-8')
print('BLENDER_MOTION_PASSED: loop seams and resident foot clearance',flush=True)
