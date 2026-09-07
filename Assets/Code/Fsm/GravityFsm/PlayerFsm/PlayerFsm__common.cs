using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using FMOD.Studio;
using FMODUnity;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Wasp;
using Random = UnityEngine.Random;
using STOP_MODE = FMOD.Studio.STOP_MODE;
using Util = Code.Misc.Util;

public partial class PlayerFsm
{
    private enum FlankType
    {
        Left,
        Right,
        None
    }

    private PlayerInput _playerInput;
    private InputBuffer _inputBuffer;
    private Camera _camera;
    private float _momentum = 0f;
    private float _stateEntryMomentum = 0f;
    private Vector3 _currentLedgePosition;
    private Vector3 _currentFlankWallNormal;
    private Vector3 _currentSlideNormal;
    private Transform _currentSlideTransform;
    private Transform _currentWallrunTransform;
    private FlankType _currentFlankType;
    private FlankType _previousWallrunSide;
    private Vector3 _checkpointVector3;
    private Quaternion _checkpointQuaternion;
    public Vector3 walkToPositionTarget;
    public float walkToPositionArrivalDistanceModifier;
    private List<ParticleSystem> _kiIndicatorParticles;
    private List<Renderer> _renderers;
    private SkinnedMeshRenderer _skinnedMeshRenderer;
    private Material _material;
    private float _timeSinceDashFinished = 0f;
    private float _slideTimer = 0f;
    private float _slideNormalExitTimer = 0f;
    private ParticleSystem _teleportParticles;
    private PlayerDeathParticles _deathParticles;
    private bool isSprinting;
    private bool _isParentSlippery = false;
    private Vector3 _previousPositionDeltaNoTimescale;
    private float _currentSlipWeight;
    private const float MaxSlideTimer = 0.15f;
    private const float MinSlideStateTimer = 0.5f;
    private ParticleSystem _splashParticles;
    private PlayerDashParticles _playerDashParticles;
    private ParticleSystem _speedLinesParticles;
    private Transform _speedLinesParticlesTransform;
    private float _timeSinceRespawn;
    
    private float _timeSinceSpeedLinesParticlesPlayed;
    private float _speedLinesParticlesDuration;

    public const int MaxComboLength = 5;
    private int _currentComboLength = 0;
    private float _comboTimer = 0;
    private const float ComboTimeoutDuration = 3.0f;
    private const float SurgeMoveSpeedModifier = 1.75f;

    private bool _movementAnimationMirror;
    private bool _wallsquattedSinceLeavingGround;
    private bool _dashSinceLeavingGround;
    public static PlayerFsm Singleton;
    
    public Interactable currentPotentialInteractable;
    public Interactable currentInteractable;
    private TightropeController _currentTightropeController;
    private Vector3 _defaultScenePosition;
    private Vector3 _teleportDestination;
    private Vector3 _teleportOrigin;
    private Vector3 _teleportDirection;
    private CinemachineVirtualCamera _teleportCamera;
    private Transform _teleportCameraLookAt;
    private Transform _currentPitonTransform;
    private static readonly Vector3 PitonTargetOffset = Vector3.up * -2f;
    public RopeSwing currentRopeSwing;
    private float _timeSinceRopeSwing;
    private CinemachineVirtualCamera _surgeStartupCamera;
    private SurgePedestal _currentSurgePedestal;
    private PlayerSurgeHalo _playerSurgeHalo;

    public static event Action<float> OnPlayerMomentumUpdated;
    public static event Action<Vector3, bool> OnPlayerPositionUpdated;
    public static event Action OnPlayerImpaleStateEntered;
    public static event Action OnPlayerGrappleStateEntered;
    public static event Action OnPlayerRacePressed;
    public static event Action<Transform, float, float> OnPlayerParentTransformChanged;
    public static event Action<int> OnPlayerComboIncremented;
    public static event Action OnPlayerComboReset;
    public static event Action<Vector3, float, float> OnPlayerRippleGenerated;
    public static event Action<Vector3, float, float> OnPlayerWakeGenerated;

    private const float SwimSurfaceRippleTimer = 0.08f;
    private bool _swimSurfaceRippleQueued = false;
    
    public const float InputMagnitudeThreshhold = 0.1f;
    private const float InteractionDistance = 2.5f;
    
    private const float ForwardRaycastDistance = 0.95f;
    private const float DynamicForwardRaycastMaximumModifier = 2f;
    // private const float CollisionMoveSphereCastRadius = 0.4f;
    // private const float GroundCollisionMoveSphereCastHeight = 0.95f;
    // private const float FallingCollisionMoveSphereCastHeight = -0.5f;
    // private const float FallingCollisionMoveSphereCastHeightYVelocityThreshhold = -10f;
    // private const float CollisionMoveSphereCastDistance = 0.45f;
    private const float FaceLedgeHeight = 0.2f;
    private const float FaceHighLedgeHeight = 2.15f;
    private const float FaceWallHeight = 2.4f;
    private const float FaceWallMaximumAngle = 50f;
    private const float FaceWallStrictMaximumAngle = 20f;
    private const float FaceRaycastSkew = 0;
    private const float MaximumFlankWallDistance = 7.5f;
    private const float FlankWallHeight = 2f;
    private const float FlankWallOpenYOffset = -1.5f;
    private const float FlankMaximumAngle = 50f;
    private const float ForceWallRotationRaycastDistance = 3f;
    private const float DashForwardRaycastDistanceOffset = 0.5f;

    public const float MaxMomentum = 15f;
    private const float MoveSpeed = 5.65f;
    private const float MaximumMomentumSpeedMod = 3.8f;
    private const float RotationSpeed = 3.5f;
    private const float CollisionMomentumLossRate = 300f;
    private const float MomentumGainRate = 20f;
    private const float MomentumLossRate = 38f;
    private const float MomentumTurnLoss = 2.5f;
    private const float NoMomentumThreshold = 0.25f;
    private const float LowMomentumThreshhold = 6.75f;
    private const float LowMomentumRotationMod = 5.25f;
    private const float LowMomentumMomentumGainMod = 1.15f;
    private const float LowMomentumMomentumLossMod = 1.25f;
    private const float GroundMoveMinimumAnimatorSpeedMod = 1.25f;
    private const float GroundMoveMaximumAnimatorSpeedMod = 2.3f;
    private const float GroundSlopeMaximumMomentumAngle = 120f;
    private const float GroundSlopeMaximumMomentumModifier = 0.55f;
    private const float SprintMomentumCutoffMultiplier = 0.65f;
    private const float SprintMomentumGainMultiplier = 3.5f;
    private const float SprintTurnLossMultiplier = 1.5f;
    private const float IdleMomentumThreshold = 0.5f;
    
    private const float JumpYVelocity = 22f; 
    private const float CoyoteTime = 0.075f;
    private const float AirControlTurningMultiplier = 0.8f;
    private const float AirControlTurningMomentumDecayModifier = 0.25f;
    private const float AirControlMomentumDecayModifier = 0.475f;
    
