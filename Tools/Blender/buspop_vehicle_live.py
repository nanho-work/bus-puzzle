import json
import math
import os
import time
from mathutils import Vector

import bpy


ROOT_COLLECTION_NAME = "BusPop Live Vehicle"
OBJECT_PREFIX = "BusPop Live "
MATERIAL_PREFIX = "BusPop Live "
CONFIG_FILE_NAME = "live_vehicle.json"
DEFAULT_RELOAD_SECONDS = 0.6
DEFAULT_SIZE_CLASS = "small"

# Blender authoring units for source/shop vehicles. Vehicle types are authored
# inside one of these fixed size classes instead of redefining the class.
SIZE_CLASS_SPECS = {
    "small": {
        "width": 0.94,
        "length": 1.36,
        "height": 0.93,
    },
    "medium": {
        "width": 0.94,
        "length": 1.84,
        "height": 0.93,
    },
    "large": {
        "width": 0.94,
        "length": 2.19,
        "height": 0.93,
    },
}


def script_dir():
    text = getattr(getattr(bpy.context, "space_data", None), "text", None)
    if text is not None and text.filepath:
        return os.path.dirname(os.path.abspath(bpy.path.abspath(text.filepath)))

    if "__file__" in globals() and __file__:
        file_path = os.path.abspath(__file__)
        if os.path.exists(file_path):
            return os.path.dirname(file_path)

    return bpy.path.abspath("//")


SCRIPT_DIR = script_dir()
CONFIG_PATH = os.path.join(SCRIPT_DIR, CONFIG_FILE_NAME)
EXPORT_DIR = os.path.join(SCRIPT_DIR, "exports")
PREVIEW_DIR = os.path.join(SCRIPT_DIR, "previews")


DEFAULT_CONFIG = {
    "vehicle": "kickboard",
    "size_class": DEFAULT_SIZE_CLASS,
    "fit_to_size_class": True,
    "show_size_box": True,
    "scale": 1.0,
    "body_color": "#1EA7FF",
    "accent_color": "#FFD23C",
    "secondary_color": "#FF4D7D",
    "glass_color": "#082A45",
    "wheel_color": "#111827",
    "metal_color": "#E8F3FF",
    "ground": True,
    "save_preview": False,
    "export_fbx": False,
    "export_dir": "exports",
    "export_name": "shop_kickboard_blue",
    "reload_seconds": DEFAULT_RELOAD_SECONDS,
}


def ensure_default_config():
    if os.path.exists(CONFIG_PATH):
        return

    os.makedirs(SCRIPT_DIR, exist_ok=True)
    with open(CONFIG_PATH, "w", encoding="utf-8") as file:
        json.dump(DEFAULT_CONFIG, file, indent=2)


def load_config():
    ensure_default_config()
    with open(CONFIG_PATH, "r", encoding="utf-8") as file:
        config = json.load(file)

    merged = dict(DEFAULT_CONFIG)
    merged.update(config)
    return merged


def hex_color(value, fallback="#FFFFFF"):
    if not isinstance(value, str):
        value = fallback
    value = value.strip().lstrip("#")
    if len(value) != 6:
        value = fallback.lstrip("#")

    try:
        red = int(value[0:2], 16) / 255.0
        green = int(value[2:4], 16) / 255.0
        blue = int(value[4:6], 16) / 255.0
    except ValueError:
        red, green, blue = 1.0, 1.0, 1.0
    return (red, green, blue, 1.0)


def clamp_float(value, default, minimum, maximum):
    try:
        number = float(value)
    except (TypeError, ValueError):
        number = default
    return max(minimum, min(maximum, number))


def normalized_size_class(value):
    if not isinstance(value, str):
        return DEFAULT_SIZE_CLASS
    key = value.strip().lower()
    if key not in SIZE_CLASS_SPECS:
        return DEFAULT_SIZE_CLASS
    return key


def size_class_spec(config):
    key = normalized_size_class(config.get("size_class"))
    base = dict(SIZE_CLASS_SPECS[key])
    custom = config.get("target_size")
    if isinstance(custom, dict):
        base["width"] = clamp_float(custom.get("width"), base["width"], 0.10, 8.0)
        base["length"] = clamp_float(custom.get("length"), base["length"], 0.10, 8.0)
        base["height"] = clamp_float(custom.get("height"), base["height"], 0.10, 8.0)
    return key, base


