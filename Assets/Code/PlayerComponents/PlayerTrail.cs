using System;
using System.Collections;
using UnityEngine;

public class PlayerTrail : MonoBehaviour
{

    private TrailRenderer _trailRenderer;
    private ParticleSystem _particles;
    private bool _isStopping = false;

    private void Awake()
    {
        _trailRenderer = GetComponentInChildren<TrailRenderer>();
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
                _isStopping = false;
                // _trailRenderer.Clear();
                
                _trailRenderer.material.color = Color.white;
                _particles.Play();
                _trailRenderer.emitting = true;
            }
        }
        else
        {
            if (_trailRenderer.emitting)
            {
                StartCoroutine(StopTrail());
            }
        }
        
    }

    private IEnumerator StopTrail()
    {

        float t = 0f;
        float d = 1f;
        _isStopping = true;
        _trailRenderer.transform.SetParent(null);
        while (t < d)
        {
            if (!_isStopping) break;
            _trailRenderer.material.color = Color.white * (t / d);
            yield return null;
            t += Time.deltaTime;
        }
        
        _particles.Stop();
        _trailRenderer.emitting = false;
        _trailRenderer.Clear();
        _trailRenderer.transform.SetParent(transform);
        _trailRenderer.transform.localPosition = Vector3.zero;
    }
}
