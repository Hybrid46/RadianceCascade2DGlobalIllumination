#ifndef PACKING_GLSL
#define PACKING_GLSL

// ------------------------------------------------------------
// Helpers
// ------------------------------------------------------------
uint u8(float v)  { return uint(clamp(v, 0.0, 1.0) * 255.0 + 0.5); }
uint u16(float v) { return uint(clamp(v, 0.0, 1.0) * 65535.0 + 0.5); }

float f8(uint v)  { return float(v) / 255.0; }
float f16(uint v) { return float(v) / 65535.0; }

// ------------------------------------------------------------
// Normalized unsigned packing (UNORM)
// ------------------------------------------------------------

// 8 -> 16 bit

vec2 packUNorm8To16(float v)
{
    uint x = u8(v);
    
    return vec2(
        float((x >> 4) & 0xF) / 15.0,
        float(x & 0xF) / 15.0
    );
}

float unpackUNorm16To8(vec2 rg)
{
    uint hi = uint(rg.r * 15.0 + 0.5);
    uint lo = uint(rg.g * 15.0 + 0.5);

    return f8((hi << 4) | lo);
}

// 16 -> 24 bit

vec3 packUNorm16To24(vec2 v)
{
    uint a = u8(v.x);
    uint b = u8(v.y);

    uint packed = (a << 8) | b;

    return vec3(
        float((packed >> 16) & 0xFF),
        float((packed >> 8)  & 0xFF),
        float(packed & 0xFF)
    ) / 255.0;
}

vec2 unpackUNorm24To16(vec3 rgb)
{
    uint packed =
        (uint(rgb.r * 255.0 + 0.5) << 16) |
        (uint(rgb.g * 255.0 + 0.5) << 8)  |
         uint(rgb.b * 255.0 + 0.5);

    return vec2(
        f8((packed >> 8) & 0xFF),
        f8(packed & 0xFF)
    );
}

// ------------------------------------------------------------
// Raw unsigned (non-normalized floats)
// ------------------------------------------------------------

// 8 -> 16 bit

vec2 packU8ToRG(uint x)
{
    return vec2(
        float((x >> 4) & 0xF) / 15.0,
        float(x & 0xF) / 15.0
    );
}

uint unpackRGToU8(vec2 rg)
{
    uint hi = uint(rg.r * 15.0 + 0.5);
    uint lo = uint(rg.g * 15.0 + 0.5);

    return (hi << 4) | lo;
}

// 16 -> 24 bit

vec3 packU16ToRGB(uint x)
{
    return vec3(
        float((x >> 16) & 0xFF),
        float((x >> 8)  & 0xFF),
        float(x & 0xFF)
    ) / 255.0;
}

uint unpackRGBToU16(vec3 rgb)
{
    return
        (uint(rgb.r * 255.0 + 0.5) << 16) |
        (uint(rgb.g * 255.0 + 0.5) << 8)  |
         uint(rgb.b * 255.0 + 0.5);
}

// ------------------------------------------------------------
// Exact float bit packing (lossless)
// ------------------------------------------------------------

// 32 -> 4x8 bit

vec4 packFloatToRGBA(float v)
{
    uint x = floatBitsToUint(v);

    return vec4(
        float((x >> 24) & 0xFF),
        float((x >> 16) & 0xFF),
        float((x >> 8)  & 0xFF),
        float(x & 0xFF)
    ) / 255.0;
}

float unpackRGBAtoFloat(vec4 rgba)
{
    uint x =
        (uint(rgba.r * 255.0 + 0.5) << 24) |
        (uint(rgba.g * 255.0 + 0.5) << 16) |
        (uint(rgba.b * 255.0 + 0.5) << 8)  |
         uint(rgba.a * 255.0 + 0.5);

    return uintBitsToFloat(x);
}

#endif // PACKING_GLSL