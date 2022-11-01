# Fluid Automata

Implementation of the Shadertoy Fluid Automata from Wyatt Flanders in Unity

# Usage

This project works in any render pipeline as all the computation are done in a compute shader.

The Prefabs folder contains example of setup for the Standard and HDRP pipelines, but the URP pipeline is supported as well.

## Material setup

Create a material with a texture property referenced as `_FluidTexture`. Once linked in the FluidAutomataController script, this material will receive the computed fluid texture in the _FluidTexture parameter in play mode.

## Affector setup

1. Place your affector transform above the fluid surface.

2. Add a collider to your fluid surface and set its layer to a layer contained in the FluidAutomataController layer mask parameter.


Note that in this example, the raycast is only done from the affector downwards.

# Source
https://www.shadertoy.com/view/WlcGDH
