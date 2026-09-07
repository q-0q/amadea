using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class ReactivePlatform : MonoBehaviour
{
    private void OnEnable()
    {
        PlayerFsm.OnPlayerParentTransformChanged += OnPlayerParentTransformChanged;
        PlayerFsm.OnPlayerFootstepEvent += OnFootstep;
    }

    private void OnDisable()
    {
        PlayerFsm.OnPlayerParentTransformChanged -= OnPlayerParentTransformChanged;
        PlayerFsm.OnPlayerFootstepEvent -= OnFootstep;
    }

    private void OnPlayerParentTransformChanged(Transform t, float momentum, float yVelocity)
    {
        if (t == null) return;
        if (!t.IsChildOf(transform)) return;
        var pos = PlayerFsm.Singleton.transform.position;
        
        var force = Vector3.up * (yVelocity * 0.5f);
        if (PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.Wallsquat))
            force = PlayerFsm.Singleton.transform.forward * 25f;
        if (PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.DashVault))
            force = Vector3.up * (-5f);
        ApplyImpact(force, pos);
    }
    
    private void OnFootstep()
    {
        var t = PlayerFsm.Singleton.parentTransform;
        if (t == null) return;
        if (!t.IsChildOf(transform)) return;
        var pos = PlayerFsm.Singleton.transform.position;
        var force = Vector3.up * -4f * Mathf.InverseLerp(0f, 10f, PlayerFsm.Singleton.GetMomentum());
        ApplyImpact(force, pos, 0.05f);
    }

[Header("Linear Spring Settings")]
    private float stiffness = 90f;
    private float damping = 5f;
    private float mass = 0.5f;

    [Header("Rotational Spring Settings")]
    private float rotationalStiffness = 90f;
    private float rotationalDamping = 5f;

    private Vector3 positionOffset;
    private Vector3 linearVelocity;

    private Vector3 rotationOffsetEuler;
    private Vector3 angularVelocity;

    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private float _bobOffset;

    private void Awake()
    {
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
        _bobOffset = Random.Range(0, 100f);
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 1. Translational Spring (Hooke's Law + Damping)
        Vector3 springForce = -stiffness * positionOffset;
        Vector3 dampingForce = -damping * linearVelocity;
        Vector3 linearAccel = (springForce + dampingForce) / mass;

        linearVelocity += linearAccel * dt;
        positionOffset += linearVelocity * dt;

        // 2. Rotational Spring
        Vector3 springTorque = -rotationalStiffness * rotationOffsetEuler;
        Vector3 dampingTorque = -rotationalDamping * angularVelocity;
        Vector3 angularAccel = (springTorque + dampingTorque) / mass;

        angularVelocity += angularAccel * dt;
        rotationOffsetEuler += angularVelocity * dt;

        // Apply transforms relative to initial state
        transform.localPosition = initialLocalPosition + positionOffset + (Vector3.up * (Mathf.Sin(Time.time * 2f + _bobOffset) * 0.5f));
        transform.localRotation = initialLocalRotation * Quaternion.Euler(rotationOffsetEuler);
    }

    private void ApplyImpact(Vector3 force, Vector3 impactPosition, float delay = 0f)

    {
        StartCoroutine(Coroutine());
        
        IEnumerator Coroutine()
        {
            yield return new WaitForSeconds(delay);
            // Direct linear impact
            linearVelocity += force / mass;

            // Torque = r x F
            // Offset vector from object origin to impact point
            Vector3 offset = impactPosition - transform.position;
            offset = offset.normalized * Mathf.Min(offset.magnitude, 5f);

            // Torque approaches zero as impact position approaches center
            Vector3 torqueImpulse = Vector3.Cross(offset, force);
            angularVelocity += torqueImpulse / mass;
        }
    }
}
