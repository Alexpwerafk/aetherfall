import bpy
import math
import os
from mathutils import Vector

OUTPUT_DIR = os.path.abspath(
    os.path.join(os.path.dirname(__file__), "..", "Assets", "Models")
)
os.makedirs(OUTPUT_DIR, exist_ok=True)


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def get_or_create_mat(name, rgba):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name=name)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = rgba
    return mat


def assign_mat(obj, mat_name, rgba=(0.5, 0.5, 0.5, 1.0)):
    mat = get_or_create_mat(mat_name, rgba)
    if obj.data.materials:
        obj.data.materials[0] = mat
    else:
        obj.data.materials.append(mat)


def add_bevel(obj, width=0.02, segments=2):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    mod = obj.modifiers.new(name="Bevel", type="BEVEL")
    mod.width = width
    mod.segments = segments
    mod.limit_method = "ANGLE"
    mod.angle_limit = math.radians(30)
    mod.use_clamp_overlap = True
    bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.select_set(False)


def attach_clean(obj, parent, world_origin):
    if parent is None:
        return
    bpy.context.view_layer.update()
    parent_world = parent.matrix_world.translation
    obj.parent = parent
    obj.location = Vector(world_origin) - parent_world


def shift_origin_world(obj, target_origin_world):
    bpy.context.view_layer.update()
    delta = Vector(target_origin_world) - obj.matrix_world.translation
    for v in obj.data.vertices:
        v.co -= delta
    obj.location = Vector(target_origin_world)


def make_box(name, size, loc, mat_name, rgba, rot=(0, 0, 0), bevel=0.02, parent=None, origin=None):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=rot)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    if bevel > 0:
        add_bevel(obj, width=bevel, segments=2)
    assign_mat(obj, mat_name, rgba)
    final_origin = Vector(origin) if origin is not None else Vector(loc)
    if origin is not None:
        shift_origin_world(obj, final_origin)
    attach_clean(obj, parent, final_origin)
    return obj


def make_tapered_prism(name, top_diam, bot_diam, height, segments, loc, mat_name, rgba, rot=(0, 0, 0), bevel=0.015, parent=None, origin=None):
    bpy.ops.mesh.primitive_cone_add(
        vertices=segments,
        radius1=0.5 * bot_diam,
        radius2=0.5 * top_diam,
        depth=height,
        location=loc,
        rotation=rot,
    )
    obj = bpy.context.active_object
    obj.name = name
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    if bevel > 0:
        add_bevel(obj, width=bevel, segments=1)
    assign_mat(obj, mat_name, rgba)
    final_origin = Vector(origin) if origin is not None else Vector(loc)
    if origin is not None:
        shift_origin_world(obj, final_origin)
    attach_clean(obj, parent, final_origin)
    return obj


def make_icosphere(name, radius, subdiv, loc, scale, mat_name, rgba, parent=None, origin=None):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdiv, radius=radius, location=loc)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    assign_mat(obj, mat_name, rgba)
    final_origin = Vector(origin) if origin is not None else Vector(loc)
    if origin is not None:
        shift_origin_world(obj, final_origin)
    attach_clean(obj, parent, final_origin)
    return obj


def make_empty(name, loc=(0, 0, 0), parent=None):
    empty = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(empty)
    empty.location = Vector(loc)
    if parent is not None:
        attach_clean(empty, parent, loc)
    return empty


def export_fbx(root_obj, filename):
    bpy.ops.object.select_all(action="DESELECT")

    def select_hierarchy(obj):
        obj.select_set(True)
        for child in obj.children:
            select_hierarchy(child)

    select_hierarchy(root_obj)
    bpy.context.view_layer.objects.active = root_obj

    filepath = os.path.join(OUTPUT_DIR, filename)
    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=True,
        object_types={"EMPTY", "MESH"},
        use_mesh_modifiers=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        mesh_smooth_type="FACE",
    )
    print(f"[BlenderGenerator] Exported {filepath}")


