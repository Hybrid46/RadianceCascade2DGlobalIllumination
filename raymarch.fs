#version 330 core

in vec2 fragTexCoord;          // from default vertex shader
out vec4 fragColor;

uniform sampler2D u_sdf;       // signed distance field (grayscale)
uniform vec2   u_resolution;   // cascade size
uniform int    u_rayCount;     // rays per probe

// 2‑D random unit circle sampler
vec2 randVec2(float seed)
{
    float angle = 2.0 * 3.14159265 * fract(sin(seed) * 43758.5453);
    return vec2(cos(angle), sin(angle));
}

// Ray‑march one ray from the probe location
float marchRay(vec2 start, vec2 dir)
{
    float t = 0.0;
    for (int i = 0; i < 128; ++i)   // hard‑coded max march steps
    {
        vec2 pos = start + t * dir;
        float dist = texture(u_sdf, pos / u_resolution).r * 2.0 - 1.0; // map [0,1] -> [-1,1]
        if (dist < 0.01) break;        // hit geometry
        t += dist;
        if (t > 5.0) break;            // clip to max distance
    }
    return t;   // radiance value: longer distance = more indirect light
}

void main()
{
    // Probe coordinates in [0,1]
    vec2 probe = fragTexCoord;

    // Accumulate contributions from many rays
    float accum = 0.0;
    for (int i = 0; i < u_rayCount; ++i)
    {
        vec2 dir = randVec2(float(i) * 12.9898 + probe.x * 78.233);
        accum += marchRay(probe, dir);
    }
    accum /= float(u_rayCount);          // average

    // Gamma correct / tone‑map
    float indirect = clamp(accum * 0.1, 0.0, 1.0); // simple scaling
    fragColor = vec4(indirect, indirect, indirect, 1.0);
}