def clear_live_scene():
    collection = bpy.data.collections.get(ROOT_COLLECTION_NAME)
    if collection is not None:
        for obj in list(collection.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(collection)

    remove_default_startup_objects()

    for mesh in list(bpy.data.meshes):
        if mesh.users == 0 and mesh.name.startswith(OBJECT_PREFIX):
            bpy.data.meshes.remove(mesh)

    for material in list(bpy.data.materials):
        if material.users == 0 and material.name.startswith(MATERIAL_PREFIX):
            bpy.data.materials.remove(material)


def remove_default_startup_objects():
    for object_name in ("Cube", "Camera", "Light"):
        obj = bpy.data.objects.get(object_name)
        if obj is not None:
            bpy.data.objects.remove(obj, do_unlink=True)


def create_collection():
    clear_live_scene()
    collection = bpy.data.collections.new(ROOT_COLLECTION_NAME)
    bpy.context.scene.collection.children.link(collection)
    return collection


def move_to_collection(obj, collection):
    collection.objects.link(obj)
    for source in list(obj.users_collection):
        if source != collection:
            source.objects.unlink(obj)
    obj.name = OBJECT_PREFIX + obj.name
    if hasattr(obj.data, "name"):
        obj.data.name = obj.name + " Mesh"
    return obj


def make_material(name, color, roughness=0.28, metallic=0.0):
    material = bpy.data.materials.new(MATERIAL_PREFIX + name)
    material.use_nodes = True
    material.diffuse_color = color

    bsdf = material.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None:
        if "Base Color" in bsdf.inputs:
            bsdf.inputs["Base Color"].default_value = color
        if "Metallic" in bsdf.inputs:
            bsdf.inputs["Metallic"].default_value = metallic
        if "Roughness" in bsdf.inputs:
            bsdf.inputs["Roughness"].default_value = roughness
        if "Alpha" in bsdf.inputs:
            bsdf.inputs["Alpha"].default_value = color[3]
        if "Specular IOR Level" in bsdf.inputs:
            bsdf.inputs["Specular IOR Level"].default_value = 0.72
        elif "Specular" in bsdf.inputs:
            bsdf.inputs["Specular"].default_value = 0.55

    return material


def create_materials(config):
    body = hex_color(config.get("body_color"), "#1EA7FF")
    accent = hex_color(config.get("accent_color"), "#FFD23C")
    secondary = hex_color(config.get("secondary_color"), "#FF4D7D")
    glass = hex_color(config.get("glass_color"), "#082A45")
    wheel = hex_color(config.get("wheel_color"), "#111827")
    metal = hex_color(config.get("metal_color"), "#E8F3FF")
    return {
        "body": make_material("Body", body, 0.20),
        "body_soft": make_material("Body Soft Highlight", lerp_color(body, (1, 1, 1, 1), 0.26), 0.18),
        "accent": make_material("Accent", accent, 0.18),
        "secondary": make_material("Secondary", secondary, 0.22),
        "glass": make_material("Glass", glass, 0.16),
        "wheel": make_material("Wheel", wheel, 0.42),
        "tire_side": make_material("Tire Side", lerp_color(wheel, (1, 1, 1, 1), 0.12), 0.36),
        "metal": make_material("Metal", metal, 0.24, 0.08),
        "shadow": make_material("Soft Ground", (0.76, 0.86, 0.92, 1), 0.55),
        "dark": make_material("Dark Detail", (0.035, 0.044, 0.060, 1), 0.36),
        "light": make_material("Light Detail", (1.0, 0.94, 0.64, 1), 0.22),
        "tail": make_material("Tail Light", (0.95, 0.10, 0.12, 1), 0.26),
    }


def lerp_color(a, b, t):
    return (
        a[0] + (b[0] - a[0]) * t,
        a[1] + (b[1] - a[1]) * t,
        a[2] + (b[2] - a[2]) * t,
        a[3] + (b[3] - a[3]) * t,
    )


def shade_smooth(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    try:
        bpy.ops.object.shade_smooth()
    except RuntimeError:
        pass
    obj.select_set(False)


def rounded_box(collection, name, material, location, scale, bevel=0.05):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = move_to_collection(bpy.context.object, collection)
    obj.name = OBJECT_PREFIX + name
    obj.data.name = obj.name + " Mesh"
    obj.dimensions = scale
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material)

    if bevel > 0:
        modifier = obj.modifiers.new("Toy bevel", "BEVEL")
        modifier.width = bevel
        modifier.segments = 5
        if hasattr(modifier, "affect"):
            modifier.affect = "EDGES"
        obj.modifiers.new("Soft weighted normals", "WEIGHTED_NORMAL")
    return obj


def mark_helper(obj):
    if obj is not None:
        obj["buspop_helper"] = True
    return obj


def is_vehicle_mesh(obj):
    return obj.type == "MESH" and not bool(obj.get("buspop_helper"))


def vehicle_meshes(collection):
    return [obj for obj in collection.objects if is_vehicle_mesh(obj)]


def calculate_world_bounds(objects):
    minimum = Vector((float("inf"), float("inf"), float("inf")))
    maximum = Vector((float("-inf"), float("-inf"), float("-inf")))
    found = False

    bpy.context.view_layer.update()
    for obj in objects:
        for corner in obj.bound_box:
            point = obj.matrix_world @ Vector(corner)
            minimum.x = min(minimum.x, point.x)
            minimum.y = min(minimum.y, point.y)
            minimum.z = min(minimum.z, point.z)
            maximum.x = max(maximum.x, point.x)
            maximum.y = max(maximum.y, point.y)
            maximum.z = max(maximum.z, point.z)
            found = True

    if not found:
        return None

    center = (minimum + maximum) * 0.5
    size = maximum - minimum
    return {
        "min": minimum,
        "max": maximum,
        "center": center,
        "size": size,
    }


def fit_vehicle_to_size_class(collection, config):
    if not config.get("fit_to_size_class", True):
        return

    objects = vehicle_meshes(collection)
    bounds = calculate_world_bounds(objects)
    if bounds is None:
        return

    size_class, target = size_class_spec(config)
    current = bounds["size"]
    fit_scale = min(
        target["width"] / max(0.001, current.x),
        target["length"] / max(0.001, current.y),
        target["height"] / max(0.001, current.z),
    )
    center = bounds["center"]

    for obj in objects:
        obj.location = center + (obj.location - center) * fit_scale
        obj.scale *= fit_scale

    bounds = calculate_world_bounds(objects)
    if bounds is None:
        return

    offset = Vector((-bounds["center"].x, -bounds["center"].y, -bounds["min"].z))
    for obj in objects:
        obj.location += offset

    print(
        "[BusPop Live] Fit %s model to %s box %.2f x %.2f x %.2f"
        % (
            str(config.get("vehicle", "vehicle")),
            size_class,
            target["width"],
            target["length"],
            target["height"],
        )
    )


def cylinder_between(collection, name, material, start, end, radius, vertices=24):
    start_vec = Vector(start)
    end_vec = Vector(end)
    direction = end_vec - start_vec
    length = direction.length
    if length <= 0.0001:
        return None

    midpoint = start_vec + direction * 0.5
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=length, location=midpoint)
    obj = move_to_collection(bpy.context.object, collection)
    obj.name = OBJECT_PREFIX + name
    obj.data.name = obj.name + " Mesh"
    obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()
    obj.data.materials.append(material)
    shade_smooth(obj)
    return obj


