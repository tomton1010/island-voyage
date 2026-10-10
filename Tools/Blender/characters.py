import math
from modeling import *

def create_resident(index):
    root=empty(f'Resident{index}');m=Mesh()
    skin_color=rgb(['BF8E68','D5AF8B','9E704E'][index]);shirt=rgb(['CE967A','759DA1','A4AE84'][index]);pants=rgb(['435966','CBB999','665D51'][index]);hair=rgb(['42372B','352B25','252A2B'][index])
    bones=[('Root',(0,0,0),(0,.16,0),None),('Hips',(0,.87,0),(0,1.02,0),'Root'),('Spine',(0,1.02,0),(0,1.19,0),'Hips'),('Chest',(0,1.19,0),(0,1.40,0),'Spine'),('Neck',(0,1.40,0),(0,1.49,0),'Chest'),('Head',(0,1.49,0),(0,1.73,0),'Neck')]
    # A continuous, shaped torso rather than a sphere with separate cylinder limbs.
    def torso_weights(p):
        y=p[1]
        if y<1.02:return {'Hips':.6,'Spine':.4}
        t=clamp((y-1.02)/.24);return {'Spine':1-t,'Chest':t}
    # The shirt has a broad shoulder line and continuous waist, avoiding floating sleeve caps.
    start=len(m.vertices);stations=[(.92,.18,.12),(1.00,.19,.128),(1.13,.204,.14),(1.25,.218,.14),(1.34,.227,.13),(1.39,.196,.105),(1.42,.078,.071)]
    for y,width,depth in stations:
        for j in range(28):
            a=j*math.tau/28;p=(math.cos(a)*width,y,math.sin(a)*depth)
            m.vertex(p,shirt,torso_weights(p))
    for row in range(len(stations)-1):
        for j in range(28):
            a=start+row*28+j;b=start+row*28+(j+1)%28;m.face((a,b,b+28,a+28))
    m.face(tuple(start+i for i in reversed(range(28))));m.face(tuple(start+(len(stations)-1)*28+i for i in range(28)))
    m.ellipsoid((0,.87,0),(.19,.16,.125),pants,{'Hips':1},10,18)
    m.tube([(0,1.35,0),(0,1.46,0)],[.075,.061],skin_color,{'Neck':1},14)
    def face_color(p):return blend(skin_color,rgb('C78976'),clamp((abs(p[0])-.08)*3)*.10)
    m.ellipsoid((0,1.60,.005),(.133,.169,.122),face_color,{'Head':1},18,28)
    # Jaw, ears, eyelids, nose, brows, lips and hair are modeled in the source mesh.
    m.ellipsoid((0,1.505,.04),(.086,.055,.08),skin_color,{'Head':1},9,14)
    m.ellipsoid((0,1.59,.13),(.025,.042,.029),skin_color,{'Head':1},8,12)
    for sign in (-1,1):
        m.ellipsoid((sign*.13,1.593,0),(.026,.043,.029),skin_color,{'Head':1},8,10)
        eye=(sign*.047,1.628,.108)
        m.ellipsoid(eye,(.023,.010,.014),rgb('DCDDD0'),{'Head':1},6,10)
        m.ellipsoid((eye[0],eye[1],eye[2]+.012),(.009,.009,.005),rgb('39434A'),{'Head':1},6,10)
        m.tube([(sign*.024,1.654,.108),(sign*.047,1.664,.112),(sign*.072,1.654,.103)],[.005,.005,.003],hair,{'Head':1},6)
    m.tube([(-.036,1.541,.109),(0,1.536,.127),(.036,1.541,.109)],[.005,.006,.004],blend(skin_color,rgb('894947'),.5),{'Head':1},6)
    m.ellipsoid((0,1.716,-.01),(.136,.065,.123),hair,{'Head':1},10,20)
    if index!=1:
        hat=rgb('D2BE94')
        m.ellipsoid((0,1.744,0),(.245,.019,.237),hat,{'Head':1},5,28)
        m.ellipsoid((0,1.789,0),(.138,.085,.134),hat,{'Head':1},9,20)
        m.tube([(.141*math.cos(i*2*math.pi/32),1.767,.137*math.sin(i*2*math.pi/32)) for i in range(33)],[.010]*33,rgb('736552'),{'Head':1},5)
        for ring in range(3):
            radius=.18+ring*.018;m.tube([(radius*math.cos(i*2*math.pi/32),1.754,radius*math.sin(i*2*math.pi/32)) for i in range(33)],[.0018]*33,rgb('B7A27F'),{'Head':1},4)
    else:
        m.ellipsoid((0,1.697,-.12),(.107,.122,.079),hair,{'Head':1},10,16)
        m.ellipsoid((0,1.555,-.14),(.071,.145,.064),hair,{'Head':1},10,14)
    # Shirt seams and buttons give scale without noisy fine textures.
    m.tube([(0,1.00,.13),(0,1.17,.135),(0,1.36,.11)],[.003]*3,blend(shirt,rgb('ECE5D4'),.4),torso_weights,5)
    for y in [1.08,1.20,1.32]:m.ellipsoid((0,y,.137),(.007,.007,.006),rgb('D9CDB7'),torso_weights,4,6)
    for sign,label in [(-1,'L'),(1,'R')]:
        hip=(sign*.106,.87,0);knee=(sign*.106,.445,.025);ankle=(sign*.106,.065,0)
        shoulder=(sign*.20,1.365,0);elbow=(sign*.270,1.095,.015);wrist=(sign*.302,.865,.035)
        bones += [(f'Thigh.{label}',hip,knee,'Hips'),(f'Shin.{label}',knee,ankle,f'Thigh.{label}'),(f'Foot.{label}',ankle,(sign*.106,.045,.15),f'Shin.{label}'),(f'Toe.{label}',(sign*.106,.045,.12),(sign*.106,.045,.21),f'Foot.{label}'),(f'UpperArm.{label}',shoulder,elbow,'Chest'),(f'Forearm.{label}',elbow,wrist,f'UpperArm.{label}'),(f'Hand.{label}',wrist,(sign*.325,.78,.05),f'Forearm.{label}')]
        def leg_weights(p,label=label):
            if p[1]>.48:return {f'Thigh.{label}':1}
            t=smooth(clamp((.5-p[1])/.12));return {f'Thigh.{label}':1-t,f'Shin.{label}':t}
        m.tube([hip,(sign*.106,.845,.002),(sign*.11,.72,.006),(sign*.105,.59,.013),(sign*.106,.50,.021),(sign*.106,.465,.024),knee,(sign*.106,.415,.021),(sign*.105,.38,.017),(sign*.103,.29,.008),(sign*.105,.13,.004),(sign*.106,.085,.001),ankle],[.091,.09,.081,.065,.062,.061,.060,.061,.062,.06,.042,.037,.036],skin_color,leg_weights,14)
        m.tube([(sign*.107,.83,0),(sign*.108,.69,.005),(sign*.106,.60,.01)],[.097,.092,.082],pants,{f'Thigh.{label}':1},14)
        m.ellipsoid((sign*.106,.84,0),(.096,.096,.128),pants,{f'Thigh.{label}':.75,'Hips':.25},10,16)
        m.ellipsoid((sign*.106,.043,.080),(.050,.036,.112),rgb('766A54'),{f'Foot.{label}':1},8,16)
        m.ellipsoid((sign*.106,.075,.020),(.043,.055,.052),skin_color,{f'Shin.{label}':.45,f'Foot.{label}':.55},9,14)
        m.tube([(sign*.157,.062,.085),(sign*.106,.078,.085),(sign*.056,.062,.085)],[.009]*3,rgb('A6987A'),{f'Foot.{label}':1},6)
        def arm_weights(p,label=label):
            t=smooth(clamp((1.15-p[1])/.13));hand=smooth(clamp((.90-p[1])/.085))
            return {f'UpperArm.{label}':(1-t)*(1-hand),f'Forearm.{label}':t*(1-hand),f'Hand.{label}':hand}
        m.tube([shoulder,(sign*.207,1.34,.001),(sign*.257,1.23,.006),(sign*.267,1.135,.012),elbow,(sign*.282,1.065,.019),(sign*.307,.98,.025),(sign*.303,.90,.033),wrist,(sign*.315,.837,.041),(sign*.324,.807,.049)],[.065,.065,.056,.047,.044,.044,.041,.033,.031,.033,.030],skin_color,arm_weights,14)
        m.tube([shoulder,(sign*.248,1.25,.006),(sign*.265,1.21,.012)],[.073,.067,.061],shirt,{f'UpperArm.{label}':1},14)
        m.ellipsoid((sign*.183,1.338,0),(.081,.096,.087),shirt,{'Chest':.40,f'UpperArm.{label}':.60},9,16)
        m.ellipsoid((sign*.325,.814,.045),(.041,.064,.027),skin_color,{f'Hand.{label}':1},9,12)
        for finger in range(4):
            x=sign*(.303+finger*.014);m.tube([(x,.79,.047),(x,.755,.055),(x,.734+abs(finger-1.5)*.009,.070)],[.007,.006,.004],skin_color,{f'Hand.{label}':1},7)
        m.tube([(sign*.289,.825,.05),(sign*.277,.799,.065),(sign*.284,.779,.079)],[.009,.008,.005],skin_color,{f'Hand.{label}':1},7)
    armature=rig('ResidentRig',bones,root);obj=m.build('Tailored clothes and human anatomy',mat='ResidentSurface',roughness=.75);soften(obj);skin(obj,armature)
    def walk(t,p):
        phase=t*2*math.pi
        drop=-.047+.008*(1-math.cos(phase*2));translate(p['Hips'],(math.sin(phase)*.012,drop,0))
        rotate(p['Hips'],(0,0,1),math.sin(phase)*2.3);rotate(p['Spine'],(0,1,0),-math.sin(phase)*2.2)
        rotate(p['Chest'],(0,0,1),-math.sin(phase)*1.5);rotate(p['Head'],(1,0,0),.8+math.cos(phase*2)*.5)
        for label,offset,sign in [('L',0,-1),('R',.5,1)]:
            u=(t+offset)%1
            if u<.6:z=.23-(.46/.6)*u;y=.063
            else:
                swing=(u-.6)/.4;z=-.23+.46*smooth(swing);y=.063+math.sin(math.pi*swing)*.105
            hip_y=.87+drop;dy=hip_y-y;a=.425;b=.381
            d=clamp(math.sqrt(dy*dy+z*z),.15,a+b-.004)
            aim=-math.atan2(z,dy);bend=math.acos(clamp((a*a+d*d-b*b)/(2*a*d),-1,1));knee=math.pi-math.acos(clamp((a*a+b*b-d*d)/(2*a*b),-1,1))
            upper=math.degrees(aim-bend);lower=math.degrees(knee)
            rotate(p[f'Thigh.{label}'],(1,0,0),upper);rotate(p[f'Shin.{label}'],(1,0,0),lower)
            rotate(p[f'Foot.{label}'],(1,0,0),-upper-lower+(-10*smooth((u-.48)/.12) if .48<u<.6 else 0))
            rotate(p[f'Toe.{label}'],(1,0,0),8*math.sin(math.pi*clamp((u-.48)/.12)))
            swing=math.sin(phase+offset*2*math.pi)
            rotate(p[f'UpperArm.{label}'],(1,0,0),-swing*18)
            rotate(p[f'Forearm.{label}'],(1,0,0),-7-5*max(0,-swing))
            rotate(p[f'Hand.{label}'],(0,0,1),sign*3)
    def idle(t,p):
        phase=t*2*math.pi;translate(p['Hips'],(math.sin(phase)*.008,-.012,0))
        rotate(p['Chest'],(1,0,0),math.sin(phase)*.8);rotate(p['Head'],(0,1,0),math.sin(phase)*4)
        for label in ['L','R']:rotate(p[f'Forearm.{label}'],(1,0,0),-6)
    def wave(t,p):
        idle(t,p);rotate(p['UpperArm.R'],(0,0,1),100);rotate(p['Forearm.R'],(0,0,1),55)
        rotate(p['Hand.R'],(1,0,0),math.sin(t*4*math.pi)*16);rotate(p['Head'],(0,1,0),-8)
    action(armature,'Walk',36,walk);action(armature,'Idle',120,idle);action(armature,'Wave',90,wave)
    armature.animation_data.action=bpy.data.actions['Idle'];return root
