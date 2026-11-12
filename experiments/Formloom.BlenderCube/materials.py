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
