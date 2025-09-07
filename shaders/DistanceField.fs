#version 330 core
in vec2 fragTexCoord;
out vec4 fragColor;

uniform sampler2D _MainTex;   // from JumpFlood (UVs)
uniform vec2 _Aspect;

void main()
{
    vec2 uv = texture(_MainTex, fragTexCoord).rg;
    fragColor = vec4(distance(fragTexCoord, uv), 0.0, 0.0, 1.0);
}
