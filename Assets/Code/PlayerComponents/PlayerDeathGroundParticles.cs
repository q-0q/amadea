using System;
using UnityEngine;

public class PlayerDeathGroundParticles : MonoBehaviour
{

    private ParticleSystem _particles;

    private void Awake()
    {
        _particles = GetComponentInChildren<ParticleSystem>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        
        
        var parent = PlayerFsm.Singleton.parentTransform;
        if (parent == null)
        {
            _particles.Stop();
        }
        
        else
        {
            parent.TryGetComponent(out PlayerDeathGround deathGround);
            if (deathGround == null)
            {
                _particles.Stop();
            }
        }

        
    }

    private void OnEnable()
    {
        PlayerFsm.OnPlayerDeathGroundImmune += OnImmune;
    }

    private void OnImmune()
    {
        if (_particles.isEmitting) return;
        
        _particles.Play();
    }

    private void OnDisable()
    {
        PlayerFsm.OnPlayerDeathGroundImmune -= OnImmune;
    }
}
