#version 330 core

in vec2 fragTexCoord;
out vec4 fragColor;

uniform sampler2D _MainTex;
uniform sampler2D _GITex;

void main()
{
    vec4 color = texture(_MainTex, fragTexCoord);
    vec3 gi = texture(_GITex, fragTexCoord).rgb;
    fragColor = vec4(color.rgb + gi, color.a);
}
