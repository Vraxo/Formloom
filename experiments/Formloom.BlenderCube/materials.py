"""Material construction for the Blender still-life experiment."""

import bpy


def principled_node(material):
    material.use_nodes = True
    nodes = material.node_tree.nodes
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    shader = nodes.new("ShaderNodeBsdfPrincipled")
    material.node_tree.links.new(shader.outputs["BSDF"], output.inputs["Surface"])
    return shader

def create_material(name, kind, treatment="baseline"):
    material = bpy.data.materials.new(name)
    shader = principled_node(material)
    shader.inputs["Metallic"].default_value = 0.0
    shader.inputs["Roughness"].default_value = 0.72

    if kind == "clay":
        shader.inputs["Base Color"].default_value = (0.62, 0.19, 0.075, 1.0)
    elif kind == "ceramic":
        shader.inputs["Base Color"].default_value = (0.045, 0.24, 0.48, 1.0)
        shader.inputs["Roughness"].default_value = 0.28
        coat = shader.inputs.get("Coat Weight") or shader.inputs.get("Clearcoat")
        if coat is not None:
            coat.default_value = 0.18
        if treatment == "illustrated":
            # A muted blue glaze, broad reflections, and restrained surface
            # variation avoid the hard, plastic-like product-render finish.
            shader.inputs["Base Color"].default_value = (0.055, 0.17, 0.29, 1.0)
            shader.inputs["Roughness"].default_value = 0.36
            if coat is not None:
                coat.default_value = 0.10
            coordinates = material.node_tree.nodes.new("ShaderNodeTexCoord")
            glaze_noise = material.node_tree.nodes.new("ShaderNodeTexNoise")
            glaze_noise.inputs["Scale"].default_value = 3.0
            glaze_noise.inputs["Detail"].default_value = 1.5
            glaze_ramp = material.node_tree.nodes.new("ShaderNodeValToRGB")
            glaze_ramp.color_ramp.elements[0].position = 0.22
            glaze_ramp.color_ramp.elements[0].color = (0.038, 0.12, 0.22, 1.0)
            glaze_ramp.color_ramp.elements[1].position = 0.78
            glaze_ramp.color_ramp.elements[1].color = (0.075, 0.23, 0.36, 1.0)
            links = material.node_tree.links
            links.new(coordinates.outputs["Generated"], glaze_noise.inputs["Vector"])
            links.new(glaze_noise.outputs["Fac"], glaze_ramp.inputs["Fac"])
            links.new(glaze_ramp.outputs["Color"], shader.inputs["Base Color"])
    elif kind == "apple":
        shader.inputs["Roughness"].default_value = 0.42
        coat = shader.inputs.get("Coat Weight") or shader.inputs.get("Clearcoat")
        if coat is not None:
            coat.default_value = 0.09

        coordinates = material.node_tree.nodes.new("ShaderNodeTexCoord")
        noise = material.node_tree.nodes.new("ShaderNodeTexNoise")
        noise.inputs["Scale"].default_value = 2.8
        noise.inputs["Detail"].default_value = 2.0
        ramp = material.node_tree.nodes.new("ShaderNodeValToRGB")
        ramp.color_ramp.elements[0].position = 0.24
        ramp.color_ramp.elements[0].color = (0.25, 0.006, 0.012, 1.0)
        ramp.color_ramp.elements[1].position = 0.76
        ramp.color_ramp.elements[1].color = (0.62, 0.045, 0.018, 1.0)
        bump = material.node_tree.nodes.new("ShaderNodeBump")
        bump.inputs["Strength"].default_value = 0.035
        bump.inputs["Distance"].default_value = 0.012

        links = material.node_tree.links
        links.new(coordinates.outputs["Generated"], noise.inputs["Vector"])
        links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
        links.new(ramp.outputs["Color"], shader.inputs["Base Color"])
        links.new(noise.outputs["Fac"], bump.inputs["Height"])
        links.new(bump.outputs["Normal"], shader.inputs["Normal"])

        if treatment == "illustrated":
            # Softer satin skin, a warmer red/orange value range, and low-
            # contrast pigment variation avoid a polished CG product finish.
            shader.inputs["Roughness"].default_value = 0.58
            if coat is not None:
                coat.default_value = 0.025
            ramp.color_ramp.elements[0].color = (0.19, 0.012, 0.012, 1.0)
            ramp.color_ramp.elements[1].color = (0.50, 0.065, 0.026, 1.0)
            noise.inputs["Scale"].default_value = 3.4
            noise.inputs["Detail"].default_value = 1.35
            bump.inputs["Strength"].default_value = 0.012
            bump.inputs["Distance"].default_value = 0.005
    elif kind == "metal":
        shader.inputs["Base Color"].default_value = (0.48, 0.52, 0.58, 1.0)
        shader.inputs["Metallic"].default_value = 0.88
        shader.inputs["Roughness"].default_value = 0.28
    elif kind == "wood":
        coordinates = material.node_tree.nodes.new("ShaderNodeTexCoord")
        mapping = material.node_tree.nodes.new("ShaderNodeMapping")
        mapping.inputs["Scale"].default_value = (1.5, 12.0, 12.0)
        wave = material.node_tree.nodes.new("ShaderNodeTexWave")
        wave.wave_type = "BANDS"
        wave.bands_direction = "X"
        wave.inputs["Scale"].default_value = 5.0
        wave.inputs["Distortion"].default_value = 3.0
        wave.inputs["Detail"].default_value = 3.0
        ramp = material.node_tree.nodes.new("ShaderNodeValToRGB")
        ramp.color_ramp.elements[0].position = 0.22
        ramp.color_ramp.elements[0].color = (0.055, 0.018, 0.006, 1.0)
        ramp.color_ramp.elements[1].position = 0.78
        ramp.color_ramp.elements[1].color = (0.48, 0.22, 0.075, 1.0)
        noise = material.node_tree.nodes.new("ShaderNodeTexNoise")
        noise.inputs["Scale"].default_value = 24.0
        bump = material.node_tree.nodes.new("ShaderNodeBump")
        bump.inputs["Strength"].default_value = 0.12
        bump.inputs["Distance"].default_value = 0.035
        links = material.node_tree.links
        links.new(coordinates.outputs["Generated"], mapping.inputs["Vector"])
        links.new(mapping.outputs["Vector"], wave.inputs["Vector"])
        links.new(wave.outputs["Color"], ramp.inputs["Fac"])
        links.new(ramp.outputs["Color"], shader.inputs["Base Color"])
        links.new(coordinates.outputs["Generated"], noise.inputs["Vector"])
        links.new(noise.outputs["Fac"], bump.inputs["Height"])
        links.new(bump.outputs["Normal"], shader.inputs["Normal"])
    else:
        raise ValueError("Unknown material: " + kind)

    return material

