"""Build a separate lava ribbon against the saved volcano mesh in Blender."""

import hashlib
import json
import math
from pathlib import Path

import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree


SOURCE = Path(__file__).resolve().parent
ROOT = SOURCE.parents[1]
REFERENCE = ROOT / "ArtSource/BossVolcanoGraybox/BossVolcano_Graybox.blend"
OUTPUT = ROOT / "Assets/Art/Environment/VolcanoCave/BossVolcano_LavaFlow.fbx"
ROWS = 121
COLUMNS = 9
SURFACE_OFFSET = 0.07


def load_volcano(scene):
    # 仅从原文件读取山体，所有新内容都写入独立文件。
    with bpy.data.libraries.load(str(REFERENCE), link=False) as (source, target):
        assert "BossVolcano_Graybox" in source.objects
        target.objects = ["BossVolcano_Graybox"]
    volcano = target.objects[0]
    scene.collection.objects.link(volcano)
    points = [volcano.matrix_world @ v.co for v in volcano.data.vertices]
    faces = [list(p.vertices) for p in volcano.data.polygons]
    return volcano, BVHTree.FromPolygons(points, faces)


def make_material():
    mat = bpy.data.materials.new("BossVolcano_Lava")
    mat.use_nodes = True
    mat.diffuse_color = (0.874, 0.255, 0, 1)
    shader = mat.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = mat.diffuse_color
    shader.inputs["Roughness"].default_value = 0.65
    shader.inputs["Emission Color"].default_value = (1, 0.12, 0.005, 1)
    shader.inputs["Emission Strength"].default_value = 2
    return mat


def build_ribbon(scene, surface):
    vertices, faces, uv_coords = [], [], []
    normals = []
    arc_length = 0.0
    last_center = None
    for row in range(ROWS):
        t = row / (ROWS - 1)
        # Blender 的 -Y 对应此项目 Unity 模型的 -Z，即玩家看向的山体正面。
        height = 23.5 - 14.05 * t
        # 入口镜头的右侧坡面：向下逐渐偏右，横向小弯折保留自然轮廓。
        center_x = 1.8 + 5.0 * t + 0.45 * math.sin(t * 5.3) + 0.15 * math.sin(t * 12)
        taper = 1 - 0.92 * max(0, (t - 0.86) / 0.14) ** 1.5
        width = (0.95 + 0.38 * math.sin(t * 7 + 0.5) ** 2) * taper
        left = -0.5 * width * (1 + 0.12 * math.sin(t * 41))
        right = 0.5 * width * (1 + 0.15 * math.sin(t * 34 + 1.1))
        center = None
        for column in range(COLUMNS):
            u = column / (COLUMNS - 1)
            x = center_x + left * (1 - u) + right * u
            hit, normal, _, _ = surface.ray_cast(Vector((x, -80, height)), Vector((0, 1, 0)))
            assert hit is not None, f"Ribbon misses mountain at row {row}, column {column}"
            assert normal.y < -0.1, "Hit must be on the outward front slope"
            # 沿外法线抬高少许，避免熔岩与山体表面重叠闪烁。
            vertices.append(hit + normal * SURFACE_OFFSET)
            normals.append(normal)
            if column == COLUMNS // 2:
                center = hit
        if last_center is not None:
            arc_length += (center - last_center).length
        last_center = center
        # U 横跨河道，V 沿下坡方向递增；以后可直接做流动纹理。
        uv_coords.extend((column / (COLUMNS - 1), arc_length / 3) for column in range(COLUMNS))

    for row in range(ROWS - 1):
        for column in range(COLUMNS - 1):
            a = row * COLUMNS + column
            for face in ((a, a + COLUMNS, a + COLUMNS + 1), (a, a + COLUMNS + 1, a + 1)):
                va, vb, vc = [vertices[i] for i in face]
                normal = (vb - va).cross(vc - va)
                faces.append(face if normal.dot(normals[a]) > 0 else tuple(reversed(face)))

    mesh = bpy.data.meshes.new("BossVolcano_LavaFlow_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    uv = mesh.uv_layers.new(name="UVMap")
    for poly in mesh.polygons:
        assert poly.area > 0.000001, "Degenerate face"
        for loop_index in poly.loop_indices:
            uv.data[loop_index].uv = uv_coords[mesh.loops[loop_index].vertex_index]
    obj = bpy.data.objects.new("BossVolcano_LavaFlow", mesh)
    scene.collection.objects.link(obj)
    mesh.materials.append(make_material())
    return obj


def validate_surface(ribbon, surface):
    minimum = float("inf")
    maximum = 0.0
    # 检查顶点和三角形内部，避免端点贴合但中间穿进山体。
    for poly in ribbon.data.polygons:
        points = [ribbon.data.vertices[i].co for i in poly.vertices]
        samples = points + [sum(points, Vector()) / 3]
        for point in samples:
            hit, normal, _, distance = surface.find_nearest(point)
            signed = (point - hit).dot(normal)
            assert signed > 0.008, f"Ribbon intersects mountain: {signed}"
            assert distance < 0.16, f"Ribbon floats away from slope: {distance}"
            minimum, maximum = min(minimum, distance), max(maximum, distance)
    return minimum, maximum


def aim_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def preview_setup(scene):
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1100, 760
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.world = bpy.data.worlds.new("LavaPreviewWorld")
    scene.world.use_nodes = True
    background = scene.world.node_tree.nodes.get("Background")
    background.inputs["Color"].default_value = (0.21, 0.25, 0.3, 1)
    background.inputs["Strength"].default_value = 0.6
    for name, location, energy in [("PreviewKey", (-20, -32, 45), 19000), ("PreviewFill", (25, -10, 25), 9000)]:
        data = bpy.data.lights.new(name, "AREA")
        data.energy, data.shape, data.size = energy, "DISK", 25
        obj = bpy.data.objects.new(name, data)
        scene.collection.objects.link(obj)
        obj.location = location
        aim_at(obj, (0, 0, 12))
    camera = bpy.data.objects.new("PreviewCamera", bpy.data.cameras.new("PreviewCamera"))
    scene.collection.objects.link(camera)
    camera.data.clip_end = 300
    camera.data.lens = 45
    scene.camera = camera
    return camera


def verify_export(ribbon):
    # FBX 回读检查世界坐标、UV 和面数；不能用它代替 Unity 实机检查。
    original = sorted(tuple(ribbon.matrix_world @ v.co) for v in ribbon.data.vertices)
    existing = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(OUTPUT))
    imported = [o for o in bpy.data.objects if o not in existing]
    meshes = [o for o in imported if o.type == "MESH"]
    assert len(meshes) == 1
    other = meshes[0]
    assert len(other.data.vertices) == len(original)
    assert len(other.data.polygons) == len(ribbon.data.polygons)
    assert len(other.data.uv_layers) == 1
    assert len(other.data.materials) == 1
    actual = sorted(tuple(other.matrix_world @ v.co) for v in other.data.vertices)
    error = max((Vector(a) - Vector(b)).length for a, b in zip(original, actual))
    assert error < 0.001, f"FBX coordinates changed: {error}"
    for obj in imported:
        bpy.data.objects.remove(obj, do_unlink=True)
    return error


