using System;
using System.Collections.Generic;
using UnityEngine;

public class DirectionalLightZone : MonoBehaviour
{
    public float radius  = 100f;
    public Color Color;
    public float intensity;

    public float ambientColorMod = 0f;
    public Color ambientColor = Color.white;
    public float speed = 1.0f;

    public static readonly List<DirectionalLightZone> Registry = new List<DirectionalLightZone>();

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    private void OnEnable()
    {
        Registry.Add(this);
    }

    private void OnDisable()
    {
        Registry.Remove(this);
    }
}
