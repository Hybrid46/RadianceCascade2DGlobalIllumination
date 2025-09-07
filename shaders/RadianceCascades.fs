// Unity original shader
// Shader "Hidden/RC2DGI/RadianceCascades2DGI"
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

//             #define TAU 6.28318530718

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

//             sampler2D _ColorTex;
//             sampler2D _DistanceTex;

//             float2 _Aspect;
//             float _RayRange;

//             float2 _CascadeResolution;
//             uint _CascadeLevel;
//             uint _CascadeCount;

//             float _SkyRadiance;
//             float3 _SkyColor;
//             float3 _SunColor;
//             float _SunAngle;

//             v2f vert(appdata v)
//             {
//                 v2f o;
//                 o.vertex = UnityObjectToClipPos(v.vertex);
//                 o.uv = v.uv;

//                 return o;
//             }
            
//             float2 CalculateRayRange(uint index, uint count)
//             {
//                 //A relatively cheap way to calculate ray ranges instead of using pow()
//                 //The values returned : 0, 3, 15, 63, 255
//                 //Dividing by 3       : 0, 1, 5, 21, 85
//                 //and the distance between each value is multiplied by 4 each time

//                 float maxValue = (1 << (count*2)) - 1;
//                 float start = (1 << (index*2)) - 1;
//                 float end = (1 << (index*2 + 2)) - 1;

//                 float2 r = float2(start, end) / maxValue;
//                 return r * _RayRange;
//             }

//             float3 SampleSkyRadiance(float a0, float a1) {
//                 // Sky integral formula taken from "Analytic Direct Illumination" - Mathis
//                 // https://www.shadertoy.com/view/NttSW7
//                 const float3 SkyColor = _SkyColor;
//                 const float3 SunColor = _SunColor;
//                 const float SunA = _SunAngle;
//                 const float SSunS = 8.0;
//                 const float ISSunS = 1/SSunS;
//                 float3 SI = SkyColor*(a1-a0-0.5*(cos(a1)-cos(a0)));
//                 SI += SunColor*(atan(SSunS*(SunA-a0))-atan(SSunS*(SunA-a1)))*ISSunS;
//                 return SI * 0.16;
//             }

//             //Raymarching
//             float4 SampleRadianceSDF(float2 rayOrigin, float2 rayDirection, float2 rayRange)
//             {
//                 float t = rayRange.x;
//                 float4 hit = float4(0, 0, 0, 1);

//                 for (int i = 0; i < 32; i++)
//                 {
//                     float2 currentPosition = rayOrigin + t * rayDirection * _Aspect.yx;

//                     if (t > rayRange.y || currentPosition.x < 0 || currentPosition.y < 0 || currentPosition.x > 1 || currentPosition.y > 1)
//                     {
//                         break;
//                     }

//                     float distance = tex2D(_DistanceTex, currentPosition).r;

//                     if (distance < 0.001)
//                     {
//                         hit = float4(tex2D(_ColorTex, currentPosition).rgb, 0);
//                         break;
//                     }

//                     t += distance;
//                 }
    
//                 return hit;
//             }

//             float4 frag(v2f input) : SV_Target
//             {
//                 float2 pixelIndex = floor(input.uv.xy * _CascadeResolution);
    
//                 uint blockSqrtCount = 1 << _CascadeLevel;//Another way to write pow(2, _CascadeLevel)
    
//                 float2 blockDim = _CascadeResolution / blockSqrtCount;
//                 float2 block2DIndex = floor(pixelIndex / blockDim);
//                 float blockIndex = block2DIndex.x + block2DIndex.y * blockSqrtCount;
    
//                 float2 coordsInBlock = fmod(pixelIndex, blockDim);
    
//                 float4 finalResult = 0;
                
//                 float2 rayOrigin = (coordsInBlock + 0.5) * blockSqrtCount;
//                 float2 rayRange = CalculateRayRange(_CascadeLevel, _CascadeCount);
    
//                 for (int i = 0; i < 4; i++)
//                 {
//                     float angleStep = TAU / (blockSqrtCount * blockSqrtCount * 4);
//                     float angleIndex = blockIndex * 4 + i;
//                     float angle = (angleIndex + 0.5) * angleStep;
    
//                     float2 rayDirection = float2(cos(angle), sin(angle));
                    
//                     float4 radiance = SampleRadianceSDF(rayOrigin / _CascadeResolution, rayDirection, rayRange);
                    
