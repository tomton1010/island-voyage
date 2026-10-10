"""Blender modeling primitives; coordinates are authored in Unity metres (Y up, +Z forward)."""
import bpy
import bmesh
import math
import random
from mathutils import Vector, Quaternion
from pathlib import Path

def V(p): return Vector((p[0], p[2], p[1]))
def rgb(hex_color):
    s = hex_color.lstrip('#')
    return tuple(int(s[i:i+2], 16) / 255 for i in (0, 2, 4)) + (1,)
def blend(a, b, t): return tuple(a[i]*(1-t)+b[i]*t for i in range(4))
def clamp(x,a=0,b=1): return min(b,max(a,x))
def smooth(t): return t*t*(3-2*t)

MATERIALS = {}
def material(name, roughness=.6):
    if name in MATERIALS: return MATERIALS[name]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    attribute = m.node_tree.nodes.new('ShaderNodeVertexColor'); attribute.layer_name='Color'
    m.node_tree.links.new(attribute.outputs['Color'], bsdf.inputs['Base Color'])
    bsdf.inputs['Roughness'].default_value=roughness
    MATERIALS[name]=m
    return m

def empty(name, parent=None, position=(0,0,0)):
    o=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(o)
    o.parent=parent;o.location=V(position);return o

class Mesh:
    def __init__(self):
        self.vertices=[];self.faces=[];self.colors=[];self.smooth=[];self.weights=[]
    def vertex(self,p,c,weights=None):
        i=len(self.vertices);self.vertices.append(tuple(V(p)));self.colors.append(c)
        self.weights.append(weights or {});return i
    def face(self,indices,smooth_shading=True):
        self.faces.append(tuple(indices));self.smooth.append(smooth_shading)
    def box(self,center,size,color,rotation=0,weights=None):
        start=len(self.vertices);angle=math.radians(rotation)
        for x,y,z in [(-1,-1,-1),(-1,-1,1),(-1,1,-1),(-1,1,1),(1,-1,-1),(1,-1,1),(1,1,-1),(1,1,1)]:
            p=(x*size[0]/2,y*size[1]/2,z*size[2]/2)
            self.vertex((center[0]+p[0]*math.cos(angle)+p[2]*math.sin(angle),center[1]+p[1],center[2]-p[0]*math.sin(angle)+p[2]*math.cos(angle)),color,weights)
        for f in [(0,4,6,2),(5,1,3,7),(1,0,2,3),(4,5,7,6),(2,6,7,3),(1,5,4,0)]:self.face([start+i for i in f],False)
    def ellipsoid(self,center,scale,color,weights=None,rings=10,segments=18):
        start=len(self.vertices)
        for r in range(rings+1):
            t=math.pi*r/rings
            for s in range(segments):
                a=2*math.pi*s/segments
                p=(center[0]+math.sin(t)*math.cos(a)*scale[0],center[1]+math.cos(t)*scale[1],center[2]+math.sin(t)*math.sin(a)*scale[2])
                c=color(p) if callable(color) else color
                w=weights(p) if callable(weights) else weights
                self.vertex(p,c,w)
        for r in range(rings):
            for s in range(segments):
                a=start+r*segments+s;b=start+r*segments+(s+1)%segments
                self.face((a,b,b+segments,a+segments))
    def tube(self,points,radii,color,weights=None,sides=10):
        start=len(self.vertices);previous_tangent=None;previous_normal=None
        for i,p in enumerate(points):
            tangent=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)])
            tangent.normalize()
            # Parallel transport keeps cross sections continuous at vertical knees/elbows.
            # Recomputing tangent.cross(Y) independently flips the frame by 180 degrees.
            if previous_normal is None:
                reference=Vector((0,0,1)) if abs(tangent.z)<.85 else Vector((0,1,0))
                a=tangent.cross(reference).normalized()
            else:
                a=previous_tangent.rotation_difference(tangent) @ previous_normal
                a=(a-tangent*a.dot(tangent)).normalized()
            b=tangent.cross(a).normalized();previous_tangent=tangent;previous_normal=a
            for s in range(sides):
                angle=s*2*math.pi/sides;q=Vector(p)+(a*math.cos(angle)+b*math.sin(angle))*radii[i]
                self.vertex(q,color,weights(q) if callable(weights) else weights)
        for i in range(len(points)-1):
            for s in range(sides):
                a=start+i*sides+s;b=start+i*sides+(s+1)%sides;self.face((a,b,b+sides,a+sides))
        self.face(tuple(start+s for s in reversed(range(sides))),False)
        self.face(tuple(start+(len(points)-1)*sides+s for s in range(sides)),False)
    def line(self,a,b,radius,color,weights=None,sides=8):self.tube([a,b],[radius,radius],color,weights,sides)
    def loft(self,stations,color,weights=None,segments=28,subdivisions=3):
        # Catmull-Rom sections avoid the straight profile bands of the previous meshes.
        samples=[]
        for i in range(len(stations)-1):
            a=stations[max(0,i-1)];b=stations[i];c=stations[i+1];d=stations[min(len(stations)-1,i+2)]
            for j in range(subdivisions):
                t=j/subdivisions
                row=tuple(.5*((2*b[k])+(-a[k]+c[k])*t+(2*a[k]-5*b[k]+4*c[k]-d[k])*t*t+(-a[k]+3*b[k]-3*c[k]+d[k])*t*t*t) for k in range(4))
                samples.append((row[0],max(.004,row[1]),max(.004,row[2]),row[3]))
        samples.append(stations[-1]);start=len(self.vertices)
        for z,w,h,cy in samples:
            for s in range(segments):
                a=s*2*math.pi/segments;p=(math.cos(a)*w,cy+math.sin(a)*h,z)
                self.vertex(p,color(p,math.sin(a)) if callable(color) else color,weights(p) if callable(weights) else weights)
        for r in range(len(samples)-1):
            for s in range(segments):
                a=start+r*segments+s;b=start+r*segments+(s+1)%segments;self.face((a,b,b+segments,a+segments))
        self.face(tuple(start+s for s in reversed(range(segments))))
        self.face(tuple(start+(len(samples)-1)*segments+s for s in range(segments)))
    def fin(self,outline,thickness,color,weights=None,steps=8):
        # Closed cambered fin, with rounded tapered edge and a thick attachment root.
        root=Vector(outline[0]);tip=Vector(outline[1]);rear=Vector(outline[2]);normal=(tip-root).cross(rear-root).normalized()
        start=len(self.vertices)
        for side in (-1,1):
            for r in range(steps+1):
                u=r/steps
                edge=root*(1-u)+tip*u
                trailing=rear*(1-u)+tip*u
                for s in range(5):
                    v=s/4;q=edge*(1-v)+trailing*v+normal*side*thickness*math.sin(math.pi*v)*((1-u)**.55)
                    self.vertex(q,color,weights(q) if callable(weights) else weights)
        row=5;layer=(steps+1)*row
        for side in range(2):
            for r in range(steps):
                for s in range(4):
                    a=start+side*layer+r*row+s;face=(a,a+1,a+row+1,a+row)
                    self.face(face if side==0 else face[::-1])
        for r in range(steps):
            for s in (0,4):
                a=start+r*row+s;self.face((a,a+row,a+row+layer,a+layer))
        self.face(tuple([start+i for i in range(row)]+[start+layer+i for i in reversed(range(row))]))
    def build(self,name,parent=None,mat='MatteSurface',roughness=.65,bevel=0):
        data=bpy.data.meshes.new(name);data.from_pydata(self.vertices,[],self.faces);data.update()
        attr=data.color_attributes.new(name='Color',type='BYTE_COLOR',domain='CORNER')
        for polygon in data.polygons:
            polygon.use_smooth=self.smooth[polygon.index]
            for loop_index in polygon.loop_indices:attr.data[loop_index].color_srgb=self.colors[data.loops[loop_index].vertex_index]
        o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);o.parent=parent
        o.data.materials.append(material(mat,roughness))
        names=set(n for weights in self.weights for n in weights)
        for name in sorted(names):
            group=o.vertex_groups.new(name=name)
            for i,weights in enumerate(self.weights):
                if name in weights and weights[name]>.0001:group.add([i],weights[name],'REPLACE')
        # Normals are recalculated consistently, including all closed lofts.
        bpy.context.view_layer.objects.active=o;o.select_set(True)
        bm=bmesh.new();bm.from_mesh(data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(data);bm.free()
        if bevel:
            mod=o.modifiers.new('Rounded craftsmanship edges','BEVEL');mod.width=bevel;mod.segments=2;mod.limit_method='ANGLE';mod.angle_limit=.65
            bpy.ops.object.modifier_apply(modifier=mod.name)
        o.select_set(False);return o

def rig(name,bones,parent=None):
    data=bpy.data.armatures.new(name);o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);o.parent=parent;o.show_in_front=True
    bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
    for name,head,tail,parent_name in bones:
        b=data.edit_bones.new(name);b.head=V(head);b.tail=V(tail)
        if parent_name:b.parent=data.edit_bones[parent_name]
    bpy.ops.object.mode_set(mode='OBJECT');o.select_set(False)
    for p in o.pose.bones:p.rotation_mode='QUATERNION'
    return o

