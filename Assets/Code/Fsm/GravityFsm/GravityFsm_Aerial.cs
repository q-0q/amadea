using UnityEngine;

public abstract partial class GravityFsm
{
    private void AerialOnUpdate()
    {
        if (Machine.IsInState(GravityFsmState.DontApplyYVelocity)) return;
        
        var v3 = GetUpVector() * (YVelocity * Time.deltaTime);
        transform.position += v3;
        
        if (!Machine.IsInState(GravityFsmState.DontLoseYVelocity))
        {
            YVelocity -= (GravityStrength * GravityStrength * Time.deltaTime * StateMapConfig.GravityStrengthMod.Get(this) * BonusGravityModifier);
        }
        TimeInAir += Time.deltaTime;
        UpdateYVelocityMetadata();
        HandleGust();
    }

    private void AerialConfigure()
    {
        Machine.Configure(GravityFsmState.Aerial)
            .OnEntryFrom(GravityFsmTrigger.StartFrameAerial, _ =>
            {
                TimeInAir = 0;
            });
    }
}