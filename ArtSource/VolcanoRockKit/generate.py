"""Extract reusable rocks from the cave source without changing that source."""

import hashlib
import json
from pathlib import Path
import uuid

import bpy
from mathutils import Vector


SOURCE = Path(__file__).resolve().parent
ROOT = SOURCE.parents[1]
REFERENCE = ROOT / "ArtSource/VolcanoCave/VolcanoCave.blend"
REFERENCE_FBX = ROOT / "Assets/Art/Environment/VolcanoCave/VolcanoCave.fbx"
OUTPUT = ROOT / "Assets/Art/Environment/VolcanoCave/Rocks"
MATERIAL_GUID = "0cf8dea180300924c8181e030014c88f"
SPECS = (
    ("VC_Boulder_A", "Rim_0_00", "all", 5.8, "height"),
    ("VC_Boulder_B", "Rim_1_02", "all", 4.0, "height"),
    ("VC_Rock_Small_A", "Rim_0_08", "secondary", 1.6, "height"),
    ("VC_Rock_Small_B", "Overhang_Corner_24_-22", "all", 1.7, "width"),
    ("VC_Rubble_A", "Rubble_00", "all", None, None),
    ("VC_Rubble_B", "Rubble_03", "all", None, None),
)


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def bounds(points):
    return (Vector(tuple(min(p[i] for p in points) for i in range(3))),
            Vector(tuple(max(p[i] for p in points) for i in range(3))))


def components(mesh):
    adjacent = [set() for _ in mesh.vertices]
    for edge in mesh.edges:
        a, b = edge.vertices
        adjacent[a].add(b)
        adjacent[b].add(a)
    unseen = set(range(len(mesh.vertices)))
    groups = []
    while unseen:
        pending = [min(unseen)]
        group = set()
        while pending:
            index = pending.pop()
            if index in group:
                continue
            group.add(index)
            pending.extend(adjacent[index] - group)
        unseen.difference_update(group)
        groups.append(group)
    return groups


def extract(source, name, selection, target_size, size_axis):
    mesh = source.data
    world = [source.matrix_world @ vertex.co for vertex in mesh.vertices]
    groups = components(mesh)
    selected = set(range(len(world)))
    if selection == "secondary":
        # The largest piece is the rim's main pillar; take a smaller piece beside it.
        groups.sort(key=lambda group: bounds([world[i] for i in group])[1].z -
                    bounds([world[i] for i in group])[0].z, reverse=True)
        selected = groups[1]
    indices = sorted(selected)
    low, high = bounds([world[i] for i in indices])
    extent = high - low
    scale = target_size / (extent.z if size_axis == "height" else extent.x) if target_size else 1
    pivot = Vector(((low.x + high.x) / 2, (low.y + high.y) / 2, low.z))
    remap = {index: new for new, index in enumerate(indices)}
    polygons = [p for p in mesh.polygons if all(i in selected for i in p.vertices)]
    result = bpy.data.meshes.new(name + "_Mesh")
    result.from_pydata([(world[i] - pivot) * scale for i in indices], [],
                       [tuple(remap[i] for i in p.vertices) for p in polygons])
    result.update()
    source_color = mesh.color_attributes["VC_Color"]
    color = result.color_attributes.new(name="VC_Color", type="FLOAT_COLOR", domain="CORNER")
    for original, copied in zip(polygons, result.polygons):
        copied.use_smooth = original.use_smooth
        for old_loop, new_loop in zip(original.loop_indices, copied.loop_indices):
            color.data[new_loop].color = source_color.data[old_loop].color
    result.color_attributes.active_color = color
    result.materials.append(mesh.materials[0])
    obj = bpy.data.objects.new(name, result)
    bpy.context.scene.collection.objects.link(obj)
    obj["source_object"] = source.name
    obj["source_selection"] = selection
    obj["pivot"] = "bottom center"
    assert len(result.materials) == 1 and result.materials[0].name == "VC_Rock"
    assert all(p.area > 1e-8 for p in result.polygons)
    return obj


