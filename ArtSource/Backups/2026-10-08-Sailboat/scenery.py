"""Editable island assets, modelled in Blender with painted materials and wind rigs."""
import bpy
import math
import random
from mathutils import Vector
from modeling import Mesh,empty,rig,skin,action,rotate,rgb,blend,clamp

WOOD=rgb('9D6843');LIGHT_WOOD=rgb('C99B68');DARK_WOOD=rgb('503E31');ROPE=rgb('C1B090')

def create_boat():
    root=empty('Sailboat');m=Mesh();rng=random.Random(129)
    # A round-bottom carvel hull. Each strake has its own restrained wood variation.
    def section(u,angle,inside=False):
        z=-2.2+u*4.7;w=(math.sin(math.pi*u)**.52)*(.88+.13*u)
        x=w*math.sin(angle);y=.46-(.82 if not inside else .77)*math.cos(angle)
        return x,y,z
    for inside in (False,True):
        for plank in range(10):
            start=len(m.vertices);c=blend(WOOD,LIGHT_WOOD,.18+(plank%3)*.14)
            for r in range(37):
                for edge in (0,1):
                    angle=-math.pi/2+(plank+edge)*math.pi/10
                    m.vertex(section(.008+.984*r/36,angle,inside),c)
            for r in range(36):m.face((start+r*2,start+r*2+1,start+r*2+3,start+r*2+2))
    for side in (-1,1):
        rail=[section(.012+.976*i/40,side*math.pi/2) for i in range(41)]
        m.tube(rail,[.055]*41,LIGHT_WOOD,sides=10)
        # Thin dark caulking between planks and framing inside the cockpit.
        for plank in range(1,5):
            seam=[section(.035+.93*i/28,side*plank*math.pi/10) for i in range(29)]
            m.tube(seam,[.006]*29,DARK_WOOD,sides=4)
    for i in range(7):
        u=.16+i*.11;points=[section(u,-math.pi/2+j*math.pi/16,True) for j in range(17)]
        m.tube(points,[.03]*17,LIGHT_WOOD,sides=6)
    for z,width in [(-1.5,1.05),(-.68,1.65),(.46,1.78),(1.52,1.3)]:
        m.box((0,.40,z),(width,.09,.28),LIGHT_WOOD)
        for s in (-1,1):m.box((s*width*.36,.18,z),(.06,.43,.09),WOOD)
    for i in range(5):m.box((-.29+i*.145,-.13,-.20),(.135,.06,2.38),blend(WOOD,LIGHT_WOOD,i*.10))
    m.box((0,.11,-.96),(.48,.34,.39),DARK_WOOD);m.box((0,.30,-.96),(.51,.04,.43),LIGHT_WOOD)
    for s in (-1,1):
        for z in (-1.28,1.25):
            m.line((s*.79,.49,z),(s*.79,.62,z),.019,rgb('7F847C'))
            m.line((s*.73,.61,z),(s*.87,.61,z),.022,rgb('7F847C'))
        m.line((s*.91,.55,-.25),(s*.91,.73,-.25),.025,rgb('777D73'))
    # Coiled mooring line, with continuous helices and a lead to the bow cleat.
    points=[]
    for i in range(145):
        t=i/144*math.pi*8;r=.13+.013*t
        points.append((-.30+math.cos(t)*r,.50+math.sin(t*3)*.002,1.5+math.sin(t)*r))
    m.tube(points,[.014]*len(points),ROPE,sides=6)
    m.line((0,.32,.20),(0,4.32,.20),.052,DARK_WOOD,sides=12)
    m.line((0,4.2,.20),(0,.52,2.27),.012,ROPE,sides=6)
    for s in (-1,1):m.line((0,4.12,.20),(s*.85,.50,-.35),.010,ROPE,sides=5)
    m.build('Carvel hull · caulking · ribs · hardware',root,'WoodSurface',.47,bevel=.009)
    rudder=empty('Rudder',root,(0,0,-2.14));r=Mesh()
    r.box((0,-.25,-.06),(.07,.85,.38),DARK_WOOD)
    r.line((0,.05,0),(0,.68,0),.032,rgb('767D73'))
    r.line((0,.65,0),(0,.65,.66),.035,LIGHT_WOOD)
    r.build('Rudder blade and tiller',rudder,'WoodSurface',.48,bevel=.02)
    sail=empty('SailRig',root);bones=[('Mast',(0,1.05,.20),(0,2.1,.20),None),('SailMid',(0,2.1,.20),(0,3.2,.20),'Mast'),('SailTop',(0,3.2,.20),(0,4.2,.20),'SailMid')]
    arm=rig('SailArmature',bones,sail);cloth=Mesh()
    for row in range(19):
        v=row/18;y=1.06+v*3.1;length=2.12*(1-v)
        for col in range(13):
            u=col/12;z=.18-length*u;x=.17*math.sin(math.pi*u)*(1-v)
            w={'Mast':clamp(1-v*2),'SailMid':max(0,1-abs(v-.5)*2),'SailTop':clamp(v*2-1)}
            total=sum(w.values());w={k:a/total for k,a in w.items() if a>0}
            c=rgb('EFE6CC') if col%4 else rgb('D8CFB8')
            cloth.vertex((x,y,z),c,w)
    for row in range(18):
        for col in range(12):
            a=row*13+col;cloth.face((a,a+1,a+14,a+13))
    skin(cloth.build('Panelled canvas mainsail',mat='CanvasSurface',roughness=.87),arm)
    boom=Mesh();boom.line((0,1.03,.22),(0,1.03,-2.0),.041,DARK_WOOD)
    boom.line((0,1.03,-1.95),(0,.45,-1.48),.012,ROPE,sides=6)
    boom.build('Boom and mainsheet',sail,'WoodSurface',.5)
    def wind(t,p):
        rotate(p['SailMid'],(0,1,0),math.sin(t*math.tau)*1.8)
        rotate(p['SailTop'],(0,1,0),math.sin(t*math.tau+.7)*2.8)
    arm.animation_data_create();arm.animation_data.action=action(arm,'Wind',120,wind)
    root['description']='Round carvel hull, plank seams, ribs, benches, cleats, rigging and skinned canvas.'
    return root

