#version 330 core

in vec2 fragTexCoord;
out vec4 fragColor;

uniform sampler2D u_cascade0;
uniform sampler2D u_cascade1;
uniform sampler2D u_cascade2;

void main()
{
    // Sample all cascades, weighted by distance from the probe
    vec3 near  = texture(u_cascade0, fragTexCoord).rgb;
    vec3 mid   = texture(u_cascade1, fragTexCoord).rgb;
    vec3 far   = texture(u_cascade2, fragTexCoord).rgb;

    // Simple blending with fixed weights
    vec3 result = near * 0.5 + mid * 0.35 + far * 0.15;
    fragColor = vec4(result, 1.0);
}
