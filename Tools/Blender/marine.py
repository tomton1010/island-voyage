import math
from modeling import *

def axial_weights(z,centers):
    order=sorted(centers.items(),key=lambda a:a[1])
    if z<=order[0][1]:return {order[0][0]:1}
    if z>=order[-1][1]:return {order[-1][0]:1}
    for (a,za),(b,zb) in zip(order,order[1:]):
        if za<=z<=zb:
            t=smooth((z-za)/(zb-za));return {a:1-t,b:t}

def create_marine(kind):
    root=empty(kind);m=Mesh()
    if kind=='Whale':
        stations=[(-3.7,.1,.11,0),(-3.05,.22,.22,0),(-2.1,.47,.48,0),(-.9,.77,.71,0),(.4,.94,.82,0),(1.5,.95,.72,0),(2.5,.77,.52,.06),(3.3,.5,.33,.12),(3.75,.025,.035,.14)]
        centers={'Head':2.6,'Spine':1.2,'Spine2':-.3,'Caudal':-1.65,'Peduncle':-2.8,'Flukes':-3.7}
        top,belly=rgb('3F5864'),rgb('CCD5D0');tail_width=1.72;tail_back=-4.5;body_segments=36;period=84
    elif kind=='Shark':
        stations=[(-1.65,.045,.065,0),(-1.2,.15,.17,0),(-.75,.24,.27,0),(-.1,.34,.33,0),(.55,.36,.31,0),(1.05,.29,.23,0),(1.5,.18,.14,0),(1.87,.018,.04,.025)]
        centers={'Head':1.4,'Spine':.7,'Spine2':0,'Caudal':-.7,'Peduncle':-1.3,'Flukes':-1.7}
        top,belly=rgb('536E76'),rgb('D6DDCF');tail_width=.84;tail_back=-2.6;body_segments=32;period=39
    else:
        stations=[(-1.35,.04,.055,0),(-1.0,.13,.14,0),(-.55,.23,.22,0),(.1,.29,.28,0),(.55,.27,.28,0),(.85,.22,.23,.02),(1.05,.105,.075,-.055),(1.35,.065,.06,-.045),(1.48,.012,.018,-.035)]
        centers={'Head':1,'Spine':.5,'Spine2':-.05,'Caudal':-.6,'Peduncle':-1.05,'Flukes':-1.4}
        top,belly=rgb('6A909E'),rgb('D1E1DB');tail_width=.65;tail_back=-1.95;body_segments=32;period=36
    def body_color(p,h):
        t=smooth(clamp((h+.36)/.92));c=blend(belly,top,t)
        # Subtle mottling and dorsal shading, not a painted straight belly seam.
        modulation=1+.026*math.sin(p[2]*6+p[0]*13)+.02*math.sin(p[2]*21)
        return tuple(clamp(v*modulation) for v in c[:3])+(1,)
    weights=lambda p:axial_weights(p[2],centers)
    m.loft(stations,body_color,weights,body_segments,4)
    bones=[('Root',(0,0,0),(0,.2,0),None)]
    names=['Head','Spine','Spine2','Caudal','Peduncle','Flukes']
    for i,name in enumerate(names):
        z=centers[name];end=centers[names[i+1]] if i<5 else tail_back
        bones.append((name,(0,0,z),(0,0,end),'Root' if i==0 else names[i-1]))
    for sign,label in [(-1,'L'),(1,'R')]:
        x=.68 if kind=='Whale' else .25;z=.95 if kind=='Whale' else .55
        tip=(sign*(2.15 if kind=='Whale' else .91 if kind=='Shark' else .63),-.36 if kind=='Whale' else -.18,-.6 if kind=='Whale' else -.38)
        base=(sign*x,-.22 if kind=='Whale' else -.1,z)
        bone_name=f'Flipper.{label}';bones.append((bone_name,base,tip,'Spine'))
        def fin_weights(p,base=base,bone_name=bone_name):
            t=smooth(clamp((abs(p[0])-abs(base[0]))/.3));return {bone_name:t,'Spine':1-t}
        m.fin([base,tip,(sign*x*.75,-.3 if kind=='Whale' else -.15,-.3)],.07 if kind=='Whale' else .03,top,fin_weights,10)
    dorsal_height=1.17 if kind=='Whale' else .95 if kind=='Shark' else .70
    base_y=.68 if kind=='Whale' else .28
    m.fin([(0,base_y,-.2),(0,dorsal_height,-.60),(0,base_y,-1 if kind=='Whale' else -.75)],.05,top,weights,12)
    if kind=='Shark':
        m.fin([(0,0,-1.6),(0,.90,-2.47),(0,.19,-2.35)],.032,top,{'Flukes':1},12)
        m.fin([(0,0,-1.6),(0,-.48,-2.32),(0,-.08,-2.18)],.027,top,{'Flukes':1},10)
        for sign in (-1,1):
            m.fin([(sign*.19,-.15,-.65),(sign*.45,-.30,-1.06),(sign*.14,-.18,-1.2)],.022,top,weights,7)
    else:
        for sign in (-1,1):
            m.fin([(0,0,centers['Flukes']),(sign*tail_width,.035,tail_back+.2),(sign*tail_width*.5,-.025,tail_back-.1)],.075 if kind=='Whale' else .035,top,{'Flukes':1},14)
    eye_x=.63 if kind=='Whale' else .235 if kind=='Shark' else .17
    eye_z=2.75 if kind=='Whale' else 1.15 if kind=='Shark' else .89
    eye_y=.12 if kind=='Whale' else .035
    def body_section(z):
        for i in range(len(stations)-1):
            if stations[i][0]<=z<=stations[i+1][0]:
                a=stations[max(0,i-1)];b=stations[i];c=stations[i+1];d=stations[min(len(stations)-1,i+2)]
                low=0.;high=1.
                def sample(t,k):return .5*((2*b[k])+(-a[k]+c[k])*t+(2*a[k]-5*b[k]+4*c[k]-d[k])*t*t+(-a[k]+3*b[k]-3*c[k]+d[k])*t*t*t)
                for _ in range(24):
                    mid=(low+high)*.5
                    if sample(mid,0)<z:low=mid
                    else:high=mid
                return tuple(sample((low+high)*.5,k) for k in (1,2,3))
        return stations[-1][1:]
    width,height,cy=body_section(eye_z)
    eye_x=width*math.sqrt(max(0,1-((eye_y-cy)/height)**2))+.006
    for sign in (-1,1):
        eye=(sign*eye_x,eye_y,eye_z);scale=.045 if kind=='Whale' else .032 if kind=='Shark' else .021
        m.ellipsoid(eye,(scale*.5,scale,scale*1.5),rgb('112730'),{'Head':1},8,12)
        m.ellipsoid((eye[0]+sign*.012,eye[1]+scale*.3,eye[2]+scale*.2),(scale*.15,scale*.16,scale*.2),rgb('F5F2D9'),{'Head':1},5,8)
        mouth_y=-.18 if kind=='Whale' else -.10 if kind=='Shark' else -.06
        mouth_start=3.45 if kind=='Whale' else 1.60 if kind=='Shark' else 1.38
        mouth=[]
        for j in range(9):
            z=mouth_start+(eye_z-.1-mouth_start)*j/8;w,h,cy=body_section(z)
            mouth.append((sign*(w*math.sqrt(1-.28**2)+.003),cy-h*.28,z))
        m.tube(mouth,[.010 if kind=='Whale' else .004]*9,rgb('334A50'),{'Head':1},6)
        if kind=='Shark':
            for j in range(5):
                z=.82-j*.105;x=sign*(.33-j*.006)
                m.tube([(x,-.17,z),(x+sign*.015,0,z-.025),(x,.18,z-.06)],[.011,.011,.006],rgb('314D57'),{'Spine':1},6)
    if kind=='Whale':
        m.ellipsoid((0,.72,1.78),(.12,.018,.13),rgb('203B46'),{'Head':1},7,12)
        for groove in range(-5,6):
            x=groove*.095
            m.tube([(x,-.48,2.8),(x*1.4,-.67,2.1),(x*1.45,-.79,.9)],[.009,.009,.004],rgb('A6BBB9'),{'Head':.3,'Spine':.7},5)
        # Humpback tubercles at the jaw and rostrum.
        for sign in (-1,1):
            for j in range(7):m.ellipsoid((sign*(.17+j*.075),.25-j*.055,3.55-j*.15),(.045,.038,.06),top,{'Head':1},5,8)
    armature=rig('MarineRig',bones,root);body=m.build('Anatomical body',mat='MarineSurface',roughness=.42);soften(body);skin(body,armature)
    def swim(t,p):
        phase=t*2*math.pi;axis=(0,1,0) if kind=='Shark' else (1,0,0)
        amplitudes=[.9,1.6,3.5,8,12,18]
        for i,name in enumerate(names):rotate(p[name],axis,math.sin(phase-i*.42)*amplitudes[i])
        for label,sign in [('L',-1),('R',1)]:rotate(p[f'Flipper.{label}'],(0,0,1),sign*(math.sin(phase-.25)*3+1.5))
        if kind=='Dolphin':rotate(p['Head'],(1,0,0),math.sin(phase)*1.3)
    action(armature,'Swim',period,swim)
    if kind=='Dolphin':
        def glide(t,p):
            phase=t*2*math.pi
            for i,name in enumerate(names):rotate(p[name],(1,0,0),math.sin(phase-i*.25)*(2+i*.9))
            for label,sign in [('L',-1),('R',1)]:rotate(p[f'Flipper.{label}'],(0,0,1),sign*12)
        action(armature,'Glide',45,glide)
        armature.animation_data.action=bpy.data.actions['Swim']
    return root