def create_palm(index):
    rng=random.Random(914+index);root=empty(f'Palm{index}');m=Mesh();height=5.8+index*.43
    lean=.65+index*.18;bones=[('Root',(0,0,0),(0,.6,0),None),('Trunk',(0,.6,0),(.24,height*.5,.08),'Root'),('Crown',(.24,height*.5,.08),(lean,height,.18),'Trunk')]
    crown=Vector((lean,height,.18))
    for i in range(10):
        a=i*math.tau/10+index*.43;tail=crown+Vector((math.cos(a),.15,math.sin(a)))
        bones.append((f'Frond{i}',crown,tail,'Crown'))
    arm=rig('PalmArmature',bones,root)
    points=[(lean*(i/20)**1.65,height*i/20,.18*(i/20)**2) for i in range(21)]
    def trunk_weights(p):
        t=clamp(p[1]/height);return {'Trunk':1-t,'Crown':t}
    m.tube(points,[.23*(1-i/28) for i in range(21)],rgb('8D7857'),trunk_weights,sides=12)
    for i in range(26):
        t=(i+.4)/27;center=Vector((lean*t**1.65,height*t,.18*t*t));r=.235*(1-t*.70)
        ring=[tuple(center+Vector((math.cos(a*math.tau/16)*r,.007*math.sin(a),math.sin(a*math.tau/16)*r))) for a in range(17)]
        m.tube(ring,[.012]*17,rgb('6C6149'),trunk_weights,sides=4)
    for i in range(10):
        a=i*math.tau/10+index*.43;length=2.75+rng.random()*.60;dire=Vector((math.cos(a),0,math.sin(a)));side=Vector((-math.sin(a),0,math.cos(a)))
        def point(t):return crown+dire*length*t+Vector((0,.90*math.sin(math.pi*t*.9)-1.45*t*t,0))
        w={f'Frond{i}':1};spine=[tuple(point(j/18)) for j in range(19)]
        m.tube(spine,[.037*(1-j/21) for j in range(19)],rgb('719743'),w,sides=6)
        for j in range(1,17):
            t=j/18;center=point(t);width=.48*math.sin(math.pi*t)**.65
            for sign in (-1,1):
                tip=center+side*width*sign+dire*.30+Vector((0,-.13,0))
                m.fin((tuple(center-dire*.07),tuple(tip),tuple(center+dire*.075)),.013,blend(rgb('285D38'),rgb('7C9C49'),.30+.4*t),w,steps=3)
    for i in range(3):m.ellipsoid(tuple(crown+Vector((math.cos(i*2.1)*.23,-.27,math.sin(i*2.1)*.23))),(.18,.24,.18),rgb('655635'),{'Crown':1},rings=7,segments=10)
    skin(m.build('Pinnate palm crown and ringed curved trunk',mat='LeafSurface',roughness=.56),arm)
    def wind(t,p):
        rotate(p['Trunk'],(0,0,1),math.sin(t*math.tau)*.32)
        rotate(p['Crown'],(1,0,0),math.sin(t*math.tau+.4)*.65)
        for i in range(10):rotate(p[f'Frond{i}'],(1,0,.3),math.sin(t*math.tau+i*.51)*2.2)
    arm.animation_data_create();arm.animation_data.action=action(arm,'Wind',180,wind)
    return root

