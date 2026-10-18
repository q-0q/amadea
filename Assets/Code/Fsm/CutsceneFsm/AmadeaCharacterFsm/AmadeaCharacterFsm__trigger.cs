using Unity.VisualScripting;
using UnityEngine;

public partial class AmadeaCharacterFsm
{

    public override void OnFireTriggers()
    {
        base.OnFireTriggers();


        var d = Vector3.Distance(PlayerFsm.Singleton.transform.position, transform.position);
        if (d < 70f)
        {
            Machine.Fire(AmadeaCharacterFsmTrigger.OnPlayerNear);
        }
        else
        {
            Machine.Fire(AmadeaCharacterFsmTrigger.OnPlayerFar);
        }
    }

    private void OnInteracted()
    {
        Machine.Fire(AmadeaCharacterFsmTrigger.OnInteracted);
    }
    
    private void OnDialogueCompleted()
    {
        Machine.Fire(AmadeaCharacterFsmTrigger.OnDialogueCompleted);
    }
}