def write_meta_if_missing(path, body):
    if path.exists():
        return
    identity = uuid.uuid5(uuid.NAMESPACE_URL, path.relative_to(ROOT).as_posix()).hex
    path.write_text("fileFormatVersion: 2\nguid: " + identity + "\n" + body, encoding="utf-8")


def importer_meta(path):
    write_meta_if_missing(path, f"""ModelImporter:
  serializedVersion: 22200
  internalIDToNameTable: []
  externalObjects:
  - first:
      type: UnityEngine:Material
      assembly: UnityEngine.CoreModule
      name: VC_Rock
    second: {{fileID: 2100000, guid: {MATERIAL_GUID}, type: 2}}
  materials:
    materialImportMode: 1
    materialName: 0
    materialSearch: 1
    materialLocation: 1
  meshes:
    globalScale: 1
    meshCompression: 0
    addColliders: 0
    useSRGBMaterialColor: 1
    sortHierarchyByName: 1
    importVisibility: 1
    importBlendShapes: 0
    importCameras: 0
    importLights: 0
    fileIdsGeneration: 2
    swapUVChannels: 0
    generateSecondaryUV: 0
    useFileUnits: 1
    weldVertices: 1
    bakeAxisConversion: 1
    preserveHierarchy: 0
    indexFormat: 0
    useFileScale: 1
  tangentSpace:
    normalSmoothAngle: 60
    normalImportMode: 0
    tangentImportMode: 3
    normalCalculationMode: 4
  importAnimation: 0
  animationType: 2
  isReadable: 0
  userData:
  assetBundleName:
  assetBundleVariant:
""")


def verify_export(obj, path):
    expected = sorted(tuple(v.co) for v in obj.data.vertices)
    expected_colors = sorted(tuple(c.color) for c in obj.data.color_attributes["VC_Color"].data)
    existing = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(path), colors_type="LINEAR")
    imported = [o for o in bpy.data.objects if o not in existing]
    meshes = [o for o in imported if o.type == "MESH"]
    assert len(meshes) == 1, "Export contains unexpected objects"
    other = meshes[0]
    actual = sorted(tuple(other.matrix_world @ v.co) for v in other.data.vertices)
    assert len(actual) == len(expected)
    assert len(other.data.polygons) == len(obj.data.polygons)
    error = max((Vector(a) - Vector(b)).length for a, b in zip(expected, actual))
    assert error < 0.001, f"FBX geometry changed: {error}"
    attributes = list(other.data.color_attributes)
    assert len(attributes) == 1 and attributes[0].domain == "CORNER"
    actual_colors = sorted(tuple(c.color) for c in attributes[0].data)
    assert len(actual_colors) == len(expected_colors)
    color_error = max(abs(a - b) for first, second in zip(expected_colors, actual_colors)
                      for a, b in zip(first, second))
    assert color_error < 0.0001, f"FBX colors changed: {color_error}"
    assert len(other.data.materials) == 1
    assert other.data.materials[0].name.split(".")[0] == "VC_Rock"
    for item in imported:
        bpy.data.objects.remove(item, do_unlink=True)
    return error, color_error


def aim_at(obj, point):
    obj.rotation_euler = (Vector(point) - obj.location).to_track_quat("-Z", "Y").to_euler()