def wheel(collection, name, materials, location, radius=0.24, thickness=0.055, width=1.0):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=48,
        radius=radius,
        depth=thickness * 1.8,
        location=location,
        rotation=(0, math.radians(90), 0),
    )
    side = move_to_collection(bpy.context.object, collection)
    side.name = OBJECT_PREFIX + name + " Wheel Disc"
    side.data.name = side.name + " Mesh"
    side.data.materials.append(materials["tire_side"])
    shade_smooth(side)

    bpy.ops.mesh.primitive_torus_add(
        major_segments=64,
        minor_segments=14,
        major_radius=radius,
        minor_radius=thickness,
        location=location,
        rotation=(0, math.radians(90), 0),
    )
    tire = move_to_collection(bpy.context.object, collection)
    tire.name = OBJECT_PREFIX + name + " Tire"
    tire.data.name = tire.name + " Mesh"
    tire.scale.x = width
    tire.data.materials.append(materials["wheel"])
    shade_smooth(tire)

    bpy.ops.mesh.primitive_cylinder_add(
        vertices=40,
        radius=radius * 0.42,
        depth=thickness * 1.35,
        location=location,
        rotation=(0, math.radians(90), 0),
    )
    hub = move_to_collection(bpy.context.object, collection)
    hub.name = OBJECT_PREFIX + name + " Hub"
    hub.data.name = hub.name + " Mesh"
    hub.data.materials.append(materials["metal"])
    shade_smooth(hub)
    return tire


def build_kickboard(collection, materials, scale):
    s = scale
    deck_z = 0.34 * s
    front_y = 0.92 * s
    rear_y = -0.92 * s

    rounded_box(collection, "Kickboard Color Deck", materials["body"], (0, 0, deck_z), (0.30 * s, 1.88 * s, 0.10 * s), 0.055 * s)
    rounded_box(collection, "Kickboard Color Deck Pad", materials["body_soft"], (-0.005 * s, -0.05 * s, deck_z + 0.065 * s), (0.24 * s, 1.42 * s, 0.030 * s), 0.030 * s)
    rounded_box(collection, "Kickboard Front Deck Color Cap", materials["body_soft"], (0, 0.76 * s, deck_z + 0.087 * s), (0.24 * s, 0.22 * s, 0.026 * s), 0.022 * s)
    rounded_box(collection, "Kickboard Rear Brake", materials["dark"], (0, -0.88 * s, deck_z + 0.12 * s), (0.24 * s, 0.20 * s, 0.050 * s), 0.035 * s)

    wheel(collection, "Kickboard Front", materials, (0, front_y, 0.18 * s), 0.17 * s, 0.045 * s, 0.84)
    wheel(collection, "Kickboard Rear", materials, (0, rear_y, 0.18 * s), 0.17 * s, 0.045 * s, 0.84)

    cylinder_between(collection, "Kickboard Rear Axle", materials["metal"], (-0.25 * s, rear_y, 0.18 * s), (0.25 * s, rear_y, 0.18 * s), 0.018 * s)
    cylinder_between(collection, "Kickboard Front Axle", materials["metal"], (-0.25 * s, front_y, 0.18 * s), (0.25 * s, front_y, 0.18 * s), 0.018 * s)
    cylinder_between(collection, "Kickboard Front Fork Left", materials["metal"], (-0.10 * s, front_y, 0.22 * s), (-0.12 * s, front_y + 0.07 * s, 0.58 * s), 0.020 * s)
    cylinder_between(collection, "Kickboard Front Fork Right", materials["metal"], (0.10 * s, front_y, 0.22 * s), (0.12 * s, front_y + 0.07 * s, 0.58 * s), 0.020 * s)
    cylinder_between(collection, "Kickboard Upright Stem", materials["metal"], (0, front_y + 0.08 * s, 0.54 * s), (0, front_y + 0.16 * s, 1.52 * s), 0.038 * s)
    cylinder_between(collection, "Kickboard Color Handlebar", materials["body"], (-0.52 * s, front_y + 0.17 * s, 1.52 * s), (0.52 * s, front_y + 0.17 * s, 1.52 * s), 0.042 * s)
    rounded_box(collection, "Kickboard Left Color Grip", materials["body_soft"], (-0.61 * s, front_y + 0.17 * s, 1.52 * s), (0.18 * s, 0.085 * s, 0.085 * s), 0.035 * s)
    rounded_box(collection, "Kickboard Right Color Grip", materials["body_soft"], (0.61 * s, front_y + 0.17 * s, 1.52 * s), (0.18 * s, 0.085 * s, 0.085 * s), 0.035 * s)