# Palette RGBAs (matching Palette.cs)
C_OBSIDIAN = (0.11, 0.13, 0.18, 1.0)
C_MARBLE = (0.21, 0.24, 0.32, 1.0)
C_CRIMSON = (0.85, 0.28, 0.33, 1.0)
C_GOLD = (0.96, 0.76, 0.42, 1.0)
C_IVORY = (0.92, 0.89, 0.82, 1.0)
C_CYAN = (0.17, 0.91, 0.96, 1.0)
C_AMBER = (1.0, 0.43, 0.15, 1.0)
C_FROST = (0.55, 0.96, 1.0, 1.0)
C_VANGUARD = (0.31, 0.43, 0.35, 1.0)
C_ALCHEMIST = (0.48, 0.31, 0.62, 1.0)
C_BOSS = (0.72, 0.20, 0.42, 1.0)
C_OIL = (0.13, 0.11, 0.10, 1.0)


def build_hero():
    clear_scene()
    root = make_empty("HeroRoot")

    body = make_empty("Body", (0, 0, 0.78), root)
    make_tapered_prism("TorsoCoat", 0.54, 0.42, 0.66, 6, (0, 0, 0.98), "Mat_HeroCrimson", C_CRIMSON, bevel=0.015, parent=body)
    make_box("ChestPlate", (0.46, 0.30, 0.34), (0, -0.04, 1.08), "Mat_ObsidianArmor", C_OBSIDIAN, bevel=0.02, parent=body)
    make_box("GoldTrimBelt", (0.42, 0.31, 0.09), (0, 0, 0.78), "Mat_FiligreeGold", C_GOLD, bevel=0.012, parent=body)
    make_tapered_prism("CoatTail", 0.44, 0.64, 0.44, 6, (0, 0.04, 0.56), "Mat_HeroCrimson", C_CRIMSON, bevel=0.012, parent=body)
    make_box("LeftPauldron", (0.22, 0.26, 0.18), (-0.33, 0, 1.26), "Mat_FiligreeGold", C_GOLD, rot=(0, math.radians(-18), 0), bevel=0.018, parent=body)
    make_box("RightPauldron", (0.22, 0.26, 0.18), (0.33, 0, 1.26), "Mat_FiligreeGold", C_GOLD, rot=(0, math.radians(18), 0), bevel=0.018, parent=body)

    head = make_empty("Head", (0, 0, 1.36), body)
    make_tapered_prism("Helm", 0.34, 0.40, 0.36, 8, (0, 0, 1.54), "Mat_ObsidianArmor", C_OBSIDIAN, bevel=0.015, parent=head)
    make_box("HelmCrest", (0.06, 0.34, 0.16), (0, 0.02, 1.72), "Mat_FiligreeGold", C_GOLD, rot=(math.radians(-10), 0, 0), bevel=0.01, parent=head)
    make_box("VisorGlow", (0.22, 0.08, 0.055), (0, -0.17, 1.53), "Mat_ArcaneCyan", C_CYAN, bevel=0.006, parent=head)
    make_box("ScarfCollar", (0.38, 0.34, 0.12), (0, 0.02, 1.35), "Mat_IvoryCloth", C_IVORY, bevel=0.015, parent=head)

    make_box(
        "Cape",
        (0.52, 0.05, 0.94),
        (0, 0.22, 0.84),
        "Mat_HeroCrimson",
        C_CRIMSON,
        rot=(math.radians(14), 0, 0),
        bevel=0.012,
        parent=body,
        origin=(0, 0.14, 1.26),
    )

    left_arm = make_empty("LeftArm", (-0.34, 0, 1.20), body)
    make_tapered_prism("LeftArmMesh", 0.18, 0.15, 0.54, 6, (-0.36, -0.02, 0.94), "Mat_IvoryCloth", C_IVORY, bevel=0.01, parent=left_arm)
    make_box("LeftGauntlet", (0.15, 0.17, 0.18), (-0.36, -0.06, 0.70), "Mat_FiligreeGold", C_GOLD, bevel=0.012, parent=left_arm)

    right_arm = make_empty("RightArm", (0.34, 0, 1.20), body)
    make_tapered_prism("RightArmMesh", 0.18, 0.15, 0.54, 6, (0.36, -0.02, 0.94), "Mat_IvoryCloth", C_IVORY, bevel=0.01, parent=right_arm)
    make_box("RightGauntlet", (0.15, 0.17, 0.18), (0.36, -0.06, 0.70), "Mat_FiligreeGold", C_GOLD, bevel=0.012, parent=right_arm)
    make_empty("WeaponMount", (0.36, -0.14, 0.65), right_arm)

    left_leg = make_empty("LeftLeg", (-0.14, 0, 0.66), root)
    make_tapered_prism("LeftLegMesh", 0.20, 0.17, 0.54, 6, (-0.14, 0, 0.40), "Mat_ObsidianArmor", C_OBSIDIAN, bevel=0.01, parent=left_leg)
    make_box("LeftBoot", (0.15, 0.24, 0.16), (-0.14, -0.04, 0.08), "Mat_FiligreeGold", C_GOLD, bevel=0.015, parent=left_leg)

    right_leg = make_empty("RightLeg", (0.14, 0, 0.66), root)
    make_tapered_prism("RightLegMesh", 0.20, 0.17, 0.54, 6, (0.14, 0, 0.40), "Mat_ObsidianArmor", C_OBSIDIAN, bevel=0.01, parent=right_leg)
    make_box("RightBoot", (0.15, 0.24, 0.16), (0.14, -0.04, 0.08), "Mat_FiligreeGold", C_GOLD, bevel=0.015, parent=right_leg)

    export_fbx(root, "Hero_Expeditioner.fbx")


