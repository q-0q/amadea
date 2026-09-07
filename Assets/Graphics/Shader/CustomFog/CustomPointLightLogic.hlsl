uniform float4 _CustomPointLightPositions[64];
uniform float4 _CustomPointLightLerps[64];
uniform float4 _CustomPointLightColors[64];
uniform int _CustomPointLightCount;


float IL(float minVal, float maxVal, float value)
{
    return saturate((value - minVal) / (maxVal - minVal));
}

void GetCustomPointLightColor_float(float3 WorldPos, float3 WorldNormal, float3 InputColor, out float3 OutColor)
{

    
    float3 finalColor = InputColor;
    
    for (int i = 0; i < _CustomPointLightCount; i++)
    {
        
        float3 lightPosition = float3(_CustomPointLightPositions[i].x, _CustomPointLightPositions[i].y, _CustomPointLightPositions[i].z);
        float3 lightDir = normalize(lightPosition - WorldPos);
        
        float d = distance(WorldPos, lightPosition);

        float lerpMax = _CustomPointLightLerps[i].y;
        if (d > lerpMax) continue;
        
        float lerpMin = _CustomPointLightLerps[i].x;
        float lerpPower = _CustomPointLightLerps[i].z;
        float lerp = IL(lerpMin, lerpMax, d);
        lerp = 1 - pow(lerp, lerpPower);
        
        // float3 color = float3(_CustomPointLightColors[i].x, _CustomPointLightColors[i].y, _CustomPointLightColors[i].z) * lerp;
        float NdotL = saturate(dot(WorldNormal, lightDir));
        float3 color = _CustomPointLightColors[i].rgb * lerp * NdotL;
        
        finalColor += color;
    }
    
    OutColor = finalColor;
}


