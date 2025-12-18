#version 330 core
precision highp float;

in vec2 fragTexCoord;
out vec4 fragColor;

uniform sampler2D _MainTex;
uniform sampler2D _GITex;

void main()
{
    vec4 color = texture(_MainTex, fragTexCoord);
    vec3 gi = texture(_GITex, fragTexCoord).rgb;
    fragColor = vec4(min(color.rgb + gi, 1), color.a);
}
