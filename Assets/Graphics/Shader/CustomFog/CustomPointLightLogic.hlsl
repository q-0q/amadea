uniform float4 _CustomPointLightPositions[64];
uniform float4 _CustomPointLightLerps[64];
uniform float4 _CustomPointLightColors[64];
uniform int _CustomPointLightCount;
uniform float _CustomPointLightCullDistance;


float IL(float minVal, float maxVal, float value)
{
    return saturate((value - minVal) / (maxVal - minVal));
}

void GetCustomPointLightColor_float(float3 WorldPos, float3 WorldNormal, float3 InputColor, float CameraDepth, out float3 OutColor)
{

    
    float3 finalColor = InputColor;
    
    for (int i = 0; i < _CustomPointLightCount; i++)
    {
        

        float3 lightToPixel = _CustomPointLightPositions[i].xyz - WorldPos;
        float distSq = dot(lightToPixel, lightToPixel);
        float lerpMax = _CustomPointLightLerps[i].y;
        
        if (distSq > lerpMax * lerpMax) continue;
        
        float d = sqrt(distSq);
        float3 lightDir = lightToPixel / d; // Cheaper than calling normalize() separately

        
        float lerpMin = _CustomPointLightLerps[i].x;
        float lerpPower = _CustomPointLightLerps[i].z;
        float lerpValue = IL(lerpMin, lerpMax, d);
        
        lerpValue = 1 - pow(lerpValue, lerpPower);

        // hijack the w value of lerp vector to indicate whether to ignore distance culling falloff in shader
        float cullDistanceFalloff = saturate(IL(_CustomPointLightCullDistance, _CustomPointLightCullDistance - 30.0, CameraDepth));
        float ignoreCulling = _CustomPointLightLerps[i].w;
        cullDistanceFalloff = lerp(cullDistanceFalloff, 1.0, ignoreCulling);
        
        float NdotL = saturate(dot(WorldNormal, lightDir));
        float3 color = _CustomPointLightColors[i].rgb * lerpValue * NdotL * cullDistanceFalloff;
        finalColor += color;
    }
    
    OutColor = finalColor;
}


