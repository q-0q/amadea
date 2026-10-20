using System;
using System.Collections;
using System.Collections.Generic;
using Code.Misc;
using UnityEngine;

public partial class PlayerFsm
{

    public static event Action<string> OnTrickAcquired;

    private void TinsicaOnUpdate()
    {
        _timeSinceJumpBufferable = 0f;
        var speedMod = Mathf.Lerp(1f, Mathf.Lerp(1.75f, 1f, Mathf.InverseLerp(0.2f, 0.45f, TimeInCurrentState())), 
            Mathf.InverseLerp(0.1f, 0.2f, TimeInCurrentState()));
        HandleCollisionMove(speedMod, false);
        transform.position += ComputeCollisionMove(Vector3.down * (3f * Time.deltaTime));
        HandleTurning(1f, false, 0f, false, 0.25f);
        
        
        var animatorSpeedMod = Mathf.Lerp(GroundMoveMinimumAnimatorSpeedMod, GroundMoveMaximumAnimatorSpeedMod, ComputeMomentumWeight()) *
                       (_isSurging ? 1.5f : 1f); // same as SetAnimatorSpeedMod() but whtout boost
        Animator.SetFloat("SpeedMod", animatorSpeedMod);

        var ledgeMountSpeed = 25f;
        if (UpdateLedgePosition(FaceHighLedgeHeight, true))
        {
            MoveYOntoLedgeLinear(0f, ledgeMountSpeed);
        }
        else if (UpdateLedgePosition(FaceLedgeHeight, true))
        {
            MoveYOntoLedgeLinear(0f, ledgeMountSpeed);
        }    
    }
    


    private void TinsicaJumpOnUpdate()
    {
        Animator.SetLayerWeight(1, 0);
        _momentum = Mathf.Max(_momentum, TinsicaEntryMomentum);
        transform.position += ComputeCollisionMove(transform.forward * (Time.deltaTime * 5f));
    }
    
    
    private void DolphinAOnUpdate()
    {

        var startupDuration = 0.1f;
        var hopDuration = 0.25f;
        var hopStartYVelocity = 25f;
        var hopEndYVelocity = 7f;
        var hopStartSpeed = 4f;
        var hopEndSpeed = 3.5f;
        // YVelocity = Mathf.Lerp(hopStartYVelocity, hopEndYVelocity, Mathf.InverseLerp(startupDuration, startupDuration + hopDuration, TimeInCurrentState()));
        // YVelocity = Mathf.Lerp(0f, YVelocity, Mathf.InverseLerp(startupDuration, startupDuration + 0.1f, TimeInCurrentState()));
        
        YVelocity = Mathf.Lerp(0f, 20f, Mathf.InverseLerp(startupDuration, startupDuration + 0.05f, TimeInCurrentState()));
        
        var speedMod = Mathf.Lerp(hopStartSpeed, hopEndSpeed, Mathf.InverseLerp(0f, 0.25f, TimeInCurrentState()));
        speedMod = Mathf.Lerp(0.35f, speedMod, Mathf.InverseLerp(startupDuration, startupDuration + 0.05f, TimeInCurrentState()));

        _momentum = MaxMomentum;
        HandleCollisionMove(speedMod, false);
        HandleTurning(1f, false, 0f, false, 0.25f);
        
        
        var animatorSpeedMod = Mathf.Lerp(GroundMoveMinimumAnimatorSpeedMod, GroundMoveMaximumAnimatorSpeedMod, ComputeMomentumWeight()) *
                               (_isSurging ? 1.5f : 1f); // same as SetAnimatorSpeedMod() but whtout boost
        Animator.SetFloat("SpeedMod", animatorSpeedMod);
        
    }

    private void DolphinBOnUpdate()
    {
        transform.position += ComputeCollisionMove(transform.forward * (Time.deltaTime * 13f));
    }
    