def main():
    if not bpy.app.background or bpy.data.filepath:
        raise RuntimeError("Run in a new background Blender process with --factory-startup")
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    scene = bpy.context.scene
    volcano, surface = load_volcano(scene)
    ribbon = build_ribbon(scene, surface)
    minimum, maximum = validate_surface(ribbon, surface)
    for obj in scene.objects:
        obj.select_set(obj == ribbon)
    bpy.context.view_layer.objects.active = ribbon
    bpy.ops.export_scene.fbx(filepath=str(OUTPUT), use_selection=True, object_types={"MESH"},
        axis_forward="-Z", axis_up="Y", apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
        bake_space_transform=True, mesh_smooth_type="FACE", add_leaf_bones=False, bake_anim=False)
    error = verify_export(ribbon)
    camera = preview_setup(scene)
    for name, position in [("preview_front", (0, -70, 4)), ("preview_side", (30, -58, 24))]:
        camera.location = position
        aim_at(camera, (0, 0, 12))
        scene.render.filepath = str(SOURCE / (name + ".png"))
        bpy.ops.render.render(write_still=True)
    camera.location = (0, -70, 4)
    aim_at(camera, (0, 0, 12))
    for obj in scene.objects:
        obj.select_set(obj == ribbon)
    bpy.context.view_layer.objects.active = ribbon
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == "VIEW_3D":
                view = area.spaces.active.region_3d
                view.view_distance, view.view_location = 48, (0, -7, 14)
                view.view_rotation = camera.rotation_euler.to_quaternion()
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / "BossVolcano_LavaFlow.blend"), compress=True)
    report = {
        "reference": str(REFERENCE.relative_to(ROOT)),
        "reference_sha256": hashlib.sha256(REFERENCE.read_bytes()).hexdigest(),
        "vertices": len(ribbon.data.vertices), "triangles": len(ribbon.data.polygons),
        "material_slots": 1, "uv_layers": 1, "front_unity": "-Z", "placement": "front-right slope",
        "surface_distance_min": minimum, "surface_distance_max": maximum,
        "fbx_roundtrip_max_error": error, "unity_playmode_verified": False,
        "fbx": str(OUTPUT.relative_to(ROOT)),
    }
    (SOURCE / "manifest.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report))


if __name__ == "__main__":
    main()
