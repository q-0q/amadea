using System.Collections;
using Code.Misc;
using DG.Tweening;
using UnityEngine;

public partial class PlayerFsm
{

    private Vector3 _gravityGunTravelStartPosition;
    private void GravityGunTravelOnUpdate()
    {
        var w = Util.SmoothLerp01(TimeInCurrentState() / 0.75f);
        transform.position = Vector3.Lerp(_gravityGunTravelStartPosition, GravityGunDestination, w);
    }
    
    private void GravityGunConfigure()
    {
        Machine.Configure(PlayerFsmState.GravityGunTravel)
            .Permit(PlayerFsmTrigger.Timeout, PlayerFsmState.HardLand)
            .OnEntry(_ =>
            {
                PlaySpeedLineParticlesForDuration(-1f);
                MakeAllRenderersInvisible();
                _gravityGunTravelStartPosition = transform.position;
                PlayerCinemachineFreeLook.Singleton.OnPlayerCinemachineFreeLookScript(GravityGunDestination - _gravityGunTravelStartPosition, 0.75f);
            })
            .OnExit(_ =>
            {
                _speedLinesParticles.Stop();
                SnapToGround();
                MakeAllRenderersVisible();
            });

    }
}