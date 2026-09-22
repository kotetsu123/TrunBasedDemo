"""Deterministic Blender 5.x authoring. Run with exec(compile(...)) through MCP.

Unity coordinates are converted to Blender (x, z, y); FBX uses -Z forward/Y up.
Only the owned VolcanoCave scene is rebuilt. The previous scene stays in the .blend.
"""
import bpy, math, random, json
from pathlib import Path
from mathutils import Vector, Quaternion

ROOT = Path('E:/UnityHub/UnityProjects/TrunBasedDemo')
SOURCE = ROOT / 'ArtSource/VolcanoCave'
OUTPUT = ROOT / 'Assets/Art/Environment/VolcanoCave'
SEED = 20260920
rng = random.Random(SEED)
layout = json.loads((SOURCE / 'layout.json').read_text(encoding='utf-8-sig'))
OUTPUT.mkdir(parents=True, exist_ok=True)

previous = bpy.context.scene
scene = bpy.data.scenes.get('VolcanoCave')
if scene:
    # Remove only generated objects. Other Blender scenes are not touched.
    for obj in list(scene.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
else:
    scene = bpy.data.scenes.new('VolcanoCave')
bpy.context.window.scene = scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1

def bpos(p): return (p[0], p[2], p[1])
def vec(d): return Vector((d['x'], d['y'], d['z']))
def material(name, color, emission=0):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    bsdf = next(n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = 0.84
    if emission:
        bsdf.inputs['Emission Color'].default_value = (*color, 1)
        bsdf.inputs['Emission Strength'].default_value = emission
    return mat

basalt = [material('VC_Basalt_'+str(i), c) for i,c in enumerate([
    (.095,.115,.145),(.14,.165,.19),(.20,.215,.235),(.12,.115,.13),(.24,.245,.25)])]
ground_mats = [material('VC_Ground_'+str(i), c) for i,c in enumerate([
    (.105,.092,.09),(.125,.114,.109),(.145,.131,.122),(.115,.105,.104)])]
lava = material('VC_Lava', (1,.11,.009), 3)
hot = material('VC_Ember', (1,.43,.03), 4)
crust = material('VC_Crust', (.055,.043,.047))

def mesh(name, vertices, faces, mats, indices=None, origin=(0,0,0)):
    data = bpy.data.meshes.new(name)
    # Swapping Y/Z changes handedness, so reverse winding too.
    data.from_pydata([bpos((p[0]-origin[0],p[1]-origin[1],p[2]-origin[2])) for p in vertices], [], [tuple(reversed(f)) for f in faces])
    data.update()
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = bpos(origin)
    for m in mats: data.materials.append(m)
    if indices:
        for poly, idx in zip(data.polygons, indices): poly.material_index = idx
    # Recalculate normals on closed meshes using bmesh, not context-dependent edit operators.
    import bmesh
    bm=bmesh.new(); bm.from_mesh(data); bmesh.ops.triangulate(bm,faces=list(bm.faces)); bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(data); bm.free()
    return obj

def rock(name, center, radii, mats=basalt, sides=7, yaw=0):
    vertices=[]; faces=[]
    for level, factor in [(0,.76),(.18,1),(.68,.88),(1,.46)]:
        for j in range(sides):
            a=2*math.pi*j/sides+yaw
            f=factor*rng.uniform(.87,1.12)
            vertices.append((center[0]+math.cos(a)*radii[0]*f,
                center[1]+(level*radii[1]) + rng.uniform(-.06,.06)*radii[1],
                center[2]+math.sin(a)*radii[2]*f))
    faces.append(tuple(reversed(range(sides))))
    for l in range(3):
        for j in range(sides):
            a=l*sides+j; b=l*sides+(j+1)%sides; c=b+sides; d=a+sides
            faces.extend([(a,b,c),(a,c,d)] if rng.random()<.35 else [(a,b,c,d)])
    faces.append(tuple(range(sides*3,sides*4)))
    return mesh(name,vertices,faces,mats,[rng.randrange(len(mats)) for _ in faces],center)

def join(objects, name):
    if not objects: return
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects: obj.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.object.join()
    objects[0].name=name
    return objects[0]

# Continuous walls follow the collider exactly at body height. Detail lives within 5 cm.
for index, wall in enumerate(layout['walls']):
    p=vec(wall['position']); s=vec(wall['scale']); c=vec(wall['center']); size=vec(wall['size'])
    qd=wall['rotation']; q=Quaternion((qd['w'],qd['x'],qd['y'],qd['z']))
    center=p+q@Vector((c.x*s.x,c.y*s.y,c.z*s.z))
    length=size.x*s.x; thickness=size.z*s.z
    outer=wall['name'].startswith('Wall_')
    n=max(3,round(length/.7)); levels=[0,.35,.95,1.6,2.15,2.65,3.0]
    heights=[rng.uniform(.92,1.14) for _ in range(n+1)]
    vertices=[]
    for side in [-1,1]:
        for k,y in enumerate(levels):
            for j in range(n+1):
                x=-length/2+length*j/n
                z=side*(thickness/2+rng.uniform(-.045,.045))
                if k==4: z*=rng.uniform(1.0,1.4)
                if k==5: z*=rng.uniform(1.3,2.5)
                if k==6: z*=rng.uniform(.3,1.1)
                world=center+q@Vector((x,y*heights[j]-center.y,z))
                vertices.append(tuple(world))
    row=n+1; half=row*len(levels); faces=[]
    for side in range(2):
        off=side*half
        for k in range(len(levels)-1):
            for j in range(n):
                a=off+k*row+j; b=a+1; d=a+row; c=d+1
                faces.extend([(a,b,c),(a,c,d)])
    for j in range(n):
        a=(len(levels)-1)*row+j; faces.append((a,a+1,a+1+half,a+half))
        faces.append((j,j+half,j+half+1,j+1))
    for j in [0,n]:
        for k in range(len(levels)-1):
            a=k*row+j; faces.append((a,a+row,a+row+half,a+half))
    wall_obj=mesh('Wall_%02d'%index,vertices,faces,basalt,[rng.choices(range(5),[2,5,3,2,1])[0] for _ in faces],tuple(center))
    # Rock outcrops above head height thicken the silhouette without narrowing the walking corridor.
    parts=[wall_obj]
    if not outer:
        for j in range(max(2,round(length/1.2))):
            v=center+q@Vector((-length/2+(j+.5)*length/max(2,round(length/1.2)),2.25-center.y,0))
            parts.append(rock('CrownPart',tuple(v),(.38,rng.uniform(.45,1.05),.32),sides=6))
        join(parts,'Wall_%02d'%index)

# Huge rock formations sit outside the original boundary, with their inner feet aligned at +/-25.
for side in range(4):
    for section in range(10):
        along=-23+section*5.1+rng.uniform(-.6,.6)
        if side<2: center=((28 if side==0 else -28),-.15,along); radii=(3.4,rng.uniform(6,10),3.3)
        else: center=(along,-.15,(28 if side==2 else -28)); radii=(3.3,rng.uniform(6,10),3.4)
        parts=[rock('MountainPart',center,radii)]
        for t in range(3):
            cc=(center[0]+rng.uniform(-1.8,1.8),rng.uniform(.2,1),center[2]+rng.uniform(-1.8,1.8))
            parts.append(rock('MountainPart',cc,(rng.uniform(1,2),rng.uniform(2.5,6),rng.uniform(1,2))))
        join(parts,'Rim_%d_%02d'%(side,section))

# Broad, nearly flat slate facets: one surface replaces the whitebox renderer without z fighting.
vertices=[]; faces=[]; n=50
for z in range(n+1):
    for x in range(n+1):
        vertices.append((-25+x+(rng.uniform(-.28,.28) if 0<x<n else 0),.015,-25+z+(rng.uniform(-.28,.28) if 0<z<n else 0)))
for z in range(n):
    for x in range(n):
        a=z*(n+1)+x; b=a+1; c=b+n+1; d=a+n+1
        faces.extend([(a,d,c),(a,c,b)])
mesh('Floor_Slate',vertices,faces,ground_mats,[rng.choices(range(4),[3,7,3,2])[0] for _ in faces])

def ribbon(name, points, widths, mat, height=.025):
    verts=[]; faces=[]
    for i,(x,z) in enumerate(points):
        a=points[max(0,i-1)]; b=points[min(len(points)-1,i+1)]
        dx=b[0]-a[0]; dz=b[1]-a[1]; mag=math.hypot(dx,dz) or 1
        w=widths[i] if isinstance(widths,list) else widths
        verts.extend([(x-dz/mag*w/2,height,z+dx/mag*w/2),(x+dz/mag*w/2,height,z-dx/mag*w/2)])
    for i in range(len(points)-1): faces.append((i*2,i*2+1,i*2+3,i*2+2))
    return mesh(name,verts,faces,[mat])

# Perimeter flows are decorative shallow seams, leaving the central gameplay route untouched.
for side in range(4):
    points=[]
    for j in range(40):
        a=-24+j*48/39; b=23.7+rng.uniform(-.3,.3)
        points.append((b if side==0 else -b,a) if side<2 else (a,b if side==2 else -b))
    ribbon('Lava_Rim_%d'%side,points,.62,lava)
    ribbon('Ember_Rim_%d'%side,points,.095,hot,.03)

# Narrow glowing fractures run at the wall foot; scattered angular rubble stays against the wall.
for index,wall in enumerate(layout['walls'][4:]):
    p=vec(wall['position']); qd=wall['rotation']; q=Quaternion((qd['w'],qd['x'],qd['y'],qd['z']))
    length=wall['scale']['x']*wall['size']['x']
    points=[]
    for j in range(max(4,round(length*2))):
        v=p+q@Vector((-length/2+length*j/(max(4,round(length*2))-1),0,.27+rng.uniform(-.02,.02)))
        points.append((v.x,v.z))
    ribbon('Lava_Fissure_%02d'%index,points,.04,lava,.025)
    pieces=[]
    for j in range(round(length)):
        v=p+q@Vector((rng.uniform(-length/2,length/2),-p.y,.10*rng.choice([-1,1])))
        pieces.append(rock('RubblePart',tuple(v),(.18,rng.uniform(.12,.4),.13),sides=5))
    join(pieces,'Rubble_%02d'%index)

# The return spawn identifies the intended approach: west face of the existing trigger.
# Center the gate on that approach, not on the trigger's large southern edge behind a wall.
exit=layout['exit']; ep=vec(exit['position'])+vec(exit['center'])
return_point=next(m for m in layout['markers'] if m['name']=='BossExitReturnSpawnPoint')
gate_x=ep.x-exit['size']['x']/2; gate_z=return_point['position']['z']; gap=4.5
def gate_point(side,height,depth): return (gate_x+depth,height,gate_z+side)
gate_parts=[]
for side in [-1,1]:
    for row in range(3):
        gate_parts.append(rock('GatePart',gate_point(side*(gap/2+.6),row*1.15,0),(1.15,1.6,.8)))
for i in range(9):
    angle=math.pi*i/8
    gate_parts.append(rock('GatePart',gate_point(math.cos(angle)*(gap/2+.45),3.3+math.sin(angle)*1.05,0),(.95,1.15,.8),yaw=angle))
join(gate_parts,'Arch_Exit')
for side in [-1,1]:
    parts=[]
    for j in range(3): parts.append(rock('TunnelPart',gate_point(side*(gap/2+.65),0,2+j*1.7),(1.15,4.1,.85)))
    join(parts,'Tunnel_'+str(side))
for j in range(3):
    parts=[rock('RoofPart',gate_point((i-1)*1.8,4.5,2+j*1.7),(1.4,1.2,1.25)) for i in range(3)]
    join(parts,'Overhang_Exit_%d'%j)

# A few overhead rock shelves in the outer corners provide a cave silhouette.
for x,z in [(-24,22),(23,23),(-24,-22),(24,-22)]:
    rock('Overhang_Corner_%d_%d'%(x,z),(x,5,z),(3,1.5,3.3))

# Export geometry only; lighting and particles are authored natively in Unity.
bpy.ops.object.select_all(action='DESELECT')
meshes=[o for o in scene.objects if o.type=='MESH']
# Bake the face palette into linear vertex colors so each rock segment needs one draw.
# Blender and Unity both read VC_Color; no texture or per-face material split is required.
shared_rock=material('VC_Rock', (1,1,1))
nodes=shared_rock.node_tree.nodes
color_node=next((n for n in nodes if n.type=='VERTEX_COLOR'),None) or nodes.new('ShaderNodeVertexColor')
color_node.layer_name='VC_Color'
shared_rock.node_tree.links.new(color_node.outputs['Color'],next(n for n in nodes if n.type=='BSDF_PRINCIPLED').inputs['Base Color'])
def linear(v): return v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4
palette={}
for i,m in enumerate(basalt):
    palette[m.name]=tuple(linear(base*.5+float(c)) for base,c in zip((.25,.27,.30),m.diffuse_color[:3]))+(1,)
for i,m in enumerate(ground_mats):
    palette[m.name]=tuple(linear(c+i*.003) for c in (.22,.205,.195))+(1,)
palette[crust.name]=tuple(linear(c) for c in crust.diffuse_color[:3])+(1,)
original_slots=sum(len(o.data.materials) for o in meshes)
for obj in meshes:
    data=obj.data
    if not all(m.name in palette for m in data.materials): continue
    attr=data.color_attributes.new(name='VC_Color',type='FLOAT_COLOR',domain='CORNER')
    colors=[palette[m.name] for m in data.materials]
    for poly in data.polygons:
        color=colors[poly.material_index]
        for loop in poly.loop_indices: attr.data[loop].color=color
        poly.material_index=0
    data.materials.clear(); data.materials.append(shared_rock)
for o in meshes: o.select_set(True)
bpy.context.view_layer.objects.active=meshes[0]
bpy.ops.export_scene.fbx(filepath=str(OUTPUT/'VolcanoCave.fbx'),use_selection=True,object_types={'MESH'},
    axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',
    bake_space_transform=True,use_mesh_modifiers=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,colors_type='LINEAR')

# Studio cameras are kept in the Blender source, outside the exported geometry.
world=bpy.data.worlds.new('VC_CaveWorld'); scene.world=world
world.color=(.08,.10,.15)
def aim(obj, point): obj.rotation_euler=(Vector(bpos(point))-obj.location).to_track_quat('-Z','Y').to_euler()
for name,loc,energy,size,color in [('Key',(-12,30,-8),18000,35,(.67,.77,1)),('Fill',(16,20,18),12000,25,(1,.4,.15))]:
    ld=bpy.data.lights.new('VC_'+name,'AREA'); ld.energy=energy; ld.shape='DISK'; ld.size=size; ld.color=color
    ob=bpy.data.objects.new('VC_'+name,ld); scene.collection.objects.link(ob); ob.location=bpos(loc); aim(ob,(0,0,0))
for name,location,target in [('Overview',(35,48,-46),(0,0,0)),('Entrance',(0,5.3,-23),(0,1,-15)),('Exit',(gate_x-7,4.5,gate_z),(gate_x+1,2,gate_z))]:
    cd=bpy.data.cameras.new('VC_'+name); co=bpy.data.objects.new('VC_'+name,cd); scene.collection.objects.link(co)
    co.location=bpos(location); aim(co,target); cd.lens=32 if name!='Overview' else 40
    if name=='Overview': scene.camera=co
scene.render.resolution_x=1440; scene.render.resolution_y=1080; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.filepath=str(SOURCE/'blender-overview.png')
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.shading.type='MATERIAL'
            area.spaces.active.region_3d.view_distance=70
            area.spaces.active.region_3d.view_location=(0,0,0)
            area.spaces.active.region_3d.view_rotation=scene.camera.rotation_euler.to_quaternion()
triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
manifest={'seed':SEED,'blender_version':bpy.app.version_string,'mesh_count':len(meshes),'triangles':triangles,
          'exit_arch_center':[gate_x,0,gate_z], 'wall_count':len(layout['walls']),
          'material_slots_before':original_slots,'material_slots_after':sum(len(o.data.materials) for o in meshes)}
(SOURCE/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'VolcanoCave.blend'))
print(json.dumps(manifest))
