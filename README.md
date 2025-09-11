Raylib Implementation of Radiance Cascades 2D Global Illumination

A C#/Raylib implementation of the Radiance Cascades 2D Global Illumination technique.

# Overview

![Gameplay GIF](RC2DGI.gif)

This project implements a real-time 2D global illumination system using Raylib and custom GLSL shaders. The technique uses a multi-step process involving jump flooding, distance field generation, and radiance cascades to simulate realistic light propagation in 2D environments.

## Features

- Real-time 2D global illumination
- Distance field generation using jump flood algorithm
- Multi-level radiance cascades for efficient light propagation
- Custom shader pipeline
- Debug visualization of intermediate render textures
- Dynamic scene with moving objects

## Requirements

- .NET 6.0 or later
- Raylib-cs (C# bindings for Raylib)
- OpenGL 3.3 compatible GPU

## How It Works

The implementation follows this pipeline:

- Scene Rendering: Draws the scene geometry to a render texture
- Screen UV Pass: Identifies occluders and stores their screen coordinates
- Jump Flood Algorithm: Computes the Voronoi diagram of the scene
- Distance Field Generation: Converts the Voronoi diagram to a distance field
- Radiance Cascades: Computes global illumination using multiple cascade levels
- Final Merge: Combines the GI result with the original scene

## Shaders

- ScreenUV.fs: Identifies occluders and stores their UV coordinates
- JumpFlood.fs: Implements the jump flood algorithm for Voronoi diagram generation
- DistanceField.fs: Converts Voronoi diagram to distance field
- RadianceCascades.fs: Main GI computation using radiance cascades
- Merge.fs: Combines GI result with scene colors

## Acknowledgments

This project is a port of Youssef Afella's Unity implementation:

- https://github.com/Youssef-Afella/UnityURP-RadianceCascades2DGI

## License

This project is available under the same license as the original implementation. Please refer to the original repository for license details.

## References

- Radiance Cascades 2D Global Illumination Paper: https://drive.google.com/file/d/1L6v1_7HY2X-LV3Ofb6oyTIxgEaP4LOI6/view
- Raylib-cs: https://github.com/ChrisDill/Raylib-cs
- Analytic Direct Illumination: https://www.shadertoy.com/view/NttSW7
- Distance Field Generation: https://www.shadertoy.com/view/lX3Sz8
- Radiance Cascades on ShaderToy: https://www.shadertoy.com/view/mtlBzX