def create_cottage():
    root=empty('Cottage');m=Mesh();wall=rgb('D6D0BA');green=rgb('376D62')
    m.box((0,1.5,0),(6,3,4.5),wall)
    for x in (-3.035,3.035):
        for z in (-2.25,2.25):m.box((x,1.5,z),(.14,3.05,.18),LIGHT_WOOD)
    # Gabled plaster ends and individually laid cedar shingles on both roof slopes.
    for x in (-3,3):
        ids=[m.vertex(p,wall) for p in [(x,3,-2.25),(x,4.45,0),(x,3,2.25)]];m.face(ids,False)
    for sign in (-1,1):
        for row in range(10):
            z=sign*(.13+row*.29);y=4.54-abs(z)*.59
            for col in range(20):
                x=-3.45+col*.36+(row%2)*.15
                a=len(m.vertices);c=blend(rgb('705443'),rgb('A58A65'),((col*3+row*7)%11)/16)
                for p in [(x,y,z),(x+.355,y,z),(x+.355,y-.19,z+sign*.32),(x,y-.19,z+sign*.32)]:m.vertex(p,c)
                m.face((a,a+1,a+2,a+3),False)
        m.line((-3.55,2.86,sign*2.85),(3.55,2.86,sign*2.85),.07,DARK_WOOD)
    m.line((-3.6,4.59,0),(3.6,4.59,0),.09,DARK_WOOD)
    for s in (-1,1):
        x=s*1.9;m.box((x,1.9,-2.305),(1.12,1.02,.11),DARK_WOOD)
        m.box((x,1.9,-2.38),(.91,.83,.03),rgb('314A4A'))
        for dx in (-.49,0,.49):m.box((x+dx,1.9,-2.4),(.042,.9,.055),LIGHT_WOOD)
        for y in (1.45,1.9,2.35):m.box((x,y,-2.4),(1.04,.04,.06),LIGHT_WOOD)
        for shutter in (-1,1):
            m.box((x+shutter*.68,1.9,-2.39),(.28,1.08,.1),green)
            for i in range(8):m.box((x+shutter*.68,1.49+i*.117,-2.455),(.26,.035,.05),blend(green,rgb('67917B'),.3))
    m.box((0,1.05,-2.31),(1.12,2.1,.13),green)
    for i in range(6):m.box((-.46+i*.18,1.05,-2.39),(.018,2.05,.02),rgb('305A53'))
    m.ellipsoid((.35,1.05,-2.45),(.035,.035,.03),rgb('AD9258'),rings=6,segments=10)
    m.box((1.8,4.1,1),(.65,2.6,.75),rgb('B9B7A7'));m.box((1.8,5.4,1),(.86,.15,.95),wall)
    for i in range(17):m.box((-3.04+i*.38,.15,-3.36),(.365,.16,2.13),blend(WOOD,LIGHT_WOOD,(i%4)*.14))
    for x in (-3,-1,1,3):
        m.line((x,-.08,-4.29),(x,2.92,-4.29),.055,DARK_WOOD)
        m.line((x,2.92,-4.29),(x,3.1,-2.2),.047,DARK_WOOD)
        for i in range(6):m.line((x,3.0,-2.4-i*.33),(x+.9,3.0,-2.4-i*.33),.019,LIGHT_WOOD)
    for i in range(5):m.box((0,-i*.17,-4.5-i*.5),(2,.17,.52),LIGHT_WOOD)
    for s in (-1,1):
        m.line((s*3,.8,-4.3),(s*3,.8,-2.4),.04,DARK_WOOD)
        for i in range(7):m.line((s*3,.24,-4.15+i*.25),(s*3,.8,-4.15+i*.25),.022,LIGHT_WOOD)
    m.build('Cedar shingles, plaster, louvered shutters and veranda',root,'WoodSurface',.72,bevel=.012)
    return root

