#version 330 core
in vec2 fragTexCoord;
out vec4 fragColor;

uniform sampler2D _MainTex; // scene color

void main()
{
    vec4 scene = texture(_MainTex, fragTexCoord);
    // treat non-black (or alpha) as occluder;
    float mask = any(greaterThan(scene.rgb, vec3(0.0))) ? 1.0 : 0.0;
    if (mask > 0.5)
    {
        // store the pixel's screen uv in rg
        fragColor = vec4(fragTexCoord, 0.0, 1.0);
    }
    else
    {
        fragColor = vec4(0.0);
    }
}