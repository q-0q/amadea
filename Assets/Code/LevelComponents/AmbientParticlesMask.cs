using System;
using System.Collections.Generic;
using UnityEngine;

public class AmbientParticlesMask : MonoBehaviour
{
    public static readonly List<Collider> AmbientParticlesMaskColliderRegistry = new List<Collider>();

    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
    }

    private void OnEnable()
    {
        AmbientParticlesMaskColliderRegistry.Add(_collider);
    }

    private void OnDisable()
    {
        AmbientParticlesMaskColliderRegistry.Remove(_collider);
    }
}
