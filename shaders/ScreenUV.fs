#version 330 core
in vec2 fragTexCoord;
out vec4 fragColor;

uniform sampler2D _MainTex;

void main()
{
    float alpha = texture(_MainTex, fragTexCoord).a;
    if (alpha > 0.5)
        fragColor = vec4(fragTexCoord, 0.0, 1.0);
    else
        fragColor = vec4(0.0);
}