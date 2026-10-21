"""editor-tooling-codex: editable CC0 Tank2 derivative, Blender 4.5.
Run: blender --background --factory-startup --python ArtSource/HeavyTank/build_heavy.py
Unity export/import: Fortress > Heavy Tank. Coordinates in helpers are Unity mount-local metres.
"""
import bpy, json, math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
materials = {}
palette = {'Main': (.25,.29,.17), 'Main_Dark':(.1625,.1885,.1105),
           'Main_Light':(.295,.3422,.2006), 'Main_Details':(.1,.115,.1),
           'Wheels':(.1,.115,.1), 'Heavy_Lens':(.12,.32,.36),
           'Heavy_Mark':(.78,.73,.52)}
for name, color in palette.items():
    mat=bpy.data.materials.new(name); mat.diffuse_color=(*color,1);mat.use_nodes=True
    bsdf=mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value=(*color,1)
    bsdf.inputs['Metallic'].default_value=.35
    bsdf.inputs['Roughness'].default_value=.78 if 'Lens' not in name else .3
    materials[name]=mat

def coord(v): return (v[0],v[2],v[1])
def tag(obj, mount, material):
    obj['mount']=mount
    obj.data.materials.append(materials[material])
    obj.location.z += 1.8 if mount else 0
    return obj

# Base meshes are the exact normalized meshes already used by the game.
data=json.loads((HERE/'main-source.json').read_text(encoding='utf-8-sig'))
for item in data['pieces']:
    a=item['vertices']; verts=[coord(a[i:i+3]) for i in range(0,len(a),3)]
    faces=[]; indexes=[]
    for index, sub in enumerate(item['submeshes']):
        t=sub['triangles']
        for i in range(0,len(t),3): faces.append((t[i+2],t[i+1],t[i])); indexes.append(index)
    mesh=bpy.data.meshes.new(item['name']);mesh.from_pydata(verts,[],faces);mesh.update()
    obj=bpy.data.objects.new(item['name'].replace('Main_','Base_'),mesh);bpy.context.collection.objects.link(obj)
    obj['mount']=item['mount'];obj.location.z=1.8 if item['mount'] else 0
    for name in item['materials']:mesh.materials.append(materials[name])
    for poly,index in zip(mesh.polygons,indexes):poly.material_index=index
    if 'Track' in obj.name: # Wider tracks only; existing body and aim pivots unchanged.
        for vertex in mesh.vertices:vertex.co.x*=1.08

def box(name, pos, size, material='Main', mount=0, tilt=0, bevel=.025):
    bpy.ops.mesh.primitive_cube_add(size=1,location=coord(pos))
    obj=bpy.context.object;obj.name=name;obj.dimensions=coord(size)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    obj.rotation_euler.x=math.radians(tilt)
    tag(obj,mount,material)
    if bevel:
        mod=obj.modifiers.new('Machined edges','BEVEL');mod.width=bevel;mod.segments=2
        obj.modifiers.new('Weighted face normals','WEIGHTED_NORMAL')
    return obj

def cylinder(name, pos, radius, depth, material='Main_Details', mount=0, rotation=(0,0,0), vertices=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=coord(pos))
    obj=bpy.context.object;obj.name=name;obj.rotation_euler=tuple(math.radians(x) for x in rotation)
    return tag(obj,mount,material)

# Segmented side skirts: lower track edges remain exposed, with visible air gaps.
for side in (-1,1):
    for n in range(5):
        z=-1.7+n*.83
        box(f'Skirt backing {side} {n}',(side*1.94,.94,z),(.12,.75,.76),'Main_Dark')
        box(f'Spaced side armor {side} {n}',(side*2.02,1.05,z),(.13,.56,.7),'Main',bevel=.04)
        box(f'Skirt upper rail {side} {n}',(side*1.99,1.4,z),(.2,.1,.75),'Main_Light')
        for dz in (-.24,.24):
            cylinder(f'Skirt bolt {side} {n} {dz}',(side*2.095,1.1,z+dz),.037,.024,rotation=(0,90,0),vertices=8)
    box(f'Front mudguard {side}',(side*1.65,1.45,2.05),(.85,.1,.5),'Main_Light',tilt=-12)
    box(f'Rear fuel container {side}',(side*1.08,1.55,-1.78),(.6,.34,.64),'Main_Dark')
    for dz in (-.22,.22):box(f'Fuel strap {side} {dz}',(side*1.08,1.73,-1.78+dz),(.63,.035,.05),'Main_Details',bevel=.006)
    box(f'Headlight housing {side}',(side*1.3,1.24,2.13),(.3,.17,.13),'Main_Details')
    box(f'Headlight glass {side}',(side*1.3,1.24,2.205),(.22,.09,.015),'Heavy_Lens',bevel=.005)
    cylinder(f'Towing lug {side}',(side*.95,.65,2.22),.12,.12,'Main_Details',rotation=(90,0,0))

# Layered glacis and engine deck louvers.
box('Glacis armor',(0,1.3,1.65),(2.45,.16,.8),'Main_Dark',tilt=12,bevel=.04)
for row in range(2):
    for col in range(5):box(f'Glacis tile {row} {col}',(-.94+col*.47,1.43+row*.07,1.88-row*.35),(.42,.12,.3),'Main',tilt=12,bevel=.015)
