using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class DualWaterPointRegistry
{
    public static readonly List<DualWaterPoint> DualWaterPoints = new();
}

public class RippleEffect : MonoBehaviour
{
    [Header("Ripple Settings")]
    public int textureSize = 2048;
    public float damping = 0.99f;
    public float speed = 0.5f;
    
    [Header("Wake Settings")]
    public float wakeDecaySpeed = 9f; // Higher = faster fade
    
    
    [Header("Resources")]
    public Shader rippleShader;
    public Shader wakeFadeShader; // New Shader for simple fading
    public Material rippleStampMaterial;
    public Shader offsetShader; // Add this!
    
    [Header("Movement Settings")]
    public Transform playerTransform;
    public float worldSize = 100f;
    private Vector3 lastPlayerPos;
    private Material offsetMat;

    private RenderTexture currRT, prevRT, tempRT, wakeRT, wakeTempRT;
    private Material rippleMat, wakeMat;
    
    private static readonly int PrevTexID = Shader.PropertyToID("_PrevRT");
    private static readonly int CurrTexID = Shader.PropertyToID("_CurrentRT");
    private static readonly int DampingID = Shader.PropertyToID("_Damping");
    private static readonly int SpeedID = Shader.PropertyToID("_Speed");
    private static readonly int CenterID = Shader.PropertyToID("_Center");
    private static readonly int WakeFadeID = Shader.PropertyToID("_WakeFade");

    void Start()
    {
        playerTransform = transform.parent;
        
        currRT = CreateRT();
        prevRT = CreateRT();
        tempRT = CreateRT();
        wakeRT = CreateRT(); // Initialize Wake Texture
        wakeTempRT = CreateRT(); // Create the second temp buffer

        rippleMat = new Material(rippleShader);
        wakeMat = new Material(wakeFadeShader);
        offsetMat = new Material(offsetShader);

        ClearRT(currRT);
        ClearRT(prevRT);
        ClearRT(tempRT);
        ClearRT(wakeRT);
        ClearRT(wakeTempRT);

        // Set Globals for Shader Graph
        Shader.SetGlobalTexture("_PlayerRippleSimulationTexture", currRT);
        Shader.SetGlobalTexture("_PlayerWakeTexture", wakeRT);
        
        lastPlayerPos = playerTransform.position;
        StartCoroutine(SimulationLoop());
    }

    


    RenderTexture CreateRT()
    {
        RenderTexture rt = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        rt.filterMode = FilterMode.Bilinear;
        rt.wrapMode = TextureWrapMode.Clamp;
        rt.Create();
        return rt;
    }

    void ClearRT(RenderTexture rt)
    {
        RenderTexture active = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = active;
    }

    [Header("Optimization")]
    private float simulationFrequency = 240f;
    private float timer = 0f;

    IEnumerator SimulationLoop()
    {
        float fixedStep = 1f / simulationFrequency;

        while (true)
        {
            if (PhotoManager.Singleton.IsActive()) yield return null;
            ApplyWorldShift();
        
            timer += Time.deltaTime;

            // Run the simulation steps needed to catch up to real time
            while (timer >= fixedStep)
            {
                // --- 1. Ripple Simulation ---
                rippleMat.SetTexture(PrevTexID, prevRT);
                rippleMat.SetTexture(CurrTexID, currRT);
                rippleMat.SetFloat(DampingID, damping);
                rippleMat.SetFloat(SpeedID, speed);
        
                Graphics.Blit(null, tempRT, rippleMat);

                // Cycle Ripple textures
                RenderTexture oldPrev = prevRT;
                prevRT = currRT;
                currRT = tempRT;
                tempRT = oldPrev;

                timer -= fixedStep;
            }

            // --- 2. Wake Simulation (Already mostly FPS independent, but kept here) ---
            // Note: Wake decay is linear/exponential per frame, 
            // which is fine as long as deltaTime is used.
            float frameFade = Mathf.Pow(0.5f, Time.deltaTime * wakeDecaySpeed);
            wakeMat.SetFloat(WakeFadeID, frameFade);
            Graphics.Blit(wakeRT, wakeTempRT, wakeMat);
    
            RenderTexture wTemp = wakeRT;
            wakeRT = wakeTempRT;
            wakeTempRT = wTemp;

            // --- 3. Update Globals ---
            Shader.SetGlobalTexture("_PlayerRippleSimulationTexture", currRT);
            Shader.SetGlobalTexture("_PlayerWakeTexture", wakeRT);

            yield return null;
        }
    }

// Helper to convert World Position to UV coordinate relative to Player
    private Vector2 WorldToSimulationUV(Vector3 worldPos)
    {
        // Calculate the offset from the player (center of simulation)
        Vector3 diff = worldPos - playerTransform.position;

        // Map world distance (-worldSize/2 to worldSize/2) to UV (0 to 1)
        float u = (diff.x / worldSize) + 0.5f;
        float v = (diff.z / worldSize) + 0.5f;

        return new Vector2(u, v);
    }