def create_photorealistic_wood_material():
    material = bpy.data.materials.new("PhotorealisticNaturalOak")
    shader = principled_node(material)
    shader.inputs["Roughness"].default_value = 0.68

    coordinates = material.node_tree.nodes.new("ShaderNodeTexCoord")
    mapping = material.node_tree.nodes.new("ShaderNodeMapping")
    mapping.inputs["Scale"].default_value = (1.0, 9.0, 1.0)

    # An anisotropically stretched 3D noise gives long, naturally wandering
    # fibres without the repeated parallel bands produced by a wave texture.
    grain = material.node_tree.nodes.new("ShaderNodeTexNoise")
    grain.inputs["Scale"].default_value = 4.8
    grain.inputs["Detail"].default_value = 3.0
    grain.inputs["Roughness"].default_value = 0.68
    grain.inputs["Distortion"].default_value = 0.48

    color_ramp = material.node_tree.nodes.new("ShaderNodeValToRGB")
    color_ramp.color_ramp.elements[0].position = 0.24
    color_ramp.color_ramp.elements[0].color = (0.17, 0.073, 0.029, 1.0)
    color_ramp.color_ramp.elements[1].position = 0.76
    color_ramp.color_ramp.elements[1].color = (0.255, 0.135, 0.058, 1.0)

    middle = color_ramp.color_ramp.elements.new(0.5)
    middle.color = (0.215, 0.105, 0.043, 1.0)

    pore_mapping = material.node_tree.nodes.new("ShaderNodeMapping")
    pore_mapping.inputs["Scale"].default_value = (1.0, 1.0, 1.0)
    pore_noise = material.node_tree.nodes.new("ShaderNodeTexNoise")
    pore_noise.inputs["Scale"].default_value = 54.0
    pore_noise.inputs["Detail"].default_value = 2.0
    pore_noise.inputs["Roughness"].default_value = 0.62

    bump = material.node_tree.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.018
    bump.inputs["Distance"].default_value = 0.002

    links = material.node_tree.links
    generated = coordinates.outputs["Generated"]
    links.new(generated, mapping.inputs["Vector"])
    links.new(mapping.outputs["Vector"], grain.inputs["Vector"])
    links.new(grain.outputs["Fac"], color_ramp.inputs["Fac"])
    links.new(color_ramp.outputs["Color"], shader.inputs["Base Color"])
    links.new(generated, pore_mapping.inputs["Vector"])
    links.new(pore_mapping.outputs["Vector"], pore_noise.inputs["Vector"])
    links.new(pore_noise.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], shader.inputs["Normal"])
    return material

