using System;
using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using UnityEngine;
using FMODUnity;
using STOP_MODE = FMOD.Studio.STOP_MODE;

public class FMODEventInstanceDistanceCuller : MonoBehaviour
{

    public static FMODEventInstanceDistanceCuller Singleton; 
    private float checkInterval = 0.25f;
    
    private readonly List<CullableInstance> registeredInstances = new List<CullableInstance>();

    public struct CullableInstance
    {
        public EventInstance Instance;
        public Transform Transform;
        public float SqrMaxDistance;
    }

    public void Register(EventInstance instance, Transform t, float maxDistance)
    {
        registeredInstances.Add(new CullableInstance 
        { 
            Instance = instance, 
            Transform = t,
            SqrMaxDistance = maxDistance * maxDistance 
        });
    }

    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        StartCoroutine(CullRoutine());
    }

    private IEnumerator CullRoutine()
    {
        while (true)
        {
            
            Vector3 listenerPos = Camera.main.transform.position;

            for (int i = 0; i < registeredInstances.Count; i++)
            {
                var item = registeredInstances[i];
                float sqrDist = (item.Transform.position - listenerPos).sqrMagnitude;
                item.Instance.getPlaybackState(out var pS);
                
                if (sqrDist <= item.SqrMaxDistance)
                {
                    if (pS == PLAYBACK_STATE.STOPPED) item.Instance.start();
                }
                else
                {
                    if (pS == PLAYBACK_STATE.PLAYING) item.Instance.stop(STOP_MODE.ALLOWFADEOUT);
                }
            }

            yield return new WaitForSeconds(checkInterval);
        }
    }
}