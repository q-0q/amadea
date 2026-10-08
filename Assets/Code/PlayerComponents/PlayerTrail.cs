using System;
using UnityEngine;

public class PlayerTrail : MonoBehaviour
{

    private TrailRenderer _trailRenderer;
    private ParticleSystem _particles;

    private void Awake()
    {
        _trailRenderer = GetComponent<TrailRenderer>();
        _trailRenderer.emitting = false;
        _particles = GetComponentInChildren<ParticleSystem>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.Updraft) && PlayerFsm.Singleton.TimeInCurrentState() < PlayerFsm.UpdraftDuration - 1f)
        {
            if (!_trailRenderer.emitting)
            {
                _particles.Play();
                _trailRenderer.emitting = true;
            }
        }
        else
        {
            _particles.Stop();
            _trailRenderer.emitting = false;
        }
        
    }
}
