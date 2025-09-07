#version 330 core

in vec2 fragTexCoord;
out vec4 fragColor;

uniform sampler2D u_sdf;       // 8‑bit SDF
uniform vec2   u_resolution;   // cascade size
uniform int    u_rayCount;     // rays per probe

// 2‑D random unit circle sampler
vec2 randVec2(float seed)
{
    float angle = 2.0 * 3.14159265 * fract(sin(seed) * 43758.5453);
    return normalize(vec2(cos(angle), sin(angle)));
}

// Ray‑march one ray from the probe location
float marchRay(vec2 start, vec2 dir)
{
    float t = 0.0;
    //TODO uniform
    vec2 texSize = vec2(textureSize(u_sdf, 0));

    for (int i = 0; i < 128; ++i)
    {
        vec2 pos = start + t * dir;

        float dist = texture(u_sdf, pos / texSize).r * 2.0 - 1.0;

        if (dist < 0.01) break;        // hit geometry
        t += dist;                     // step forward
        if (t > 5.0)   break;          // clip to max distance
    }
    return t;   // radiance value: longer distance = more indirect light
}

void main()
{
    vec2 worldStart = fragTexCoord * vec2(textureSize(u_sdf, 0));

    // Accumulate contributions from many rays
    float accum = 0.0;

    for (int i = 0; i < u_rayCount; ++i)
    {
        vec2 dir = randVec2(float(i) * 12.9898 + fragTexCoord.x * 78.233);
        accum += marchRay(worldStart, dir);
    }

    accum /= float(u_rayCount);          // average

    float indirect = clamp(accum * 0.1, 0.0, 1.0);
    fragColor = vec4(indirect, indirect, indirect, 1.0);
}
