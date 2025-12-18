#version 330 core
//#define PACKING
precision highp float;

in vec2 fragTexCoord;
out vec4 fragColor;

uniform sampler2D _MainTex;

#ifdef PACKING
vec2 packUNorm16(float v)
{
    v = clamp(v, 0.0, 1.0);
    uint x = uint(v * 65535.0 + 0.5);

    return vec2(
        float((x >> 8) & uint(255)),
        float(x & uint(255))
    ) / 255.0;
}
#endif

void main()
{
    vec2 uv = texture(_MainTex, fragTexCoord).rg;
    // Calculate distance in screen space
    vec2 screenPos = fragTexCoord;
    vec2 storedPos = uv;
    float dist = distance(screenPos, storedPos);

    #ifdef PACKING
        fragColor = vec4(packUNorm16(dist), 0.0, 1.0);
    #else
        fragColor = vec4(dist, 0.0, 0.0, 1.0);
    #endif
}