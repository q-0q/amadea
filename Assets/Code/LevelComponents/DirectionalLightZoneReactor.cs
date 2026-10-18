using System;
using Unity.VisualScripting;
using UnityEngine;

public class DirectionalLightZoneReactor : MonoBehaviour
{

    private float _baseIntensity;

    private Color _baseColor;
    private Color _baseAmbientColor;

    private Light _light;

    private void Awake()
    {
        _light = GetComponent<Light>();
        _baseIntensity = _light.intensity;
        _baseColor = _light.color;
        _baseAmbientColor = RenderSettings.ambientLight;
    }

    private void Start()
    {

        // OnPlayerTeleported(Vector3.zero);
    }

    // Update is called once per frame
    void Update()
    {
        GetValuesFromPlayerPosition(out var desiredIntensity, out var desiredColor, out var desiredAmbientColor, out var speed);
        _light.intensity = Mathf.Lerp(_light.intensity, desiredIntensity, speed * Time.deltaTime);
        _light.color = Color.Lerp(_light.color, desiredColor, speed * Time.deltaTime);
        RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, desiredAmbientColor, speed * Time.deltaTime);
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
        GetValuesFromPlayerPosition(out var desiredIntensity, out var desiredColor, out var desiredAmbientColor, out _);
        _light.intensity = desiredIntensity;
        _light.color = desiredColor;
        RenderSettings.ambientLight = desiredAmbientColor;
    }


    private void GetValuesFromPlayerPosition(out float intensity, out Color color, out Color ambientColor, out float speed)
    {
        intensity = _baseIntensity;
        color = _baseColor;
        ambientColor = _baseAmbientColor;
        speed = 1.0f;
        
        var playerPosition = PlayerFsm.Singleton.transform.position;
        foreach (var zone in DirectionalLightZone.Registry)
        {
            var d = Vector3.Distance(playerPosition, zone.transform.position);
            if (d < zone.radius)
            {
                intensity = zone.intensity;
                color = zone.Color;
                speed = zone.speed;
                ambientColor = Color.Lerp(_baseAmbientColor, zone.ambientColor, zone.ambientColorMod);
            }
        }
    }
}
