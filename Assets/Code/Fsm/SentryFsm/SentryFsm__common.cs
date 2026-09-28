using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Code.TriggerParams;
using FMOD.Studio;
using UnityEngine;
using UnityEngine.Serialization;

public partial class SentryFsm
{

    public bool passive = false;
    
    private Vector3 _trackingRotationalVelocity;
    private Vector3 _lastTrackedPlayerPosition;
    
    private LineRenderer _lineRenderer;
    private GameObject _laserEnd;
    public Transform eye;
    private float _obstructionTimer;
    private const float MaxObstructionDuration = 1f;
    
    
    
    private const float MaxAngularSpeed = 15f;
    private const  float AngularDamping = 1f;

    private Vector3 currentAngularVelocity;

    private Vector3 _searchEnterAxis;
    private Vector3 _searchEnterSpeed;

    private bool _blinking;
    private float _blinkTimer;

    private const float DownwardsBlindspotAngle = 30f;
    public float Range = 80f;
    private float wakeDurationOffset;

    private EventInstance _blinkingInstance;
    private EventInstance _trackingInstance;

    private void OnTriggerProxyStay(Collider obj)
    {

    }

    private void OnTriggerProxyExit(Collider obj)
    {

    }
    
    private void UpdateTrackingVelocity()
    {
        Vector3 directionToTarget = GetPlayerPosition() - eye.position;
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        Quaternion deltaRotation = targetRotation * Quaternion.Inverse(eye.rotation);
    
        deltaRotation.ToAngleAxis(out float angleInDegrees, out Vector3 axis);
        if (angleInDegrees > 180f) angleInDegrees -= 360f;

        Vector3 targetAngularVelocity = Vector3.zero;

        if (Mathf.Abs(angleInDegrees) > 0.01f && axis.sqrMagnitude > 0.001f)
        {
            float desiredSpeed = angleInDegrees / Time.deltaTime;
            float mod = Mathf.Lerp(0.5f, 3f, Mathf.InverseLerp(5f, 10f, Mathf.Abs(angleInDegrees)));
            float clampedSpeed = Mathf.Clamp(desiredSpeed, -MaxAngularSpeed, MaxAngularSpeed) * mod;
            targetAngularVelocity = axis.normalized * clampedSpeed;
        }
        
        float smoothingSpeed = 4f;
        currentAngularVelocity = Vector3.Lerp(currentAngularVelocity, targetAngularVelocity, Time.deltaTime * smoothingSpeed);
    }

    private void RotateWithTrackingVelocity()
    {
        float currentSpeed = currentAngularVelocity.magnitude;
        if (currentSpeed > 0.01f)
        {
            // 1. Calculate the proposed rotation for this frame
            Quaternion step = Quaternion.AngleAxis(currentSpeed * Time.deltaTime, currentAngularVelocity.normalized);
            Quaternion proposedRotation = step * eye.rotation;

            // 2. Measure angle between proposed forward vector and -transform.up
            Vector3 proposedForward = proposedRotation * Vector3.forward;
            Vector3 straightDown = -transform.up;
            float angleToDown = Vector3.Angle(proposedForward, straightDown);

            // 3. Clamp direction if inside the deadzone
            if (angleToDown < DownwardsBlindspotAngle)
            {
                Vector3 axis = Vector3.Cross(straightDown, proposedForward);
                if (axis.sqrMagnitude < 0.0001f)
                {
                    axis = transform.right; // Fallback axis if perfectly aligned
                }

                // Push forward vector back to the edge of the deadzone boundary
                Vector3 clampedForward = Quaternion.AngleAxis(DownwardsBlindspotAngle, axis.normalized) * straightDown;
                Vector3 proposedUp = proposedRotation * Vector3.up;
            
                proposedRotation = Quaternion.LookRotation(clampedForward, proposedUp);
            }

            eye.rotation = proposedRotation;
        }
    }

    private void DampenTrackingVelocity()
    {
        currentAngularVelocity = Vector3.Lerp(currentAngularVelocity, Vector3.zero, Time.deltaTime * AngularDamping);
    }

    private Vector3 GetPlayerPosition()
    {
        return PlayerFsm.Singleton.transform.position + Vector3.up * 1.25f;
    }
    
    private void UpdateLineRenderer()
    {

        if (passive)
        {
            _lineRenderer.SetPosition(1, eye.position);
            return;
        }
        
        var maxDistance = 1000f;
        var didPlayerCollide = false;
        var didEnvCollide = false;

        RaycastHit playerHit;
        RaycastHit envHit;
        if (Physics.Raycast(eye.position, eye.forward, out playerHit, maxDistance, LayerMask.GetMask("Player"), QueryTriggerInteraction.Collide))
        {
            didPlayerCollide = true;
        }
        if (Physics.Raycast(eye.position, eye.forward, out envHit, maxDistance, GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore))
        {
            didEnvCollide = true;
        }

        if (didPlayerCollide || didEnvCollide)
        {
            if (!didPlayerCollide) _lineRenderer.SetPosition(1, envHit.point);
            else if (!didEnvCollide) _lineRenderer.SetPosition(1, playerHit.point);
            else
            {
                _lineRenderer.SetPosition(1, playerHit.distance < envHit.distance ? playerHit.point : envHit.point);
            }
        }
        else
        {
            _lineRenderer.SetPosition(1, eye.position + (eye.forward * maxDistance));
        }

        _laserEnd.transform.position = Vector3.MoveTowards(_lineRenderer.GetPosition(1), _lineRenderer.GetPosition(0), 0.5f);
    }
    
}