//                     if(radiance.a != 0){
//                         if (_CascadeLevel != _CascadeCount - 1)
//                         {
//                             //Merging with the Upper Cascade (_MainTex)
//                             float2 position = coordsInBlock * 0.5 + 0.25;
//                             float2 positionOffset = float2(fmod(angleIndex, blockSqrtCount * 2), floor(angleIndex / (blockSqrtCount * 2)));

//                             position = clamp(position, 0.5, blockDim * 0.5 - 0.5);
            
//                             float4 rad = tex2D(_MainTex, (position + positionOffset * blockDim * 0.5) / _CascadeResolution);
                        
//                             radiance.rgb += rad.rgb * radiance.a;
//                             radiance.a *= rad.a;
//                         }else{
//                             //if this is the Top Cascade and there is no other cascades to merge with, we merge it with the sky radiance instead
//                             float3 sky = SampleSkyRadiance(angle, angle + angleStep) * _SkyRadiance;    
//                             radiance.rgb += (sky / angleStep) * 2;
//                         }
//                     }
                    
//                     finalResult += radiance * 0.25;
//                 }

//                 return finalResult;
//             }

//             ENDCG
//         }
//     }
// }

#version 330 core
in vec2 fragTexCoord;
out vec4 fragColor;

uniform sampler2D _ColorTex;
uniform sampler2D _DistanceTex;

uniform vec2 _CascadeResolution;
uniform int  _CascadeLevel;
uniform int  _CascadeCount;
uniform vec2 _Aspect;
uniform vec2 _RayRange;

uniform vec3 _SkyColor;
uniform vec3 _SunColor;
uniform float _SunAngle;
uniform float _SkyRadiance;

// --- helpers ----------------------------------------------------

float sampleSky(float a0, float a1)
{
    const float3 skyCol = _SkyColor;
    const float3 sunCol = _SunColor;
    const float sunA   = _SunAngle;
    const float ss     = 8.0;
    const float invss  = 1.0/ss;
    float3 si = skyCol * (a1-a0-0.5*(cos(a1)-cos(a0)));
    si += sunCol * (atan(ss*(sunA-a0))-atan(ss*(sunA-a1)))*invss;
    return clamp(si, 0.0, 1.0)*0.16;
}

// --- ray‑march ----------------------------------------------------

vec4 march(float2 origin, float2 dir, float2 range)
{
    float t = range.x;
    vec4 hit = vec4(0.0);
    for (int i = 0; i < 32; ++i)
    {
        float2 pos = origin + t*dir*_Aspect.yx;
        if (t > range.y ||
            pos.x < 0.0 || pos.y < 0.0 || pos.x > 1.0 || pos.y > 1.0)
            break;

        float dist = texture(_DistanceTex, pos).r;
        if (dist < 0.001)
        {
            hit = vec4(texture(_ColorTex, pos).rgb, 0.0);
            break;
        }
        t += dist;
    }
    return hit;
}

// --- main ---------------------------------------------------------

void main()
{
    vec2 pixel = floor(fragTexCoord * _CascadeResolution);
    float2 blockDim = _CascadeResolution / pow(2.0, float(_CascadeLevel));
    float2 blockIdx = floor(pixel / blockDim);
    float block = blockIdx.x + blockIdx.y * pow(2.0, float(_CascadeLevel));

    float2 rayOrigin = (mod(pixel, blockDim) + 0.5) * pow(2.0, float(_CascadeLevel));
    float2 rayRange = vec2(0.0, _RayRange);

    vec4 res = vec4(0.0);

    int samples = 4; // four rays per block
    for (int i = 0; i < samples; ++i)
    {
        float a = (block + 0.5 + float(i)/float(samples)) *
                  (6.28318530718 / float(pow(2.0, 2.0*float(_CascadeLevel) + 2.0));

        float2 dir = vec2(cos(a), sin(a));
        vec4 radiance = march(rayOrigin/_CascadeResolution, dir, rayRange);

        if (radiance.a > 0.0)
        {
            if (_CascadeLevel != _CascadeCount-1)
            {
                // merge with upper cascade
                float2 pos = mod(blockIdx*0.5, blockDim*0.5) + 0.25;
                vec4 up   = texture(_ColorTex, (pos + blockIdx*blockDim*0.5) / _CascadeResolution);
                radiance.rgb += up.rgb * radiance.a;
                radiance.a   *= up.a;
            }
            else
            {
                // sky contribution
                vec3 sky = sampleSky(a, a+6.28318530718/float(samples));
                radiance.rgb += sky * _SkyRadiance;
            }
        }
        res += radiance * 0.25;
    }

    fragColor = res;
}
