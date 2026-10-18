using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class CustomFogManager : MonoBehaviour
{
    public static CustomFogManager Singleton;

    private void Awake()
    {
        Singleton = this;
    }

    private CustomFogController _currentController;
    public static readonly List<CustomFogController> CustomFogControllerRegistry = new();
    public static readonly List<CustomFogObserver> CustomFogObserverRegistry = new();
    public const float LerpStrength = 1f;

    private Material _vignetteMaterial;

    public void SetCurrentController(CustomFogController controller, bool snap = false)
    {
        _currentController = controller;
        if (snap) LerpAllValues(-1f);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (PlayerFsm.Singleton == null) return;


        _currentController = null;
        UpdateCurrentController(true);
    }

    private void UpdateCurrentController(bool snap)
    {
        if (PlayerFsm.Singleton == null) return;
        
        var colliders = Physics.OverlapSphere(PlayerFsm.Singleton.transform.position, 5f,
            LayerMask.GetMask("CustomFogController"), QueryTriggerInteraction.Collide);
        CustomFogController _currentHighestPriorityController = null;
        foreach (var collider in colliders)
        {
            if (collider == null) continue;
            collider.TryGetComponent(out CustomFogController controller);
            if (controller == null) continue;
            if (controller.Priority < 0) continue;
            if (_currentHighestPriorityController != null && _currentHighestPriorityController.Priority >= controller.Priority) continue;
            _currentHighestPriorityController = controller;
            SetCurrentController(controller, snap);
        }
    }

    // Update is called once per frame
    public void Update()
    {
        if (!Application.isPlaying)
        {
            ApplyEditorSettings();
            return;
        }
        
        UpdateObserversVectorArray();
        UpdateCurrentController(false);
        if (_currentController == null) return;
        LerpAllValues(LerpStrength * _currentController.LerpStrengthMultiplier);
    }

    private void LerpAllValues(float strength)
    {
        if (_currentController == null) return;
            
        LerpColor("_CustomFogColor", _currentController.Color, strength);
            
        LerpFloat("_CustomFogNearLowerBlanketMin", _currentController.NearLowerBlanketMin, strength);
        LerpFloat("_CustomFogNearLowerBlanketMax", _currentController.NearLowerBlanketMax, strength);
        LerpFloat("_CustomFogNearLowerBlanketPower", _currentController.NearLowerBlanketPower, strength);
        LerpFloat("_CustomFogMaxNearLowerBlanketFactor", _currentController.MaxNearLowerBlanketFactor, strength);
        
        LerpFloat("_CustomFogFarLowerBlanketMin", _currentController.FarLowerBlanketMin, strength);
        LerpFloat("_CustomFogFarLowerBlanketMax", _currentController.FarLowerBlanketMax, strength);
        LerpFloat("_CustomFogFarLowerBlanketPower", _currentController.FarLowerBlanketPower, strength);
        LerpFloat("_CustomFogMaxFarLowerBlanketFactor", _currentController.MaxFarLowerBlanketFactor, strength);
        
        LerpFloat("_CustomFogLowerBlanketDistanceMin", _currentController.LowerBlanketDistanceMin, strength);
        LerpFloat("_CustomFogLowerBlanketDistanceMax", _currentController.LowerBlanketDistanceMax, strength);
        LerpFloat("_CustomFogLowerBlanketDistancePower", _currentController.LowerBlanketDistancePower, strength);
        
        LerpFloat("_CustomFogUpperBlanketMin", _currentController.UpperBlanketMin, strength);
        LerpFloat("_CustomFogUpperBlanketMax", _currentController.UpperBlanketMax, strength);
        LerpFloat("_CustomFogUpperBlanketPower", _currentController.UpperBlanketPower, strength);
        LerpFloat("_CustomFogMaxUpperBlanketFactor", _currentController.MaxUpperBlanketFactor, strength);
            
        LerpFloat("_CustomFogDepthMin", _currentController.DepthMin, strength);
        LerpFloat("_CustomFogDepthMax", _currentController.DepthMax, strength);
        LerpFloat("_CustomFogDepthPower", _currentController.DepthPower, strength);
        LerpFloat("_CustomFogMaxDepthFactor", _currentController.MaxDepthFactor, strength);
        
        LerpFloat("_CustomFogSkyboxLift", _currentController.SkyboxLift, strength);
        
        LerpFloat("_CustomVignetteLerpMin", _currentController.VignetteLerpMin, strength);
        LerpFloat("_CustomVignetteLerpMax", _currentController.VignetteLerpMax, strength);
        LerpFloat("_CustomVignetteAlpha", _currentController.VignetteAlpha, strength);
        LerpColor("_CustomVignetteColor", _currentController.VignetteColor, strength);
        LerpFloat("_CustomVignetteLerpPower", _currentController.VignetteLerpPower, strength);
    }

    private static void ApplyEditorSettings()
    {
        
        _observerPositions[0] = new Vector4(0,0,0, 0);
        Shader.SetGlobalInt(CountID, 1);
        
        Shader.SetGlobalColor("_CustomFogColor", Color.darkGray);
        Shader.SetGlobalFloat("_CustomFogMaxNearLowerBlanketFactor", 0);
        Shader.SetGlobalFloat("_CustomFogMaxFarLowerBlanketFactor", 0);
        Shader.SetGlobalFloat("_CustomFogMaxUpperBlanketFactor", 0);
        Shader.SetGlobalFloat("_CustomFogMaxDepthFactor", 0);
    }

    private static void LerpFloat(string name, float value, float strength)
    {
        if (strength < 0)
        {
            Shader.SetGlobalFloat(name, value);
            return;
        }
        Shader.SetGlobalFloat(name, Mathf.Lerp(Shader.GetGlobalFloat(name), value, Time.deltaTime * strength));
    }
    
    private static void LerpColor(string name, Color value, float strength)
    {
        if (strength < 0)
        {
            Shader.SetGlobalColor(name, value);
            return;
        }
        Shader.SetGlobalColor(name, Color.Lerp(Shader.GetGlobalColor(name), value, Time.deltaTime * strength));
    }

    private static void LerpVector(string name, Vector3 value, float strength)
    {
        if (strength < 0)
        {
            Shader.SetGlobalVector(name, value);
            return;
        }
        Shader.SetGlobalVector(name, Vector3.Lerp(Shader.GetGlobalVector(name), value, Time.deltaTime * strength));
    }

    
    private static Vector4[] _observerPositions = new Vector4[64]; // Max 64 points
    private static readonly int PointsID = Shader.PropertyToID("_CustomFogObservers");
    private static readonly int CountID = Shader.PropertyToID("_CustomFogObserverCount");
    
    private void UpdateObserversVectorArray()
    {
        int count = Mathf.Min(CustomFogObserverRegistry.Count, 64);
        
        for (int i = 0; i < 64; i++)
        {
            if (i < count)
            {
                Vector3 pos = CustomFogObserverRegistry[i].transform.position;
                // We store position in xyz and radius in w
                
                
                _observerPositions[i] = new Vector4(pos.x, pos.y, pos.z, CustomFogObserverRegistry[i].isPlayer ? -1.0f : CustomFogObserverRegistry[i].radiusMultiplier);
            }
            else
            {
                _observerPositions[i] = new Vector4(0,0,0, 0);
            }
        }

        // Send the data to all shaders globally
        Shader.SetGlobalVectorArray(PointsID, _observerPositions);
        Shader.SetGlobalInt(CountID, count);
    }

    private void OnEnable()
    {
        PlayerFsm.OnPlayerTeleported += OnPlayerTeleported;
    }

    private void OnDisable()
    {
        PlayerFsm.OnPlayerTeleported -= OnPlayerTeleported;
    }

    private void OnPlayerTeleported(Vector3 obj)
    {
        UpdateCurrentController(true);
    }
}