def skin(mesh,armature):
    mod=mesh.modifiers.new('Anatomical skin','ARMATURE');mod.object=armature
    mesh.parent=armature

def soften(mesh,level=1):
    bpy.context.view_layer.objects.active=mesh
    mod=mesh.modifiers.new('Smooth anatomical surface','SUBSURF');mod.levels=level;mod.render_levels=level
    bpy.ops.object.modifier_apply(modifier=mod.name)
    for polygon in mesh.data.polygons:polygon.use_smooth=True

def rotate(pose,axis,angle):
    q=pose.bone.matrix_local.to_quaternion()
    pose.rotation_quaternion=q.inverted() @ Quaternion(V(axis).normalized(),-math.radians(angle)) @ q

def translate(pose,offset):
    pose.location=pose.bone.matrix_local.to_quaternion().inverted() @ V(offset)

def action(armature,name,frames,pose_function):
    armature.animation_data_create();a=bpy.data.actions.new(name);armature.animation_data.action=a
    bpy.context.scene.frame_start=0;bpy.context.scene.frame_end=frames
    for frame in range(frames+1):
        bpy.context.scene.frame_set(frame)
        for p in armature.pose.bones:p.rotation_quaternion=Quaternion();p.location=(0,0,0);p.scale=(1,1,1)
        pose_function(frame/frames,armature.pose.bones)
        for p in armature.pose.bones:
            p.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=p.name)
            p.keyframe_insert(data_path='location',frame=frame,group=p.name)
    a.use_fake_user=True
    # Store named clips as NLA strips. Each source .blend retains editable actions.
    track=armature.animation_data.nla_tracks.new();track.name=name
    strip=track.strips.new(name,0,a);strip.action_frame_start=0;strip.action_frame_end=frames;track.mute=True
    return a

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True);MATERIALS.clear()
    bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
    bpy.context.scene.render.fps=30

