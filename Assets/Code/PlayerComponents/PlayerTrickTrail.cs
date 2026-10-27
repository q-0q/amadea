using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class PlayerTrickTrail : MonoBehaviour
{
    public Transform follow;
    public float emitDuration;
    public float emitWPower = 1.0f;
    public float alphaFalloffStart = 0.1f;
    public float alphaFalloffEnd = 0.2f;
    public float alphaWPower = 1.0f;
    public List<string> subscriptions;
    public float delay = 0f;
    public float particlesDelay = 0f;

    private TrailRenderer _trail;
    private CustomTrailRenderer _customTrail;
    private SplineContainer _spline;
    private ParticleSystem _particleSystem;
    private bool _endRequested;

    private void Awake()
    {
        _trail = GetComponentInChildren<TrailRenderer>();
        if (_trail != null)
        {
            _trail.transform.SetParent(null);
            _trail.emitting = false;
        }
        
        _customTrail = GetComponentInChildren<CustomTrailRenderer>();
        if (_customTrail != null)
        {
            _customTrail.emitting = false;
        }
        
        
        
        _endRequested = false;
        _particleSystem = GetComponentInChildren<ParticleSystem>();
        _spline = GetComponentInChildren<SplineContainer>();




    }
    

    private void OnEnable()
    {
        PlayerFsm.OnPlayerTrickTrailStartRequested += OnStartRequested;
        PlayerFsm.OnPlayerTrickTrailEndRequested += OnEndRequested;
    }

    private void OnDisable()
    {
        PlayerFsm.OnPlayerTrickTrailStartRequested -= OnStartRequested;
        PlayerFsm.OnPlayerTrickTrailEndRequested -= OnEndRequested;
    }


    private void OnEndRequested(string obj)
    {
        if (!subscriptions.Contains(obj)) return;
        _endRequested = true;

    }

    private void OnStartRequested(string obj)
    {
        if (!subscriptions.Contains(obj)) return;
        StartCoroutine(TrailCoroutine());
        StartCoroutine(ParticlesCoroutine());


        IEnumerator ParticlesCoroutine()
        {
            if (_particleSystem == null) yield break;
            yield return new WaitForSeconds(particlesDelay);
            _particleSystem.Play();
        }
        
        IEnumerator TrailCoroutine()
        {
            _endRequested = false;

            if (_trail != null) _trail.transform.position = _spline.EvaluatePosition(0);
            if (_customTrail != null) _customTrail.transform.position = _spline.EvaluatePosition(0);
            
            yield return new WaitForSeconds(delay);
            
            if (_trail != null) _trail.Clear();
            if (_customTrail != null) _customTrail.ClearTrail();

            var t = 0f;
            var d = Mathf.Max(alphaFalloffEnd, emitDuration);
            while (t < d)
            {
                var emitW = Mathf.Clamp01(t / emitDuration);
                emitW = Mathf.Clamp01(Mathf.Pow(emitW, emitWPower));

                if (_trail != null)
                {
                    _trail.transform.rotation = Quaternion.LookRotation(_trail.transform.forward, _spline.EvaluateUpVector(emitW));
                    if (emitW > 0.35f) _trail.transform.position = Vector3.Lerp(_trail.transform.position, _spline.EvaluatePosition(emitW), Time.deltaTime * 25f);
                    else _trail.transform.position = _spline.EvaluatePosition(emitW);
                }

                if (_customTrail != null)
                {
                    _customTrail.transform.rotation = Quaternion.LookRotation(_customTrail.transform.forward, _spline.EvaluateUpVector(emitW));
                    _customTrail.transform.position = _spline.EvaluatePosition(emitW);
                }
                
                var alphaW = 1f - Mathf.InverseLerp(alphaFalloffStart, alphaFalloffEnd, t);
                if (_trail != null) _trail.material.SetFloat("_Alpha", alphaW);
                if (_customTrail != null) _customTrail.lineRenderer.material.SetFloat("_Alpha", alphaW);
                
                t += Time.deltaTime;
                yield return null;
                if (_trail != null) _trail.emitting = true;
                if (_customTrail != null) _customTrail.emitting = true;

                if (_endRequested)
                {
                    if (obj == "Dash" && (PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.Dash) ||
                                          PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState
                                              .FallAfterDash)))
                    {
                        
                    }
                    else
                    {
                        _endRequested = false;
                        break;
                    }
                }
            }
            
            if (_trail != null) _trail.material.SetFloat("_Alpha", 0f);
            if (_trail != null) _trail.emitting = false;
            
            if (_customTrail != null) _customTrail.lineRenderer.material.SetFloat("_Alpha", 0f);
            if (_customTrail != null) _customTrail.emitting = false;
        }
    }

    // Update is called once per frame
    void Update()
    {


        
    }
}
