using DG.Tweening;
using UnityEngine;
using Wasp;

public partial class PlayerFsm
{

    private void ForcePitonRotation()
    {
        var quaternion = Quaternion.LookRotation(_currentPitonTransform.forward, GetUpVector());
        transform.rotation = Quaternion.Slerp(transform.rotation, quaternion,
            RotationSpeed * Time.deltaTime * ForceWallRotationSpeed);
    }
    
    private void PitonHomingOnUpdate()
    {
        ForcePitonRotation();
        transform.position = Vector3.Lerp(transform.position, _currentPitonTransform.position + GetUpVector() * -2f, Time.deltaTime * 8f);
    }

    private void PitonsquatOnUpdate()
    {
        
    }

    private void PitonConfigure()
    {

        Machine.Configure(PlayerFsmState.PitonHoming)
            .SubstateOf(GravityFsmState.Aerial)
            .SubstateOf(PlayerFsmState.ForceWallRotation)
            .SubstateOf(GravityFsmState.DontApplyYVelocity)
            .SubstateOf(GravityFsmState.RespectParentTransform)
            .SubstateOf(GravityFsmState.IgnoreDepenetration)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.FallAfterPitonHoming)
            .PermitIf(PlayerFsmTrigger.Jump, PlayerFsmState.PitonFlipsquat, _ => TimeInCurrentState() > 0.25f)
            // .Permit(GravityFsmTrigger.StartFrameGrounded, PlayerFsmState.Landsquat)
            .OnEntry(@params =>
            {
                if (@params is not PitonParam pitonParam) return;
                _currentPitonTransform.DOShakeRotation(0.175f, 0.3f, 10);
                _currentPitonTransform.DOShakePosition(0.175f, 0.3f, 10);
                _currentPitonTransform = pitonParam.Piton;
                _currentPitonTransform.GetComponent<PitonController>().Rotate = true;
                _wallsquattedSinceLeavingGround = true;
                YVelocity = 0f;
                LastUpwardsPosition = transform.position;
                _currentPitonTransform.GetComponent<PitonController>().PlayLatchEvent();
                
                if (_currentPitonTransform != parentTransform)
                {
                    parentTransform = _currentPitonTransform;
                    _previousParentTransformPosition = parentTransform.position;
                    _previousParentRotation = parentTransform.rotation;
                    OnParentTransformChanged(parentTransform);
                }
            } )
            .OnExit(_ =>
            {
                _currentPitonTransform.GetComponent<PitonController>().Rotate = false;
                
            });

        Machine.Configure(PlayerFsmState.Pitonsquat)
            .SubstateOf(GravityFsmState.Aerial)
            .SubstateOf(GravityFsmState.DontApplyYVelocity)
            .SubstateOf(GravityFsmState.RespectParentTransform)
            .Permit(PlayerFsmTrigger.Jump, PlayerFsmState.PitonFlipsquat)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.Fall);

        Machine.Configure(PlayerFsmState.PitonFlipsquat)
            .SubstateOf(GravityFsmState.DontApplyYVelocity)
            .SubstateOf(GravityFsmState.RespectParentTransform)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.PitonFlip)
            .OnEntry(_ =>
            {
                transform.position = _currentPitonTransform.position + GetUpVector() * -2f;
                _wallsquattedSinceLeavingGround = false;
                _dashSinceLeavingGround = false;
                _previousWallrunSide = FlankType.None;
                _currentFlankType = FlankType.None;
                currentRopeSwing = null;
            });
        
        Machine.Configure(PlayerFsmState.PitonFlip)
            .SubstateOf(GravityFsmState.Aerial)
            .SubstateOf(PlayerFsmState.Landable)
            .SubstateOf(PlayerFsmState.AirControl)
            .SubstateOf(PlayerFsmState.PitonInteractable)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.Fall)
            .PermitIf(PlayerFsmTrigger.Dash, PlayerFsmState.Dashsquat, CanDash)
            .SubstateOf(PlayerFsmState.WallInteractable)
            .OnExitFrom(GravityFsmTrigger.StartFrameGrounded, _ =>
            {
                EndSurge();
            })
            .OnEntry(_ =>
            {
                _momentum = 5f;
                YVelocity = 32f; // 36;
                _currentPitonTransform.DOComplete();
                _currentPitonTransform.DOShakeRotation(1f, 0.4f, 20);
                _currentPitonTransform.DOShakePosition(1f, 0.4f, 20);
                _currentPitonTransform.GetComponent<PitonController>().PlayFlipEvent();
            });

        Machine.Configure(PlayerFsmState.PitonInteractable)
            .PermitIf(PlayerFsmTrigger.EnterPitonTrigger, PlayerFsmState.PitonHoming, @params =>
            {
                if (@params is not PitonParam pitonParam) return false;
                if (Vector3.Angle(transform.forward, pitonParam.Piton.forward) >= 100f) return false;
                var angle = Vector3.Angle(GetDownVector(), pitonParam.Piton.forward);
                if (angle <= 45f) return false;
                if (Machine.IsInState(PlayerFsmState.FallAfterPitonHoming) && TimeInCurrentState() < 0.5f) return false;
                if (Machine.IsInState(PlayerFsmState.Jump) && TimeInCurrentState() < 0.1f) return false;
                return YVelocity < 15f;
            });

        Machine.Configure(PlayerFsmState.FallAfterPitonHoming)
            .SubstateOf(PlayerFsmState.Fall);

    }
}