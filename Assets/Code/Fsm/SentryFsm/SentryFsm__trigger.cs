using Unity.VisualScripting;
using UnityEngine;

public partial class SentryFsm
{

    public override void OnFireTriggers()
    {
        base.OnFireTriggers();
        
        if (_blinking && _blinkTimer > 0.75f && !passive)
        {
            Machine.Fire(SentryFsmTrigger.Shoot);
        }


        if (IsPlayerInView())
        {
            _obstructionTimer = 0f;
            Machine.Fire(SentryFsmTrigger.PlayerInView);
        }
        else
        {
            Machine.Fire(SentryFsmTrigger.PlayerOutOfView);
        }
        

    }

    private bool IsPlayerInView()
    {

        if (PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.SentryImmune)) return false;
        
        var toPlayer = GetPlayerPosition() - eye.position;
        // if (Vector3.Angle(toPlayer, -transform.up) < DownwardsBlindspotAngle) return false;
        if (Physics.Raycast(eye.position, toPlayer, out var hit, toPlayer.magnitude, GetEnvironmentalLayermask(),
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        // reduce range when player is above
        // todo: probably should convert to local space
        var rangeMod = Mathf.Lerp(1f, 0.5f, Mathf.InverseLerp(10, 20f, toPlayer.y));
        if (toPlayer.magnitude > Range * rangeMod)
        {
            return false;
        }

        return true;
    }

    private void OnPlayerCultTrialDeath()
    {
        Machine.Jump(SentryFsm.SentryFsmState.Idle);
    }
}