from pathlib import Path
import math

import bpy
from mathutils import Vector


REPO_ROOT = Path(__file__).resolve().parents[2]
BLEND_PATH = REPO_ROOT / "Tools" / "Blender" / "DailyChallengeStadium.blend"
FBX_PATH = REPO_ROOT / "Assets" / "BusPuzzle" / "Resources" / "EventModels" / "DailyChallengeStadium.fbx"


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def make_material(name, color, roughness=0.72, metallic=0.0):
    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = roughness
        bsdf.inputs["Metallic"].default_value = metallic
    return material


def apply_bevel(obj, amount=0.025):
    bevel = obj.modifiers.new("Soft Low Poly Bevel", "BEVEL")
    bevel.width = amount
    bevel.segments = 1
    bevel.affect = "EDGES"
    obj.modifiers.new("Weighted Corner Normals", "WEIGHTED_NORMAL")


def create_box(name, location, dimensions, material, rotation=(0.0, 0.0, 0.0), bevel=0.018):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.data.name = f"{name} Mesh"
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if material is not None:
        obj.data.materials.append(material)
    if bevel > 0.0:
        apply_bevel(obj, bevel)
    return obj


def create_cylinder_between(name, start, end, radius, material, sides=8):
    start_v = Vector(start)
    end_v = Vector(end)
    direction = end_v - start_v
    length = direction.length
    if length <= 0.001:
        return None

    center = (start_v + end_v) * 0.5
    bpy.ops.mesh.primitive_cylinder_add(vertices=sides, radius=radius, depth=length, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.data.name = f"{name} Mesh"
    obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()
    if material is not None:
        obj.data.materials.append(material)
    apply_bevel(obj, radius * 0.12)
    return obj


def create_elliptic_band(name, rx_outer, ry_outer, rx_inner, ry_inner, y_offset, z_center, thickness, material, segments=28, start_deg=20.0, end_deg=160.0):
    verts = []
    faces = []
    angles = [math.radians(start_deg + (end_deg - start_deg) * i / segments) for i in range(segments + 1)]

    for z in (z_center + thickness * 0.5, z_center - thickness * 0.5):
        for angle in angles:
            verts.append((rx_outer * math.cos(angle), y_offset + ry_outer * math.sin(angle), z))
        for angle in angles:
            verts.append((rx_inner * math.cos(angle), y_offset + ry_inner * math.sin(angle), z))

    count = len(angles)
    top_outer = 0
    top_inner = count
    bottom_outer = count * 2
    bottom_inner = count * 3

    for i in range(count - 1):
        faces.append((top_outer + i, top_outer + i + 1, top_inner + i + 1, top_inner + i))
        faces.append((bottom_outer + i + 1, bottom_outer + i, bottom_inner + i, bottom_inner + i + 1))
        faces.append((top_outer + i + 1, bottom_outer + i + 1, bottom_inner + i + 1, top_inner + i + 1))
        faces.append((top_outer + i, top_inner + i, bottom_inner + i, bottom_outer + i))
        faces.append((top_outer + i, bottom_outer + i, bottom_outer + i + 1, top_outer + i + 1))
        faces.append((top_inner + i + 1, bottom_inner + i + 1, bottom_inner + i, top_inner + i))

    faces.append((top_outer, top_inner, bottom_inner, bottom_outer))
    faces.append((top_outer + count - 1, bottom_outer + count - 1, bottom_inner + count - 1, top_inner + count - 1))

    mesh = bpy.data.meshes.new(f"{name} Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    if material is not None:
        mesh.materials.append(material)
    apply_bevel(obj, 0.012)
    return obj


def create_arc_posts(prefix, count, rx, ry, y_offset, z_base, z_top, material):
    for index in range(count):
        t = index / max(1, count - 1)
        angle = math.radians(28.0 + 124.0 * t)
        x = rx * math.cos(angle)
        y = y_offset + ry * math.sin(angle)
        create_cylinder_between(
            f"{prefix} {index + 1:02d}",
            (x, y, z_base),
            (x * 1.05, y + 0.06, z_top),
            0.030,
            material,
            sides=8,
        )


def create_scene():
    clear_scene()

    concrete = make_material("StadiumConcrete", (0.64, 0.72, 0.72, 1.0))
    concrete_shade = make_material("StadiumConcreteShade", (0.42, 0.51, 0.55, 1.0))
    upper = make_material("StadiumUpperBowl", (0.36, 0.48, 0.55, 1.0))
    roof = make_material("StadiumRoof", (0.93, 0.98, 1.00, 1.0))
    roof_shade = make_material("StadiumRoofShade", (0.68, 0.78, 0.82, 1.0))
    glass = make_material("StadiumGlass", (0.16, 0.62, 0.78, 1.0), roughness=0.44)
    tunnel = make_material("StadiumTunnel", (0.045, 0.060, 0.075, 1.0))
    accent = make_material("StadiumAccent", (0.02, 0.34, 0.55, 1.0))
    cable = make_material("StadiumCable", (0.96, 0.97, 0.96, 1.0))
    gold_trim = make_material("StadiumGoldTrim", (1.00, 0.70, 0.12, 1.0))
    seat_gold = make_material("StadiumSeatGold", (1.00, 0.72, 0.16, 1.0))
    seat_red = make_material("StadiumSeatRed", (0.90, 0.16, 0.18, 1.0))
    seat_blue = make_material("StadiumSeatBlue", (0.14, 0.41, 0.78, 1.0))
    turf = make_material("StadiumPitchTurf", (0.20, 0.56, 0.28, 1.0))
    line = make_material("StadiumPitchLine", (0.96, 0.98, 0.92, 1.0))

    root = bpy.data.objects.new("DailyChallengeStadium", None)
    bpy.context.collection.objects.link(root)

    # Local coordinates: X is screen left/right, Y is board depth, Z is height.
    create_box("StadiumConcrete Front Concourse", (0.0, -0.43, 0.34), (5.16, 0.48, 0.68), concrete, bevel=0.030).parent = root
    create_box("StadiumConcreteShade Lower Recess", (-1.10, -0.72, 0.22), (2.38, 0.070, 0.18), concrete_shade, bevel=0.010).parent = root
    create_box("StadiumConcreteShade Lower Recess Right", (1.05, -0.72, 0.22), (1.28, 0.070, 0.18), concrete_shade, bevel=0.010).parent = root
    create_box("StadiumConcreteShade Mid Shadow Band", (0.0, -0.725, 0.70), (4.96, 0.080, 0.11), concrete_shade, bevel=0.010).parent = root
    create_box("StadiumGlass Front Ribbon", (-0.82, -0.70, 0.56), (2.20, 0.08, 0.26), glass, bevel=0.012).parent = root
    create_box("StadiumGlass Front Ribbon Right", (0.74, -0.70, 0.56), (0.92, 0.08, 0.26), glass, bevel=0.012).parent = root
    create_box("StadiumGlass Lower Window Band", (-1.30, -0.745, 0.38), (1.48, 0.065, 0.12), glass, bevel=0.008).parent = root
    create_box("StadiumGlass Lower Window Band Center", (0.10, -0.745, 0.38), (0.88, 0.065, 0.12), glass, bevel=0.008).parent = root
    create_box("StadiumGlass Lower Window Band Right", (1.22, -0.745, 0.38), (0.92, 0.065, 0.12), glass, bevel=0.008).parent = root
    create_box("StadiumUpperBowl Rear Mass", (0.0, 0.10, 0.72), (5.36, 0.62, 0.44), upper, bevel=0.035).parent = root

    create_elliptic_band("StadiumRoof Curved Canopy", 2.88, 1.06, 2.18, 0.60, -0.24, 1.18, 0.12, roof).parent = root
    create_elliptic_band("StadiumRoofShade Underside Arc", 2.66, 0.88, 2.23, 0.61, -0.24, 1.03, 0.055, roof_shade, segments=26, start_deg=24.0, end_deg=156.0).parent = root
    create_elliptic_band("StadiumAccent Inner Fascia", 2.44, 0.78, 2.05, 0.50, -0.24, 0.94, 0.10, accent, segments=24, start_deg=24.0, end_deg=156.0).parent = root
    create_elliptic_band("StadiumSeatGold Row", 2.10, 0.58, 1.92, 0.46, -0.25, 0.84, 0.050, seat_gold, segments=22, start_deg=28.0, end_deg=70.0).parent = root
    create_elliptic_band("StadiumSeatBlue Row", 2.10, 0.58, 1.92, 0.46, -0.25, 0.84, 0.050, seat_blue, segments=22, start_deg=70.0, end_deg=118.0).parent = root
    create_elliptic_band("StadiumSeatRed Row", 2.10, 0.58, 1.92, 0.46, -0.25, 0.84, 0.050, seat_red, segments=22, start_deg=118.0, end_deg=152.0).parent = root

    create_box("StadiumRoof Front Lip", (0.0, -0.75, 0.96), (5.62, 0.18, 0.18), roof, bevel=0.026).parent = root
    create_box("StadiumRoofShade Front Underside", (0.0, -0.78, 0.86), (5.36, 0.10, 0.090), roof_shade, bevel=0.012).parent = root
    create_box("StadiumAccent Front Scoreboard Ribbon", (-0.62, -0.86, 1.04), (1.48, 0.055, 0.080), accent, bevel=0.010).parent = root
    create_box("StadiumGoldTrim Front Scoreboard Tick", (0.26, -0.895, 1.04), (0.18, 0.045, 0.086), gold_trim, bevel=0.006).parent = root
    create_box("StadiumTunnel Dark Interior", (0.0, -0.78, 0.73), (4.32, 0.12, 0.24), tunnel, bevel=0.012).parent = root

    for index, x in enumerate([-2.18, -1.46, -0.74, 0.0, 0.74, 1.46, 2.18]):
        material = concrete if index % 2 == 0 else accent
        create_box(f"StadiumConcrete Front Column {index + 1}", (x, -0.80, 0.47), (0.10, 0.14, 0.72), material, bevel=0.016).parent = root

    # Main exit portal, intentionally offset to match the passenger spawn/queue path.
    create_box("StadiumTunnel Entrance Mouth", (1.62, -0.90, 0.42), (0.92, 0.24, 0.62), tunnel, bevel=0.018).parent = root
    create_box("StadiumConcrete Entrance Left Pier", (1.08, -0.88, 0.45), (0.14, 0.28, 0.70), concrete, bevel=0.018).parent = root
    create_box("StadiumConcrete Entrance Right Pier", (2.16, -0.88, 0.45), (0.14, 0.28, 0.70), concrete, bevel=0.018).parent = root
    create_box("StadiumRoof Entrance Header", (1.62, -0.91, 0.83), (1.20, 0.28, 0.15), roof, bevel=0.020).parent = root
    create_box("StadiumAccent Entrance Sign", (1.62, -1.06, 0.97), (0.82, 0.06, 0.10), accent, bevel=0.012).parent = root
    create_box("StadiumGoldTrim Entrance Top Light", (1.62, -1.105, 0.90), (0.72, 0.030, 0.052), gold_trim, bevel=0.006).parent = root
    create_box("StadiumGoldTrim Entrance Left Light", (1.25, -1.105, 0.58), (0.080, 0.030, 0.13), gold_trim, bevel=0.005).parent = root
    create_box("StadiumGoldTrim Entrance Right Light", (1.99, -1.105, 0.58), (0.080, 0.030, 0.13), gold_trim, bevel=0.005).parent = root

    # Visible inner pitch helps the top-down silhouette read as a stadium, not a flat building.
    create_box("StadiumPitchTurf Center", (0.0, 0.12, 0.61), (1.54, 0.58, 0.035), turf, bevel=0.035).parent = root
    create_box("StadiumPitchLine Center", (0.0, 0.12, 0.64), (1.42, 0.030, 0.012), line, bevel=0.004).parent = root
    create_box("StadiumPitchLine Mid", (0.0, 0.12, 0.65), (0.050, 0.50, 0.012), line, bevel=0.004).parent = root

    create_arc_posts("StadiumRoof White Mast", 7, 2.64, 0.92, -0.25, 1.18, 1.70, roof)
    for obj in bpy.context.scene.objects:
        if obj.name.startswith("StadiumRoof White Mast"):
            obj.parent = root

    mast_xs = [-2.48, -1.65, -0.82, 0.0, 0.82, 1.65, 2.48]
    for index, x in enumerate(mast_xs):
        tip = (x * 1.03, 0.62, 1.70)
        create_cylinder_between(f"StadiumCable Inner {index + 1}", tip, (x * 0.72, 0.02, 1.17), 0.014, cable, sides=6).parent = root
        create_cylinder_between(f"StadiumCable Outer {index + 1}", tip, (x * 1.07, -0.72, 0.98), 0.012, cable, sides=6).parent = root

    # Small side details make the facade feel designed without adding visual noise.
    for index, x in enumerate([-2.35, -1.92, -1.08, -0.38, 0.38, 1.08, 2.35]):
        create_box(f"StadiumGlass Skylight {index + 1}", (x, -0.16, 1.27), (0.28, 0.38, 0.020), glass, rotation=(math.radians(0.0), math.radians(0.0), 0.0), bevel=0.006).parent = root

    for obj in bpy.context.scene.objects:
        if obj.type == "MESH":
            obj.select_set(True)
        else:
            obj.select_set(False)
    bpy.context.view_layer.objects.active = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR", center="MEDIAN")

    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    bpy.ops.export_scene.fbx(
        filepath=str(FBX_PATH),
        use_selection=False,
        object_types={"EMPTY", "MESH"},
        apply_unit_scale=True,
        bake_space_transform=False,
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        use_mesh_modifiers=True,
    )


if __name__ == "__main__":
    create_scene()
