"""Generate a volcano greybox in a fresh background Blender process."""

import json
import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector

SOURCE = Path(__file__).resolve().parent
ROOT = SOURCE.parents[1]
OUTPUT = ROOT / "Assets/Art/Environment/VolcanoCave/BossVolcano_Graybox.fbx"
SEED = 20261005
SEGMENTS = 32


def aim_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def material(name, color):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    shader = next(n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    shader.inputs["Base Color"].default_value = (*color, 1)
    shader.inputs["Roughness"].default_value = 0.92
    return mat


def build_model(scene):
    rng = random.Random(SEED)
    # 从山脚向外侧山口上升，再向内下降到坑底；每组数值是半径和高度。
    rings = [(20, 0), (17, 2.5), (13, 7), (9, 14), (5.2, 24), (3.8, 23.6), (2.2, 17.5)]
    angles = [2 * math.pi * i / SEGMENTS for i in range(SEGMENTS)]
    ridges = [1 + 0.08 * math.sin(a * 5 + 0.6) + rng.uniform(-0.025, 0.025) for a in angles]
    rim = [0.55 * math.sin(a * 3 + 0.3) + rng.uniform(-0.18, 0.18) for a in angles]
    vertices, faces = [], []
    for ring_index, (radius, height) in enumerate(rings):
        for i, angle in enumerate(angles):
            # 山脊沿高度延续，轻微不规则让轮廓保留岩石感。
            r = radius * ridges[i] * (1 + rng.uniform(-0.018, 0.018))
            z = height + (rim[i] if ring_index in (4, 5) else 0)
            if ring_index in (1, 2, 3):
                z += rng.uniform(-0.3, 0.3)
            vertices.append((r * math.cos(angle), r * math.sin(angle) * 0.85, z))
    for ring_index in range(len(rings) - 1):
        for i in range(SEGMENTS):
            j = (i + 1) % SEGMENTS
            a, b = ring_index * SEGMENTS + i, ring_index * SEGMENTS + j
            c, d = b + SEGMENTS, a + SEGMENTS
            faces.extend(((a, b, c), (a, c, d)) if (ring_index + i) % 2 else ((a, b, d), (b, c, d)))
    bottom, floor = len(vertices), len(vertices) + 1
    vertices.extend([(0, 0, 0), (0, 0, 17.1)])
    last_ring = (len(rings) - 1) * SEGMENTS
    for i in range(SEGMENTS):
        j = (i + 1) % SEGMENTS
        faces.extend(((bottom, j, i), (last_ring + i, last_ring + j, floor)))
    mesh = bpy.data.meshes.new("BossVolcano_Graybox_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    # 确认闭合、没有退化面，且法线朝外。
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    assert all(e.is_manifold for e in bm.edges), "Mesh is not closed"
    assert all(f.calc_area() > 0.00001 for f in bm.faces), "Degenerate face"
    assert bm.calc_volume(signed=True) > 0, "Normals face inward"
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("BossVolcano_Graybox", mesh)
    scene.collection.objects.link(obj)
    mesh.materials.append(material("BossVolcano_Gray", (0.32, 0.34, 0.37)))
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    return obj


def preview_setup(scene):
    # 这些地板、灯光和相机只用于源文件预览，不导出到 FBX。
    mesh = bpy.data.meshes.new("PreviewGround")
    mesh.from_pydata([(-60, -60, -0.05), (60, -60, -0.05), (60, 60, -0.05), (-60, 60, -0.05)], [], [(0, 1, 2, 3)])
    mesh.update()
    ground = bpy.data.objects.new("PreviewGround", mesh)
    scene.collection.objects.link(ground)
    mesh.materials.append(material("PreviewGround_Gray", (0.21, 0.23, 0.26)))
    scene.world = bpy.data.worlds.new("PreviewWorld")
    scene.world.use_nodes = True
    node = next(n for n in scene.world.node_tree.nodes if n.type == "BACKGROUND")
    node.inputs["Color"].default_value = (0.3, 0.34, 0.4, 1)
    node.inputs["Strength"].default_value = 0.5
    for name, position, power, size in [("PreviewKey", (-22, -30, 50), 23000, 25), ("PreviewFill", (25, 4, 30), 12000, 22)]:
        data = bpy.data.lights.new(name, "AREA")
        data.energy, data.size = power, size
        obj = bpy.data.objects.new(name, data)
        scene.collection.objects.link(obj)
        obj.location = position
        aim_at(obj, (0, 0, 10))
    camera = bpy.data.objects.new("PreviewCamera", bpy.data.cameras.new("PreviewCamera"))
    scene.collection.objects.link(camera)
    camera.data.clip_end = 300
    scene.camera = camera
    scene.render.resolution_x, scene.render.resolution_y = 1100, 800
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    return camera


def main():
    # 仅允许新开的后台进程执行，不清理用户正在编辑的 Blender 文件。
    if not bpy.app.background or bpy.data.filepath:
        raise RuntimeError("Use blender --background --factory-startup --python generate.py")
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    scene = bpy.context.scene
    scene.name = "BossVolcanoGraybox"
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    model = build_model(scene)
    bpy.ops.export_scene.fbx(filepath=str(OUTPUT), use_selection=True, object_types={"MESH"},
        axis_forward="-Z", axis_up="Y", apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
        bake_space_transform=True, mesh_smooth_type="FACE", add_leaf_bones=False, bake_anim=False)
    camera = preview_setup(scene)
    for name, position, lens in [("preview", (52, -64, 43), 52), ("preview_front", (0, -75, 6), 42)]:
        camera.location, camera.data.lens = position, lens
        aim_at(camera, (0, 0, 9))
        scene.render.filepath = str(SOURCE / (name + ".png"))
        bpy.ops.render.render(write_still=True)
    camera.location, camera.data.lens = (52, -64, 43), 52
    aim_at(camera, (0, 0, 9))
    for obj in scene.objects:
        obj.select_set(obj == model)
    bpy.context.view_layer.objects.active = model
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == "VIEW_3D":
                view = area.spaces.active.region_3d
                view.view_distance, view.view_location = 65, (0, 0, 10)
                view.view_rotation = camera.rotation_euler.to_quaternion()
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / "BossVolcano_Graybox.blend"), compress=True)
    report = {"seed": SEED, "blender_version": bpy.app.version_string, "mesh_count": 1,
        "vertices": len(model.data.vertices), "triangles": len(model.data.polygons),
        "dimensions_blender_xyz": list(model.dimensions), "pivot": "bottom center",
        "material_slots": 1, "closed_manifold": True, "fbx": str(OUTPUT.relative_to(ROOT))}
    (SOURCE / "manifest.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report))


if __name__ == "__main__":
    main()
