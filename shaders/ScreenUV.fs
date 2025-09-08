#version 330 core
in vec2 fragTexCoord;
out vec4 fragColor;

uniform sampler2D _MainTex;

void main()
{
    float alpha = any(greaterThan(texture(_MainTex, fragTexCoord).rgb, vec3(0.0))) ? 1.0 : 0.0;
    if (alpha > 0.5)
        fragColor = vec4(fragTexCoord, 0.0, 1.0);
    else
        fragColor = vec4(0.0);
}