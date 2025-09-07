#version 330 core

in vec2 fragTexCoord;
out vec4 fragColor;

uniform sampler2D u_scene;
uniform sampler2D u_gi;
uniform vec2   u_resolution;

void main()
{
    vec3 scene = texture(u_scene, fragTexCoord).rgb;
    vec3 gi    = texture(u_gi, fragTexCoord).rgb;
    vec3 col   = scene + gi;                // additive
    // Tonemap / gamma
    col = col / (col + vec3(1.0));
    fragColor = vec4(col, 1.0);
}
