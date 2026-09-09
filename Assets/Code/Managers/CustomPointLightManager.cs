using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CustomPointLightManager : MonoBehaviour
{
    public static CustomPointLightManager Singleton;

    private void Awake()
    {
        Singleton = this;
    }
    
    public static readonly List<CustomPointLight> UnculledCustomPointLightRegistry = new();
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    
    // Update is called once per frame
    public void Update()
    {
        UpdateObserversVectorArray();
    }

    
    
    private static Vector4[] _lightPositions = new Vector4[64];
    private static Vector4[] _lightLerps = new Vector4[64];
    private static Vector4[] _lightColors = new Vector4[64];
    
    private static readonly int CountID = Shader.PropertyToID("_CustomPointLightCount");
    private static readonly int PositionsID = Shader.PropertyToID("_CustomPointLightPositions");
    private static readonly int LerpsID = Shader.PropertyToID("_CustomPointLightLerps");
    private static readonly int ColorsID = Shader.PropertyToID("_CustomPointLightColors");
    private static readonly int CullDistanceID = Shader.PropertyToID("_CustomPointLightCullDistance");
    private const float LightCullDistance = 100f;
    
    private void UpdateObserversVectorArray()
    {

        var culledLightColliders = Physics.OverlapSphere(Camera.main.transform.position, LightCullDistance,
            LayerMask.GetMask("CustomPointLight"), QueryTriggerInteraction.Collide).ToList();

        var lights = new List<CustomPointLight>();
        

        foreach (var unculledLight in UnculledCustomPointLightRegistry)
        {
            lights.Add(unculledLight);
        }

        foreach (var collider in culledLightColliders)
        {
            lights.Add(collider.GetComponent<CustomPointLight>());
        }
        
        int count = Mathf.Min(lights.Count, 64);
        
        for (int i = 0; i < 64; i++)
        {
            if (i < count)
            {
                var l = lights[i];
                Vector3 pos = l.transform.position;
                _lightPositions[i] = new Vector4(pos.x, pos.y, pos.z, 0f);
                
                // hijack the w value of lerp vector to indicate whether to ignore distance culling falloff in shader
                _lightLerps[i] = new Vector4(l.distanceLerpMin, l.distanceLerpMax, l.distanceLerpPower, l.preventDistanceCulling ? 1f : 0f);
                _lightColors[i] = new Vector4(l.Color.r, l.Color.g, l.Color.b, 0f);
            }
            else
            {
                _lightPositions[i] = new Vector4(0,0,0, 0);
            }
        }

        // Send the data to all shaders globally
        Shader.SetGlobalInt(CountID, count);
        Shader.SetGlobalVectorArray(PositionsID, _lightPositions);
        Shader.SetGlobalVectorArray(LerpsID, _lightLerps);
        Shader.SetGlobalVectorArray(ColorsID, _lightColors);
        Shader.SetGlobalFloat(CullDistanceID, LightCullDistance);
    }
    

}
