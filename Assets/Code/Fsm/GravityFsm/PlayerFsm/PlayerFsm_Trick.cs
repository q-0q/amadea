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
    
    private void TrickConfigure()
    {
        
        Machine.Configure(PlayerFsmState.TinsicaUsable)
            .PermitIf(PlayerFsmTrigger.Trick, PlayerFsmState.Tinsica, _ =>
            {
                if (Machine.IsInState(PlayerFsmState.SlowVaultFinish) && TimeInCurrentState() < 0.1f) return false;
                return PlayerManaManager.Singleton.GetCurrentAvailableMana() >= 1 && SaveSystem.GetTrick("Tinsica");
            });
        
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
                _currentLedgePosition = transform.position;
                _inputBuffer.ConsumeBuffer("Trick");
                StartCoroutine(TrickTintCoroutine(0.155f));
                _playerTrickParticles.InvokeTinsica();
                _momentum = Mathf.Max(_momentum, TinsicaEntryMomentum);
                PlayerManaManager.Singleton.Consume();
            });
        
        Machine.Configure(PlayerFsmState.TinsicaJumpsquat)
            .SubstateOf(GravityFsmState.Grounded)
            .SubstateOf(PlayerFsmState.LockMomentum)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.TinsicaJump)
            .OnEntry(_ =>
            {
                Animator.SetLayerWeight(1, 0);
                _inputBuffer.ConsumeBuffer("Jump");
                FMODUnity.RuntimeManager.PlayOneShotAttached(jumpFmodEvent, gameObject);
                OnPlayerFootstep();
            })
            .OnExitFrom(FsmTrigger.Timeout, _ =>
            {
                OnPlayerFootstep();
            });
        
        Machine.Configure(PlayerFsmState.TinsicaJump)
            .SubstateOf(GravityFsmState.Aerial)
            .SubstateOf(PlayerFsmState.Landable)
            .SubstateOf(PlayerFsmState.AirControl)
            .SubstateOf(PlayerFsmState.PitonInteractable)
            .PermitIf(PlayerFsmTrigger.Dash, PlayerFsmState.Dashsquat, CanDash)
            .SubstateOf(PlayerFsmState.WallInteractable)
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
                PlayerManaManager.Singleton.Consume();
                YVelocity = 21f;
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


