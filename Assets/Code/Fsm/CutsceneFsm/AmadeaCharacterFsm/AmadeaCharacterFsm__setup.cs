using DG.Tweening;
using FMOD.Studio;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class AmadeaCharacterFsm
{

    public override void SetupMachine()
    {
        base.SetupMachine();

        Machine.Configure(AmadeaCharacterFsmState.Sit)
            .Permit(AmadeaCharacterFsmTrigger.OnPlayerNear, AmadeaCharacterFsmState.SitToStand);
        
        Machine.Configure(AmadeaCharacterFsmState.SitToStand)
            .Permit(AmadeaCharacterFsmTrigger.Timeout, AmadeaCharacterFsmState.Stand);
        
        Machine.Configure(AmadeaCharacterFsmState.Stand)
            .Permit(AmadeaCharacterFsmTrigger.OnPlayerFar, AmadeaCharacterFsmState.Sit);

    }

    public override void SetupStateMaps()
    {
        base.SetupStateMaps();
        
        
        StateMapConfig.Duration.Add(AmadeaCharacterFsmState.SitToStand, 3f);
        
        StateMapConfig.AnimationTrigger.Add(AmadeaCharacterFsmState.Sit, "Sit");
        StateMapConfig.AnimationTrigger.Add(AmadeaCharacterFsmState.SitToStand, "SitToStand");
        StateMapConfig.AnimationTrigger.Add(AmadeaCharacterFsmState.Stand, "Stand");
    }
}