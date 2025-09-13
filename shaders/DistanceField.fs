#version 330 core
in vec2 fragTexCoord;
out vec4 fragColor;

//both textures are same size so aspect ratio is unnecessary!
//uniform vec2 _Aspect;
uniform sampler2D _MainTex;

void main()
{
    vec2 uv = texture(_MainTex, fragTexCoord).rg;
    // Calculate distance in screen space
    vec2 screenPos = fragTexCoord; // * _Aspect;
    vec2 storedPos = uv; // * _Aspect;
    float dist = distance(screenPos, storedPos);
    fragColor = vec4(dist, 0.0, 0.0, 1.0);
}