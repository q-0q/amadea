using Code.Misc;
using UnityEngine;

public partial class PlayerFsm
{
    

    private void TrialTeleportOnUpdate()
    {
        var w = Mathf.InverseLerp(TrialTeleportStartupDuration, TrialTeleportDuration - TrialTeleportStartupDuration, TimeInCurrentState());
        w = Util.SmoothLerp01(w);
        transform.position = LerpWithArc(_teleportOrigin, _teleportDestination, w, 2f);
        
        
        if (TimeInCurrentState() > TrialTeleportStartupDuration && PreviousTimeInCurrentState() < TrialTeleportStartupDuration)
        {
            PlayerCinemachineFreeLook.Singleton.OnPlayerCinemachineFreeLookScript(_teleportDirection, TrialTeleportDuration - TrialTeleportStartupDuration);

            _teleportCamera.transform.position = (_teleportDestination + GetUpVector() * 20f) - (_teleportDirection * 20f);
            _teleportCameraLookAt.position = _teleportDestination;
            _teleportCamera.Priority = 20;
        }
    }
    
    private void TrialTeleportConfigure()
    {
        Machine.Configure(PlayerFsmState.TrialTeleport)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.GroundMove)
            .SubstateOf(GravityFsmState.IgnoreDepenetration)
            .OnEntry(_ =>
            {
                YVelocity = 0;
                _momentum = 0;
                Animator.SetFloat("Momentum",0);
                _teleportOrigin = transform.position;
                _teleportParticles.transform.position = transform.position;
                _teleportParticles.Play();
                MakeAllRenderersInvisible();
                
            })
            .OnExit(_ =>
            {
                isSprinting = false;
                EndSurge();
                LastUpwardsPosition = transform.position;
                transform.position = _teleportDestination;
                transform.rotation = Quaternion.LookRotation(_teleportDirection, GetUpVector());
                _teleportParticles.transform.position = transform.position;
                _teleportParticles.Play();
                MakeAllRenderersVisible();
                _teleportCamera.Priority = -20;
            });
    }

    public void MakeAllRenderersVisible()
    {
        foreach (var r in _renderers)
        {
            r.enabled = true;
        }
    }

    public void MakeAllRenderersInvisible()
    {
        foreach (var r in _renderers)
        {
            if (r.name == "TeleportParticles") continue;
            if (r.name.Contains("AmbientParticles")) continue;
            r.enabled = false;
        }
    }

    public static Vector3 LerpWithArc(Vector3 start, Vector3 end, float t, float height)
    {
        // Clamp t for safety
        t = Mathf.Clamp01(t);

        // Base linear interpolation
        Vector3 position = Vector3.Lerp(start, end, t);

        // Quadratic arc: peaks at t = 0.5, zero at t = 0 and t = 1
        float arc = 4f * height * t * (1f - t);

        // Apply arc in the world-up direction
        position += Vector3.up * arc;

        return position;
    }
}