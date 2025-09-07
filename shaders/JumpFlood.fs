//Unity original shader
// Shader "Hidden/RC2DGI/JumpFlood"
// {
//     Properties
//     {
//         _MainTex ("Texture", 2D) = "white" {}
//     }
//     SubShader
//     {
//         Cull Off
//         ZWrite Off
//         ZTest Always

//         Pass
//         {
//             CGPROGRAM
            
//             #pragma vertex vert
//             #pragma fragment frag
            
//             #include "UnityCG.cginc"

//             struct appdata
//             {
//                 float4 vertex : POSITION;
//                 float2 uv : TEXCOORD0;
//             };

//             struct v2f
//             {
//                 float2 uv : TEXCOORD0;
//                 float4 vertex : SV_POSITION;
//             };

//             sampler2D _MainTex;
//             float _StepSize;
//             float2 _Aspect;

//             v2f vert(appdata v)
//             {
//                 v2f o;
//                 o.vertex = UnityObjectToClipPos(v.vertex);
//                 o.uv = v.uv;

//                 return o;
//             }

//             float2 frag(v2f i) : SV_Target
//             {
//                 float min_dist = 1;
//                 float2 min_dist_uv = float2(0, 0);

//                 [unroll]
//                 for (int y = -1; y <= 1; y ++)
//                 {
//                     [unroll]
//                     for (int x = -1; x <= 1; x ++)
//                     {
//                         float2 peekUV = i.uv + float2(x, y) * _Aspect.yx * _StepSize;

//                         float2 peek = tex2D(_MainTex, peekUV).xy;
//                         if (all(peek))
//                         {
//                             float2 dir = peek - i.uv;
//                             float dist = dot(dir, dir);
//                             if (dist < min_dist)
//                             {
//                                 min_dist = dist;
//                                 min_dist_uv = peek;
//                             }
//                         }
//                     }
//                 }

//                 return min_dist_uv;
//             }
//             ENDCG
//         }
//     }
// }

#version 330 core
in vec2 fragTexCoord;
out vec4 fragColor;

uniform sampler2D _MainTex;   // contains UVs from ScreenUV pass
uniform float _StepSize;
uniform vec2 _Aspect;         // screen / max(screen.x, screen.y)

void main()
{
    float minDist = 1.0;
    vec2 bestUV  = vec2(0.0);

    for (int y = -1; y <= 1; ++y)
    {
        for (int x = -1; x <= 1; ++x)
        {
            vec2 peekUV = fragTexCoord + vec2(x, y) * _Aspect.yx * _StepSize;
            vec2 peek   = texture(_MainTex, peekUV).xy;

            if (peek.x != 0.0 && peek.y != 0.0)           // skip empty
            {
                vec2 dir  = peek - fragTexCoord;
                float d   = dot(dir, dir);
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
