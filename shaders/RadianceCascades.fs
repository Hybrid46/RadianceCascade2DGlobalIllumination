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

uniform sampler2D _MainTex;
uniform sampler2D _ColorTex;
uniform sampler2D _DistanceTex;

uniform vec2 _Aspect;             // x = aspect.x, y = aspect.y (same semantics as Unity float2)
uniform float _RayRange;

uniform vec2 _CascadeResolution;  // resolution used for cascades (width, height)
uniform int _CascadeLevel;        // current cascade level (int)
uniform int _CascadeCount;        // total number of cascades

uniform float _SkyRadiance;
uniform vec3 _SkyColor;
uniform vec3 _SunColor;
uniform float _SunAngle;

const float TAU = 6.28318530718;

// ----------------- Helpers -----------------

vec2 CalculateRayRange(int index, int count)
{
    // replicate logic in original (bit shifts). returns [start, end] scaled by _RayRange
    int maxValue = (1 << (count * 2)) - 1;
    int start = (1 << (index * 2)) - 1;
    int end = (1 << (index * 2 + 2)) - 1;
    vec2 r = vec2(float(start), float(end)) / float(maxValue);
    return r * _RayRange;
}

vec3 SampleSkyRadiance(float a0, float a1)
{
    // Sky integral formula from "Analytic Direct Illumination"
    const float SSunS = 8.0;
    const float ISSunS = 1.0 / SSunS;

    vec3 SI = _SkyColor * (a1 - a0 - 0.5 * (cos(a1) - cos(a0)));
    SI += _SunColor * (atan(SSunS * (_SunAngle - a0)) - atan(SSunS * (_SunAngle - a1))) * ISSunS;
    return SI * 0.16;
}

// Ray-marching over SDF stored in _DistanceTex, color in _ColorTex
vec4 SampleRadianceSDF(vec2 rayOrigin, vec2 rayDirection, vec2 rayRange)
{
    float t = rayRange.x;
    vec4 hit = vec4(0.0, 0.0, 0.0, 1.0);

    for (int i = 0; i < 32; ++i)
    {
        vec2 currentPosition = rayOrigin + t * rayDirection * _Aspect.yx;

        // if outside range, break
        if (t > rayRange.y || currentPosition.x < 0.0 || currentPosition.y < 0.0 || currentPosition.x > 1.0 || currentPosition.y > 1.0)
        {
            break;
        }

        float distance = texture(_DistanceTex, currentPosition).r;

        if (distance < 0.001)
        {
            vec3 c = texture(_ColorTex, currentPosition).rgb;
            hit = vec4(c, 0.0);
            break;
        }

        t += distance;
    }

    return hit;
}

// ----------------- Main -----------------

void main()
{
    // pixel index in cascade grid
    vec2 pixelIndex = floor(fragTexCoord * _CascadeResolution);

    int blockSqrtCount = 1 << _CascadeLevel; // pow(2, _CascadeLevel)
    vec2 blockDim = _CascadeResolution / float(blockSqrtCount);
    vec2 block2DIndex = floor(pixelIndex / blockDim);
    float blockIndexF = block2DIndex.x + block2DIndex.y * float(blockSqrtCount);
    int blockIndex = int(blockIndexF + 0.5);

    vec2 coordsInBlock = mod(pixelIndex, blockDim);

    vec4 finalResult = vec4(0.0);

    // ray origin is in some normalized coordinates relative to cascade grid
    vec2 rayOrigin = (coordsInBlock + 0.5) * float(blockSqrtCount);
    vec2 rayRange = CalculateRayRange(_CascadeLevel, _CascadeCount);

    for (int i = 0; i < 4; ++i)
    {
        float angleStep = TAU / float(blockSqrtCount * blockSqrtCount * 4);
        int angleIndex = blockIndex * 4 + i;
        float angle = (float(angleIndex) + 0.5) * angleStep;

        vec2 rayDirection = vec2(cos(angle), sin(angle));

        vec4 radiance = SampleRadianceSDF(rayOrigin / _CascadeResolution, rayDirection, rayRange);

        if (radiance.a != 0.0)
        {
            if (_CascadeLevel != (_CascadeCount - 1))
            {
                // Merging with the Upper Cascade (_MainTex)
                // position logic from original shader
                vec2 position = coordsInBlock * 0.5 + 0.25;
                float blockSqrtCountTimes2 = float(blockSqrtCount * 2);
                float positionOffsetX = mod(float(angleIndex), blockSqrtCountTimes2);
                float positionOffsetY = floor(float(angleIndex) / blockSqrtCountTimes2);

                // clamp position between 0.5 and blockDim*0.5 - 0.5 (original clamps scalars; replicate)
                vec2 minPos = vec2(0.5);
                vec2 maxPos = blockDim * 0.5 - vec2(0.5);
                position = clamp(position, minPos, maxPos);

                vec2 positionOffset = vec2(positionOffsetX, positionOffsetY);

                vec2 samplePos = (position + positionOffset * (blockDim * 0.5)) / _CascadeResolution;
                vec4 rad = texture(_MainTex, samplePos);

                radiance.rgb += rad.rgb * radiance.a;
                radiance.a *= rad.a;
            }
            else
            {
                // top cascade: merge with sky radiance
                vec3 sky = SampleSkyRadiance(angle, angle + angleStep) * _SkyRadiance;
                radiance.rgb += (sky / angleStep) * 2.0;
            }
        }

        finalResult += radiance * 0.25;
    }

    fragColor = finalResult;
}