def build_weapons():
    # 1. Solar Greatsword (Fire)
    clear_scene()
    root = make_empty("SolarGreatsword")
    make_tapered_prism("Grip", 0.07, 0.08, 0.34, 8, (0, 0, 0.0), "Mat_ObsidianArmor", C_OBSIDIAN, bevel=0.006, parent=root)
    make_box("Pommel", (0.11, 0.11, 0.11), (0, 0, -0.22), "Mat_FiligreeGold", C_GOLD, rot=(0, math.radians(45), 0), bevel=0.01, parent=root)
    make_box("Crossguard", (0.46, 0.10, 0.09), (0, 0, 0.19), "Mat_FiligreeGold", C_GOLD, bevel=0.012, parent=root)
    make_box("Blade", (0.15, 0.045, 1.22), (0, 0, 0.84), "Mat_ObsidianArmor", C_OBSIDIAN, bevel=0.01, parent=root)
    make_box("SolarCore", (0.055, 0.06, 1.02), (0, 0, 0.80), "Mat_FireAmber", C_AMBER, bevel=0.006, parent=root)
    make_tapered_prism("BladeTip", 0.01, 0.20, 0.25, 4, (0, 0, 1.56), "Mat_FiligreeGold", C_GOLD, rot=(0, 0, math.radians(45)), bevel=0.005, parent=root)
    export_fbx(root, "Weapon_SolarGreatsword.fbx")

    # 2. Storm Halberd (Lightning)
    clear_scene()
    root = make_empty("StormHalberd")
    make_tapered_prism("Shaft", 0.06, 0.06, 1.72, 8, (0, 0, 0.60), "Mat_ObsidianArmor", C_OBSIDIAN, bevel=0.004, parent=root)
    make_tapered_prism("SpearTip", 0.01, 0.16, 0.42, 4, (0, 0, 1.65), "Mat_FiligreeGold", C_GOLD, bevel=0.006, parent=root)
    make_box("AxeBlade", (0.34, 0.05, 0.42), (0.18, 0, 1.32), "Mat_MarbleTrim", C_MARBLE, rot=(0, math.radians(12), 0), bevel=0.01, parent=root)
    make_icosphere("StormCore", 0.10, 1, (0, 0, 1.36), (1.0, 0.7, 1.3), "Mat_ArcaneCyan", C_CYAN, parent=root)
    export_fbx(root, "Weapon_StormHalberd.fbx")

    # 3. Frost Runeblade (Frost)
    clear_scene()
    root = make_empty("FrostRuneblade")
    make_tapered_prism("Grip", 0.065, 0.075, 0.34, 8, (0, 0, 0.0), "Mat_MarbleTrim", C_MARBLE, bevel=0.006, parent=root)
    make_box("Guard", (0.38, 0.09, 0.09), (0, 0, 0.18), "Mat_IvoryCloth", C_IVORY, bevel=0.012, parent=root)
    make_box("IceBlade", (0.14, 0.045, 1.15), (0, 0, 0.78), "Mat_FrostIce", C_FROST, bevel=0.01, parent=root)
    make_tapered_prism("IceTip", 0.01, 0.18, 0.28, 4, (0, 0, 1.48), "Mat_FrostIce", C_FROST, rot=(0, 0, math.radians(45)), bevel=0.005, parent=root)
    export_fbx(root, "Weapon_FrostRuneblade.fbx")


