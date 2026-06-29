#version 330 core
precision highp float;

in vec2 fragTexCoord;
out vec4 fragColor;

uniform sampler2D _MainTex;
uniform vec2      _Resolution;
uniform float     _BlurRadius = 1.0;

void main()
{
    vec2 texelSize = 1.0 / _Resolution;
    vec4 result = vec4(0.0);

    // 3x3 Gaussian weights (normalized to 1.0)
    float wCorner = 0.0625f;
    float wEdge   = 0.125f;
    float wCenter = 0.250f;

    // Corners
    result += texture(_MainTex, fragTexCoord + vec2(-1, -1) * texelSize * _BlurRadius) * wCorner;
    result += texture(_MainTex, fragTexCoord + vec2( 1, -1) * texelSize * _BlurRadius) * wCorner;
    result += texture(_MainTex, fragTexCoord + vec2(-1,  1) * texelSize * _BlurRadius) * wCorner;
    result += texture(_MainTex, fragTexCoord + vec2( 1,  1) * texelSize * _BlurRadius) * wCorner;

    // Edges
    result += texture(_MainTex, fragTexCoord + vec2( 0, -1) * texelSize * _BlurRadius) * wEdge;
    result += texture(_MainTex, fragTexCoord + vec2( 0,  1) * texelSize * _BlurRadius) * wEdge;
    result += texture(_MainTex, fragTexCoord + vec2(-1,  0) * texelSize * _BlurRadius) * wEdge;
    result += texture(_MainTex, fragTexCoord + vec2( 1,  0) * texelSize * _BlurRadius) * wEdge;

    // Center
    result += texture(_MainTex, fragTexCoord) * wCenter;

    fragColor = result;
}