def preview(scene, assets):
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1200, 800
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.world = bpy.data.worlds.new("RockKitPreviewWorld")
    scene.world.use_nodes = True
    scene.world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.3, 0.32, 0.35, 1)
    scene.world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.7
    # Presentation positions are saved in the source only; each FBX was exported at the origin.
    for obj, position in zip(assets, [(-5, 4, 0), (4, 4, 0), (-5, -2, 0), (-1, -2, 0),
                                    (3, -2, 0), (7, -2, 0)]):
        obj.location = position
    for name, location, energy, size in (("PreviewKey", (-8, -8, 15), 3200, 8),
                                         ("PreviewFill", (12, -2, 10), 1800, 10)):
        data = bpy.data.lights.new(name, "AREA")
        data.energy, data.size = energy, size
        light = bpy.data.objects.new(name, data)
        scene.collection.objects.link(light)
        light.location = location
        aim_at(light, (0, 1, 1))
    data = bpy.data.cameras.new("PreviewCamera")
    data.type, data.ortho_scale = "ORTHO", 20
    camera = bpy.data.objects.new("PreviewCamera", data)
    scene.collection.objects.link(camera)
    camera.location = (10, -20, 17)
    aim_at(camera, (0, 1, 1.5))
    scene.camera = camera
    scene.render.filepath = str(SOURCE / "preview.png")
    bpy.ops.render.render(write_still=True)
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == "VIEW_3D":
                view = area.spaces.active.region_3d
                view.view_distance, view.view_location = 23, (0, 1, 1.5)
                view.view_rotation = camera.rotation_euler.to_quaternion()


def main():
    if not bpy.app.background or bpy.data.filepath:
        raise RuntimeError("Use a fresh background process with --factory-startup")
    originals = {path: digest(path) for path in (REFERENCE, REFERENCE_FBX)}
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    scene = bpy.context.scene
    scene.unit_settings.system, scene.unit_settings.scale_length = "METRIC", 1
    names = sorted({spec[1] for spec in SPECS})
    with bpy.data.libraries.load(str(REFERENCE), link=False) as (available, loaded):
        assert all(name in available.objects for name in names)
        loaded.objects = names
    sources = {obj.name: obj for obj in loaded.objects}
    OUTPUT.mkdir(parents=True, exist_ok=True)
    write_meta_if_missing(OUTPUT.with_suffix(".meta"), "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n")
    assets, report = [], []
    for name, source, selection, target_size, size_axis in SPECS:
        obj = extract(sources[source], name, selection, target_size, size_axis)
        for item in scene.objects:
            item.select_set(item == obj)
        bpy.context.view_layer.objects.active = obj
        path = OUTPUT / (name + ".fbx")
        bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True, object_types={"MESH"},
            axis_forward="-Z", axis_up="Y", apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
            bake_space_transform=True, mesh_smooth_type="FACE", add_leaf_bones=False, bake_anim=False,
            colors_type="LINEAR")
        error, color_error = verify_export(obj, path)
        importer_meta(path.with_suffix(".fbx.meta"))
        low, high = bounds([v.co for v in obj.data.vertices])
        extent = high - low
        assert abs(low.z) < 0.00001 and abs(low.x + high.x) < 0.00001 and abs(low.y + high.y) < 0.00001
        report.append({"name": name, "source_object": source, "selection": selection,
            "fbx": path.relative_to(ROOT).as_posix(), "dimensions_unity_xyz": [extent.x, extent.z, extent.y],
            "vertices": len(obj.data.vertices), "triangles": len(obj.data.polygons),
            "pieces": len(components(obj.data)), "fbx_geometry_error": error, "fbx_color_error": color_error})
        assets.append(obj)
    for source in sources.values():
        bpy.data.objects.remove(source, do_unlink=True)
    preview(scene, assets)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / "VolcanoRockKit.blend"), compress=True)
    assert all(digest(path) == previous for path, previous in originals.items()), "Original cave files changed"
    manifest = {"reference": REFERENCE.relative_to(ROOT).as_posix(), "reference_sha256": originals[REFERENCE],
        "reference_fbx_sha256": originals[REFERENCE_FBX], "blender_version": bpy.app.version_string,
        "pivot": "bottom center", "material": "VC_Rock", "generated_colliders": False,
        "unity_playmode_verified": False, "assets": report}
    (SOURCE / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(manifest))


if __name__ == "__main__":
    main()