def create_photorealistic_plaster_material():
    material = bpy.data.materials.new("PhotorealisticWarmPlaster")
    shader = principled_node(material)
    shader.inputs["Base Color"].default_value = (0.34, 0.285, 0.22, 1.0)
    shader.inputs["Roughness"].default_value = 0.92
    coordinates = material.node_tree.nodes.new("ShaderNodeTexCoord")
    broad_noise = material.node_tree.nodes.new("ShaderNodeTexNoise")
    broad_noise.inputs["Scale"].default_value = 7.0
    broad_noise.inputs["Detail"].default_value = 3.0
    broad_noise.inputs["Roughness"].default_value = 0.78
    color_ramp = material.node_tree.nodes.new("ShaderNodeValToRGB")
    color_ramp.color_ramp.elements[0].color = (0.27, 0.225, 0.17, 1.0)
    color_ramp.color_ramp.elements[1].color = (0.39, 0.33, 0.255, 1.0)
    fine_noise = material.node_tree.nodes.new("ShaderNodeTexNoise")
    fine_noise.inputs["Scale"].default_value = 75.0
    fine_noise.inputs["Detail"].default_value = 2.0
    bump = material.node_tree.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.10
    bump.inputs["Distance"].default_value = 0.009
    links = material.node_tree.links
    links.new(coordinates.outputs["Generated"], broad_noise.inputs["Vector"])
    links.new(broad_noise.outputs["Fac"], color_ramp.inputs["Fac"])
    links.new(color_ramp.outputs["Color"], shader.inputs["Base Color"])
    links.new(coordinates.outputs["Generated"], fine_noise.inputs["Vector"])
    links.new(fine_noise.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], shader.inputs["Normal"])
    return material


