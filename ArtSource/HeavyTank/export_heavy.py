"""Export the current edited .blend without regenerating its geometry. editor-tooling-codex."""
import bpy, json
from pathlib import Path
HERE=Path(__file__).resolve().parent
models=[o for o in bpy.context.scene.objects if o.type=='MESH' and 'mount' in o]
# Bake a compact runtime mesh per mount, retaining hard/beveled split normals.
deps=bpy.context.evaluated_depsgraph_get(); pieces=[]
for mount in range(3):
    output={'name':'Heavy_'+('Hull','Turret','Gun')[mount],'mount':mount,'vertices':[],'normals':[],'uv':[],'materials':[],'submeshes':[]}
    for obj in models:
        if obj['mount']!=mount:continue
        evaluated=obj.evaluated_get(deps);mesh=evaluated.to_mesh();mesh.calc_loop_triangles()
        normal_matrix=obj.matrix_world.to_3x3().inverted().transposed()
        for tri in mesh.loop_triangles:
            name=mesh.materials[tri.material_index].name
            if name not in output['materials']:output['materials'].append(name);output['submeshes'].append({'triangles':[]})
            indices=[]
            for loop_idx in reversed(tri.loops):
                loop=mesh.loops[loop_idx];v=obj.matrix_world@mesh.vertices[loop.vertex_index].co
                v.z-=1.8 if mount else 0
                normal=(normal_matrix@mesh.corner_normals[loop_idx].vector).normalized()
                indices.append(len(output['vertices'])//3)
                output['vertices'].extend((v.x,v.z,v.y));output['normals'].extend((normal.x,normal.z,normal.y));output['uv'].extend((0,0))
            output['submeshes'][output['materials'].index(name)]['triangles'].extend(indices)
        evaluated.to_mesh_clear()
    pieces.append(output)
(HERE/'heavy-runtime.json').write_text(json.dumps({'pieces':pieces},separators=(',',':')),encoding='utf-8')

bpy.ops.object.select_all(action='DESELECT')
for obj in models:obj.select_set(True)
bpy.context.view_layer.objects.active=models[0]
bpy.ops.export_scene.fbx(filepath=str(HERE/'HeavyTank.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,bake_anim=False,axis_forward='-Z',axis_up='Y')


print('HEAVY_EXPORT', {'objects':len(models),'triangles':sum(len(p['vertices'])//9 for p in pieces),'mounts':len(pieces)})