def build_bicycle(collection, materials, scale):
    s = scale
    rear = Vector((0, -0.78 * s, 0.34 * s))
    front = Vector((0, 0.82 * s, 0.34 * s))
    crank = Vector((0, -0.08 * s, 0.48 * s))
    seat = Vector((0, -0.38 * s, 0.98 * s))
    handle = Vector((0, 0.58 * s, 1.02 * s))

    wheel(collection, "Bicycle Rear", materials, rear, 0.34 * s, 0.040 * s, 0.76)
    wheel(collection, "Bicycle Front", materials, front, 0.34 * s, 0.040 * s, 0.76)

    tube = 0.050 * s
    cylinder_between(collection, "Bicycle Down Tube", materials["body"], seat, crank, tube)
    cylinder_between(collection, "Bicycle Top Tube", materials["body"], seat, handle, tube)
    cylinder_between(collection, "Bicycle Chain Stay", materials["body"], rear, crank, tube)
    cylinder_between(collection, "Bicycle Seat Stay", materials["body"], rear, seat, tube)
    cylinder_between(collection, "Bicycle Fork", materials["metal"], front, handle, tube * 0.86)
    cylinder_between(collection, "Bicycle Handle Stem", materials["metal"], handle, (0, 0.72 * s, 1.22 * s), tube * 0.82)
    cylinder_between(collection, "Bicycle Color Handlebar", materials["body"], (-0.36 * s, 0.72 * s, 1.22 * s), (0.36 * s, 0.72 * s, 1.22 * s), tube)
    rounded_box(collection, "Bicycle Left Color Grip", materials["body_soft"], (-0.45 * s, 0.72 * s, 1.22 * s), (0.16 * s, 0.08 * s, 0.08 * s), 0.030 * s)
    rounded_box(collection, "Bicycle Right Color Grip", materials["body_soft"], (0.45 * s, 0.72 * s, 1.22 * s), (0.16 * s, 0.08 * s, 0.08 * s), 0.030 * s)
    rounded_box(collection, "Bicycle Seat", materials["dark"], (0, -0.42 * s, 1.06 * s), (0.42 * s, 0.20 * s, 0.08 * s), 0.035 * s)
    rounded_box(collection, "Bicycle Front Basket", materials["metal"], (0, 0.98 * s, 0.86 * s), (0.46 * s, 0.30 * s, 0.22 * s), 0.04 * s)
    rounded_box(collection, "Bicycle Basket Color Plate", materials["body_soft"], (0, 1.14 * s, 0.88 * s), (0.36 * s, 0.035 * s, 0.15 * s), 0.018 * s)
    rounded_box(collection, "Bicycle Frame Pop Plate", materials["body_soft"], (0, -0.06 * s, 0.68 * s), (0.22 * s, 0.18 * s, 0.080 * s), 0.040 * s)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=0.10 * s, location=crank)
    hub = move_to_collection(bpy.context.object, collection)
    hub.name = OBJECT_PREFIX + "Bicycle Crank Pop"
    hub.data.name = hub.name + " Mesh"
    hub.data.materials.append(materials["secondary"])
    shade_smooth(hub)


def build_motorcycle(collection, materials, scale):
    s = scale
    rear = Vector((0, -0.82 * s, 0.34 * s))
    front = Vector((0, 0.86 * s, 0.34 * s))
    wheel(collection, "Motorcycle Rear", materials, rear, 0.34 * s, 0.055 * s, 1.0)
    wheel(collection, "Motorcycle Front", materials, front, 0.34 * s, 0.052 * s, 0.88)

    rounded_box(collection, "Motorcycle Lower Body", materials["body"], (0, -0.10 * s, 0.62 * s), (0.48 * s, 1.18 * s, 0.30 * s), 0.12 * s)
    rounded_box(collection, "Motorcycle Tank", materials["body_soft"], (0, 0.18 * s, 0.82 * s), (0.48 * s, 0.62 * s, 0.28 * s), 0.14 * s)
    rounded_box(collection, "Motorcycle Seat", materials["dark"], (0, -0.44 * s, 0.88 * s), (0.44 * s, 0.58 * s, 0.13 * s), 0.08 * s)
    rounded_box(collection, "Motorcycle Accent Pop", materials["accent"], (0, 0.18 * s, 0.99 * s), (0.24 * s, 0.32 * s, 0.035 * s), 0.03 * s)

    cylinder_between(collection, "Motorcycle Front Fork Left", materials["metal"], (-0.10 * s, 0.78 * s, 0.36 * s), (-0.12 * s, 0.58 * s, 1.02 * s), 0.025 * s)
    cylinder_between(collection, "Motorcycle Front Fork Right", materials["metal"], (0.10 * s, 0.78 * s, 0.36 * s), (0.12 * s, 0.58 * s, 1.02 * s), 0.025 * s)
    cylinder_between(collection, "Motorcycle Handlebar", materials["accent"], (-0.42 * s, 0.55 * s, 1.08 * s), (0.42 * s, 0.55 * s, 1.08 * s), 0.035 * s)
    rounded_box(collection, "Motorcycle Headlight", materials["light"], (0, 0.70 * s, 0.90 * s), (0.26 * s, 0.08 * s, 0.16 * s), 0.04 * s)
    rounded_box(collection, "Motorcycle Tail Light", materials["tail"], (0, -0.96 * s, 0.76 * s), (0.20 * s, 0.06 * s, 0.12 * s), 0.025 * s)