    public void AddRipple(Vector3 worldPos, float strength, float radius)
    {
        Vector2 uv = WorldToSimulationUV(worldPos);
    
        // Safety check: Don't stamp if it's outside our simulation bounds
        if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) return;

        rippleStampMaterial.SetVector(CenterID, new Vector4(uv.x, uv.y, strength, radius));

        Graphics.Blit(currRT, tempRT, rippleStampMaterial);
        RenderTexture t = currRT; currRT = tempRT; tempRT = t;
    }

    public void AddWake(Vector3 worldPos, float strength, float radius)
    {
        Vector2 uv = WorldToSimulationUV(worldPos);

        if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) return;
    
        // Apply the specific wake multipliers you had previously
        rippleStampMaterial.SetVector(CenterID, new Vector4(uv.x, uv.y, strength * 6f, radius * 5f));
    
        Graphics.Blit(wakeRT, wakeTempRT, rippleStampMaterial);
        RenderTexture w = wakeRT; wakeRT = wakeTempRT; wakeTempRT = w;
    }

    // Update Shift to be generic so it doesn't accidentally use the Ripple temp for the Wake
        void ShiftTexture(RenderTexture rt, RenderTexture buffer)
        {
            Graphics.Blit(rt, buffer, offsetMat);
            Graphics.Blit(buffer, rt); 
        }
    
        void ApplyWorldShift()
        {
            Vector3 delta = playerTransform.position - lastPlayerPos;
            if (delta.sqrMagnitude < 0.0001f) return;

            // 1. Normalize the arbitrary up vector
            Vector3 up = PlayerFsm.Singleton.GetUpVector().normalized;

            // 2. Derive two orthogonal axes (Tangent and Bitangent) on the plane
            // We use Vector3.forward as a reference, with a fallback if 'up' is parallel to it
            Vector3 tangent = Vector3.Cross(up, Vector3.forward);
            if (tangent.sqrMagnitude < 0.0001f)
            {
                tangent = Vector3.Cross(up, Vector3.up);
            }
            tangent.Normalize();
    
            Vector3 bitangent = Vector3.Cross(up, tangent);

            // 3. Project the 3D movement delta onto the plane's 2D axes
            float u = Vector3.Dot(delta, tangent);
            float v = Vector3.Dot(delta, bitangent);

            Vector2 uvOffset = new Vector2(u / worldSize, v / worldSize);
            offsetMat.SetVector("_Offset", uvOffset);

            ShiftTexture(currRT, tempRT);
            ShiftTexture(prevRT, tempRT);
            ShiftTexture(wakeRT, wakeTempRT);

            lastPlayerPos = playerTransform.position;
        }

    void ShiftTexture(RenderTexture rt)
    {
        Graphics.Blit(rt, tempRT, offsetMat);
        Graphics.Blit(tempRT, rt); 
    }

    private void OnEnable()
    {
        PlayerFsm.OnPlayerRippleGenerated += AddRipple;
        PlayerSplashParticles.OnPlayerSplashParticleTriggerEnter += AddRipple;
        PlayerFsm.OnPlayerWakeGenerated += AddWake;
    }

    private void OnDisable()
    {
        PlayerFsm.OnPlayerRippleGenerated -= AddRipple;
        PlayerSplashParticles.OnPlayerSplashParticleTriggerEnter -= AddRipple;
        PlayerFsm.OnPlayerWakeGenerated -= AddWake;
    }
    
    
    // ----- Update DualWater shader positions and radii
    
    private Vector4[] _positions = new Vector4[64]; // Max 64 points
    private static readonly int PointsID = Shader.PropertyToID("_Points");
    private static readonly int CountID = Shader.PropertyToID("_PointCount");
    private void Update()
    {
        
        // if (DualWaterPointRegistry.DualWaterPoints == null || DualWaterPointRegistry.DualWaterPoints.Count == 0) return;

        int count = Mathf.Min(DualWaterPointRegistry.DualWaterPoints.Count, 64);
        
        for (int i = 0; i < 64; i++)
        {
            if (i < count)
            {
                Vector3 pos = DualWaterPointRegistry.DualWaterPoints[i].transform.position;
                // We store position in xyz and radius in w
                
                
                _positions[i] = new Vector4(pos.x, pos.y, pos.z, DualWaterPointRegistry.DualWaterPoints[i].Radius);
            }
            else
            {
                _positions[i] = new Vector4(0,0,0, 0);
            }
        }

        // Send the data to all shaders globally
        Shader.SetGlobalVectorArray(PointsID, _positions);
        Shader.SetGlobalInt(CountID, count);
    }
}