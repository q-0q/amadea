using UnityEngine;

public partial class PlayerFsm
{
    // private void GrappleStartupOnUpdate()
    // {
    //     Animator.SetLayerWeight(2, 0);
    //     var transformPosition = new Vector3(PlayerWeaponFsm.Singleton.transform.position.x, transform.position.y,
    //         PlayerWeaponFsm.Singleton.transform.position.z);
    //     var forward = transformPosition - transform.position;
    //     transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(forward, GetUpVector()),
    //         Time.deltaTime * GrappleStartupRotationLerpStrength);
    //
    //     var destinationPosition = new Vector3(transform.position.x,
    //         PlayerWeaponFsm.Singleton.transform.position.y + GrappleStartupYPositionOffset, transform.position.z);
    //
    //     transform.position = Vector3.Lerp(transform.position,
    //         destinationPosition,
    //         Time.deltaTime * GrappleStartupYPositionLerpStrength);
    //
    //     _momentum = Mathf.Max(0, _momentum - MomentumLossRate * Time.deltaTime * GrappleStartupMomentumLossMod);
    //     HandleCollisionMove();
    // }
    //
    // private void GrappleStartupConfigure()
    // {
    //     Machine.Configure(PlayerFsmState.GrappleStartup)
    //         .SubstateOf(GravityFsmState.DontApplyYVelocity)
    //         .Permit(FsmTrigger.Timeout, PlayerFsmState.Grapple)
    //         .OnEntry(_ =>
    //         {
    //             // ReplaceAnimatorTrigger("GrappleStartup");
    //             YVelocity = 10;
    //             _inputBuffer.ConsumeBuffer("Attack");
    //         }).OnExit(_ => { YVelocity = 0; });
    //
    //     
    // }
}