def prepare_viewport():
    # Save a useful, coloured modelling view rather than the empty factory viewport.
    from mathutils import Euler
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    points=[o.matrix_world @ Vector(corner) for o in meshes for corner in o.bound_box]
    if not points:return
    low=Vector(tuple(min(p[i] for p in points) for i in range(3)))
    high=Vector(tuple(max(p[i] for p in points) for i in range(3)))
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type!='VIEW_3D':continue
            space=area.spaces.active;space.shading.color_type='VERTEX';space.shading.light='STUDIO'
            space.shading.show_shadows=True;space.shading.show_cavity=True;space.shading.cavity_type='BOTH'
            space.overlay.show_extras=False;space.region_3d.view_location=(low+high)*.5
            space.region_3d.view_distance=max(high-low)*1.7
            space.region_3d.view_rotation=Euler((math.radians(65),0,math.radians(145))).to_quaternion()

def export(name,root,source_dir,model_dir,report,animated=False):
    empty('ModelForward',root,(0,0,1))
    scene=bpy.context.scene;scene.frame_set(0)
    if animated:
        for o in scene.objects:
            if o.type=='ARMATURE' and o.animation_data and o.animation_data.action:
                scene.frame_end=int(o.animation_data.action.frame_range[1])
    bpy.context.view_layer.update();prepare_viewport()
    bpy.ops.wm.save_as_mainfile(filepath=str(source_dir/f'{name}.blend'))
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=str(model_dir/f'{name}.fbx'),use_selection=True,object_types={'MESH','ARMATURE','EMPTY'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,use_armature_deform_only=False,bake_anim=animated,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=True,bake_anim_simplify_factor=0,colors_type='SRGB',use_custom_props=True)
    meshes=[o for o in scene.objects if o.type=='MESH']
    vertices=sum(len(o.data.vertices) for o in meshes);triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
    bone_count=sum(len(o.data.bones) for o in scene.objects if o.type=='ARMATURE')
    animations=[a.name for a in bpy.data.actions if a.users>0] if animated else []
    for o in meshes:
        if any(m.type=='ARMATURE' for m in o.modifiers):
            for v in o.data.vertices:
                total=sum(g.weight for g in v.groups)
                if abs(total-1)>.015:raise RuntimeError(f'Non-normalized weights in {name}: {v.index} = {total}')
    report.append(dict(name=name,vertices=vertices,triangles=triangles,bones=bone_count,animations=animations))
    print(f'BLENDER_ASSET: {name} vertices={vertices} triangles={triangles} bones={bone_count} clips={animations}',flush=True)