def build_city_bus(collection, materials, scale):
    s = scale
    body_width = 0.68 * s
    half_width = body_width * 0.5
    front_y = 1.76 * s
    rear_y = -1.76 * s

    rounded_box(collection, "City Bus Low Long Body", materials["body"], (0, 0, 0.40 * s), (body_width, 3.46 * s, 0.54 * s), 0.025 * s)
    rounded_box(collection, "City Bus Upper Body Cap", materials["body"], (0, -0.03 * s, 0.69 * s), (body_width * 0.96, 3.20 * s, 0.15 * s), 0.035 * s)
    rounded_box(collection, "City Bus Flat Roof Highlight", materials["body"], (0, -0.05 * s, 0.80 * s), (0.56 * s, 2.82 * s, 0.050 * s), 0.022 * s)
    rounded_box(collection, "City Bus Roof AC Long Unit", materials["body"], (0, -0.18 * s, 0.875 * s), (0.32 * s, 0.58 * s, 0.070 * s), 0.022 * s)
    rounded_box(collection, "City Bus Roof Vent", materials["body"], (0, 0.82 * s, 0.865 * s), (0.20 * s, 0.20 * s, 0.052 * s), 0.018 * s)
    rounded_box(collection, "City Bus Lower Yellow Skirt", materials["accent"], (0, -0.04 * s, 0.22 * s), (body_width + 0.018 * s, 3.10 * s, 0.070 * s), 0.006 * s)
    rounded_box(collection, "City Bus Thin Body Belt", materials["body_soft"], (0, -0.10 * s, 0.49 * s), (body_width + 0.012 * s, 2.92 * s, 0.020 * s), 0.004 * s)
    rounded_box(collection, "City Bus Front Flat Face", materials["body"], (0, front_y + 0.012 * s, 0.43 * s), (0.60 * s, 0.034 * s, 0.54 * s), 0.016 * s)
    rounded_box(collection, "City Bus Front Route Sign", materials["dark"], (0, front_y + 0.034 * s, 0.73 * s), (0.45 * s, 0.022 * s, 0.075 * s), 0.006 * s)
    rounded_box(collection, "City Bus Front Glass", materials["glass"], (0, front_y + 0.038 * s, 0.59 * s), (0.50 * s, 0.024 * s, 0.24 * s), 0.010 * s)
    rounded_box(collection, "City Bus Front Bumper", materials["body_soft"], (0, front_y + 0.040 * s, 0.20 * s), (0.48 * s, 0.020 * s, 0.060 * s), 0.008 * s)
    rounded_box(collection, "City Bus Front Grille", materials["dark"], (0, front_y + 0.042 * s, 0.31 * s), (0.26 * s, 0.018 * s, 0.040 * s), 0.006 * s)
    rounded_box(collection, "City Bus Front Left Lamp", materials["light"], (-0.23 * s, front_y + 0.044 * s, 0.35 * s), (0.090 * s, 0.016 * s, 0.045 * s), 0.006 * s)
    rounded_box(collection, "City Bus Front Right Lamp", materials["light"], (0.23 * s, front_y + 0.044 * s, 0.35 * s), (0.090 * s, 0.016 * s, 0.045 * s), 0.006 * s)
    rounded_box(collection, "City Bus Front Plate", materials["metal"], (0, front_y + 0.045 * s, 0.13 * s), (0.18 * s, 0.014 * s, 0.035 * s), 0.004 * s)

    cylinder_between(collection, "City Bus Left Wiper", materials["dark"], (-0.17 * s, front_y + 0.050 * s, 0.48 * s), (-0.03 * s, front_y + 0.053 * s, 0.61 * s), 0.004 * s, 10)
    cylinder_between(collection, "City Bus Right Wiper", materials["dark"], (0.17 * s, front_y + 0.050 * s, 0.48 * s), (0.03 * s, front_y + 0.053 * s, 0.61 * s), 0.004 * s, 10)

    for side, label in [(-1, "Left"), (1, "Right")]:
        side_x = side * (half_width + 0.014 * s)
        outward_x = side_x + side * 0.006 * s
        rounded_box(collection, "City Bus %s Continuous Black Window Band" % label, materials["dark"], (outward_x, -0.36 * s, 0.64 * s), (0.026 * s, 2.44 * s, 0.250 * s), 0.006 * s)
        for index, y in enumerate([-1.15, -0.78, -0.41, -0.04, 0.33, 0.70]):
            rounded_box(collection, "City Bus %s Side Glass %d" % (label, index + 1), materials["glass"], (outward_x + side * 0.004 * s, y * s, 0.65 * s), (0.028 * s, 0.29 * s, 0.180 * s), 0.004 * s)
        for index, y in enumerate([-1.33, -0.96, -0.59, -0.22, 0.15, 0.52]):
            rounded_box(collection, "City Bus %s Window Pillar %d" % (label, index + 1), materials["dark"], (outward_x + side * 0.008 * s, y * s, 0.65 * s), (0.012 * s, 0.022 * s, 0.240 * s), 0.002 * s)
        rounded_box(collection, "City Bus %s Front Door Black Frame" % label, materials["dark"], (outward_x, 1.13 * s, 0.51 * s), (0.030 * s, 0.34 * s, 0.48 * s), 0.006 * s)
        rounded_box(collection, "City Bus %s Door Upper Glass Left" % label, materials["glass"], (outward_x + side * 0.004 * s, 1.06 * s, 0.63 * s), (0.032 * s, 0.12 * s, 0.20 * s), 0.004 * s)
        rounded_box(collection, "City Bus %s Door Upper Glass Right" % label, materials["glass"], (outward_x + side * 0.004 * s, 1.21 * s, 0.63 * s), (0.032 * s, 0.12 * s, 0.20 * s), 0.004 * s)
        rounded_box(collection, "City Bus %s Door Lower Panel" % label, materials["dark"], (outward_x + side * 0.004 * s, 1.13 * s, 0.35 * s), (0.030 * s, 0.28 * s, 0.16 * s), 0.004 * s)
        rounded_box(collection, "City Bus %s Door Split Line" % label, materials["body_soft"], (outward_x + side * 0.008 * s, 1.135 * s, 0.50 * s), (0.010 * s, 0.012 * s, 0.44 * s), 0.002 * s)
        for index, y in enumerate([-1.20, -0.76, -0.32, 0.12, 0.56]):
            rounded_box(collection, "City Bus %s Lower Panel Seam %d" % (label, index + 1), materials["body_soft"], (outward_x + side * 0.006 * s, y * s, 0.35 * s), (0.010 * s, 0.014 * s, 0.22 * s), 0.001 * s)
        rounded_box(collection, "City Bus %s Front Amber Marker" % label, materials["light"], (outward_x + side * 0.008 * s, 1.38 * s, 0.30 * s), (0.016 * s, 0.052 * s, 0.030 * s), 0.003 * s)
        rounded_box(collection, "City Bus %s Rear Red Marker" % label, materials["tail"], (outward_x + side * 0.008 * s, -1.48 * s, 0.32 * s), (0.016 * s, 0.052 * s, 0.034 * s), 0.003 * s)
        rounded_box(collection, "City Bus %s Front Wheel Dark Recess" % label, materials["dark"], (outward_x + side * 0.004 * s, 0.82 * s, 0.19 * s), (0.024 * s, 0.42 * s, 0.22 * s), 0.020 * s)
        rounded_box(collection, "City Bus %s Rear Wheel Dark Recess" % label, materials["dark"], (outward_x + side * 0.004 * s, -0.96 * s, 0.19 * s), (0.024 * s, 0.42 * s, 0.22 * s), 0.020 * s)
        cylinder_between(collection, "City Bus %s Mirror Arm" % label, materials["dark"], (side * 0.31 * s, front_y - 0.03 * s, 0.63 * s), (side * 0.47 * s, front_y + 0.06 * s, 0.69 * s), 0.006 * s, 10)
        rounded_box(collection, "City Bus %s Rect Mirror" % label, materials["dark"], (side * 0.50 * s, front_y + 0.08 * s, 0.67 * s), (0.050 * s, 0.030 * s, 0.105 * s), 0.006 * s)

    rounded_box(collection, "City Bus Rear Flat Face", materials["body"], (0, rear_y - 0.012 * s, 0.42 * s), (0.60 * s, 0.034 * s, 0.52 * s), 0.016 * s)
    rounded_box(collection, "City Bus Rear Glass", materials["glass"], (0, rear_y - 0.038 * s, 0.62 * s), (0.45 * s, 0.024 * s, 0.22 * s), 0.010 * s)
    rounded_box(collection, "City Bus Rear Hatch Panel", materials["body_soft"], (0, rear_y - 0.041 * s, 0.34 * s), (0.40 * s, 0.018 * s, 0.14 * s), 0.006 * s)
    rounded_box(collection, "City Bus Rear Tail Stack Left", materials["tail"], (-0.28 * s, rear_y - 0.043 * s, 0.42 * s), (0.046 * s, 0.016 * s, 0.15 * s), 0.005 * s)
    rounded_box(collection, "City Bus Rear Tail Stack Right", materials["tail"], (0.28 * s, rear_y - 0.043 * s, 0.42 * s), (0.046 * s, 0.016 * s, 0.15 * s), 0.005 * s)
    rounded_box(collection, "City Bus Rear Plate", materials["metal"], (0, rear_y - 0.045 * s, 0.18 * s), (0.17 * s, 0.012 * s, 0.035 * s), 0.004 * s)

    wheel(collection, "City Bus Front Left", materials, (-0.375 * s, 0.82 * s, 0.16 * s), 0.155 * s, 0.044 * s, 0.72)
    wheel(collection, "City Bus Front Right", materials, (0.375 * s, 0.82 * s, 0.16 * s), 0.155 * s, 0.044 * s, 0.72)
    wheel(collection, "City Bus Rear Left", materials, (-0.375 * s, -0.96 * s, 0.16 * s), 0.155 * s, 0.044 * s, 0.72)
    wheel(collection, "City Bus Rear Right", materials, (0.375 * s, -0.96 * s, 0.16 * s), 0.155 * s, 0.044 * s, 0.72)


