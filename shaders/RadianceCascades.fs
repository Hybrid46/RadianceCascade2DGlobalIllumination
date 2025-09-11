#version 330 core

in vec2 fragTexCoord;
out vec4 fragColor;

uniform sampler2D _MainTex;
uniform sampler2D _EmissiveTex;
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
            vec3 baseColor = texture(_ColorTex, currentPosition).rgb;
            vec3 emission  = texture(_EmissiveTex, currentPosition).rgb;

            if (length(emission) > 0.0) {
                hit = vec4(emission, 1.0); // <-- Treat emission as radiance
            } else {
                hit = vec4(baseColor, 0.0); // <-- normal surface
            }
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
