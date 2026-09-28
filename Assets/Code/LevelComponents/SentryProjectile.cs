using System;
using System.Collections;
using Cinemachine;
using FMOD;
using FMOD.Studio;
using UnityEngine;
using Random = UnityEngine.Random;
using Util = Code.Misc.Util;

public class SentryProjectile : MonoBehaviour
{

    private const float Speed = 220f;
    private Vector3 _direction;
    private float _lifetime;

    private bool _impacted;
    private ParticleSystem _impactParticles;

    private CinemachineImpulseSource _impulse;
    private GameObject _deathCollider;
    private EventInstance _ambientInstance;
    
    public void SetDirection(Vector3 direction)
    {
        _direction = direction;
        //
        // _direction = Vector3.RotateTowards(
        //     direction, 
        //     Random.onUnitSphere, 
        //     5f * Mathf.Deg2Rad, 
        //     0f
        // );
    }

    private void Awake()
    {
        _impacted = false;
        _lifetime = 0f;
        _impactParticles = transform.Find("Fx").Find("ImpactParticles").GetComponent<ParticleSystem>();
        _impulse = GetComponentInChildren<CinemachineImpulseSource>();
        _deathCollider = GetComponentInChildren<PlayerDeathCollider>().gameObject;
        _deathCollider.SetActive(false);
        _ambientInstance =
            FMODUnity.RuntimeManager.CreateInstance(
                FMODUnity.RuntimeManager.PathToEventReference("event:/SentryProjectileSynth"));
        
        FMODUnity.RuntimeManager.AttachInstanceToGameObject(_ambientInstance, gameObject);
        _ambientInstance.start();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Util.InvokeSphereEffect(transform.position - Vector3.up, Vector3.one * 6f, 1.25f, 0.8f, -0.5f);
    }

    // Update is called once per frame
    void Update()
    {
        if (_impacted) return;
        _lifetime += Time.deltaTime;
        
        var toPlayer = PlayerFsm.Singleton.transform.position - transform.position;
        // _direction = Vector3.RotateTowards(_direction, toPlayer, 1f * Time.deltaTime, 1000f).normalized;
        
        var positionDelta = _direction.normalized * (Speed * Time.deltaTime);

        if (Physics.Raycast(transform.position, positionDelta, out var hit, positionDelta.magnitude,
                Fsm.GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore))
        {
            StartCoroutine(ImpactCoroutine());
            transform.rotation = Quaternion.LookRotation(hit.normal, Vector3.up);
        }
        
        transform.position += positionDelta;
        
        
        if (toPlayer.magnitude < 2f)
        {
            StartCoroutine(ImpactCoroutine());
            PlayerFsm.Singleton.InvokePlayerDeath();

        }

        if (_lifetime >= 5f)
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator ImpactCoroutine()
    {
        _impacted = true;
        _impactParticles.Play();
        _impulse.GenerateImpulse();


        var radius = 5f;
        var toPlayer = PlayerFsm.Singleton.transform.position - transform.position;
        print(toPlayer.magnitude);
        if (toPlayer.magnitude < radius)
        {
            var origin = transform.position;
            // PlayerFsm.Singleton.InvokePlayerDeath();
            if (!Physics.Raycast(origin, toPlayer, out var hit, Mathf.Min(radius, toPlayer.magnitude),
                    Fsm.GetEnvironmentalLayermask(),
                    QueryTriggerInteraction.Ignore)) PlayerFsm.Singleton.InvokePlayerDeath();
        }
        
        FMODUnity.RuntimeManager.PlayOneShotAttached(FMODUnity.RuntimeManager.PathToEventReference("event:/SentryProjectileRubble"), gameObject);
        FMODUnity.RuntimeManager.PlayOneShotAttached(FMODUnity.RuntimeManager.PathToEventReference("event:/SentryProjectileImpact"), gameObject);
        _ambientInstance.stop(STOP_MODE.ALLOWFADEOUT);
        Util.InvokeSphereEffect(transform.position - Vector3.up, Vector3.one * 8f, 1.5f, 0.8f, -1f);
        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        _ambientInstance.stop(STOP_MODE.ALLOWFADEOUT);
    }
}
