#version 330 core
precision highp float;

in vec2 fragTexCoord;
out vec4 fragColor;

uniform sampler2D _MainTex;   // contains UVs from previous pass
uniform float _StepSize;
uniform vec2 _Aspect;         // screen / max(screen.x, screen.y)

void main()
{
    float minDist = 1.0;
    vec2 bestUV   = vec2(0.0);

    for (int y = -1; y <= 1; ++y)
    {
        for (int x = -1; x <= 1; ++x)
        {
            vec2 peekUV = fragTexCoord + vec2(x, y) * _Aspect.yx * _StepSize;
            vec2 peek   = texture(_MainTex, peekUV).xy;

            if (peek.x != 0.0 && peek.y != 0.0) // skip empty
            {
                vec2 dir = peek - fragTexCoord;
                float d  = dot(dir, dir);

                if (d < minDist)
                {
                    minDist = d;
                    bestUV  = peek;
                }
            }
        }
    }

    fragColor = vec4(bestUV, 0.0, 1.0);
}
