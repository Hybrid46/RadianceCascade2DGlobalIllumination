#version 330 core
in vec2 fragTexCoord;
out vec4 fragColor;

uniform sampler2D _MainTex;
uniform vec2 _Aspect;

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
    // Calculate distance in screen space
    vec2 screenPos = fragTexCoord;
    vec4 storedPos = texture(_MainTex, fragTexCoord);
    vec2 encodedPos = vec2(DecodeFloatRG(storedPos.rg), DecodeFloatRG(storedPos.ba));

    vec2 aspectAdjustedFragCoord = fragTexCoord * _Aspect;
    vec2 aspectAdjustedEncodedPos = encodedPos * _Aspect;
    
    float dist = distance(aspectAdjustedFragCoord, aspectAdjustedEncodedPos);

    fragColor = vec4(EncodeFloatRG(dist), 0.0, 1.0);
}