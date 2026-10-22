using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FMOD.Studio;
using UnityEditor;
using UnityEngine;
using Wasp;

public partial class PlayerFsm
{

    private void SwimOnUpdate()
    {
        if (_playerInput.actions["Sprint"].IsPressed()) isSprinting = true;
        
        HandleInputMomentumChange();
        HandleTurning(1f, false, 1f, false, isSprinting ? 0.5f : 1f);
        
        SetAnimatorMomentum();
        SetAnimatorSpeedMod();

        if (!WaterRaycast(out var swimRaycastParam)) return;
        if (!swimRaycastParam.obj.TryGetComponent(out WaterHazardType waterHazardType)) return;
        if (waterHazardType.type != WaterHazardType.Type.Freeze)
        {
            _freezeTimer = 0;
            freezeFmodInstance.stop(STOP_MODE.ALLOWFADEOUT);
            return;
        }

        if (_freezeTimer < 0.01f) freezeFmodInstance.start(); // kinda a hack but w/e
        _freezeTimer += Time.deltaTime;
    }
    
    
    private void SwimSurfaceRiseOnUpdate()
    {
        var desiredYVelocity = 25f;
        
        if (WaterRaycast(out var swimRaycastParam))
        {
            desiredYVelocity = Mathf.Lerp(0f, 25f, Mathf.InverseLerp(4f, -0.80f, swimRaycastParam.distance));

            
            
            if (swimRaycastParam.WaterHazardType == WaterHazardType.Type.InstantDrown)
            {
                _momentum = Mathf.Lerp(_momentum, 0,
                    Time.deltaTime * Mathf.Lerp(2f, 10f, Mathf.InverseLerp(0, 0.5f, TimeInCurrentState())));
            };
        }
        
        YVelocity = Mathf.Lerp(YVelocity, desiredYVelocity,
            Time.deltaTime * Mathf.Lerp(1f, 20f, Mathf.InverseLerp(0, 0.45f, TimeInCurrentState())));
        
        HandleCollisionMove(Mathf.Lerp(0.1925f, 0.0925f * 0.75f, Mathf.InverseLerp(0, 0.4f, TimeInCurrentState())));
    }
    
    
    private void DrownOnUpdate()
    {
        if (!_swimSurfaceRippleQueued) StartCoroutine(SwimSurfaceRippleCoroutine());
        transform.position += Vector3.down * (Time.deltaTime * 0.5f);
    }
    
    private void SwimSurfaceOnUpdate()
    {
        if (WaterRaycast(out var swimRaycastParam))
        {
            transform.position += ComputeCollisionMove(((swimRaycastParam.point + Vector3.up * -0.80f) - transform.position) * (Time.deltaTime * 15f));
            
            if (swimRaycastParam.WaterHazardType == WaterHazardType.Type.InstantDrown)
            {
                _momentum = Mathf.Lerp(_momentum, 0,
                    Time.deltaTime * Mathf.Lerp(2f, 10f, Mathf.InverseLerp(0, 0.5f, TimeInCurrentState())));
            };
            
            if (swimRaycastParam.WaterHazardType == WaterHazardType.Type.Freeze)
            {
                InvokePlayerDeath();
            };
        }

        if (!PhotoManager.Singleton.IsActive()) OnPlayerWakeGenerated?.Invoke(GetRipplePosition(), Mathf.Lerp(0.075f, 0.075f, ComputeMomentumWeight()), Mathf.Lerp(0.0011f, 0.0009f, ComputeMomentumWeight()));
        if (!_swimSurfaceRippleQueued) StartCoroutine(SwimSurfaceRippleCoroutine());
        HandleCollisionMove(0.0925f * 0.75f);
    }

    private IEnumerator SwimSurfaceRippleCoroutine()
    {
        _swimSurfaceRippleQueued = true;
        OnPlayerRippleGenerated?.Invoke(GetRipplePosition(), 0.5f, 0.001f);
        yield return new WaitForSeconds(Mathf.Lerp(SwimSurfaceRippleTimer * 7f, SwimSurfaceRippleTimer, ComputeMomentumWeight()));
        _swimSurfaceRippleQueued = false;
    }

    private Vector3 GetRipplePosition()
    {
        return transform.position + transform.forward * Mathf.Lerp(0f, 1f, ComputeMomentumWeight());
    }

