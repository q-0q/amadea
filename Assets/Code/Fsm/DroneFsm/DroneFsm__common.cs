using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Code.TriggerParams;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public partial class DroneFsm
{


    private DroneStation _station;
    private GameObject _lights;
    private Vector3 _previousTargetPosition;
    private Vector3 _velocityRef;
    private PlayerInput _playerInput;
    private Transform _vibrator;
    private ParticleSystem _pulseParticles;

    public static event Action<Vector3> OnDronePulsed;
    public const float DronePulseRadius = 18f;

    private void OnTriggerProxyStay(Collider obj)
    {

    }

    private void OnTriggerProxyExit(Collider obj)
    {

    }

private void FollowPlayer()
{
    if (GameMenu.Singleton.IsMenuOpen()) return;
    
    var targetPosition = GetTargetFollowPosition();
    var predictionTime = 0.25f;
    var responsiveness = 0.35f;
    
    // Tilt settings (Consider moving these to class-level variables to adjust in the Inspector)
    float maxSpeedForTilt = 10f; // Speed at which maximum tilt is reached
    float maxTiltAngle = 40f;    // Maximum forward tilt in degrees

    // 1. Calculate the target's current speed and direction (velocity)
    Vector3 targetVelocity = (targetPosition - _previousTargetPosition) / Time.deltaTime;
    targetVelocity = new Vector3(targetVelocity.x, targetVelocity.y * 0.25f, targetVelocity.z);
    
    // 2. Predict where the target will be in the future
    Vector3 predictedPosition = targetPosition + (targetVelocity * predictionTime);

    // 3. Smoothly move towards that predicted position
    var newPosition = Vector3.SmoothDamp(
        transform.position, 
        predictedPosition, 
        ref _velocityRef, 
        responsiveness
    );

    var deltaPosition = newPosition - transform.position;
    transform.position = newPosition;
    
    // 4. Handle Rotation and Tilt
    // Calculate the current actual speed of the follower
    float currentSpeed = deltaPosition.magnitude / Time.deltaTime;

    // Use InverseLerp to find where our speed falls between 0 and maxSpeedForTilt (returns 0.0 to 1.0)
    float speedNormalized = Mathf.InverseLerp(0f, maxSpeedForTilt, currentSpeed);

    // Use Lerp to map that 0.0-1.0 value to an actual rotation angle
    float tiltAngle = Mathf.Lerp(0f, maxTiltAngle, speedNormalized);

    // Get the standard rotation pointing in the direction of movement
    Quaternion baseLookRotation = Quaternion.LookRotation(PlayerFsm.Singleton.transform.forward, Vector3.up);

    // Multiply by an Euler rotation on the X axis to pitch it forward
    Quaternion targetRotation = baseLookRotation * Quaternion.Euler(tiltAngle, 0f, 0f);

    // Smoothly rotate towards the final tilted rotation
    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);

    // 5. Save current target position for the next frame's math
    _previousTargetPosition = targetPosition;
}

    private void FollowPlayerRedux()
    {
        var targetPosition = PlayerFsm.Singleton.transform.position + (Vector3.up * 7f) +
                             (PlayerFsm.Singleton.transform.forward * Mathf.Lerp(0f, 5f,
                                 Mathf.InverseLerp(0, 10f, PlayerFsm.Singleton.GetMomentum())));

        transform.position = Vector3.Slerp(transform.position, targetPosition, Time.deltaTime * 5f);
        
        Quaternion baseLookRotation = Quaternion.LookRotation(PlayerFsm.Singleton.transform.forward, Vector3.up);

        // Multiply by an Euler rotation on the X axis to pitch it forward
        Quaternion targetRotation = baseLookRotation * Quaternion.Euler(0f, 0f, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
    }

    private Vector3 GetTargetFollowPosition()
    {
        return PlayerFsm.Singleton.transform.position + (Vector3.up * 7f) +
               (PlayerFsm.Singleton.transform.forward * Mathf.Lerp(0f, 3f,
                   Mathf.InverseLerp(0, 10f, PlayerFsm.Singleton.GetMomentum())));
    }

    public void SetDroneStation(DroneStation station)
    {
        _station = station;
    }

    private void ReturnToIdlePosition(float speed)
    {
        transform.position =
            Vector3.Lerp(transform.position, _station.GetDronePosition().position, Time.deltaTime * speed);

        transform.rotation =
            Quaternion.Slerp(transform.rotation, _station.GetDronePosition().rotation, Time.deltaTime * 5f);
    }

}