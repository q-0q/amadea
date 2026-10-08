using System;
using UnityEngine;

public partial class PlayerFsm
{
    private void AirControlOnUpdate()
    {
        
        if (_momentum < 5f)
        {
            isSprinting = false;
            EndSurge();
        }
        
        // ANTI-PATTERN!
        var increaseMultiplier = 0.1f;
        var decreaseMultiplier = AirControlMomentumDecayModifier;
        var turningMultiplier = AirControlTurningMultiplier;
        var forceForwardInput = false;
        var animationTurnMod = 1f;
        if (Machine.IsInState(PlayerFsmState.Updraft))
        {
            increaseMultiplier = 1f;
            // turningMultiplier = Mathf.Lerp(AirControlTurningMultiplier * 1.5f, AirControlTurningMultiplier, Mathf.InverseLerp(60f, 20f, YVelocity));
            decreaseMultiplier *= 0.25f;
            forceForwardInput = false;
        }
        
        if (Machine.IsInState(PlayerFsmState.PitonFlip))
        {
            // increaseMultiplier = Mathf.Lerp(0.1f, 0.8f, Mathf.InverseLerp(60f, 0f, YVelocity));
            // turningMultiplier = Mathf.Lerp(AirControlTurningMultiplier * 1.5f, AirControlTurningMultiplier, Mathf.InverseLerp(60f, 20f, YVelocity));
            forceForwardInput = false;
        }
        
        if (Machine.IsInState(PlayerFsmState.RopeSwingJump))
        {
            // increaseMultiplier = Mathf.Lerp(0.1f, 0.8f, Mathf.InverseLerp(60f, 0f, YVelocity));
            // turningMultiplier = Mathf.Lerp(AirControlTurningMultiplier * 1.5f, AirControlTurningMultiplier, Mathf.InverseLerp(60f, 20f, YVelocity));
            animationTurnMod = Mathf.Lerp(0, 1f, Mathf.InverseLerp(0.5f, 1f, TimeInCurrentState()));
        }
        
        if (Machine.IsInState(PlayerFsmState.LongFall))
        {
            // increaseMultiplier = Mathf.Lerp(0.1f, 0.8f, Mathf.InverseLerp(60f, 0f, YVelocity));
            // turningMultiplier = Mathf.Lerp(AirControlTurningMultiplier * 1.5f, AirControlTurningMultiplier, Mathf.InverseLerp(60f, 20f, YVelocity));
            animationTurnMod = 0.1f;
        }
        
        if (_momentum < 8f)
        {
            increaseMultiplier = 1.5f;
            turningMultiplier = 1.5f;
        }

        HandleTurning(turningMultiplier, forceForwardInput, AirControlTurningMomentumDecayModifier, false, animationTurnMod);
        HandleInputMomentumChange(increaseMultiplier, decreaseMultiplier);
    }
}