    private const float UpdateLedgePositionEpsilon = 3f;
    private const float VaultMinimumYVelocity = -2f;
    private const float VaultMinimumMomentum = 6f;
    private const float VaultMinimumMomentumOnUpdate = 8f;
    private const float VaultHangLedgeYOffset = -2.5f;
    private const float VaultHangLedgeLerpStrength = 60f;
    private const float VaultTurningMultiplier = 0.75f;
    private const float VaultMinimumAnimatorSpeedMod = 0.5f;
    private const float VaultMaximumAnimatorSpeedMod = 1.1f;
    private const float VaultLedgeLerpStrength = 30f;
    private const float LedgeArrivalDistance = 0.05f;
    private bool _arrivedAtLedge;
    private const float MediumVaultHangMinimumYVelocity = 12f;
    private const float SlowVaultFinishLedgeLerpStrength = 25f;
    private const float SlowVaultFinishForwardSpeed = 3.5f;
    private const float WallsquatMinimumYVelocity = 10f;
    private const float WallSquatMinimumMomentum = 0f;
    public const float WallsquatMinimumDuration = 0.2f;
    private const float WallstepMinimumYVelocityGain = 18f;
    private const float WallstepMaximumYVelocityGain = 23.5f;
    private const float WallstepMinimumDuration = 0.35f;
    private const float ForceWallRotationSpeed = 3f;
    private const float WallRunMinimumEntryMomentum = 9f;
    private const float WallRunMinimumMomentum = 7f;
    private const float
        WallRunMinimumYVelocity = 13f; // It's pretty important that this value is larger than WallsquatMinimumYVelocity

    private const float FlankAlignmentRotationSpeed = 25f;
    private const float FlankWallVacuumStrength = 20f;
    private const float WallrunJumpAngle = 75f;
    
    private const float HardLandAirDiff = -11;
    private const float HardLandExitMomentum = 4f;
    private const float HardLandForwardSpeed = 10f;
    private const float HardLandForwardDuration = 0.105f;
    
    private const float HardLandRollExitMomentum = 12f;
    private const float HardLandRollMinimumMomentum = 7.5f;
    private const float HardLandRollForwardSpeed = 20f;
    
    private const float HardTurnMinimumAngle = 130f;
    private const float HardTurnMinimumMomentum = 12.5f;
    private const float HardTurnMomentumLossModifier = 1.5f;
    
    private const float DashEntryMomentumGain = 5f;
    private const float DashEntryMinimumMomentum = 12f;
    private const float DashsquatTurnMultiplier = 2.25f;
    private const float DashForwardSpeed = 30f; // **** 24
    private const float DashRaycastHeightOffset = 0f;
    private const float SkipWindowDuration = 0.2f;
    private const float SkipForwardBonusSpeed = 3.5f;
    private const float SkipYVelocity = 28f;
    
    
    
    private const float ImpaleMovementModifier = 1f;
    private const float ImpaleMomentumOffset = 2.5f;
    private const float ImpaleMinimumMomentumAfterOffset = 6f;
    private const float ImpaleMomentumLerpStrenth = 10f;
    
    private const float GrappleStartupRotationLerpStrength = 13f;
    private const float GrappleStartupYPositionLerpStrength = 3f;
    private const float GrappleStartupYPositionOffset = 1f;
    private const float GrappleStartupMomentumLossMod = 1.25f;

    private const float WalkToPositionTurnPhaseAngle = 30f;
    private const float WalkToPositionMomentum = 8f;
    private const float WalkToPositionMomentumLerpStrength = 9f;
    private const float ArriveAtWalkPositionTargetDistance = 3.25f;
    private const float ArriveAtWalkPositionTargetRangedDistance = 10f;

    private const float TrialTeleportStartupDuration = 0.5f;
    private const float TrialTeleportDuration = 1.5f;

    private const float PitonMaximumWallInteractYVelocity = 5f;


    private MinorLeyline _currentMinorLeyline;
    private Transform _currentMinorLeylineTrigger;
    private const float MinorLeylineSpeed = 50f;
    private bool _currentMinorLeylineDirection = false;
    private float _currentMinorLeylineWeight = 0;
    private PlayerMinorLeylineHalo _minorLeylineHalo;
    private float _timeSinceMinorLeyline = 100f;
    private float _timeSinceMinorLeylineUp = 100f;

    private const float TinsicaDuration = 0.75f;
    private const float TinsicaEntryMomentum = 9.5f;


    private const float KiMomentumThreshhold = 11.5f;
    
    public EventReference jumpFmodEvent;
    public EventReference landFmodEvent;
    public EventReference impactFmodEvent;
    public EventReference skipFmodEvent;
    public EventReference dashFmodEvent;
    public EventReference climbFmodEvent;
    public EventReference footstepFmodEvent;
    public EventReference comboIncrementFmodEvent;
    public EventReference comboTriggerFmodEvent;
    public EventReference comboActiveFmodEvent;
    public EventReference deathFmodEvent;
    public EventReference slideFmodEvent;
    public EventReference slipAmbientEvent;
    public EventReference hardlandCinematicEventReference;
    public EventReference hardlandEventReference;
    public EventReference dashWhooshEventReference;
    public EventReference skipWhooshEventReference;
    public EventReference surgeStartupFmodEvent;
    public EventReference surgeEndFmodEvent;
    public EventReference windRushFmodEvent;
    public EventReference freezeFmodEvent;
    public EventReference swimFootstepFmodEvent;
    public EventReference splashFmodEvent;
    
    private EventInstance activeFmodInstance;
    private EventInstance slideFmodInstance;
    private EventInstance surgeStartupFmodInstance;
    private EventInstance windRushFmodInstance;
    private EventInstance freezeFmodInstance;
    
    
    
    private EventInstance slipAmbientFmodInstance;
    private float _timeSinceLastFootstep;
    private bool _isSurging;
    private float _surgeStartupInitialFov;
    private static float _desiredWindRushFmodAmount = 0f;
    private float _timeSinceSurgeStarted = 0f;
    private float _freezeTimer;
    public float _timeSinceBoostStarted;
    private const float BoostSpeedMultiplier = 1.75f;
    private const float MaxSurgeDuration = 2.5f;

    private const float BoostSpeedDuration = 2.75f;
    public static event Action OnPlayerCultTrialDeath;
    public static event Action<Vector3> OnPlayerTeleported;
    private const float SwimFreezeDuration = 2.5f;

    private float _dialogueEntryMomentum;
    private PlayerTrickParticles _playerTrickParticles;
    private bool _dialogueIdle;
    private bool _isSurgeQueued;
    private float _timeSinceSurgeQueued;
    public static event Action<String> OnItemCollected;


    private bool IsHitValidFlank(RaycastHit hit, bool left)
    {
        float DistanceFromPointToPlane(Vector3 pointA, Vector3 pointB, Vector3 planeNormal)
        {
            Vector3 AB = pointA - pointB;
            float distance = Vector3.Dot(AB, planeNormal.normalized);
            return Mathf.Abs(distance); // Absolute value to get the unsigned distance
        }
        
        float flipMod = left ? -1f : 1f;
        var distance = DistanceFromPointToPlane(transform.position, hit.point, hit.normal);
        var angle = Vector3.Angle(transform.right * flipMod, hit.normal);

        var forwardAngle = Vector3.Angle(transform.forward, hit.normal);
        if (forwardAngle < 65f) return false;
        

        var isHitValidFlank = distance < 2.0f && angle < FlankMaximumAngle;
        
        // if (!Machine.IsInState(PlayerFsmState.Wallrun) && isHitValidFlank) print(forwardAngle);
        return isHitValidFlank;;
    }

