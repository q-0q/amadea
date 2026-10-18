using System;
using UnityEngine;

public class SceneAmbientParticles : MonoBehaviour
{
    private ParticleSystem ps;

    void Start()
    {
        if (PlayerFsm.Singleton == null) return;

        transform.SetParent(PlayerFsm.Singleton.transform);
        transform.localPosition = Vector3.zero;

        ps = GetComponent<ParticleSystem>();
        if (ps == null) return;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Play();
        
        
        
        var trigger = ps.trigger;
        foreach (var collider in AmbientParticlesMask.AmbientParticlesMaskColliderRegistry)
        {
            trigger.AddCollider(collider);
        }
    }

    private void Update()
    {
        // if (ps == null) return;
        //
        // var isMaskActive = false;
        // foreach (var mask in AmbientParticlesMask.AmbientParticlesMaskRegistry)
        // {
        //     var d = Vector3.Distance(transform.position, mask.transform.position);
        //     if (d < mask.radius)
        //     {
        //         isMaskActive = true;
        //         break;
        //     }
        // }
        //
        // if (isMaskActive) ps.Stop(true);
        // else
        // {
        //     var main = ps.main;
        //     main.prewarm = false;
        //     ps.Play();
        //     main.prewarm = true;
        // };

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
        return;
        if (ps == null) return;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Play();
    }
}