def create_photorealistic_apple_material():
    """Create a waxy red apple material with visible natural peel detail."""
    material = bpy.data.materials.new("PhotorealisticAppleSkin")
    shader = principled_node(material)
    nodes = material.node_tree.nodes
    links = material.node_tree.links

    # Scale the peel detail for the actual 384px output: the lenticels need to
    # occupy roughly one pixel, while epidermal relief should break up highlights
    # without reading as coarse bumps.
    coordinates = nodes.new("ShaderNodeTexCoord")
    # Generated coordinates are automatically mapped over this mesh. The Object
    # output requires an explicit reference object; leaving it unset collapses
    # these procedural textures and makes the peel appear uniformly smooth.
    vector = coordinates.outputs["Generated"]

    pigment_noise = nodes.new("ShaderNodeTexNoise")
    pigment_noise.inputs["Scale"].default_value = 3.2
    pigment_noise.inputs["Detail"].default_value = 1.6
    pigment_noise.inputs["Roughness"].default_value = 0.72
    pigment_ramp = nodes.new("ShaderNodeValToRGB")
    pigment_ramp.color_ramp.elements[0].position = 0.20
    pigment_ramp.color_ramp.elements[0].color = (0.235, 0.008, 0.010, 1.0)
    pigment_ramp.color_ramp.elements[1].position = 0.80
    pigment_ramp.color_ramp.elements[1].color = (0.32, 0.018, 0.014, 1.0)
    links.new(vector, pigment_noise.inputs["Vector"])
    links.new(pigment_noise.outputs["Fac"], pigment_ramp.inputs["Fac"])

    # Lenticels are small warm pinpricks, not large dots. At this scale the
    # centre and soft edge survive the final render without covering the peel.
    lenticels = nodes.new("ShaderNodeTexVoronoi")
    lenticels.inputs["Scale"].default_value = 48.0
    lenticels.inputs["Randomness"].default_value = 1.0
    lenticel_ramp = nodes.new("ShaderNodeValToRGB")
    lenticel_ramp.color_ramp.elements[0].position = 0.055
    lenticel_ramp.color_ramp.elements[0].color = (0.31, 0.075, 0.032, 1.0)
    lenticel_ramp.color_ramp.elements[1].position = 0.22
    lenticel_ramp.color_ramp.elements[1].color = (0.29, 0.012, 0.012, 1.0)
    lenticel_edge = lenticel_ramp.color_ramp.elements.new(0.12)
    lenticel_edge.color = (0.30, 0.035, 0.018, 1.0)

    lenticel_mix = nodes.new("ShaderNodeMixRGB")
    lenticel_mix.blend_type = "MIX"
    lenticel_mix.inputs["Fac"].default_value = 0.08
    links.new(vector, lenticels.inputs["Vector"])
    links.new(lenticels.outputs["Distance"], lenticel_ramp.inputs["Fac"])
    links.new(pigment_ramp.outputs["Color"], lenticel_mix.inputs[1])
    links.new(lenticel_ramp.outputs["Color"], lenticel_mix.inputs[2])
    links.new(lenticel_mix.outputs["Color"], shader.inputs["Base Color"])

    # The peel has a gently pebbled epidermis. Lower spatial frequency is
    # deliberate: sub-pixel noise was being erased by denoising at this size.
    epidermis_noise = nodes.new("ShaderNodeTexNoise")
    epidermis_noise.inputs["Scale"].default_value = 92.0
    epidermis_noise.inputs["Detail"].default_value = 2.5
    epidermis_noise.inputs["Roughness"].default_value = 0.72
    epidermis_bump = nodes.new("ShaderNodeBump")
    epidermis_bump.inputs["Strength"].default_value = 0.065
    epidermis_bump.inputs["Distance"].default_value = 0.00055
    links.new(vector, epidermis_noise.inputs["Vector"])
    links.new(epidermis_noise.outputs["Fac"], epidermis_bump.inputs["Height"])

    # A shallow depression beneath each lenticel gives the freckles a little
    # physical definition instead of relying on colour contrast alone.
    lenticel_height = nodes.new("ShaderNodeValToRGB")
    lenticel_height.color_ramp.elements[0].position = 0.055
    lenticel_height.color_ramp.elements[0].color = (0.0, 0.0, 0.0, 1.0)
    lenticel_height.color_ramp.elements[1].position = 0.22
    lenticel_height.color_ramp.elements[1].color = (1.0, 1.0, 1.0, 1.0)
    lenticel_bump = nodes.new("ShaderNodeBump")
    lenticel_bump.inputs["Strength"].default_value = 0.025
    lenticel_bump.inputs["Distance"].default_value = 0.00022
    links.new(lenticels.outputs["Distance"], lenticel_height.inputs["Fac"])
    links.new(lenticel_height.outputs["Color"], lenticel_bump.inputs["Height"])
    links.new(epidermis_bump.outputs["Normal"], lenticel_bump.inputs["Normal"])

    # Fine pores finish the surface, but remain subordinate to the epidermis.
    pore_noise = nodes.new("ShaderNodeTexNoise")
    pore_noise.inputs["Scale"].default_value = 220.0
    pore_noise.inputs["Detail"].default_value = 2.0
    pore_bump = nodes.new("ShaderNodeBump")
    pore_bump.inputs["Strength"].default_value = 0.025
    pore_bump.inputs["Distance"].default_value = 0.00016
    links.new(vector, pore_noise.inputs["Vector"])
    links.new(pore_noise.outputs["Fac"], pore_bump.inputs["Height"])
    links.new(lenticel_bump.outputs["Normal"], pore_bump.inputs["Normal"])
    links.new(pore_bump.outputs["Normal"], shader.inputs["Normal"])

    # Waxy but not glossy: slight local roughness changes soften the highlights.
    roughness_ramp = nodes.new("ShaderNodeValToRGB")
    roughness_ramp.color_ramp.elements[0].color = (0.37, 0.37, 0.37, 1.0)
    roughness_ramp.color_ramp.elements[1].color = (0.46, 0.46, 0.46, 1.0)
    links.new(epidermis_noise.outputs["Fac"], roughness_ramp.inputs["Fac"])
    links.new(roughness_ramp.outputs["Color"], shader.inputs["Roughness"])

    apple_coat = shader.inputs.get("Coat Weight") or shader.inputs.get("Clearcoat")
    if apple_coat is not None:
        apple_coat.default_value = 0.025
    return material