for n in range(9):box(f'Engine louver {n}',(0,1.49,-1.92+n*.085),(.85,.03,.035),'Main_Details',bevel=.005)

# Turret add-on armor stays on turret pivot, with a clear central gun channel.
for side in (-1,1):
    for n in range(3):
        box(f'Turret cheek backing {side} {n}',(side*(.55+n*.34),.05,1.48),(.33,.55,.21),'Main_Dark',1,tilt=18)
        for row in range(2):
            box(f'Turret cheek tile {side} {n} {row}',(side*(.55+n*.34),-.08+row*.26,1.61-row*.08),(.29,.22,.12),'Main_Light',1,tilt=18,bevel=.015)
    for n in range(4):
        box(f'Turret flank tile {side} {n}',(side*1.46,.08,-.96+n*.55),(.18,.48,.49),'Main',1,bevel=.035)
    for n in range(3):
        cylinder(f'Smoke launcher {side} {n}',(side*1.38,.42,.4-n*.24),.075,.28,'Main_Details',1,rotation=(25,side*40,0))
    box(f'Turret bustle rack rail {side}',(side*1.18,.06,-1.71),(.09,.46,.5),'Main_Details',1)
box('Bustle equipment box',(0,.06,-1.73),(2.26,.42,.42),'Main_Dark',1,bevel=.045)
for x in (-.8,0,.8):box(f'Rack band {x}',(x,.07,-1.96),(.055,.46,.035),'Main_Light',1,bevel=.006)
cylinder('Commander hatch',(.61,.47,-.3),.38,.12,'Main_Dark',1,vertices=24)
box('Hatch handle',(.61,.56,-.35),(.2,.06,.06),'Main_Details',1,bevel=.01)
box('Optic pedestal',(-.62,.48,.3),(.44,.2,.42),'Main_Dark',1)
box('Optic window',(-.62,.5,.52),(.3,.09,.025),'Heavy_Lens',1,bevel=.008)
cylinder('Antenna base',(-1.02,.46,-.85),.085,.12,'Main_Details',1)
cylinder('Antenna whip',(-1.02,.96,-.85),.012,.95,'Main_Details',1,vertices=8)
box('Recognition stripe left',(-.98,.44,.48),(.12,.025,.9),'Heavy_Mark',1,bevel=.004)
box('Recognition stripe right',(-.79,.44,.48),(.07,.025,.9),'Heavy_Mark',1,bevel=.004)

# Barrel sleeve and collars move with recoil. Do not extend the 3.8 m muzzle.
cylinder('Thermal sleeve',(0,0,1.47),.208,1.7,'Main',2,rotation=(90,0,0),vertices=16)
for z in (.64,2.3):cylinder(f'Sleeve collar {z}',(0,0,z),.228,.11,'Main_Dark',2,rotation=(90,0,0),vertices=16)

exec(compile((HERE/'export_heavy.py').read_text(encoding='utf-8-sig'),str(HERE/'export_heavy.py'),'exec'))

# Studio belongs to its own collection and is excluded from FBX/runtime exports.
studio=bpy.data.collections.new('STUDIO - presentation only');bpy.context.scene.collection.children.link(studio)
def studio_obj(obj):
    for collection in list(obj.users_collection):collection.objects.unlink(obj)
    studio.objects.link(obj)
bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;floor.name='Studio floor';studio_obj(floor)
floor_mat=bpy.data.materials.new('Studio slate');floor_mat.diffuse_color=(.045,.055,.065,1);floor.data.materials.append(floor_mat)
def aim(obj, point):obj.rotation_euler=(Vector(point)-obj.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(8,10,6));camera=bpy.context.object;studio_obj(camera);aim(camera,(0,.3,1.6));camera.data.type='ORTHO';camera.data.ortho_scale=8.3;bpy.context.scene.camera=camera
for name,position,power,size,color in [('Key',(2,5,9),1700,7,(1,.91,.78)),('Fill',(-6,1,5),1250,6,(.66,.8,1)),('Rim',(1,-6,7),2000,5,(1,.79,.53))]:
    bpy.ops.object.light_add(type='AREA',location=position);light=bpy.context.object;light.name=name;studio_obj(light);light.data.energy=power;light.data.shape='DISK';light.data.size=size;light.data.color=color;aim(light,(0,0,1))
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.world.color=(.16,.16,.16);scene.view_settings.view_transform='AgX';scene.render.image_settings.file_format='PNG'
scene.render.resolution_x=1600;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.render.filepath=str(HERE/'HeavyTank-preview.png')
bpy.ops.object.select_all(action='DESELECT')
models[0].select_set(True);bpy.context.view_layer.objects.active=models[0]
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.shading.color_type='MATERIAL'
            area.spaces.active.region_3d.view_perspective='CAMERA'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'HeavyTank.blend'))
bpy.ops.render.render(write_still=True)
scene.render.resolution_x=1024;scene.render.resolution_y=512;camera.data.ortho_scale=9.6
scene.render.filepath=str(ROOT/'Assets/Fortress/Art/TankRoles/HeavyPortrait.png');bpy.ops.render.render(write_still=True)
print('HEAVY_EXPORT',{'objects':len(models),'triangles':sum(len(p['vertices'])//9 for p in pieces),'mounts':len(pieces)})