def create_jetty():
    root=empty('Jetty');m=Mesh()
    for i in range(24):
        m.box((0,.4,-7+i*.60),(2.5,.16,.565),blend(WOOD,LIGHT_WOOD,.25+(i%4)*.12))
        for s in (-1,1):
            for z in (-.13,.13):m.ellipsoid((s*.95,.487,-7+i*.6+z),(.016,.006,.016),DARK_WOOD,rings=3,segments=6)
    for s in (-1,1):
        m.line((s*.8,.15,-7.3),(s*.8,.15,7),.11,DARK_WOOD)
        for i in range(6):
            z=-6+i*2.4;m.line((s*1.14,-1.7,z),(s*1.14,1.07,z),.10,DARK_WOOD,sides=10)
            m.line((s*1.12,-.8,z),(s*1.12,.3,z+1.2),.055,WOOD)
            if i<5:
                points=[(s*1.14,.90-.21*math.sin(t*math.pi),z+t*2.4) for t in [j/12 for j in range(13)]]
                m.tube(points,[.021]*13,ROPE,sides=6)
    m.build('Weathered planks, pegs, braces and mooring posts',root,'WoodSurface',.64,bevel=.015)
    return root

def create_reef(index):
    root=empty(f'Reef{index}');m=Mesh();rng=random.Random(471+index*33)
    m.ellipsoid((0,-.08,0),(1.4,.2,1.15),rgb('758B7B'),rings=5,segments=12)
    purple=rgb(['735983','A57C80','A58F57','658E88','8D7195'][index])
    for i in range([1,5,0,1,2][index]):
        x=rng.uniform(-1,.3);z=rng.uniform(-.8,.7);h=rng.uniform(.6,1.4);radius=rng.uniform(.16,.25);start=len(m.vertices)
        # Genuine hollow tube sponges, with a recessed inner wall.
        for r,y in [(radius,0),(radius*.94,h),(radius*.67,h),(radius*.52,h*.67)]:
            for j in range(16):
                a=j*math.tau/16;col=blend(purple,rgb('C9AEA7'),.22) if y==h else purple
                m.vertex((x+math.cos(a)*r,y,z+math.sin(a)*r),col)
        for row in range(3):
            for j in range(16):
                a=start+row*16+j;b=start+row*16+(j+1)%16;m.face((a,b,b+16,a+16))
        m.ellipsoid((x,h*.64,z),(radius*.56,.025,radius*.56),rgb('3A3F43'),rings=3,segments=12)
    for k in range([5,0,0,3,2][index]):
        base=Vector((rng.uniform(-.9,1),0,rng.uniform(-1,1)));pink=rgb('B97876')
        stem=base+Vector((0,.7,0));m.line(base,stem,.055,pink)
        for j in range(-3,4):
            end=base+Vector((j*.20,1.10-abs(j)*.13,.05*math.sin(j)))
            m.tube([tuple(stem-Vector((0,.24,0))),tuple((stem+end)/2),tuple(end)],[.037,.023,.009],pink,sides=7)
            for sign in (-1,1):m.line(tuple(end-Vector((0,.18,0))),tuple(end+Vector((sign*.09,.045,0))),.009,blend(pink,rgb('DDAC93'),.3),sides=5)
    for k in range([1,1,6,1,3][index]):
        center=(rng.uniform(.2,1.3),.16,rng.uniform(-.9,.9));c=rgb('A6A96B')
        for layer in range(3):
            p=(center[0],center[1]+layer*.20,center[2]);size=(.54-layer*.08,.055,.43-layer*.065)
            m.ellipsoid(p,size,blend(c,rgb('D0C495'),layer*.12),rings=4,segments=16)
            for i in range(8):
                a=i*math.tau/8;m.line((p[0]+.07*math.cos(a),p[1]+.049,p[2]+.07*math.sin(a)),(p[0]+size[0]*.9*math.cos(a),p[1]+.032,p[2]+size[2]*.9*math.sin(a)),.006,blend(c,rgb('E8D5AC'),.4),sides=4)
    m.build('Hollow sponges, branching sea fans and ribbed plate coral',root,'ReefSurface',.75)
    return root

