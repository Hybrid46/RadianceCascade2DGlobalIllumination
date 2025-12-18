#version 330 core

precision highp float;

in vec2 fragTexCoord;
out vec4 fragColor;

uniform sampler2D _MainTex;

void main()
{
    vec4 mainColor = texture(_MainTex, fragTexCoord);

    // treat non-black as occluder;
    if (any(greaterThan(mainColor.rgb, vec3(0.0))))
    {
        // store the pixel's screen uv in rg
        fragColor = vec4(fragTexCoord, 0.0, 1.0);
    }
    else
    {
        fragColor = vec4(0.0, 0.0, 0.0, 1.0);
    }
}