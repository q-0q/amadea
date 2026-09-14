using System;
using UnityEngine;

public class LightPlatform : MonoBehaviour
{
    private Collider _collider;
    private ParticleSystem _particleSystem;
    private Renderer _renderer;
    private CustomPointLight _light;

    private void Awake()
    {
        _collider = transform.Find("Cube").GetComponent<Collider>();
        _particleSystem = GetComponentInChildren<ParticleSystem>();
        _renderer = GetComponentInChildren<Renderer>();
        _light = GetComponentInChildren<CustomPointLight>();
        
        TurnOff();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void TurnOff()
    {
        _collider.enabled = false;
        _particleSystem.Stop();
        _particleSystem.Clear();
        _renderer.enabled = false;
        _light.gameObject.SetActive(false);
    }
    
    public void TurnOn()
    {
        _collider.enabled = true;
        _particleSystem.Play();
        _renderer.enabled = true;
        _light.gameObject.SetActive(true);
    }
}