def build_enemies():
    # 1. Corrupted Vanguard
    clear_scene()
    root = make_empty("Enemy_CorruptedVanguard")
    body = make_empty("Body", (0, 0, 0.82), root)
    make_tapered_prism("Torso", 0.76, 0.56, 0.72, 6, (0, 0, 1.02), "Mat_EnemyVanguard", C_VANGUARD, bevel=0.02, parent=body)
    make_box("ChestBand", (0.62, 0.42, 0.16), (0, -0.02, 1.12), "Mat_ObsidianArmor", C_OBSIDIAN, bevel=0.018, parent=body)

    head = make_empty("Head", (0, 0, 1.44), body)
    make_tapered_prism("Helm", 0.36, 0.46, 0.36, 6, (0, 0, 1.58), "Mat_ObsidianArmor", C_OBSIDIAN, bevel=0.015, parent=head)
    make_tapered_prism("LeftHorn", 0.02, 0.10, 0.28, 5, (-0.18, -0.04, 1.78), "Mat_FiligreeGold", C_GOLD, rot=(0, math.radians(-22), 0), bevel=0.004, parent=head)
    make_tapered_prism("RightHorn", 0.02, 0.10, 0.28, 5, (0.18, -0.04, 1.78), "Mat_FiligreeGold", C_GOLD, rot=(0, math.radians(22), 0), bevel=0.004, parent=head)
    make_box("EyeSlit", (0.24, 0.08, 0.06), (0, -0.19, 1.58), "Mat_FireAmber", C_AMBER, bevel=0.006, parent=head)

    left_arm = make_empty("LeftArm", (-0.42, 0, 1.24), body)
    make_tapered_prism("LeftArmMesh", 0.20, 0.17, 0.56, 6, (-0.44, 0, 0.96), "Mat_EnemyVanguard", C_VANGUARD, bevel=0.012, parent=left_arm)
    make_box("TowerShield", (0.10, 0.52, 0.88), (-0.56, -0.18, 0.90), "Mat_ObsidianArmor", C_OBSIDIAN, bevel=0.018, parent=left_arm)
    make_box("ShieldTrim", (0.12, 0.22, 0.62), (-0.58, -0.18, 0.90), "Mat_FiligreeGold", C_GOLD, bevel=0.01, parent=left_arm)

    right_arm = make_empty("RightArm", (0.42, 0, 1.24), body)
    make_tapered_prism("RightArmMesh", 0.20, 0.17, 0.56, 6, (0.44, 0, 0.96), "Mat_EnemyVanguard", C_VANGUARD, bevel=0.012, parent=right_arm)
    make_box("CleaverBlade", (0.09, 0.26, 0.90), (0.46, -0.26, 0.95), "Mat_MarbleTrim", C_MARBLE, rot=(math.radians(-68), 0, 0), bevel=0.012, parent=right_arm)

    left_leg = make_empty("LeftLeg", (-0.18, 0, 0.68), root)
    make_tapered_prism("LeftLegMesh", 0.24, 0.20, 0.68, 6, (-0.18, 0, 0.34), "Mat_ObsidianArmor", C_OBSIDIAN, bevel=0.012, parent=left_leg)
    right_leg = make_empty("RightLeg", (0.18, 0, 0.68), root)
    make_tapered_prism("RightLegMesh", 0.24, 0.20, 0.68, 6, (0.18, 0, 0.34), "Mat_ObsidianArmor", C_OBSIDIAN, bevel=0.012, parent=right_leg)
    export_fbx(root, "Enemy_CorruptedVanguard.fbx")

    # 2. Arcane Alchemist
    clear_scene()
    root = make_empty("Enemy_ArcaneAlchemist")
    body = make_empty("Body", (0, 0, 0.72), root)
    make_tapered_prism("Robe", 0.44, 0.78, 1.28, 8, (0, 0, 0.66), "Mat_EnemyAlchemist", C_ALCHEMIST, bevel=0.015, parent=body)
    make_box("Mantle", (0.56, 0.38, 0.20), (0, 0.02, 1.24), "Mat_FiligreeGold", C_GOLD, bevel=0.018, parent=body)
    make_icosphere("BeltFlask", 0.11, 1, (-0.28, -0.14, 0.74), (1, 1, 1.2), "Mat_ArcaneCyan", C_CYAN, parent=body)

    head = make_empty("Head", (0, 0, 1.38), body)
    make_tapered_prism("Hood", 0.16, 0.46, 0.46, 6, (0, 0.03, 1.58), "Mat_EnemyAlchemist", C_ALCHEMIST, rot=(math.radians(12), 0, 0), bevel=0.015, parent=head)
    make_box("AlchEyes", (0.20, 0.08, 0.06), (0, -0.16, 1.54), "Mat_ArcaneCyan", C_CYAN, bevel=0.006, parent=head)

    left_arm = make_empty("LeftArm", (-0.32, 0, 1.18), body)
    make_tapered_prism("LeftSleeve", 0.16, 0.22, 0.50, 6, (-0.34, -0.08, 0.94), "Mat_EnemyAlchemist", C_ALCHEMIST, bevel=0.012, parent=left_arm)

    right_arm = make_empty("RightArm", (0.32, 0, 1.18), body)
    make_tapered_prism("RightSleeve", 0.16, 0.22, 0.50, 6, (0.34, -0.08, 0.94), "Mat_EnemyAlchemist", C_ALCHEMIST, bevel=0.012, parent=right_arm)
    make_tapered_prism("StaffRod", 0.06, 0.06, 1.75, 6, (0.40, -0.22, 0.92), "Mat_ObsidianArmor", C_OBSIDIAN, bevel=0.005, parent=right_arm)
    make_icosphere("StaffOrb", 0.15, 1, (0.40, -0.22, 1.84), (1, 1, 1.25), "Mat_ArcaneCyan", C_CYAN, parent=right_arm)

    left_leg = make_empty("LeftLeg", (-0.14, 0, 0.35), root)
    make_box("LeftFoot", (0.14, 0.22, 0.14), (-0.14, -0.04, 0.07), "Mat_ObsidianArmor", C_OBSIDIAN, bevel=0.012, parent=left_leg)
    right_leg = make_empty("RightLeg", (0.14, 0, 0.35), root)
    make_box("RightFoot", (0.14, 0.22, 0.14), (0.14, -0.04, 0.07), "Mat_ObsidianArmor", C_OBSIDIAN, bevel=0.012, parent=right_leg)
    export_fbx(root, "Enemy_ArcaneAlchemist.fbx")

    # 3. Boss: Chrono-Inquisitor Malakor
    clear_scene()
    root = make_empty("Enemy_ChronoInquisitorBoss")
    body = make_empty("Body", (0, 0, 1.25), root)
    make_tapered_prism("BossTorso", 1.05, 0.72, 1.05, 8, (0, 0, 1.55), "Mat_BossEclipse", C_BOSS, bevel=0.025, parent=body)
    make_tapered_prism("BossSkirt", 0.72, 1.15, 0.78, 8, (0, 0, 0.78), "Mat_ObsidianArmor", C_OBSIDIAN, bevel=0.02, parent=body)
    make_box("BossLeftWingShoulder", (0.42, 0.44, 0.30), (-0.60, 0, 2.02), "Mat_FiligreeGold", C_GOLD, rot=(0, math.radians(-22), 0), bevel=0.02, parent=body)
    make_box("BossRightWingShoulder", (0.42, 0.44, 0.30), (0.60, 0, 2.02), "Mat_FiligreeGold", C_GOLD, rot=(0, math.radians(22), 0), bevel=0.02, parent=body)

    halo = make_empty("HaloRing", (0, 0.34, 2.25), body)
    for i in range(8):
        ang = i * (2 * math.pi / 8)
        hx = math.cos(ang) * 0.80
        hz = 2.25 + math.sin(ang) * 0.80
        make_box(f"HaloTooth_{i}", (0.14, 0.08, 0.22), (hx, 0.34, hz), "Mat_FiligreeGold", C_GOLD, rot=(0, -ang + math.pi / 2, 0), bevel=0.01, parent=halo)
    make_icosphere("HaloCore", 0.16, 1, (0, 0.34, 2.25), (1.2, 0.5, 1.2), "Mat_FireAmber", C_AMBER, parent=halo)

    head = make_empty("Head", (0, 0, 2.22), body)
    make_tapered_prism("BossCrownHelm", 0.50, 0.56, 0.54, 8, (0, 0, 2.42), "Mat_ObsidianArmor", C_OBSIDIAN, bevel=0.02, parent=head)
    make_box("BossCrownSpire", (0.14, 0.14, 0.42), (0, -0.05, 2.78), "Mat_FiligreeGold", C_GOLD, bevel=0.012, parent=head)
    make_box("BossVisor", (0.30, 0.10, 0.08), (0, -0.25, 2.40), "Mat_FireAmber", C_AMBER, bevel=0.008, parent=head)

    left_arm = make_empty("LeftArm", (-0.64, 0, 1.88), body)
    make_tapered_prism("LeftArmMesh", 0.26, 0.20, 0.84, 6, (-0.66, 0, 1.48), "Mat_BossEclipse", C_BOSS, bevel=0.015, parent=left_arm)
    make_box("LeftClaw", (0.22, 0.22, 0.26), (-0.66, -0.08, 1.05), "Mat_FiligreeGold", C_GOLD, bevel=0.015, parent=left_arm)

    right_arm = make_empty("RightArm", (0.64, 0, 1.88), body)
    make_tapered_prism("RightArmMesh", 0.26, 0.20, 0.84, 6, (0.66, 0, 1.48), "Mat_BossEclipse", C_BOSS, bevel=0.015, parent=right_arm)
    make_tapered_prism("BossHalberdShaft", 0.08, 0.08, 2.85, 8, (0.72, -0.22, 1.55), "Mat_ObsidianArmor", C_OBSIDIAN, bevel=0.008, parent=right_arm)
    make_box("BossHalberdBladeA", (0.50, 0.08, 0.66), (0.96, -0.22, 2.55), "Mat_FiligreeGold", C_GOLD, bevel=0.015, parent=right_arm)
    make_box("BossHalberdBladeB", (0.36, 0.08, 0.50), (0.52, -0.22, 2.55), "Mat_FireAmber", C_AMBER, bevel=0.012, parent=right_arm)

    left_leg = make_empty("LeftLeg", (-0.26, 0, 0.96), root)
    make_tapered_prism("LeftLegMesh", 0.30, 0.24, 0.96, 6, (-0.26, 0, 0.48), "Mat_ObsidianArmor", C_OBSIDIAN, bevel=0.015, parent=left_leg)
    right_leg = make_empty("RightLeg", (0.26, 0, 0.96), root)
    make_tapered_prism("RightLegMesh", 0.30, 0.24, 0.96, 6, (0.26, 0, 0.48), "Mat_ObsidianArmor", C_OBSIDIAN, bevel=0.015, parent=right_leg)
    export_fbx(root, "Enemy_ChronoInquisitorBoss.fbx")


