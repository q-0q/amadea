using System;
using FMOD.Studio;
using UnityEngine;

public class LightPlatform : MonoBehaviour
{
    private Collider _collider;
    private ParticleSystem _particleSystem;
    private Renderer _renderer;
    private CustomPointLight _light;
    private bool isPlayerParented = false;
    private float _timeSinceParentParented = 0f;
    private const float PlayerKillTimeout = 1.5f;
    private EventInstance _eventInstance;

    private void Awake()
    {
        _collider = transform.Find("Cube").GetComponent<Collider>();
        _particleSystem = GetComponentInChildren<ParticleSystem>();
        _renderer = GetComponentInChildren<Renderer>();
        _light = GetComponentInChildren<CustomPointLight>();
        _eventInstance = FMODUnity.RuntimeManager.CreateInstance("event:/LightPlatformCharge");
        
        TurnOff();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void Update()
    {
        if (!PlayerFsm.Singleton.Machine.IsInState(GravityFsm.GravityFsmState.RespectParentTransform)) isPlayerParented = false;
        
        if (isPlayerParented) _timeSinceParentParented += Time.deltaTime;
        else
        {
            _timeSinceParentParented = 0f;
            _eventInstance.stop(STOP_MODE.ALLOWFADEOUT);
        }
        
        // _renderer.material.SetFloat("_GlowWeight", (_timeSinceParentParented / PlayerKillTimeout) * 0.75f);
        // if (_timeSinceParentParented > PlayerKillTimeout) PlayerFsm.Singleton.InvokePlayerDeath();
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
    
    
    private void OnEnable()
    {
        PlayerFsm.OnPlayerParentTransformChanged += OnPlayerParentTransformChanged;
    }

    private void OnDisable()
    {
        PlayerFsm.OnPlayerParentTransformChanged -= OnPlayerParentTransformChanged;
    }

    private void OnPlayerParentTransformChanged(Transform t, float momentum, float yVelocity)
    {
        if (t == null)
        {
            isPlayerParented = false;
            _eventInstance.stop(STOP_MODE.ALLOWFADEOUT);
            return;
        };
        if (!t.IsChildOf(transform))
        {
            isPlayerParented = false;
            _eventInstance.stop(STOP_MODE.ALLOWFADEOUT);
            return;
        }
        
        isPlayerParented = true;
        
        // FMODUnity.RuntimeManager.AttachInstanceToGameObject(_eventInstance, PlayerFsm.Singleton.gameObject);
        // _eventInstance.start();

    }
}