def create_rock(index):
    root=empty(f'Rock{index}');m=Mesh();rng=random.Random(732+index)
    # Smooth organic stones have low-frequency irregular silhouettes.
    m.ellipsoid((0,.23,0),(1,.53,.77),lambda p:blend(rgb('85877A'),rgb('B5B1A0'),clamp(p[1]*.7)),rings=10,segments=18)
    for i,(x,y,z) in enumerate(m.vertices):
        f=1+.09*math.sin(x*3.7+z*2.9+index)+.045*math.cos(y*6.1)
        m.vertices[i]=(x*f,y*f,z*f)
    m.build('Water worn limestone',root,'StoneSurface',.78)
    return root

def create_prop(name):
    root=empty(name);m=Mesh()
    if name=='Bench':
        for i in range(4):m.box((0,.90,i*.19),(3.2,.12,.17),LIGHT_WOOD)
        for i in range(3):m.box((0,1.30+i*.22,.70),(3.2,.19,.12),blend(WOOD,LIGHT_WOOD,i*.17))
        for s in (-1,1):
            m.box((s*1.25,.49,.29),(.16,.95,.7),DARK_WOOD);m.box((s*1.25,1.23,.75),(.11,.86,.12),DARK_WOOD)
    elif name=='Noticeboard':
        for s in (-1,1):m.line((s*.85,0,0),(s*.85,2.15,0),.065,DARK_WOOD)
        m.box((0,1.60,0),(2.15,1,.15),LIGHT_WOOD);m.box((0,1.60,-.09),(1.92,.78,.035),rgb('376958'))
        for i in range(3):
            m.box((-.60+i*.58,1.63,-.115),(.39,.52,.008),rgb('D7CBB0'),rotation=(-4+i*5))
            m.ellipsoid((-.60+i*.58,1.85,-.124),(.018,.018,.008),rgb('955B46'),rings=4,segments=7)
        m.box((0,2.18,0),(2.42,.12,.54),DARK_WOOD)
    elif name=='Shrub':
        rng=random.Random(613)
        for i in range(16):
            a=i*2.4;start=(math.cos(a)*.18,0,math.sin(a)*.18);tip=(math.cos(a)*rng.uniform(.45,.85),rng.uniform(.40,.85),math.sin(a)*rng.uniform(.45,.85))
            m.line(start,tip,.015,rgb('4D6640'),sides=5)
            for s in (-1,1):
                base=Vector(start)*.25+Vector(tip)*.75;m.fin((tuple(base-Vector((.02,.12,0))),tuple(Vector(tip)+Vector((s*.16,.06,.09))),tuple(base+Vector((.02,.08,0)))),.01,rgb('3B7045'),steps=3)
            if i%3==0:
                for j in range(5):m.ellipsoid((tip[0]+.065*math.cos(j*math.tau/5),tip[1],tip[2]+.065*math.sin(j*math.tau/5)),(.066,.028,.066),rgb('D39699'),rings=4,segments=6)
    elif name=='Shell':
        m.ellipsoid((0,.055,0),(.16,.075,.23),rgb('D8C6AC'),rings=6,segments=16)
        for i in range(9):
            a=(i-4)*.18;points=[(math.sin(a)*t*.20,.10+math.sin(t*math.pi)*.019,-.18+t*.38) for t in [j/9 for j in range(10)]]
            m.tube(points,[.006]*10,rgb('AF9781'),sides=4)
    elif name=='Starfish':
        c=rgb('BC7955');m.ellipsoid((0,.035,0),(.15,.043,.15),c,rings=5,segments=12)
        for i in range(5):
            a=i*math.tau/5;points=[(math.cos(a)*r,.04+math.sin(r*math.pi/.4)*.025,math.sin(a)*r) for r in (.07,.17,.28,.39)]
            m.tube(points,[.09,.07,.04,.005],c,sides=9)
            for j in range(4):m.ellipsoid((math.cos(a)*(.11+j*.067),.08,math.sin(a)*(.11+j*.067)),(.012,.007,.012),rgb('D4A575'),rings=3,segments=5)
    m.build(name+' crafted mesh',root,'WoodSurface' if name in ('Bench','Noticeboard') else 'LeafSurface' if name=='Shrub' else 'MatteSurface',.72,bevel=.012 if name in ('Bench','Noticeboard') else 0)
    return root

def create_static(name):
    return {'Sailboat':create_boat,'Cottage':create_cottage,'Jetty':create_jetty}.get(name,lambda:create_prop(name))()
