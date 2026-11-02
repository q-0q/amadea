using System;
using System.Collections;
using UnityEngine;

public class MoveTeach : MonoBehaviour
{

    public string trickId;
    private Interactable _interactable;
    public Renderer textRenderer;
    private CustomPointLight _light;
    private ParticleSystem _particleSystem;

    private void Awake()
    {
        _interactable = GetComponentInChildren<Interactable>();
        _light = GetComponentInChildren<CustomPointLight>();
        _particleSystem = GetComponentInChildren<ParticleSystem>();

        _particleSystem.Play();
        
        if (SaveSystem.GetTrick(trickId))
        {
            _interactable.SetEnabled(false);
            textRenderer.material.SetFloat("_Weight_1", 1f);
            _light.gameObject.SetActive(false);
            _particleSystem.Stop();
            _particleSystem.Clear();
        };
    }

    private void OnEnable()
    {
        _interactable.OnInteracted += OnInteracted;
    }

    private void OnDisable()
    {
        _interactable.OnInteracted -= OnInteracted;
    }

    private void OnInteracted()
    {
        PlayerFsm.Singleton.AcquireTrick(trickId);
        _interactable.SetEnabled(false);
        textRenderer.material.SetFloat("_Weight_1", 1f);
        _light.gameObject.SetActive(false);
        _particleSystem.Stop();
        
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