    private void SwimConfigure()
    {
        Machine.Configure(PlayerFsmState.Swim)
            .SubstateOf(GravityFsmState.Aerial)
            .SubstateOf(PlayerFsmState.Landable)

            .SubstateOf(GravityFsmState.DontLoseYVelocity)
            .OnEntry(_ =>
            {
                _wallsquattedSinceLeavingGround = false;
                _dashSinceLeavingGround = false;
                _previousWallrunSide = FlankType.None;
                _currentFlankType = FlankType.None;
                currentRopeSwing = null;
            });
        
        Machine.Configure(PlayerFsmState.SwimSurfaceRise)
            .PermitIf(PlayerFsmTrigger.SwimTriggerRaycastHit, PlayerFsmState.SwimSurface, @params => IsSwimTriggerAtSurface(@params) && YVelocity < 2f)
            .SubstateOf(PlayerFsmState.Swim)
            // .PermitIf(PlayerFsmTrigger.SwimTriggerRaycastHit, PlayerFsmState.Drown, @params =>
            // {
            //     if (TimeInCurrentState() < 0.75f) return false;
            //     if (@params is not SwimRaycastParam SwimRaycastParam) return false;
            //     return SwimRaycastParam.drown;
            // }, 2)
            .OnEntry(_ =>
            {
                if (YVelocity > -5f) return;
                if (WaterRaycast(out var swimRaycastParam))
                {
                    _splashParticles.transform.position = swimRaycastParam.point;
                    _splashParticles.Play();
                    FMODUnity.RuntimeManager.PlayOneShotAttached(splashFmodEvent, _splashParticles.gameObject);
                }
                OnPlayerRippleGenerated?.Invoke(transform.position, 1.0f, 0.005f);
                OnPlayerWakeGenerated?.Invoke(transform.position, 1.0f, 0.0025f);
            });
        
        Machine.Configure(PlayerFsmState.SwimSurface)
            .PermitIf(PlayerFsmTrigger.SwimTriggerRaycastHit, PlayerFsmState.Drown, @params =>
            {
                if (_momentum > 7f) return false;
                if (@params is not SwimRaycastParam SwimRaycastParam) return false;
                return SwimRaycastParam.WaterHazardType == WaterHazardType.Type.InstantDrown;
            },3)
            .PermitIf(PlayerFsmTrigger.FaceLedge, PlayerFsmState.Vault, CanVault, 1)
            .PermitIf(PlayerFsmTrigger.FaceLedge, PlayerFsmState.MediumVaultHang, _ => !Machine.IsInState(PlayerFsmState.PitonFlip) || YVelocity < PitonMaximumWallInteractYVelocity)
            .PermitIf(PlayerFsmTrigger.FaceWall, PlayerFsmState.Wallsquat,
                _ => _momentum > WallSquatMinimumMomentum && WallsquatVelocityChecker() && !_wallsquattedSinceLeavingGround)
            .PermitIf(PlayerFsmTrigger.FaceWallStrict, PlayerFsmState.Wallsquat,
                _ => _momentum > WallSquatMinimumMomentum && WallsquatVelocityChecker() && !_wallsquattedSinceLeavingGround)
            .PermitIf(PlayerFsmTrigger.FaceHighLedge, PlayerFsmState.Wallsquat,
                _ => _momentum > WallSquatMinimumMomentum && WallsquatVelocityChecker() && !_wallsquattedSinceLeavingGround)
            .OnEntry(_ =>
            {
                YVelocity = 0;
            })
            .SubstateOf(PlayerFsmState.Swim);

        Machine.Configure(PlayerFsmState.DiveFall)
            .SubstateOf(PlayerFsmState.Fall);

        Machine.Configure(PlayerFsmState.Drown)
            .OnEntry(_ =>
            {
                EndSurge();
            })
            .Permit(FsmTrigger.Timeout, PlayerFsmState.Dying);
    }


    private bool IsSwimTrigger(TriggerParams triggerParams)
    {
        if (triggerParams is not SwimRaycastParam SwimRaycastParam) return false;
        var distance = SwimRaycastParam.distance;
        return distance < 3f;
    }
    
    private bool IsSwimTriggerAtSurface(TriggerParams triggerParams)
    {
        if (triggerParams is not SwimRaycastParam SwimRaycastParam) return false;
        var distance = SwimRaycastParam.distance;
        return distance > 3f;
    }
    
}