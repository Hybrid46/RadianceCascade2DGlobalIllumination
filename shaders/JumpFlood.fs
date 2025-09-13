#version 330 core

in vec2 fragTexCoord;
out vec4 fragColor;

uniform sampler2D _MainTex;   // contains UVs from previous pass
uniform float _StepSize;
uniform vec2 _Aspect;         // screen / max(screen.x, screen.y)

vec2 EncodeFloatRG(float v) {
    float enc = clamp(v, 0.0, 1.0) * 65535.0;
    float hi = floor(enc / 256.0);
    float lo = enc - hi * 256.0;
    return vec2(hi, lo) / 255.0;
}

float DecodeFloatRG(vec2 rg) {
    vec2 b = floor(rg * 255.0 + 0.5); // round to nearest
    return (b.x * 256.0 + b.y) / 65535.0;
}

void main()
{
    float minDist = 1.0;
    vec2 bestUV   = vec2(0.0);

    for (int y = -1; y <= 1; ++y)
    {
        for (int x = -1; x <= 1; ++x)
        {
            vec2 peekUV = fragTexCoord + vec2(x, y) * _Aspect.yx * _StepSize;
            vec4 sampledPeek = texture(_MainTex, peekUV);
            vec2 peek   = vec2(DecodeFloatRG(sampledPeek.rg),DecodeFloatRG(sampledPeek.ba));

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

    fragColor = vec4(EncodeFloatRG(bestUV.x), EncodeFloatRG(bestUV.y));
}