    private void TrickConfigure()
    {
        
        Machine.Configure(PlayerFsmState.TinsicaUsable)
            .PermitIf(PlayerFsmTrigger.Trick, PlayerFsmState.Tinsica, _ =>
            {
                if (Machine.IsInState(PlayerFsmState.Fall))
                {
                    if (!CoyoteTimeClause()) return false;
                    if (_timeSinceTinsicaExited < CoyoteTime) return false;
                }
                if (Machine.IsInState(PlayerFsmState.SlowVaultFinish) && TimeInCurrentState() < 0.1f) return false;
                return PlayerManaManager.Singleton.GetCurrentAvailableMana() >= 1 && SaveSystem.GetTrick("Tinsica");
            }, 5);
        
        Machine.Configure(PlayerFsmState.Tinsica)
            // .SubstateOf(GravityFsmState.Grounded)
            .SubstateOf(PlayerFsmState.DisplaceFoliage)
            .PermitIf(PlayerFsmTrigger.Jump, PlayerFsmState.TinsicaJump, _ =>
            {
                return TimeInCurrentState() > 0.4 * ComputeTiniscaDurationMod() && PlayerManaManager.Singleton.GetCurrentAvailableMana() >= 1;;
            }, 2)
            // .PermitIf(PlayerFsmTrigger.Jump, PlayerFsmState.Jump, _ =>
            // {
            //     return TimeInCurrentState() > 0.4 * ComputeTiniscaDurationMod()  && PlayerManaManager.Singleton.GetCurrentAvailableMana() < 1;
            // })
            .PermitIf(FsmTrigger.Timeout, PlayerFsmState.GroundMove, _ =>
            {
                var durationMod = ComputeTiniscaDurationMod();
                var duration = TinsicaDuration * durationMod;
                return TimeInCurrentState() >= duration;
            })
            .OnEntry(_ =>
            {
                // _currentLedgePosition = transform.position;
                _inputBuffer.ConsumeBuffer("Trick");
                StartCoroutine(TrickTintCoroutine(0.155f));
                _playerTrickParticles.InvokeTinsica();
                FMODUnity.RuntimeManager.PlayOneShotAttached(FMODUnity.RuntimeManager.PathToEventReference("event:/Trick"), gameObject);
                FMODUnity.RuntimeManager.PlayOneShotAttached(dashWhooshEventReference, gameObject);
                _momentum = Mathf.Max(_momentum, TinsicaEntryMomentum);
                OnPlayerFootstep();
                YVelocity = 0;
                LastUpwardsY = transform.position.y;
                
                PlayerManaManager.Singleton.Consume();
            })
            .OnExit(_ =>
            {
                _timeSinceTinsicaExited = 0f;
            });
        
        
        Machine.Configure(PlayerFsmState.TinsicaJump)
            .SubstateOf(GravityFsmState.Aerial)
            .SubstateOf(PlayerFsmState.Landable)
            .SubstateOf(PlayerFsmState.AirControl)
            .SubstateOf(PlayerFsmState.PitonInteractable)
            .PermitIf(PlayerFsmTrigger.Dash, PlayerFsmState.Dashsquat, CanDash)
            .SubstateOf(PlayerFsmState.WallInteractable)
            .SubstateOf(PlayerFsmState.DolphinUsable)
            .PermitIf(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.LandsquatAfterDash, @params => !IsSlideTrigger(@params) && YVelocity < 0.5f && _momentum > 5f, 1)
            // .PermitIf(PlayerFsmTrigger.IsAboveWater, PlayerFsmState.DiveFall, _ =>
            // {
            //     return TimeInCurrentState() > 0.6f;
            // })
            .OnExitFrom(GravityFsmTrigger.StartFrameGrounded, _ =>
            {
                EndSurge();
            })
            .OnEntry(_ =>
            {
                // _momentum = 5f;
                StartCoroutine(TrickTintCoroutine(0.1f));
                _inputBuffer.ConsumeBuffer("Jump");
                _playerTrickParticles.InvokeTinsicaJump();
                FMODUnity.RuntimeManager.PlayOneShotAttached(FMODUnity.RuntimeManager.PathToEventReference("event:/Trick"), gameObject);
                FMODUnity.RuntimeManager.PlayOneShotAttached(skipWhooshEventReference, gameObject);
                PlayerManaManager.Singleton.Consume();
                OnPlayerFootstep();
                YVelocity = 21f;
            });
        
        
        
        
        
        Machine.Configure(PlayerFsmState.DolphinUsable)
            .PermitIf(PlayerFsmTrigger.Trick, PlayerFsmState.DolphinA, _ =>
            {
                return PlayerManaManager.Singleton.GetCurrentAvailableMana() >= 1 && SaveSystem.GetTrick("Dolphin");
            });
        
        Machine.Configure(PlayerFsmState.DolphinA)
            .SubstateOf(PlayerFsmState.Landable)
            .SubstateOf(PlayerFsmState.Aerial)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.DolphinB)
            .OnEntry(_ =>
            {
                // _currentLedgePosition = transform.position;
                _inputBuffer.ConsumeBuffer("Trick");
                StartCoroutine(TrickTintCoroutine(0));
                _playerTrickParticles.InvokeTinsica();
                FMODUnity.RuntimeManager.PlayOneShotAttached(FMODUnity.RuntimeManager.PathToEventReference("event:/Trick"), gameObject);
                FMODUnity.RuntimeManager.PlayOneShotAttached(dashWhooshEventReference, gameObject);
                _momentum = Mathf.Max(_momentum, TinsicaEntryMomentum);
                
                LastUpwardsY = transform.position.y;
                
                PlayerManaManager.Singleton.Consume();
            })
            .OnExit(_ =>
            {
                
            });
        
        Machine.Configure(PlayerFsmState.DolphinB)
            .SubstateOf(GravityFsmState.Aerial)
            .SubstateOf(PlayerFsmState.Landable)
            .SubstateOf(PlayerFsmState.AirControl)
            .SubstateOf(PlayerFsmState.PitonInteractable)
            .SubstateOf(PlayerFsmState.WallInteractable)
            .PermitIf(PlayerFsmTrigger.Dash, PlayerFsmState.Dashsquat, CanDash)
            // .Permit(FsmTrigger.Timeout, PlayerFsmState.Fall)
            .OnEntry(_ =>
            {

            })
            .OnExit(_ =>
            {
                
            });
    }

    private float ComputeTiniscaDurationMod()
    {
        var comboMultiplier = GetCurrentSurgeSpeedMultiplier();
        var boostMultiplier = GetCurrentBoostSpeedMultiplier();
        var miscMultiplier = GetCurrentMiscSpeedMultiplier();
        
        return Mathf.Lerp(1.3f, 1f, Mathf.InverseLerp(TinsicaEntryMomentum, MaxMomentum, _momentum)) *
               (1f / SurgeMoveSpeedModifier);
    }

    private IEnumerator TrickTintCoroutine(float delay = 0)
    {
        if (_isSurgeQueued) yield break;
        yield return new WaitForSeconds(delay);
        var t = 0f;
        var d = 0.15f;
        Shader.SetGlobalColor("_PlayerTintColor", Color.white);
        while (t < d)
        {
            var w = Util.SmoothLerp01(t / d);

            Shader.SetGlobalFloat("_PlayerTintWeight", 1f - w);
            t += Time.deltaTime;
            yield return null;
        }

        Shader.SetGlobalFloat("_PlayerTintWeight", 0);
    }

    public void AcquireTrick(string trick)
    {
        SaveSystem.WriteTrick(trick);
        OnTrickAcquired?.Invoke(trick);
    }
}


