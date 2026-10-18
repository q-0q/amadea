using System;
using Cinemachine;
using UnityEngine;

public partial class PlayerWeaponFsm
{
    private Transform _subTransform;
    private Vector3 _subTransformBaseLocalPosition;
    private Vector3 _impaleActiveTargetPosition;
    private CinemachineImpulseSource _impulseSource;
    // public static PlayerWeaponFsm Singleton;
    private const float IdleOrbitRadius = 3f;
    private const float IdleOrbitHeight = 3.5f;
    private const float IdlePositionLerpStrength = 3f;
    private const float IdleRotationLerpStrength = 5f;
    private const float ImpaleStartupOrbitRadius = 1f;
    private const float ImpaleStartupOrbitHeight = 4.5f;
    private const float ImpaleStartupPositionLerpStrength = 27.5f;
    private const float ImpaleStartupRotationLerpStrength = 15f;
    private const float ImpaleStartupPullbackSpeed = 0f;
    private const float ImpaleActiveForwardSpeed = 45f;
    private const float ImpaleActiveForwardSpeedEndTimeThreshhold = 0.195f;
    private const float ImpaleActiveForwardSpeedEndModifier = 0.25f;
    private const float ImpaleActiveMaxDistance = 10f;
    private const float ImpaleStuckRecoveryPullbackSpeed = 14f;
    public static event Action<Vector3, bool> OnPlayerWeaponPositionUpdated;
    private const float ImpaleStartupOrbitCenterForwardOffset = 5f;
    private const float ImpaleStuckPlayerGrappleRotationLerpStrength = 30f;
    private const float ImpaleStuckPlayerGrappleTailPullForce = 25f;
}