    private float ComputeDynamicForwardRaycastDistance()
    {
        // var offset = Machine.IsInState(PlayerFsmState.Dash) ? DashForwardRaycastDistanceOffset : 0f;
        return (Mathf.Lerp(1f, DynamicForwardRaycastMaximumModifier, ComputeMomentumWeight()) * ForwardRaycastDistance *
                GetRaycastTimeModifier());
    }
    
    
    private void MoveYOntoLedge(float yOffset, float lerpStrength)
    {
        var newY = lerpStrength < 0 ? _currentLedgePosition.y : Mathf.Lerp(transform.position.y, _currentLedgePosition.y + yOffset, Time.deltaTime * lerpStrength);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
    
    private void MoveYOntoLedgeLinear(float yOffset, float speed)
    {
        var desiredY = _currentLedgePosition.y + yOffset;
        var delta = speed * Time.deltaTime;
        var newY = transform.position.y > desiredY
            ? Mathf.Max(desiredY, transform.position.y - delta)
            : Mathf.Min(desiredY, transform.position.y + delta);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private bool UpdateLedgePosition(float ledgeHeight, bool upwardsOnly = false)
    {

        if (_arrivedAtLedge && !Machine.IsInState(PlayerFsmState.Tinsica)) return true; // or, just return true to allow continued lerping without updating value
        
        var wallDistance = 5f;
        var wallHitOrigin = transform.position + (Vector3.up * 0.25f);
        if (Physics.Raycast(wallHitOrigin, transform.forward, out var forwardHit, 5f, GetEnvironmentalLayermask()))
        {
            wallDistance = forwardHit.distance;
        }
        else
        {
            Debug.DrawLine(wallHitOrigin, wallHitOrigin + transform.forward * 5f, Color.orange);
        }
        
        var forwardOffset = wallDistance + 0.1f;
        var downwardRaycastOrigin = transform.position + (Vector3.up * (ledgeHeight + UpdateLedgePositionEpsilon)) + transform.forward *
            (forwardOffset);
        Debug.DrawLine(downwardRaycastOrigin, downwardRaycastOrigin - (Vector3.up * (ledgeHeight + UpdateLedgePositionEpsilon)), Color.green);

        if (!Physics.Raycast(downwardRaycastOrigin, -Vector3.up, out var hit,
                (ledgeHeight + UpdateLedgePositionEpsilon) * GetRaycastTimeModifier(), GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore)) return false;

        // if (slope > 60f) return false;
        if (upwardsOnly && hit.point.y < transform.position.y) return false;
        var slope = Vector3.Angle(hit.normal, Vector3.up);
        var delta = hit.point.y - transform.position.y;
        if (delta <= LedgeArrivalDistance) _arrivedAtLedge = true;
        _currentLedgePosition = hit.point;
        return true;
    }
    


    private void HandleFlankAlignment()
    {
        if (_currentFlankType == FlankType.None) return; 
        var rotationMod = _currentFlankType == FlankType.Left ? -1f : 1f;
        var forward = Quaternion.Euler(0f, 90f * rotationMod, 0f) * _currentFlankWallNormal;
        Debug.DrawRay(transform.position, forward, Color.yellow);
        var lookRotation = Quaternion.LookRotation(forward, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * FlankAlignmentRotationSpeed);
    }

    private void HandleTurning(float multiplier = 1f, bool forceForwardInput = false,
        float momentumDecayMultiplier = 1f, bool ignoreTurnAnimationLayer = false, float animationTurnModifier = 1f)
    {
        if (PhotoManager.Singleton.IsActive()) return;
        var v3 = GetInputMovementVector3();
        var inputVector3 = forceForwardInput ? MirrorInputForward(v3, transform.forward) : v3;
        
        var v2 = GetInputMovementVector2();
        if (v2.magnitude < InputMagnitudeThreshhold)
        {
            inputVector3 = transform.forward;
        }
        
        HandleTurningCore(multiplier, momentumDecayMultiplier, inputVector3, ignoreTurnAnimationLayer, animationTurnModifier);
    }

    private void HandleTurningCore(float multiplier, float momentumDecayMultiplier, Vector3 direction, bool ignoreTurnAnimationLayer = false, float animationTurnModifier = 1f)
    {
        float momentumWeight = ComputeMomentumWeight();
        var angle = Vector3.SignedAngle(direction.normalized, transform.forward.normalized, Vector3.up);
        
        if (!ignoreTurnAnimationLayer)
        {
            var animationDesiredTurnAmount = Mathf.InverseLerp(40f, -40f, angle);
            // animationDesiredTurnAmount *= Mathf.Lerp(1f, 2f, Mathf.InverseLerp(10f, 5f, _momentum));
            
            animationDesiredTurnAmount = Mathf.Lerp(-1, 1, animationDesiredTurnAmount) * animationTurnModifier;
            
            var turnAmount = Animator.GetFloat("TurnAmount");
            var turnLerpSpeed = Mathf.Abs(animationDesiredTurnAmount) > Mathf.Abs(turnAmount) ? 10f : 4.5f;
            Animator.SetFloat("TurnAmount",
                Mathf.Lerp(turnAmount, animationDesiredTurnAmount, Time.deltaTime * turnLerpSpeed));
            Animator.SetLayerWeight(1, Mathf.Abs(turnAmount) * momentumWeight);
        }
            
        var momentumDesiredTurnAmount = Mathf.InverseLerp(170f, -170f, angle);
        momentumDesiredTurnAmount = Mathf.Lerp(-1, 1, momentumDesiredTurnAmount);
        
        if ((Machine.IsInState(PlayerFsmState.GroundMove) || Machine.IsInState(PlayerFsmState.Swim)))
        {
            if (_isSurging)
            {
                multiplier *= 0.75f;
                momentumDesiredTurnAmount *= 0.25f;
            }
            else if (Mathf.Abs(momentumDesiredTurnAmount) > 0.5f)
            {
                EndSurge();
                isSprinting = false;
            }
        }

        var sprintLoss = isSprinting ? SprintTurnLossMultiplier : 1f;
        _momentum = Mathf.Max(0, _momentum - (MomentumLossRate * Time.deltaTime *
                                              Mathf.Abs(momentumDesiredTurnAmount) * momentumWeight * MomentumTurnLoss * momentumDecayMultiplier * sprintLoss));
        
        var quaternion = Quaternion.LookRotation(direction.normalized, Vector3.up);
        
        // var lowMomentumRotationMod = _momentum < LowMomentumThreshhold ? LowMomentumRotationMod : 1f;
        var lowMomentumRotationMod = Mathf.Lerp(LowMomentumRotationMod, 1f,
            Mathf.InverseLerp(LowMomentumThreshhold - 1f, LowMomentumThreshhold, _momentum));
        transform.rotation = Quaternion.Slerp(transform.rotation, quaternion, RotationSpeed * Time.deltaTime * lowMomentumRotationMod * multiplier);
    }

    private void HandleInputMomentumChange(float increaseMultiplier = 1f, float decreaseMultiplier = 1f, bool removeV2 = false)
    { 
        if (PhotoManager.Singleton.IsActive()) return;
        var sprinting = isSprinting;
        var isSprintingOver = !isSprinting && _momentum >= MaxMomentum * SprintMomentumCutoffMultiplier;
        var v2 = removeV2 ? Vector2.zero : GetInputMovementVector2();
        if (v2.magnitude > InputMagnitudeThreshhold && !isSprintingOver)
        {
            var grounded = Machine.IsInState(GravityFsmState.Grounded);
            
            var lowMomentumMomentumGainMod = _momentum < LowMomentumThreshhold ? LowMomentumMomentumGainMod : 1f;
            // var superSteepComponent = Mathf.InverseLerp(50f, 55f, Vector3.Angle(GroundNormal, Vector3.up));
            var superSteepComponent = 0f;
            var weight = (Mathf.Max(Mathf.InverseLerp(110f, 140f, GroundForwardSlope), superSteepComponent));
            var slopeMaxMomentumMod = Mathf.Lerp(1f, GroundSlopeMaximumMomentumModifier,
                weight);

            if (!grounded) slopeMaxMomentumMod = 1f;
            
            var localMaximum = MaxMomentum * slopeMaxMomentumMod * (!sprinting && grounded ? SprintMomentumCutoffMultiplier : 1f);
            var sprintMomentumGainMod = (sprinting && grounded ? SprintMomentumGainMultiplier : 1f);
            
            _momentum = Mathf.Min(localMaximum, _momentum + MomentumGainRate  * lowMomentumMomentumGainMod * increaseMultiplier * sprintMomentumGainMod * Time.deltaTime);
            
            Machine.Fire(PlayerFsm.PlayerFsmTrigger.Accelerating);
        }
        else
        {
            if (Machine.IsInState(PlayerFsmState.GroundMove) || Machine.IsInState(PlayerFsmState.Swim))
            {
                EndSurge();
                isSprinting = false;
            };
            var lowMomentumMomentumLossMod = _momentum < LowMomentumThreshhold ? LowMomentumMomentumLossMod : 1f;
            _momentum = Mathf.Max(0, _momentum - (MomentumLossRate * lowMomentumMomentumLossMod * decreaseMultiplier * Time.deltaTime));
            
            if (_momentum < IdleMomentumThreshold) Machine.Fire(PlayerFsm.PlayerFsmTrigger.IdleMomentumThresholdPassedDecelerating);
        }
        
    }
    
    
    private Vector2 GetInputMovementVector2()
    {
        if (CutsceneManager.Singleton.IsCutscenePlayerDisabled())
        {
            return Vector3.zero;
        }
        
        return _playerInput.actions["Move"].ReadValue<Vector2>();
    }
    
    public Vector3 GetInputMovementVector3()
    {
        var v2 = GetInputMovementVector2();
        return Quaternion.Euler(0, _camera.transform.rotation.eulerAngles.y, 0) * new Vector3(v2.x, 0, v2.y);
    }
    
    public Vector3 GetMouseVector3()
    {
        Vector2 mouseScreenPosition = Input.mousePosition;
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 v2 = mouseScreenPosition - screenCenter;
        return Quaternion.Euler(0, _camera.transform.rotation.eulerAngles.y, 0) * new Vector3(v2.x, 0, v2.y).normalized;
    }
    

    
    private float ComputeMomentumWeight()
    {
        return Mathf.InverseLerp(0, MaxMomentum, _momentum);
    }

    private Vector3 ComputeDesiredMove()
    {
        return ComputeDesiredMoveWithoutTimescale() * Time.deltaTime;
    }
    
    private Vector3 ComputeDesiredMoveWithoutTimescale()
    {
        var value = Mathf.Lerp(0f, MaximumMomentumSpeedMod, ComputeMomentumWeight());
        var m = ComputeAllMovementMultipliers();
        return transform.forward.normalized * (MoveSpeed * value * m);
    }

    public float ComputeAllMovementMultipliers()
    {
        var comboMultiplier = GetCurrentSurgeSpeedMultiplier();
        var boostMultiplier = GetCurrentBoostSpeedMultiplier();
        var miscMultiplier = GetCurrentMiscSpeedMultiplier();
        var m = comboMultiplier * boostMultiplier * miscMultiplier;
        return m;
    }

    private float GetCurrentMiscSpeedMultiplier()
    {
        return Mathf.Lerp(2.5f, 1f, Mathf.InverseLerp(0.25f, 0.6f, _timeSinceMinorLeyline));
    }

    private float GetCurrentBoostSpeedMultiplier()
    {
        if (!_isSurging && _isSurgeQueued) return 1.35f;
        return Mathf.Lerp(1f, BoostSpeedMultiplier, Mathf.InverseLerp(BoostSpeedDuration, 0, _timeSinceBoostStarted));
    }


    private float GetCurrentSurgeSpeedMultiplier()
    {
        if (!_isSurging) return 1f;
        return Mathf.Lerp(SurgeMoveSpeedModifier, 1f, Mathf.InverseLerp(MaxSurgeDuration - 1.5f, MaxSurgeDuration, _timeSinceSurgeStarted));
    }

    private void SetAnimatorMomentum()
    {
        var currentMomentumFloat = Animator.GetFloat("Momentum");
        Animator.SetFloat("Momentum", Mathf.Lerp(currentMomentumFloat, ComputeMomentumWeight(), Time.deltaTime * 10f));
    }

    private void HandleCollisionMove(float modifier = 1f, bool updateMomentum = true, float slopeModifier = 1f)
    {
        if (GameMenu.Singleton.IsMenuOpen()) return;
        var desiredMove = ApplyTractionNoTimescale(ComputeDesiredMoveWithoutTimescale() * modifier) * Time.deltaTime;
        
        var collisionMove = ComputeCollisionMove(desiredMove);
        transform.position += collisionMove;
        
        if (Machine.IsInState(PlayerFsmState.Wallsquat)) return;
        if (Machine.IsInState(PlayerFsmState.SlowVaultHang)) return;
        if (Machine.IsInState(PlayerFsmState.Wallrun)) return;
        if (Machine.IsInState(PlayerFsmState.SlideLateral)) return;
        if (Machine.IsInState(GravityFsmState.Aerial) && YVelocity > WallsquatMinimumYVelocity - 1f) return;
        
        var collisionRatio = (desiredMove.magnitude + 1f) / (collisionMove.magnitude + 1f);
        if (updateMomentum) _momentum = Mathf.Max(0, _momentum - (MomentumLossRate * Time.deltaTime * (collisionRatio - 1f) * CollisionMomentumLossRate));
        // if (collisionRatio > 1f && isSprinting && _momentum < 8f)
        // {
        //     isSprinting = false;
        //     ResetCombo();
        // }
    }

    private Vector3 ApplyTractionNoTimescale(Vector3 desiredMoveNoTimescale)
    {

        // if (Input.GetKey(KeyCode.P)) desiredMove = Vector3.zero;
        if ((!Machine.IsInState(GravityFsmState.Grounded) || !_isParentSlippery) || true)
        {
            _currentSlipWeight = 0f;
            HandleSlipAudio();
            return desiredMoveNoTimescale;
        };
        
        
        var lerpStrength = 1.5f;
        var afterTraction = Vector3.Lerp(new Vector3(_previousPositionDeltaNoTimescale.x, 0, _previousPositionDeltaNoTimescale.z), desiredMoveNoTimescale, lerpStrength * Time.deltaTime);
        if (afterTraction.magnitude < 0.75f) afterTraction = desiredMoveNoTimescale;
        UpdateSlipWeight(desiredMoveNoTimescale, afterTraction);
        
        HandleSlipAudio();
        
        return afterTraction;
    }

    private void HandleSlipAudio()
    {
        
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("PlayerSlipWeight", _currentSlipWeight);
    }

    private void UpdateSlipWeight(Vector3 desiredMove, Vector3 afterTraction)
    {
        var footstepMod = 1f;
        
        //hack
        if (Machine.IsInState(PlayerFsm.PlayerFsmState.Jumpsquat))
        {
            _currentSlipWeight = 0;
            return;
        }
        
        if (Machine.IsInState(PlayerFsm.PlayerFsmState.GroundMove))
        {
            var tailLength = Mathf.Lerp(0.5f, 0.15f, Mathf.InverseLerp(0.3f, 0.5f, ComputeMomentumWeight()));
            footstepMod = Mathf.Lerp(1f, 0f, Mathf.InverseLerp(0.05f, 0.05f + tailLength, _timeSinceLastFootstep));
        }
        _currentSlipWeight = GetSlipWeight(desiredMove, afterTraction) * footstepMod;
    }
    
    private float GetSlipWeight(Vector3 desiredMove, Vector3 afterTraction)
    {
        Vector3 desiredFlat = new Vector3(desiredMove.x, 0f, desiredMove.z);
        Vector3 tractionFlat = new Vector3(afterTraction.x, 0f, afterTraction.z);

        float desiredMag = desiredFlat.magnitude;
        float tractionMag = tractionFlat.magnitude;

        if (desiredMag < 0.75f && tractionMag < 0.75f)
            return Mathf.Lerp(0f, 1f, Mathf.InverseLerp(0.4f, 0.75f, tractionMag));

        float speedDifference = Mathf.Abs(desiredMag * (desiredMag < tractionMag ? 2f : 1f) - tractionMag) / desiredMag;
        float speedFactor = Mathf.Clamp01(speedDifference);

        float directionFactor = 0f;

        if (tractionMag > 0.001f)
        {
            float dot = Vector3.Dot(desiredFlat.normalized, tractionFlat.normalized);
            directionFactor = Mathf.Clamp01(1f - dot);
        }


        float slipWeight = Mathf.Pow(speedFactor * directionFactor, 0.5f);
        return slipWeight;
    }
    

    public void InvokeBoost(bool jump, float momentumWeight)
    {
        if (jump) Machine.Fire(PlayerFsmTrigger.Jump);
        _momentum = MaxMomentum * momentumWeight;
    }

    public Vector3 GetPreviousPositionDelta()
    {
        return _previousPositionDeltaNoTimescale;
    }
    

    
    public static Vector3 MirrorInputForward(Vector3 input, Vector3 forward, float clampRatio = 0f)
    {
        if (input == Vector3.zero)
            return Vector3.zero;

        // Project both vectors onto the XZ plane (ignore vertical component)
        Vector3 inputFlat = new Vector3(input.x, 0f, input.z).normalized;
        Vector3 forwardFlat = new Vector3(forward.x, 0f, forward.z).normalized;

        float dot = Vector3.Dot(inputFlat, forwardFlat);

        if (dot >= 0f)
        {
            // Input is within 90 degrees of Forward — return as-is
            return input;
        }
        else
        {
            return input - (forward.normalized * dot);

            // // Mirror the input vector across the forward's perpendicular plane
            // // First, get the right vector (90° rotation from forward)
            // Vector3 right = Vector3.Cross(Vector3.up, forwardFlat).normalized;
            //
            // // Project input onto the forward-right basis
            // float f = Vector3.Dot(inputFlat, forwardFlat);
            // float r = Vector3.Dot(inputFlat, right);
            //
            // // Mirror the forward component (flip sign of f)
            // float mirroredF = -f;
            //
            // // Reconstruct the mirrored vector
            // Vector3 mirrored = (mirroredF * forwardFlat) + (r * right);
            //
            // // Scale by original input magnitude (preserve intensity)
            // return mirrored.normalized * input.magnitude;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(_currentLedgePosition, 0.25f);
        
        var raycastLength = GroundedRaycastLength * GetRaycastTimeModifier();
        var forward = transform.forward * (GroundedRaycastForwardOffset * GetRaycastTimeModifier());
        Gizmos.color = Color.blue;
        var transformPosition = transform.position + Vector3.up * (2f * raycastLength) + forward;
        Gizmos.DrawSphere(transformPosition, 0.35f);
        Gizmos.DrawSphere(transformPosition - Vector3.up * raycastLength * 4f, 0.35f);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(_safeGroundPosition, 0.35f);
    }
    
    private bool CanGrapple(TriggerParams? triggerParams)
    {
        return PlayerWeaponFsm.Singleton.Machine.IsInState(PlayerWeaponFsm.PlayerWeaponFsmState.ImpaleStuck);
    }
    
    private bool CanImpale(TriggerParams? triggerParams)
    {
        var machine = PlayerWeaponFsm.Singleton.Machine;
        return machine.IsInState(PlayerWeaponFsm.PlayerWeaponFsmState.Idle) || machine.IsInState(PlayerWeaponFsm.PlayerWeaponFsmState.ImpaleStartup);
    }
    
    private bool CanDash(TriggerParams? triggerParams)
    {
        return (YVelocity < 6f || IsInGust) && !_dashSinceLeavingGround;
    }

    private void OnContactHitboxCollide()
    {
        Machine.Fire(PlayerFsmTrigger.ContactHitboxTrigger);
    }



    
    private void HandleKiEffects()
    {

        var on = _currentComboLength >= MaxComboLength;
        
        foreach (var p in _kiIndicatorParticles)
        {
            p.Stop();
            // if (on && !p.isEmitting) p.Play();
            // else if (!on) p.Stop();
        }

        var desiredGlowWeight = on ? 3.5f : 0f;
        var currentGlowWeight =  _material.GetFloat("_GlowWeight");
        var f = on ? 5f : 10f;
        _material.SetFloat("_GlowWeight", Mathf.Lerp(currentGlowWeight, desiredGlowWeight, Time.deltaTime * f));
    }

    private float GetCurrentDashRaycastHeightOffset()
    {
        return Machine.IsInState(PlayerFsmState.Dash) || Machine.IsInState(PlayerFsmState.Skip) ? DashRaycastHeightOffset : 0;
    }
    

    protected override void OnParentTransformChanged(Transform t)
    {

        t.TryGetComponent(out PlayerSlipperyIndicator tractionIndicator);
        _isParentSlippery = tractionIndicator != null;

        OnPlayerParentTransformChanged?.Invoke(t, _momentum, YVelocity);
        base.OnParentTransformChanged(t);
    }
    
    
    private void Reset()
    {
        SceneLoader.Singleton.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ApplyMetaSaveData(MetaSaveSystem.MetaSaveData metaSaveData)
    {
        // CameraLookSensitivityProcessor.SetSensitivityModifier(metaSaveData.cameraSensitivityModifier);
        transform.Find("AmbientParticles").gameObject.SetActive(metaSaveData.enableAmbientParticles);
    }


    private void OnPlayerFootstep()
    {
        StartCoroutine(QueueFootstep());
        _timeSinceLastFootstep = 0f;

    }

    private void OnPlayerFootstepDelay(float delay)
    {
        StartCoroutine(QueueFootstep(delay));
    }
    
    private IEnumerator QueueFootstep(float minimumDelay = 0)
    {
        
        yield return new WaitForFixedUpdate(); // basically just delay a frame to handle race conditions between the updating of the parent transform and the entry of some substate
        var maximumWaitTime = 0.5f;
        var t = 0f;
        while (t < maximumWaitTime)
        {
            if (parentTransform != null && t > minimumDelay)
            {
                var fmodMaterialLabel = "Stone";
                var renderer = parentTransform.GetComponentInChildren<MeshRenderer>();
                if (renderer == null) yield break;
                var parentMaterialName = renderer.material.name;
                if (parentMaterialName.Contains("Snow") || parentMaterialName.Contains("Grass")) fmodMaterialLabel = "Snow";
                if (parentMaterialName.Contains("Metal")) fmodMaterialLabel = "Metal";
                if (parentMaterialName.Contains("Combo")) fmodMaterialLabel = "Glass";
                if (parentMaterialName.Contains("Ice")) fmodMaterialLabel = "Glass";
                FMODUnity.RuntimeManager.StudioSystem.setParameterByNameWithLabel("PlayerFootstepMaterial", fmodMaterialLabel);
                FMODUnity.RuntimeManager.PlayOneShotAttached(footstepFmodEvent, gameObject);
                yield break;
            }
            t += Time.deltaTime;
            yield return null;
        }
    }
    
    private void OnPlayerSwimFootstep()
    {
        print("test");
        FMODUnity.RuntimeManager.PlayOneShotAttached(swimFootstepFmodEvent, gameObject);
    }

    public float GetMomentum()
    {
        return _momentum;
    }
    
    public void SetMomentum(float momentum)
    {
        _momentum = momentum;
    }

    public void SetTeleportDestination(Vector3 destination, Vector3 direction)
    {
        _teleportDestination = destination;
        _teleportDirection = direction;
    }

    public Vector3 GetTeleportDestination()
    {
        return _teleportDestination;
    }

    private void UpdateShaderGlobals()
    {
        Shader.SetGlobalVector("_PlayerWorldPosition", transform.position);

        var shaderGrounded = Shader.GetGlobalFloat("_PlayerGrounded");
        shaderGrounded += Time.deltaTime * 5f * (Machine.IsInState(PlayerFsmState.DisplaceFoliage) ? 2f : -0.5f);
        shaderGrounded = Mathf.Clamp(shaderGrounded, 0f, 1f);
        Shader.SetGlobalFloat("_PlayerGrounded", shaderGrounded);
        
        var shaderCombo = Shader.GetGlobalFloat("_PlayerCombo");
        shaderCombo += Time.deltaTime * 5f * (_isSurging ? 0.5f : -0.5f);
        shaderCombo = Mathf.Clamp(shaderCombo, 0f, 1f);
        Shader.SetGlobalFloat("_PlayerCombo", shaderCombo);
    }

    private bool IsComboSystemEnabled()
    {
        return false; //TODO
    }
    private void IncrementCombo()
    {
        if (!IsComboSystemEnabled()) return;
        if (!isSprinting) return;
        _comboTimer = 0;
        _currentComboLength++;

        if (_currentComboLength > 1)
        {
            FMODUnity.RuntimeManager.StudioSystem.setParameterByName("PlayerComboDuration",
                Mathf.InverseLerp(0, MaxComboLength, _currentComboLength));
            FMODUnity.RuntimeManager.PlayOneShotAttached(comboIncrementFmodEvent, gameObject);
        }

        if (_currentComboLength >= MaxComboLength)
        {
            var spherePrefab = Resources.Load("Prefab/Fsm/SphereEffect") as GameObject;
            var spherePosition = transform.position + Vector3.up;
            var sphereObject = Instantiate(spherePrefab, spherePosition,
                Quaternion.identity, null);
            sphereObject.GetComponent<SphereEffect>().SetConfig(Vector3.one * 2f, 1.25f, 0.8f, -1f);
        }
        
        OnPlayerComboIncremented?.Invoke(_currentComboLength);
        
    }

    private void InvokeSurge()
    {
        
        StartCoroutine(InvokeSurgeTrail());
        FMODUnity.RuntimeManager.PlayOneShotAttached(comboTriggerFmodEvent, gameObject);
        _timeSinceSurgeStarted = 0f;

        _speedLinesParticlesDuration = -1f;
        _speedLinesParticles.Play();

    }
    
    IEnumerator InvokeSurgeTrail()
    {
        var triggerPrefab = Resources.Load("Prefab/Fsm/SphereEffect") as GameObject;
        var triggerPosition = _skinnedMeshRenderer.transform.position;
        yield return new WaitForSeconds(0.05f);
        var triggerObject = Instantiate(triggerPrefab, triggerPosition,
            Quaternion.identity, null);
        triggerObject.GetComponent<SphereEffect>().SetConfig(Vector3.one * 15f, 1.25f, 0.6f, -4.5f);

            
        RuntimeManager.AttachInstanceToGameObject(activeFmodInstance, gameObject);
        activeFmodInstance.start();
            
            
        while (_isSurging){
            if (Machine.IsInState(PlayerFsmState.TrialTeleport)) break;
            var comboMeshPrefab = Resources.Load("Prefab/Fsm/PlayerComboMesh") as GameObject;
            var position = _skinnedMeshRenderer.transform.position;
            var rotation = _skinnedMeshRenderer.transform.rotation;
            var mesh = new Mesh();
            _skinnedMeshRenderer.BakeMesh(mesh);
            yield return new WaitForSeconds(Random.Range(0.06f, 0.05f));
            var comboMeshObject = Instantiate(comboMeshPrefab, position,
                rotation, null);
            comboMeshObject.TryGetComponent(out MeshFilter meshFilter);
            meshFilter.mesh = mesh;
        }
            
        activeFmodInstance.stop(STOP_MODE.ALLOWFADEOUT);

        yield break;
    }

    private void UpdateFmodWindRushAmount()
    {
        if (_isSurging && _timeSinceSurgeStarted > 0.175f)
        { 
            _desiredWindRushFmodAmount = Mathf.Lerp(1f, 0.5f, Mathf.InverseLerp(2f, 4.5f, _timeSinceSurgeStarted));
        }
        else if (Machine.IsInState(GravityFsmState.Aerial))
        {
            _desiredWindRushFmodAmount = Mathf.Lerp(0f, 1f, Mathf.InverseLerp(0f, -70f, CurrentFallDistance()));
        }
        else
        {
            _desiredWindRushFmodAmount = 0;
        }
        FMODUnity.RuntimeManager.StudioSystem.getParameterByName("WindRushAmount", out var currentAmount);
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("WindRushAmount", Mathf.Lerp(currentAmount, _desiredWindRushFmodAmount, Time.deltaTime * 10f));
    }

    private void StartSurge()
    {
        _momentum = MaxMomentum;
        isSprinting = true;
        _isSurging = true;
        InvokeSurge();
    }

    private void EndSurge()
    {
        if (_isSurging)
        {
            FMODUnity.RuntimeManager.PlayOneShotAttached(surgeEndFmodEvent, gameObject);
            // _playerSurgeHalo.StartBreak();
        }
        _speedLinesParticles.Stop();
        _isSurging = false;
        ResetCombo();
    }
    
    private void ResetCombo()
    {
        
        _currentComboLength = 0;
        OnPlayerComboReset?.Invoke();
    }

    public int GetComboLength()
    {
        return _currentComboLength;
    }
    
    public bool GetIsSurging()
    {
        return _isSurging;
    }

    public void InvokePlayerDeath()
    {
        print("death invoked");
        if (Machine.IsInState(PlayerFsmState.Dying) || Machine.IsInState(PlayerFsmState.Dead) || Machine.IsInState(PlayerFsmState.TrialTeleport)) return;
        if (Physics.CheckSphere(transform.position, 1f, LayerMask.GetMask("DeathColliderMask"), QueryTriggerInteraction.Collide)) return;
        if (CultTrialManager.Singleton.isCurseEnabled)
        {
            CultTrialManager.Singleton.ReplenishCurseDuration(false);
            CultTrialManager.Singleton.isCurseTicking = false;
            SetPositionFromSaveData();
            Machine.Jump(PlayerFsm.PlayerFsmState.HardLand);
            OnPlayerCultTrialDeath?.Invoke();
            return;
        }
        
        Machine.Jump(PlayerFsmState.Dying);
    }

    private void SnapToGround()
    {
        ReplaceAnimatorTrigger("GroundMove");
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out RaycastHit hit,20f, GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore))
        {
            transform.position = hit.point;
        }
    }

    private void UpdateMusicDistanceAttenuation()
    {

        var newDistance = 0f;
        foreach (var attenuator in MusicDistanceAttenuatorRegistry.Attenuators)
        {
            var attenuation = Mathf.InverseLerp(attenuator.maxDistance, attenuator.minDistance,
                Vector3.Distance(transform.position, attenuator.transform.position));
            
            if (attenuation <= 0.01f) continue;
            newDistance = attenuation;
            break;
        }

        RuntimeManager.StudioSystem.getParameterByName("MusicDistance", out float currentDistance);
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("MusicDistance", Mathf.Lerp(currentDistance, newDistance, Time.deltaTime * 2f));
    }

    private void HandleSlideTimer()
    {
        GetGroundedRaycastHit(out var groundedRaycastHit);
        if (groundedRaycastHit.collider == null)
        {
            _slideTimer = 0;
            return;
        };
        if (IsSlideTriggerCore(groundedRaycastHit))
        {
            _slideTimer += Time.deltaTime;
        }
        else
        {
            _slideTimer = 0;
        }
    }


    private void HandleRaycastKill()
    {
        if (CurrentFallDistance() > -50f) return;
        var origin = transform.position + Vector3.up * 30f;
        if (Physics.SphereCast(origin, 15f, Vector3.down, out _, 130f, GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore)) return;
        if (Physics.SphereCast(origin, 15f, Vector3.down, out _, 130f, LayerMask.GetMask("Water"), QueryTriggerInteraction.Collide)) return;
        if (TimeInCurrentState() < 1f) return;
        InvokePlayerDeath();
    }

    private void FireSwimTrigger()
    {
        if (WaterRaycast(out var swimRaycastParam))
        {
            Machine.Fire(PlayerFsmTrigger.SwimTriggerRaycastHit, swimRaycastParam);
        }
    }

    private bool WaterRaycast(out SwimRaycastParam swimRaycastParam)
    {

        swimRaycastParam = new SwimRaycastParam();
        WaterHazardType.Type HazardHelper(Vector3 position, GameObject waterObject)
        {
            foreach (var dualWaterPoint in DualWaterPointRegistry.DualWaterPoints)
            {
                if (Vector3.Distance(position, dualWaterPoint.transform.position) < dualWaterPoint.Radius) return WaterHazardType.Type.None;
            }

            var hazard = waterObject.GetComponent<WaterHazardType>();
            if (hazard == null) return WaterHazardType.Type.None;
            if (hazard.type == WaterHazardType.Type.InstantDrown) return WaterHazardType.Type.InstantDrown;
            if (hazard.type == WaterHazardType.Type.Freeze && _freezeTimer > SwimFreezeDuration) return WaterHazardType.Type.Freeze;

            return WaterHazardType.Type.None;
        }
        
        swimRaycastParam.WaterHazardType = WaterHazardType.Type.None;
        var origin = transform.position + Vector3.up * 5f;
        var maxDistance = 10f;
        if (Physics.Raycast(origin, Vector3.down, out var hit, maxDistance, LayerMask.GetMask("Water")))
        {
            swimRaycastParam.WaterHazardType = HazardHelper(hit.point, hit.transform.gameObject);
            swimRaycastParam.point = hit.point;
            swimRaycastParam.obj = hit.transform.gameObject;
            swimRaycastParam.distance = hit.distance;
            return true;
        };


        var colliders = Physics.OverlapSphere(transform.position, 0.5f, LayerMask.GetMask("Water"));
        foreach (var c in colliders)
        {
            swimRaycastParam.point = origin;
            swimRaycastParam.WaterHazardType = HazardHelper(origin, c.gameObject);
            swimRaycastParam.obj = c.gameObject;
            swimRaycastParam.distance = 0;
            return true;
        }

        return false;

    }

    public float GetFreezeWeight()
    {
        return _freezeTimer / SwimFreezeDuration;
    }

    public bool GetNearbyCultTrial(out CultTrialFsm cultTrialFsm)
    {
        cultTrialFsm = null;
        var colliders = Physics.OverlapSphere(transform.position, 3f, LayerMask.GetMask("CultTrialStartingLine"),
            QueryTriggerInteraction.Ignore);
        foreach (var c in colliders)
        {
            var fsm = c.transform.parent.parent.GetComponent<CultTrialFsm>();
            cultTrialFsm = fsm;
            return true;
        }

        return false;
    }

    public void OnCultTrialBoostTrigger()
    {

        _momentum = MaxMomentum;
        isSprinting = true;
        _timeSinceBoostStarted = 0f;
        
        
        StartCoroutine(TrailCoroutine());
        StartCoroutine(CameraCoroutine());
        StartCoroutine(TimescaleCoroutine());
        StartCoroutine(GravityCoroutine());
        

        IEnumerator CameraCoroutine()
        {
            var t = 0f;
            var d = 0.125f;
            yield return new WaitForSecondsRealtime(0.4f);
            var freeLook = PlayerCinemachineFreeLook.Singleton.GetFreeLook();
            var baseFov = PlayerCinemachineFreeLook.Singleton.GetBaseFov();
            while (t < d)
            {
                var w = Util.SmoothLerp01(t / d);

                freeLook.m_Lens.FieldOfView = Mathf.Lerp(baseFov, baseFov + 3f, w);

                var offset = freeLook.transform.GetComponent<CinemachineCameraOffset>();
                offset.m_Offset = Vector3.Lerp(Vector3.zero, new Vector3(0, 0, 1.5f), w);
                
                t += Time.deltaTime;
                yield return null;
            }

            
            StartCoroutine(SpeedBoostCameraCleanupCoroutine(0.2f));
        }
        
        IEnumerator TimescaleCoroutine()
        {
            var f = 0.3f;
            
            
            var t = 0f;
            var d = 0.25f;
            while (t < d)
            {
                Time.timeScale = Mathf.Lerp(1f, f, t / d); 
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.2f);
            
            t = 0f;
            d = 0.1f;
            while (t < d)
            {
                Time.timeScale = Mathf.Lerp(f, 1f, t / d); 
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            
            Time.timeScale = 1f;
        }
        
        IEnumerator GravityCoroutine()
        {
            yield return new WaitForSecondsRealtime(0.4f);
            BonusGravityModifier = 0.75f;
            var t = 0f;
            var d = BoostSpeedDuration;
            while (t < d)
            {
                t += Time.deltaTime;
                yield return null;
            }
            
            BonusGravityModifier = 1f;
        }
        
        IEnumerator TrailCoroutine()
        {
            
            // var triggerPrefab = Resources.Load("Prefab/Fsm/SphereEffect") as GameObject;
            // var triggerPosition = _skinnedMeshRenderer.transform.position;
            // yield return new WaitForSeconds(0.05f);
            // var triggerObject = Instantiate(triggerPrefab, triggerPosition,
            //     Quaternion.identity, null);
            // triggerObject.GetComponent<SphereEffect>().SetConfig(Vector3.one * 15f, 1.25f, 0.6f, -4.5f);

            yield return new WaitForSecondsRealtime(0.4f);
            PlaySpeedLineParticlesForDuration(BoostSpeedDuration);
            var d = BoostSpeedDuration * 1.15f;
            
            while (_timeSinceBoostStarted < d){
                if (Machine.IsInState(PlayerFsmState.TrialTeleport)) break;
                var comboMeshPrefab = Resources.Load("Prefab/Fsm/PlayerComboMesh") as GameObject;
                var position = _skinnedMeshRenderer.transform.position;
                var rotation = _skinnedMeshRenderer.transform.rotation;
                var mesh = new Mesh();
                _skinnedMeshRenderer.BakeMesh(mesh);
                var seconds = Random.Range(0.06f, 0.05f);
                yield return new WaitForSeconds(seconds);
                var comboMeshObject = Instantiate(comboMeshPrefab, position,
                    rotation, null);
                comboMeshObject.TryGetComponent(out MeshFilter meshFilter);
                meshFilter.mesh = mesh;
            }
        }
        
    }
    
    private void PlaySpeedLineParticlesForDuration(float duration)
    {
        _speedLinesParticles.Play();
        _timeSinceSpeedLinesParticlesPlayed = 0;
        _speedLinesParticlesDuration = duration;
    }

    private void HandleSpeedLines()
    {
        _speedLinesParticles.transform.position = Camera.main.transform.position;
        _speedLinesParticles.transform.rotation = Quaternion.Lerp(_speedLinesParticles.transform.rotation, Camera.main.transform.rotation, Time.deltaTime * 10f);
        _timeSinceSpeedLinesParticlesPlayed += Time.deltaTime;
        if (_timeSinceSpeedLinesParticlesPlayed > _speedLinesParticlesDuration && _speedLinesParticlesDuration > 0) _speedLinesParticles.Stop();
    }

    private void SetSafeGroundPosition()
    {
        // if (TimeInCurrentState() < 0.5f) return;

        var rayCount = 8;
        var originHeight = 5f;
        var originRadius = 5f;
        var maxDistance = 10f;
        
        // todo: dont set safe ground position on moving platforms?.PermitIf(PlayerFsmTrigger.Dash, PlayerFsmState.Dashsquat, @params => CanDash(@params) && TimeInCurrentState() > 0.1f); // microfall dash prevention hack
        for (int i = 0; i < rayCount; i++)
        {
            var origin = transform.position + Vector3.up * originHeight + Quaternion.Euler(0, 360f * ((float)i
                / rayCount), 0) * (Vector3.forward * originRadius);
            
            if (!Physics.Raycast(origin, Vector3.down, out var hit, maxDistance, Fsm.GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore)) return;
            if (hit.transform.TryGetComponent(out PlayerDeathGround _)) return;
            Debug.DrawRay(origin, Vector3.down * maxDistance, Color.red);
        }

        _safeGroundPosition = transform.position;

    }

    private void OnIntroCutsceneWarp(Vector3 obj)
    {
        OnPlayerTeleported?.Invoke(obj);
    }

    private void HandleSteepAnimationLayers()
    {
        var currentWeight = Animator.GetLayerWeight(3);
        var currentSteepness = Animator.GetFloat("Steepness");
        var w = 0f;
        var steepness = 0f;
        
        if (Machine.IsInState(PlayerFsmState.GroundMove))
        {
            if (GroundForwardSlope > 100f)
            {
                steepness = 1f;
                w = Mathf.InverseLerp(100f, 145f, GroundForwardSlope);
            }
            if (GroundForwardSlope < 80f)
            {
                steepness = -1f;
                w = Mathf.InverseLerp(80f, 35f, GroundForwardSlope) * -1f;
            }
        }
        
        Animator.SetFloat("Steepness", Mathf.Lerp(currentSteepness, steepness, Time.deltaTime * 15f));
        Animator.SetLayerWeight(3, Mathf.Lerp(currentWeight, Mathf.Abs(w), Time.deltaTime * 7f));
        
    }
    
    public void CollectItem(string id)
    {
        SaveSystem.WriteItem(id);
        
        if (!SaveSystem.GetPersistentEventCompleted("item-collected"))
        {
            StartCoroutine(Coroutine());
            IEnumerator Coroutine()
            {
                yield return new WaitForSeconds(5.75f);
                TutorialCanvas.Singleton.ShowTutorialText("Open bag", "Inventory");
            }
            SaveSystem.WritePersistentEvent("item-collected");
        }
        
        OnItemCollected?.Invoke(KeyItemRegistry.KeyItemRegistrations[id].displayName);
    }

    public void QueueSurge()
    {
        _isSurgeQueued = true;
        _timeSinceSurgeQueued = 0;
        Shader.SetGlobalFloat("_PlayerTintWeight", 1f);
        Shader.SetGlobalColor("_PlayerTintColor", Color.white);
    }

    private void HandleSurge()
    {
        BonusGravityModifier = _isSurging || _isSurgeQueued ? 0.85f : 1f;
        _timeSinceSurgeStarted += Time.deltaTime;
        _timeSinceSurgeQueued += Time.deltaTime;
        if (!_isSurgeQueued && !_isSurging) return;
        if (_isSurgeQueued && _timeSinceSurgeQueued > 0.15f && 
            !Machine.IsInState(PlayerFsmState.Tinsica) &&
            !(Machine.IsInState(PlayerFsmState.TinsicaJump) && TimeInCurrentState() < 0.5f) && 
            !(Machine.IsInState(PlayerFsmState.Dashsquat)) && 
            !(Machine.IsInState(PlayerFsmState.Dash) ) && 
            !(Machine.IsInState(PlayerFsmState.FallAfterDash) && TimeInCurrentState() < 0.25f) && 
            !(Machine.IsInState(PlayerFsmState.Skipsquat))&&
            !(Machine.IsInState(PlayerFsmState.LandsquatAfterDash))&&
            !(Machine.IsInState(PlayerFsmState.Wallrun) && TimeInCurrentState() < 0.75f) &&
            !(Machine.IsInState(PlayerFsmState.Jumpsquat)) &&
            !(Machine.IsInState(PlayerFsmState.Jump) && TimeInCurrentState() < 0.25f) &&
            !(Machine.IsInState(PlayerFsmState.Skip) && TimeInCurrentState() < 0.3f) &&
            !(Machine.IsInState(PlayerFsmState.Dying) || Machine.IsInState(PlayerFsmState.Dead) || Machine.IsInState(PlayerFsmState.Respawn))
            
            
           )
            
        {
            _isSurging = true;
            _isSurgeQueued = false;
            isSprinting = true;
            FMODUnity.RuntimeManager.PlayOneShotAttached(comboTriggerFmodEvent, gameObject);
            _timeSinceSurgeStarted = 0f;
            PlaySpeedLineParticlesForDuration(1.5f);

            Machine.Jump(PlayerFsmState.SurgeDash);
        } else if (_isSurgeQueued)
        {
            Shader.SetGlobalColor("_PlayerTintColor", Color.white);
            Shader.SetGlobalFloat("_PlayerTintWeight", 1f);
            return;
        } else if (_timeSinceSurgeStarted > MaxSurgeDuration)
        {
            EndSurge();
        }
        
        Shader.SetGlobalFloat("_PlayerTintWeight", Mathf.Lerp(1f, 0f, Mathf.InverseLerp(0, 0.2f, _timeSinceSurgeStarted)));
        
        
        
    }

    private IEnumerator SurgeTrailCoroutine()
    {
        while (true){
            if (_isSurgeQueued) yield return new WaitForSeconds(Random.Range(0.12f, 0.1f));
            yield return new WaitForSeconds(Random.Range(0.06f, 0.05f));
            if ((!_isSurging || _timeSinceSurgeStarted > 1.5f) && !_isSurgeQueued) continue;
            if (Machine.IsInState(PlayerFsmState.TrialTeleport)) continue;
            var comboMeshPrefab = Resources.Load("Prefab/Fsm/PlayerComboMesh") as GameObject;
            var position = _skinnedMeshRenderer.transform.position;
            var rotation = _skinnedMeshRenderer.transform.rotation;
            var mesh = new Mesh();
            _skinnedMeshRenderer.BakeMesh(mesh);
            var comboMeshObject = Instantiate(comboMeshPrefab, position,
                rotation, null);
            comboMeshObject.TryGetComponent(out MeshFilter meshFilter);
            meshFilter.mesh = mesh;
        }
    }

    public float GetTimeSinceRespawn()
    {
        return _timeSinceRespawn;
    }
    
    

    
}