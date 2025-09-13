#version 330 core
in vec2 fragTexCoord;
out vec4 fragColor;

uniform sampler2D _MainTex; // scene color

vec2 EncodeFloatRG(float v) {
    float enc = clamp(v, 0.0, 1.0) * 65535.0;
    float hi = floor(enc / 256.0);
    float lo = enc - hi * 256.0;
    return vec2(hi, lo) / 255.0;
}

float DecodeFloatRG(vec2 rg) {
    vec2 b = floor(rg * 255.0 + 0.5); // round to nearest
    return (b.x * 256.0 + b.y) / 65535.0;
}

void main()
{
    vec4 scene = texture(_MainTex, fragTexCoord);
    // treat non-black (or alpha) as occluder;
    float mask = any(greaterThan(scene.rgb, vec3(0.0))) ? 1.0 : 0.0;
    if (mask > 0.5)
    {
        // store the pixel's screen uv in rgba
        fragColor = vec4(EncodeFloatRG(fragTexCoord.r), EncodeFloatRG(fragTexCoord.g));
    }
    else
    {
        fragColor = vec4(0.0);
    }
}