def build_environment_props():
    # 1. Env_SanctumTile (2x2 stone tile with bevel trim)
    clear_scene()
    root = make_empty("Env_SanctumTile")
    make_box("StoneBase", (1.96, 1.96, 0.24), (0, 0, -0.14), "Mat_ObsidianFloor", C_OBSIDIAN, bevel=0.03, parent=root)
    make_box("InlayTrim", (1.68, 1.68, 0.04), (0, 0, -0.01), "Mat_MarbleTrim", C_MARBLE, bevel=0.015, parent=root)
    export_fbx(root, "Env_SanctumTile.fbx")

    # 2. Env_RuinedPillar
    clear_scene()
    root = make_empty("Env_RuinedPillar")
    make_box("Plinth", (1.25, 1.25, 0.45), (0, 0, 0.225), "Mat_MarbleTrim", C_MARBLE, bevel=0.04, parent=root)
    make_tapered_prism("Column", 0.85, 0.98, 2.95, 8, (0, 0, 1.90), "Mat_ObsidianFloor", C_OBSIDIAN, bevel=0.025, parent=root)
    make_box("RuneBand", (0.98, 0.98, 0.20), (0, 0, 1.85), "Mat_FiligreeGold", C_GOLD, bevel=0.02, parent=root)
    make_box("Capital", (1.20, 1.20, 0.38), (0, 0, 3.55), "Mat_MarbleTrim", C_MARBLE, bevel=0.04, parent=root)
    export_fbx(root, "Env_RuinedPillar.fbx")

    # 3. Env_ArcaneBrazier
    clear_scene()
    root = make_empty("Env_ArcaneBrazier")
    make_tapered_prism("Pedestal", 0.52, 0.85, 0.72, 6, (0, 0, 0.36), "Mat_MarbleTrim", C_MARBLE, bevel=0.02, parent=root)
    make_tapered_prism("Bowl", 1.05, 0.52, 0.42, 8, (0, 0, 0.90), "Mat_ObsidianArmor", C_OBSIDIAN, bevel=0.02, parent=root)
    make_icosphere("EmberCore", 0.26, 1, (0, 0, 1.08), (1.0, 1.0, 0.75), "Mat_FireAmber", C_AMBER, parent=root)
    export_fbx(root, "Env_ArcaneBrazier.fbx")

    # 4. Env_GoldenChest (hinged Lid child!)
    clear_scene()
    root = make_empty("Env_GoldenChest")
    make_box("ChestBase", (1.10, 0.72, 0.52), (0, 0, 0.26), "Mat_ObsidianArmor", C_OBSIDIAN, bevel=0.03, parent=root)
    make_box("ChestLock", (0.18, 0.08, 0.20), (0, -0.37, 0.46), "Mat_FiligreeGold", C_GOLD, bevel=0.015, parent=root)
    lid = make_empty("ChestLid", (0, 0.34, 0.52), root)
    make_box("LidTop", (1.14, 0.76, 0.28), (0, 0, 0.65), "Mat_FiligreeGold", C_GOLD, bevel=0.035, parent=lid, origin=(0, 0.34, 0.52))
    export_fbx(root, "Env_GoldenChest.fbx")

    # 5. Env_ExplosiveBarrel
    clear_scene()
    root = make_empty("Env_ExplosiveBarrel")
    make_tapered_prism("BarrelBody", 0.84, 0.84, 1.05, 10, (0, 0, 0.525), "Mat_OilDark", C_OIL, bevel=0.025, parent=root)
    make_tapered_prism("BandTop", 0.88, 0.88, 0.12, 10, (0, 0, 0.85), "Mat_HeroCrimson", C_CRIMSON, bevel=0.012, parent=root)
    make_tapered_prism("BandBot", 0.88, 0.88, 0.12, 10, (0, 0, 0.22), "Mat_HeroCrimson", C_CRIMSON, bevel=0.012, parent=root)
    make_icosphere("HazardRune", 0.18, 1, (0, 0, 1.06), (1.0, 1.0, 0.45), "Mat_FireAmber", C_AMBER, parent=root)
    export_fbx(root, "Env_ExplosiveBarrel.fbx")

    # 6. Env_RuneCrystalSwitch
    clear_scene()
    root = make_empty("Env_RuneCrystalSwitch")
    make_tapered_prism("Pedestal", 0.72, 0.96, 0.68, 6, (0, 0, 0.34), "Mat_MarbleTrim", C_MARBLE, bevel=0.02, parent=root)
    crystal = make_empty("CrystalSpin", (0, 0, 1.25), root)
    make_tapered_prism("CrystalTop", 0.02, 0.46, 0.56, 4, (0, 0, 1.52), "Mat_ArcaneCyan", C_CYAN, bevel=0.008, parent=crystal)
    make_tapered_prism("CrystalBot", 0.46, 0.02, 0.40, 4, (0, 0, 1.04), "Mat_ArcaneCyan", C_CYAN, bevel=0.008, parent=crystal)
    export_fbx(root, "Env_RuneCrystalSwitch.fbx")

    # 7. Env_SpikeTrap
    clear_scene()
    root = make_empty("Env_SpikeTrap")
    make_box("TrapPlate", (1.85, 1.85, 0.10), (0, 0, 0.03), "Mat_ObsidianArmor", C_OBSIDIAN, bevel=0.02, parent=root)
    make_box("TrapBorder", (1.92, 1.92, 0.06), (0, 0, 0.02), "Mat_FiligreeGold", C_GOLD, bevel=0.015, parent=root)
    spikes = make_empty("Spikes", (0, 0, 0), root)
    for gx in (-0.52, 0.0, 0.52):
        for gy in (-0.52, 0.0, 0.52):
            make_tapered_prism(f"Spike_{gx}_{gy}", 0.02, 0.22, 0.72, 4, (gx, gy, 0.25), "Mat_MarbleTrim", C_MARBLE, bevel=0.004, parent=spikes)
    export_fbx(root, "Env_SpikeTrap.fbx")


if __name__ == "__main__":
    build_hero()
    build_weapons()
    build_enemies()
    build_environment_props()
    print("[BlenderGenerator] ALL MODELS GENERATED SUCCESSFULLY.")