def build_korean_bus(collection, materials, scale):
    s = scale
    width = 0.74 * s
    length = 3.28 * s
    height = 0.72 * s
    front_y = length * 0.5
    rear_y = -length * 0.5
    side_x = width * 0.5

    # One readable box body first, then flat surface details for mobile clarity.
    rounded_box(collection, "Korean Bus Single Box Body", materials["body"], (0, 0, 0.40 * s), (width, length, height), 0.070 * s)
    rounded_box(collection, "Korean Bus Roof Soft Highlight", materials["body_soft"], (0, -0.08 * s, 0.79 * s), (width * 0.78, length * 0.78, 0.045 * s), 0.040 * s)
    rounded_box(collection, "Korean Bus Lower Color Belt", materials["accent"], (0, -0.02 * s, 0.18 * s), (width + 0.018 * s, length * 0.90, 0.080 * s), 0.018 * s)

    # Front face: windshield, route sign, lamps.
    rounded_box(collection, "Korean Bus Front Windshield", materials["glass"], (0, front_y + 0.022 * s, 0.58 * s), (width * 0.66, 0.020 * s, 0.27 * s), 0.014 * s)
    rounded_box(collection, "Korean Bus Front Route Sign", materials["dark"], (0, front_y + 0.024 * s, 0.77 * s), (width * 0.46, 0.018 * s, 0.060 * s), 0.006 * s)
    rounded_box(collection, "Korean Bus Front Grille", materials["dark"], (0, front_y + 0.026 * s, 0.30 * s), (width * 0.38, 0.016 * s, 0.050 * s), 0.004 * s)
    rounded_box(collection, "Korean Bus Front Left Headlight", materials["light"], (-width * 0.32, front_y + 0.028 * s, 0.35 * s), (0.075 * s, 0.014 * s, 0.045 * s), 0.006 * s)
    rounded_box(collection, "Korean Bus Front Right Headlight", materials["light"], (width * 0.32, front_y + 0.028 * s, 0.35 * s), (0.075 * s, 0.014 * s, 0.045 * s), 0.006 * s)
    rounded_box(collection, "Korean Bus Front Plate", materials["metal"], (0, front_y + 0.029 * s, 0.18 * s), (0.18 * s, 0.012 * s, 0.035 * s), 0.004 * s)

    # Boarding side: windows plus a clear front door.
    boarding_x = side_x + 0.020 * s
    rounded_box(collection, "Korean Bus Boarding Side Window Band", materials["body"], (boarding_x, -0.32 * s, 0.61 * s), (0.020 * s, 2.24 * s, 0.25 * s), 0.006 * s)
    for index, y in enumerate([-1.08, -0.70, -0.32, 0.06, 0.44]):
        rounded_box(collection, "Korean Bus Boarding Side Window %d" % (index + 1), materials["glass"], (boarding_x + 0.006 * s, y * s, 0.62 * s), (0.020 * s, 0.29 * s, 0.17 * s), 0.004 * s)
    rounded_box(collection, "Korean Bus Boarding Door Frame", materials["body"], (boarding_x + 0.004 * s, 1.02 * s, 0.48 * s), (0.024 * s, 0.38 * s, 0.48 * s), 0.006 * s)
    rounded_box(collection, "Korean Bus Boarding Door Glass Upper", materials["glass"], (boarding_x + 0.009 * s, 1.03 * s, 0.61 * s), (0.024 * s, 0.25 * s, 0.18 * s), 0.004 * s)
    rounded_box(collection, "Korean Bus Boarding Door Lower Panel", materials["body_soft"], (boarding_x + 0.010 * s, 1.03 * s, 0.35 * s), (0.020 * s, 0.25 * s, 0.15 * s), 0.004 * s)
    rounded_box(collection, "Korean Bus Boarding Door Split", materials["accent"], (boarding_x + 0.014 * s, 1.03 * s, 0.49 * s), (0.010 * s, 0.016 * s, 0.42 * s), 0.001 * s)

    # Opposite side: only windows, no boarding door.
    opposite_x = -side_x - 0.020 * s
    rounded_box(collection, "Korean Bus Opposite Side Window Band", materials["body"], (opposite_x, -0.16 * s, 0.61 * s), (0.020 * s, 2.62 * s, 0.25 * s), 0.006 * s)
    for index, y in enumerate([-1.18, -0.80, -0.42, -0.04, 0.34, 0.72]):
        rounded_box(collection, "Korean Bus Opposite Side Window %d" % (index + 1), materials["glass"], (opposite_x - 0.006 * s, y * s, 0.62 * s), (0.020 * s, 0.29 * s, 0.17 * s), 0.004 * s)

    # Rear face: rear glass and vertical tail lamps.
    rounded_box(collection, "Korean Bus Rear Window", materials["glass"], (0, rear_y - 0.022 * s, 0.60 * s), (width * 0.58, 0.020 * s, 0.22 * s), 0.012 * s)
    rounded_box(collection, "Korean Bus Rear Hatch", materials["body_soft"], (0, rear_y - 0.024 * s, 0.33 * s), (width * 0.46, 0.016 * s, 0.13 * s), 0.006 * s)
    rounded_box(collection, "Korean Bus Rear Left Tail Light", materials["tail"], (-width * 0.39, rear_y - 0.026 * s, 0.40 * s), (0.050 * s, 0.014 * s, 0.15 * s), 0.005 * s)
    rounded_box(collection, "Korean Bus Rear Right Tail Light", materials["tail"], (width * 0.39, rear_y - 0.026 * s, 0.40 * s), (0.050 * s, 0.014 * s, 0.15 * s), 0.005 * s)
    rounded_box(collection, "Korean Bus Rear Plate", materials["metal"], (0, rear_y - 0.028 * s, 0.17 * s), (0.18 * s, 0.012 * s, 0.035 * s), 0.004 * s)

    # Four low wheels visible from the game camera.
    wheel(collection, "Korean Bus Front Left", materials, (-0.43 * s, 0.90 * s, 0.15 * s), 0.150 * s, 0.042 * s, 0.70)
    wheel(collection, "Korean Bus Front Right", materials, (0.43 * s, 0.90 * s, 0.15 * s), 0.150 * s, 0.042 * s, 0.70)
    wheel(collection, "Korean Bus Rear Left", materials, (-0.43 * s, -0.98 * s, 0.15 * s), 0.150 * s, 0.042 * s, 0.70)
    wheel(collection, "Korean Bus Rear Right", materials, (0.43 * s, -0.98 * s, 0.15 * s), 0.150 * s, 0.042 * s, 0.70)