SPECIES=[('Clownfish','E58A3B',.16,.22,.56),('BlueTang','3564AC',.12,.26,.63),('YellowTang','EBC34E',.105,.27,.54),('Butterflyfish','E9D3A2',.105,.25,.53),('Damselfish','777DB5',.12,.19,.43),('Sardine','96B7B9',.09,.12,.66),('Mackerel','547D8F',.12,.16,.78),('FlyingFish','87B4BC',.09,.12,.64)]
def create_fish(index):
    name,hex_color,width,height,length=SPECIES[index];root=empty(name);m=Mesh();top=rgb(hex_color);belly=blend(top,rgb('E6E2CB'),.62)
    centers={'Head':length*.46,'Spine':length*.15,'Caudal':-length*.32,'Tail':-length*.77}
    weights=lambda p:axial_weights(p[2],centers)
    def color(p,h):
        c=blend(belly,top,smooth(clamp((h+.6)/1.25)))
        z=p[2]/length
        if index==0:
            if any(abs(z-band)<.092 for band in (.49,-.08,-.53)):c=rgb('F7EEE0')
            elif any(abs(z-band)<.125 for band in (.49,-.08,-.53)):c=rgb('383F46')
        elif index==1 and h>.05 and -.35<z<.4:c=blend(c,rgb('1E344D'),.75)
        elif index==3:
            if abs(z-.42)<.085:c=rgb('373F49')
            elif math.sin(z*25+p[1]*14)>.6:c=blend(c,rgb('647887'),.5)
        elif index==6 and h>.1:c=blend(c,rgb('233F51'),smooth(clamp((math.sin(z*45+h*9)-.2)*1.8))*.7)
        return c
    stations=[(-length*.8,width*.13,height*.17,0),(-length*.52,width*.58,height*.62,0),(-length*.18,width,height,0),(length*.22,width*.94,height*.94,0),(length*.57,width*.58,height*.6,0),(length*.83,width*.045,height*.08,0)]
    m.loft(stations,color,weights,22,2)
    tail=rgb('EDD06A') if index==1 else blend(top,rgb('D6D7C1'),.15)
    m.fin([(0,0,-length*.69),(0,height*.85,-length*1.24),(0,0,-length*1.10)],.017,tail,{'Tail':1},7)
    m.fin([(0,0,-length*.69),(0,-height*.85,-length*1.24),(0,0,-length*1.10)],.017,tail,{'Tail':1},7)
    m.fin([(0,height*.77,length*.35),(0,height*1.45,-length*.10),(0,height*.58,-length*.61)],.014,top,weights,8)
    m.fin([(0,-height*.70,-length*.02),(0,-height*1.12,-length*.4),(0,-height*.55,-length*.63)],.009,tail,weights,5)
    bones=[('Root',(0,0,0),(0,.07,0),None),('Head',(0,0,centers['Head']),(0,0,centers['Spine']),'Root'),('Spine',(0,0,centers['Spine']),(0,0,centers['Caudal']),'Head'),('Caudal',(0,0,centers['Caudal']),(0,0,centers['Tail']),'Spine'),('Tail',(0,0,centers['Tail']),(0,0,-length*1.2),'Caudal')]
    for sign,label in [(-1,'L'),(1,'R')]:
        base=(sign*width*.77,-height*.15,length*.23);tip=(sign*(width*4 if index==7 else width*1.7),-height*.15,-length*.33)
        bones.append((f'Pectoral.{label}',base,tip,'Spine'))
        m.fin([base,tip,(sign*width*.70,-height*.28,-length*.36)],.009,tail,{f'Pectoral.{label}':1},6)
        eye=(sign*width*.69,height*.28,length*.57)
        m.ellipsoid(eye,(.021,.035,.036),rgb('DAE0CA'),{'Head':1},6,10)
        m.ellipsoid((eye[0]+sign*.01,eye[1],eye[2]+.005),(.016,.024,.026),rgb('152A35'),{'Head':1},6,10)
        m.ellipsoid((eye[0]+sign*.021,eye[1]+.008,eye[2]+.012),(.006,.008,.008),rgb('F8F2DD'),{'Head':1},4,6)
        m.tube([(sign*width*.5,-height*.48,length*.43),(sign*width*.84,-height*.05,length*.39),(sign*width*.5,height*.47,length*.29)],[.007,.008,.005],blend(top,rgb('203944'),.45),{'Head':1},5)
    m.ellipsoid((0,-height*.04,length*.79),(.045,.012,.015),blend(top,rgb('302F2C'),.35),{'Head':1},5,8)
    armature=rig('FishRig',bones,root);obj=m.build('Detailed fish skin',mat='MarineSurface',roughness=.32);skin(obj,armature)
    def swim(t,p):
        phase=t*2*math.pi
        for i,(name,amp) in enumerate([('Head',1),('Spine',3),('Caudal',12),('Tail',23)]):rotate(p[name],(0,1,0),math.sin(phase-i*.48)*amp)
        for label,sign in [('L',-1),('R',1)]:rotate(p[f'Pectoral.{label}'],(0,0,1),sign*(6+math.sin(phase*2)*5))
    action(armature,'Swim',20 if index<5 else 26,swim)
    return root
