using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class CustomPointLight : MonoBehaviour
{



    public Color Color;
    public float distanceLerpMin = 5;
    public float distanceLerpMax = 15f;
    public float distanceLerpPower = 0.5f;

    private void Awake()
    {
        var sphereCollider = gameObject.AddComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        gameObject.layer = LayerMask.NameToLayer("CustomPointLight");
        SetWorldRadius(sphereCollider, distanceLerpMax);
    }

    private void Start()
    {
    }

    private void OnEnable()
    {
        CustomPointLightManager.CustomPointLightRegistry.Add(this);
    }

    private void OnDisable()
    {
        CustomPointLightManager.CustomPointLightRegistry.Remove(this);
    }

    private void Update()
    {

    }
    
    public void SetWorldRadius(SphereCollider collider, float desiredWorldRadius)
    {
        if (collider == null) return;

        Vector3 scale = collider.transform.lossyScale;
        
        // Find the largest axis of the lossyScale
        float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));

        if (Mathf.Approximately(maxScale, 0f))
        {
            Debug.LogWarning("Transform scale is zero; cannot calculate valid collider radius.");
            return;
        }

        // Adjust the local radius to hit the target world size
        collider.radius = desiredWorldRadius / maxScale;
    }
}
