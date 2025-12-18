#ifndef PACKING_GLSL
#define PACKING_GLSL

vec2 packUNorm16(float v)
{
    v = clamp(v, 0.0, 1.0);
    uint x = uint(v * 65535.0 + 0.5);

    return vec2(
        float((x >> 8) & uint(255)),
        float(x & uint(255))
    ) / 255.0;
}

float unpackUNorm16(vec2 rg)
{
    uint x =
        (uint(rg.r * 255.0 + 0.5) << 8) |
         uint(rg.g * 255.0 + 0.5);

    return float(x) / 65535.0;
}

#endif // PACKING_GLSL