using UnityEngine;

public partial class PlayerFsm
{
    private void LockMomentumOnUpdate()
    {
        if (Machine.IsInState(PlayerFsmState.Dash)) return;
        if (Machine.IsInState(PlayerFsmState.SurgeDash)) return;
        if (Machine.IsInState(PlayerFsmState.Updraft)) return;
        Animator.SetFloat("SpeedMod", Mathf.Lerp(GroundMoveMinimumAnimatorSpeedMod, GroundMoveMaximumAnimatorSpeedMod, ComputeMomentumWeight()));
        SetAnimatorMomentum();


        if (!Machine.IsInState(PlayerFsmState.Wallrun) && !Machine.IsInState(PlayerFsmState.HardTurn) && !Machine.IsInState(PlayerFsmState.Slide) && !Machine.IsInState(PlayerFsmState.PitonHoming) && !Machine.IsInState(PlayerFsmState.Wallsquat))
        {
            if (!Machine.IsInState(PlayerFsmState.Wallstep))
            {
                HandleInputMomentumChange();
            }
            
            var animationTurnModifier = 1f;
            if (Machine.IsInState(PlayerFsmState.Jumpsquat) || Machine.IsInState(PlayerFsmState.Landsquat))
                animationTurnModifier = 0f;
            else if (isSprinting) animationTurnModifier = 0.5f;
            HandleTurning(1f, false, 1f, false, animationTurnModifier);
        }
        
        HandleCollisionMove();
    }
}