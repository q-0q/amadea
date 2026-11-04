using System;
using Code.Misc;
using Code.TriggerParams;
using FMOD.Studio;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using Wasp;

public partial class PlayerFsm
{
    public static event Action<bool> OnPlayerEnteredSlideLateral;
    
    private void SlideLateralOnUpdate()
    {
        Animator.SetLayerWeight(1, 0);
        SetAnimatorMomentum();
        var rotationMod = _currentFlankType == FlankType.Left ? -1f : 1f;
        var flattenedNormal = new Vector3(_currentSlideNormal.x, 0, _currentSlideNormal.z);
        var forward = Quaternion.Euler(0f, 90f * rotationMod, 0f) * flattenedNormal;
        var lookRotation = Quaternion.LookRotation(forward, GetUpVector());
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * FlankAlignmentRotationSpeed);
        
        HandleCollisionMove(0.25f);

    }
    
    private void FallAfterSlideLateralOnUpdate()
    {
        Animator.SetLayerWeight(1, 0);
        SetAnimatorMomentum();
        var rotationMod = _currentFlankType == FlankType.Left ? -1f : 1f;
        var flattenedNormal = new Vector3(_currentSlideNormal.x, 0, _currentSlideNormal.z);
        var forward = Quaternion.Euler(0f, 90f * rotationMod, 0f) * flattenedNormal;
        var lookRotation = Quaternion.LookRotation(forward, GetUpVector());
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * FlankAlignmentRotationSpeed);
        
        HandleCollisionMove(0.15f);
        
        transform.position +=
            ComputeCollisionMove(flattenedNormal.normalized * (Time.deltaTime * FlankWallVacuumStrength));

    }
    
    private void SlideDownOnUpdate()
    {

        
        var flattenedNormal = new Vector3(_currentSlideNormal.x, 0, _currentSlideNormal.z).normalized;
        
        var lookRotation = Quaternion.LookRotation(flattenedNormal, GetUpVector());
        Animator.SetLayerWeight(1, 0);
 
        SetAnimatorMomentum();
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * FlankAlignmentRotationSpeed);


        
        HandleCollisionMove(0.15f);

    }

    private void SlideOnUpdate()
    {
        GetGroundedRaycastHit(out var groundedRaycastHit);
        if (groundedRaycastHit.collider != null)
        {
            if (IsSlideTriggerCore(groundedRaycastHit))
            {
                if (_currentSlideTransform != groundedRaycastHit.transform)
                {
                    _currentSlideTransform = groundedRaycastHit.transform;
                    if (Vector3.Angle(_currentSlideNormal, groundedRaycastHit.normal) > 20f)
                    {
                        if (Machine.IsInState(PlayerFsmState.SlideDown))
                        {
                            YVelocity = 0f;
                            Machine.Jump(PlayerFsmState.SlideLateral,
                                new RaycastHitParam() { Hit = groundedRaycastHit });
                        }
                        else if (TimeInCurrentState() >= 0.5f) YVelocity = 8f;
                    };
                }
                _currentSlideNormal = groundedRaycastHit.normal;
            }
        } 
        
        var flattenedNormal = new Vector3(_currentSlideNormal.x, 0, _currentSlideNormal.z).normalized;

        var vacuumRaycastOrigin = transform.position + GetUpVector() * 1.5f;
        if (Physics.Raycast(vacuumRaycastOrigin, -flattenedNormal, out RaycastHit vacuumHit, 3f, GetEnvironmentalLayermask()))
        {
            var distance = Vector3.Distance(transform.position, vacuumHit.point);
            var distanceMod = Mathf.Lerp(0.1f, 1f, Mathf.InverseLerp(2.5f, 3.25f, distance));
            transform.position +=
                ComputeCollisionMove(-flattenedNormal.normalized * (Time.deltaTime * FlankWallVacuumStrength * distanceMod));
        }
    }

    private void SlideConfigure()
    {
        Machine.Configure(PlayerFsmState.Slide)
            .SubstateOf(GravityFsmState.Aerial)
            .SubstateOf(GravityFsmState.RespectParentTransform)
            .SubstateOf(PlayerFsmState.PitonInteractable)
            
            .PermitIf(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.Landsquat,
                @params => !IsSlideTrigger(@params) && TimeInCurrentState() >= 0.25f)
            .PermitIf(GravityFsmTrigger.StartFrameAerial, PlayerFsmState.FallAfterSlide, _ => YVelocity < 0.5f, 5)
            .PermitIf(PlayerFsmTrigger.Jump, PlayerFsmState.Skipsquat,
                _ => _timeSinceDashFinished <= SkipWindowDuration, 1)
            
            .PermitIf(GravityFsmTrigger.StartFrameAerial, PlayerFsmState.Jumpsquat, _ => _inputBuffer.IsBuffered("Jump"), 7)
            .PermitIf(GravityFsmTrigger.StartFrameAerial, PlayerFsmState.Dashsquat, @params => _inputBuffer.IsBuffered("Dash") && CanDash(@params), 8)

            .PermitIf(PlayerFsmTrigger.Jump, PlayerFsmState.Jumpsquat, _ => TimeInCurrentState() > 0.4f)
            .PermitIf(PlayerFsmTrigger.FaceLedge, PlayerFsmState.Vault, _ => YVelocity > VaultMinimumYVelocity, 1)
            .PermitIf(PlayerFsmTrigger.FaceLedge, PlayerFsmState.MediumVaultHang, _ => true)
            .PermitIf(PlayerFsmTrigger.FaceWallStrict, PlayerFsmState.Wallsquat,
                _ => _momentum > WallSquatMinimumMomentum && TimeInCurrentState() > 0.25f)
            .PermitIf(PlayerFsmTrigger.FaceHighLedge, PlayerFsmState.Wallsquat,
                _ => _momentum > WallSquatMinimumMomentum)
            .PermitIf(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.HardLandRoll,
                @params => (!IsSlideTrigger(@params) && CurrentFallDistance() < HardLandAirDiff && _momentum > HardLandRollMinimumMomentum), 4)
            .OnEntry(_ =>
            {
                
                FMODUnity.RuntimeManager.AttachInstanceToGameObject(slideFmodInstance, gameObject);
                slideFmodInstance.start();
            })
            .OnEntry(@params =>
            {
                if (@params is not RaycastHitParam raycastHitParam) return;
                _currentSlideNormal = raycastHitParam.Hit.normal;
                _currentSlideTransform = raycastHitParam.Hit.transform;
            })

            .OnExit(_ =>
            {
                slideFmodInstance.stop(STOP_MODE.ALLOWFADEOUT);
                _slideTimer = 0;
            });
        // .OnExitFrom(GravityFsmTrigger.StartFrameAerial, _ => { _momentum = 7f; });


        Machine.Configure(PlayerFsmState.SlideLateral)
            .SubstateOf(PlayerFsmState.Slide)
            .PermitIf(GravityFsmTrigger.StartFrameAerial, PlayerFsmState.FallAfterSlideLateral, _ => YVelocity < 0.5f, 6)
            // .PermitIf(PlayerFsm.PlayerFsmTrigger.SoftTurnLeft, PlayerFsmState.SlideDown, _ => _currentFlankType == FlankType.Right && TimeInCurrentState() > MinSlideStateTimer)
            .PermitIf(PlayerFsm.PlayerFsmTrigger.HardTurn, PlayerFsmState.SlideDown, _ => TimeInCurrentState() > MinSlideStateTimer, 2)
            // .PermitIf(PlayerFsm.PlayerFsmTrigger.SoftTurnRight, PlayerFsmState.SlideDown, _ => _currentFlankType == FlankType.Left && TimeInCurrentState() > MinSlideStateTimer)
            .OnEntry(@params =>
            {
                YVelocity += 10f;
                _wallsquattedSinceLeavingGround = false;
                _dashSinceLeavingGround = false;
                _previousWallrunSide = FlankType.None;

                _momentum = Mathf.Max(_momentum, 13f);
                IncrementCombo();

                if (@params is not RaycastHitParam raycastHitParam) return;
                var flattenedNormal = new Vector3(raycastHitParam.Hit.normal.x, 0, raycastHitParam.Hit.normal.z);
                var signedAngle = Vector3.SignedAngle(flattenedNormal, transform.forward, GetUpVector());
                bool flip = signedAngle > 0;
                OnPlayerEnteredSlideLateral?.Invoke(flip);
                _currentFlankType = flip ? FlankType.Right : FlankType.Left;
                Animator.SetFloat("Flip", flip ? 0f : 1f);
            })
            .OnEntryFrom(PlayerFsmTrigger.SoftTurnRight, _ =>
            {
                OnPlayerEnteredSlideLateral?.Invoke(true);
                _currentFlankType = FlankType.Right;
                Animator.SetFloat("Flip", 0f);
            })
            .OnEntryFrom(PlayerFsmTrigger.SoftTurnLeft, _ =>
            {
                OnPlayerEnteredSlideLateral?.Invoke(false);
                _currentFlankType = FlankType.Left;
                Animator.SetFloat("Flip", 1f);
            })
            .OnExitFrom(GravityFsmTrigger.StartFrameGrounded, _ =>
            {
                var rotationMod = _currentFlankType == FlankType.Left ? -1f : 1f;
                var flattenedNormal = new Vector3(_currentSlideNormal.x, 0, _currentSlideNormal.z);
                var forward = Quaternion.Euler(0f, 25f * rotationMod, 0f) * flattenedNormal;
                transform.rotation = Quaternion.LookRotation(forward, GetUpVector());
            })
            .OnExitFrom(PlayerFsmTrigger.Jump, _ =>
            {
                _momentum = MaxMomentum;
                var rotationMod = _currentFlankType == FlankType.Left ? -1f : 1f;
                var flattenedNormal = new Vector3(_currentSlideNormal.x, 0, _currentSlideNormal.z);
                var forward = Quaternion.Euler(0f, 45 * rotationMod, 0f) * flattenedNormal;
                transform.rotation = Quaternion.LookRotation(forward, GetUpVector());
            });

        Machine.Configure(PlayerFsmState.SlideDown)
            .SubstateOf(PlayerFsmState.Slide)
            .PermitIf(PlayerFsm.PlayerFsmTrigger.SoftTurnLeft, PlayerFsmState.SlideLateral, _ => TimeInCurrentState() > MinSlideStateTimer)
            .PermitIf(PlayerFsm.PlayerFsmTrigger.SoftTurnRight, PlayerFsmState.SlideLateral, _ => TimeInCurrentState() > MinSlideStateTimer)
            .OnEntry(@params =>
            {
                _wallsquattedSinceLeavingGround = false;
                _dashSinceLeavingGround = false;
                _previousWallrunSide = FlankType.None;
                _momentum = Mathf.Max(_momentum, 3f);
                IncrementCombo();
            })
            .OnExitFrom(PlayerFsm.PlayerFsmTrigger.SoftTurnLeft, _ =>
            {
                _currentFlankType = FlankType.Left;
            })
            .OnExitFrom(PlayerFsm.PlayerFsmTrigger.SoftTurnRight, _ =>
            {
                _currentFlankType = FlankType.Right;
            })
            .OnExit(_ =>
            {
                _momentum = 9f;
            });

        Machine.Configure(PlayerFsmState.SlideInteractable)
            .PermitIf(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.SlideLateral,
                @params =>
                {
                    return IsSlideTrigger(@params) &&
                           !(Machine.IsInState(PlayerFsmState.Jump) && TimeInCurrentState() < 0.25f) &&
                           !(Machine.IsInState(PlayerFsmState.Skip) && TimeInCurrentState() < 0.3f) &&
                           (Machine.IsInState(PlayerFsmState.Aerial) || _slideTimer > MaxSlideTimer);
                }, 6)
            .PermitIf(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.SlideDown,
                @params =>
                {
                    if (!IsSlideTrigger(@params)) return false;
            
                    if ((Machine.IsInState(PlayerFsmState.Jump) && TimeInCurrentState() < 0.25f) ||
                        (Machine.IsInState(PlayerFsmState.Skip) && TimeInCurrentState() < 0.3f)) return false;
            
                    if (!Machine.IsInState(PlayerFsmState.Aerial) && _slideTimer < MaxSlideTimer) return false;
                    
                    if (@params is not RaycastHitParam raycastHitParam) return false;
            
                    var angle = Vector3.Angle(transform.forward,
                        new Vector3(raycastHitParam.Hit.normal.x, 0, raycastHitParam.Hit.normal.z));
            
                    if (_momentum < 6f) return true;
                    if (angle < 30f) return true;
                    if (angle > 150f) return true;
                    return false;
                }, 7);
        
        
        Machine.Configure(PlayerFsmState.FallAfterSlide)
            .SubstateOf(GravityFsmState.Aerial)
            .SubstateOf(PlayerFsmState.Landable)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.Fall);
        
        Machine.Configure(PlayerFsmState.FallAfterSlideLateral)
            .SubstateOf(GravityFsmState.Aerial)
            .SubstateOf(PlayerFsmState.Landable)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.Fall);
    }
    
    
    private bool IsSlideTrigger(TriggerParams triggerParams)
    {
        if (triggerParams is not RaycastHitParam raycastHitParam) return false;
        return IsSlideTriggerCore(raycastHitParam.Hit);
    }
    
    private bool IsSlideTriggerCore(RaycastHit hit)
    {
        var playerSlideIndicator = hit.transform.GetComponent<PlayerSlideIndicator>();
        if (playerSlideIndicator == null) return false;
        if (playerSlideIndicator.allNormals) return true;

        var ray = hit;
            
        
        // hack for stopping corner sliding
        var originOffset = Vector3.zero;
        if (Machine.IsInState(PlayerFsmState.SlideLateral))
        {
            var flattenedNormal = new Vector3(_currentSlideNormal.x, 0, _currentSlideNormal.z);
            originOffset = flattenedNormal.normalized;
        }

        if (Physics.Raycast(hit.point + originOffset + GetUpVector() * 3f, GetDownVector(), out RaycastHit recast, 6f,
                GetEnvironmentalLayermask()))
        {
            ray = recast;
        }
        else return true; // <- perhaps my favorite line of code i have ever written

        var val = Vector3.Angle(ray.normal, ray.transform.up) < 10f && Vector3.Angle(ray.normal, GetUpVector()) < 60f;
        if (!val)
        {
            Debug.DrawRay(ray.point, GetUpVector(), Color.blue, 1f);
            Debug.DrawRay(ray.point, ray.transform.up, Color.red, 1f);
            Debug.DrawRay(ray.point, ray.normal, Color.yellow, 1f);
        }
        return val;
    }
    
}