def build_ground(collection, materials):
    mark_helper(rounded_box(collection, "Display Base", materials["shadow"], (0, 0, -0.04), (3.2, 3.2, 0.04), 0.18))


def build_size_box(collection, config):
    if not config.get("show_size_box", False):
        return

    size_class, target = size_class_spec(config)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, target["height"] * 0.5))
    obj = mark_helper(move_to_collection(bpy.context.object, collection))
    obj.name = OBJECT_PREFIX + "Size Box " + size_class.title()
    obj.data.name = obj.name + " Mesh"
    obj.dimensions = (target["width"], target["length"], target["height"])
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.display_type = "WIRE"
    obj.hide_render = True
    obj.show_in_front = True


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def setup_camera_and_lights(collection, config):
    camera = bpy.data.objects.get("BusPop Live Camera")
    if camera is None:
        camera_data = bpy.data.cameras.new("BusPop Live Camera")
        camera = bpy.data.objects.new("BusPop Live Camera", camera_data)
        bpy.context.scene.collection.objects.link(camera)

    camera.location = (3.0, -4.6, 2.7)
    camera.data.lens = 58
    camera.data.dof.use_dof = False
    look_at(camera, (0, 0, 0.55))
    bpy.context.scene.camera = camera

    for name, location, energy, size in [
        ("BusPop Live Key Light", (-3.0, -4.0, 6.0), 520, 4.0),
        ("BusPop Live Rim Light", (3.6, 2.0, 4.0), 130, 3.0),
    ]:
        light = bpy.data.objects.get(name)
        if light is None:
            light_data = bpy.data.lights.new(name, "AREA")
            light = bpy.data.objects.new(name, light_data)
            bpy.context.scene.collection.objects.link(light)
        light.location = location
        light.data.energy = energy
        light.data.size = size
        look_at(light, (0, 0, 0.4))

    bpy.context.scene.render.resolution_x = int(config.get("preview_width", 1400))
    bpy.context.scene.render.resolution_y = int(config.get("preview_height", 1400))
    if hasattr(bpy.context.scene, "eevee"):
        bpy.context.scene.eevee.taa_render_samples = 64

    world = bpy.context.scene.world
    if world is not None:
        world.color = (0.64, 0.82, 1.0)


