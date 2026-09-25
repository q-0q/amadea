using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MusicDistanceAttenuatorRegistry
{
    public static List<MusicDistanceAttenuator> Attenuators = new();
}
public class MusicDistanceAttenuator : MonoBehaviour
{

    public float minDistance = 30f;

    public float maxDistance = 40f;

    public float scalar = 1.0f;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, minDistance);
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnEnable() => MusicDistanceAttenuatorRegistry.Attenuators.Add(this);
    void OnDisable() => MusicDistanceAttenuatorRegistry.Attenuators.Remove(this);
}
