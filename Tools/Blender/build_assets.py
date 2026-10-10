import bpy
import sys
import json
from pathlib import Path

HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE))
from modeling import reset,export
from marine import create_marine,create_fish,SPECIES

WORKSPACE=HERE.parent.parent
SOURCES=WORKSPACE/'ArtSource'/'Blender'
MODELS=WORKSPACE/'IslandVoyage'/'Assets'/'Resources'/'Blender'/'Models'
SOURCES.mkdir(parents=True,exist_ok=True);MODELS.mkdir(parents=True,exist_ok=True)
chosen=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
report=[]
def requested(name):return not chosen or name in chosen
for name in ['Whale','Shark','Dolphin']:
    if requested(name):reset();root=create_marine(name);export(name,root,SOURCES,MODELS,report,True)
for i,(name,*_) in enumerate(SPECIES):
    if requested(name):reset();root=create_fish(i);export(name,root,SOURCES,MODELS,report,True)
for i in range(3):
    name=f'Resident{i}'
    if requested(name):
        from characters import create_resident
        reset();root=create_resident(i);export(name,root,SOURCES,MODELS,report,True)
for i in range(4):
    name=f'Palm{i}'
    if requested(name):
        from scenery import create_palm
        reset();root=create_palm(i);export(name,root,SOURCES,MODELS,report,True)
for name in ['Sailboat','Cottage','Jetty','Bench','Noticeboard','Shrub','Shell','Starfish']:
    if requested(name):
        from scenery import create_static
        reset();root=create_static(name);export(name,root,SOURCES,MODELS,report,name=='Sailboat')
for i in range(5):
    name=f'Reef{i}'
    if requested(name):
        from scenery import create_reef
        reset();root=create_reef(i);export(name,root,SOURCES,MODELS,report)
for i in range(3):
    name=f'Rock{i}'
    if requested(name):
        from scenery import create_rock
        reset();root=create_rock(i);export(name,root,SOURCES,MODELS,report)
report_path=SOURCES/('PilotReport.json' if chosen else 'AssetReport.json')
report_path.write_text(json.dumps({'blender':bpy.app.version_string,'assets':report},ensure_ascii=False,indent=2),encoding='utf-8')
print(f'BLENDER_ASSET_BUILD_PASSED: {len(report)} assets; report={report_path}',flush=True)