def build_vehicle(config):
    collection = create_collection()
    materials = create_materials(config)
    scale = clamp_float(config.get("scale"), 1.0, 0.25, 4.0)
    vehicle = str(config.get("vehicle", "kickboard")).strip().lower()

    if vehicle in ("kickboard", "scooter"):
        build_kickboard(collection, materials, scale)
    elif vehicle in ("bike", "bicycle"):
        build_bicycle(collection, materials, scale)
    elif vehicle in ("motorcycle", "motorbike"):
        build_motorcycle(collection, materials, scale)
    elif vehicle in ("korean_bus", "simple_bus", "box_bus"):
        build_korean_bus(collection, materials, scale)
    elif vehicle in ("bus", "toy_bus", "city_bus", "large_bus"):
        build_city_bus(collection, materials, scale)
    else:
        print("[BusPop Live] Unknown vehicle '%s'. Falling back to kickboard." % vehicle)
        build_kickboard(collection, materials, scale)

    fit_vehicle_to_size_class(collection, config)
    if config.get("ground", True):
        build_ground(collection, materials)
    build_size_box(collection, config)
    setup_camera_and_lights(collection, config)
    maybe_save_preview(config)
    maybe_export_fbx(collection, config)
    print("[BusPop Live] Updated vehicle: %s" % vehicle)


def maybe_save_preview(config):
    if not config.get("save_preview", False):
        return

    os.makedirs(PREVIEW_DIR, exist_ok=True)
    name = str(config.get("export_name", "buspop_vehicle_preview"))
    path = os.path.join(PREVIEW_DIR, name + ".png")
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("[BusPop Live] Preview saved: %s" % path)


def maybe_export_fbx(collection, config):
    if not config.get("export_fbx", False):
        return

    export_dir = resolve_output_dir(config.get("export_dir"), EXPORT_DIR)
    os.makedirs(export_dir, exist_ok=True)
    name = str(config.get("export_name", "buspop_vehicle"))
    path = os.path.join(export_dir, name + ".fbx")

    bpy.ops.object.select_all(action="DESELECT")
    for obj in collection.objects:
        if is_vehicle_mesh(obj):
            obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"MESH"})
    print("[BusPop Live] FBX exported: %s" % path)


def resolve_output_dir(value, fallback):
    if not isinstance(value, str) or len(value.strip()) == 0:
        return fallback

    value = value.strip()
    if os.path.isabs(value):
        return value

    return os.path.normpath(os.path.join(SCRIPT_DIR, value))


def get_config_mtime():
    try:
        return os.path.getmtime(CONFIG_PATH)
    except OSError:
        return 0


def maybe_reload():
    namespace = bpy.app.driver_namespace
    last_mtime = namespace.get("buspop_live_last_mtime")
    current_mtime = get_config_mtime()
    if last_mtime == current_mtime:
        return

    try:
        config = load_config()
    except Exception as exc:
        print("[BusPop Live] Could not read live_vehicle.json: %s" % exc)
        return

    namespace["buspop_live_last_mtime"] = current_mtime
    build_vehicle(config)


def start_live_reload():
    ensure_default_config()
    token = str(time.time())
    namespace = bpy.app.driver_namespace
    namespace["buspop_live_token"] = token
    namespace["buspop_live_last_mtime"] = None

    def timer_callback():
        if bpy.app.driver_namespace.get("buspop_live_token") != token:
            return None

        try:
            maybe_reload()
            config = load_config()
            interval = clamp_float(config.get("reload_seconds"), DEFAULT_RELOAD_SECONDS, 0.2, 5.0)
        except Exception as exc:
            print("[BusPop Live] Reload failed: %s" % exc)
            interval = DEFAULT_RELOAD_SECONDS
        return interval

    maybe_reload()
    bpy.app.timers.register(timer_callback, first_interval=DEFAULT_RELOAD_SECONDS)
    print("[BusPop Live] Watching: %s" % CONFIG_PATH)


start_live_reload()
