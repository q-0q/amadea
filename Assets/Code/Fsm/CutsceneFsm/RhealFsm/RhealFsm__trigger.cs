using Unity.VisualScripting;
using UnityEngine;

public partial class RhealFsm
{

    public override void OnFireTriggers()
    {
        base.OnFireTriggers();
    }
    
    
    private void TinsicaDialogueCompleted()
    {
        if (_tinsicaDialogueController.currentDialogueIndex == 3)
        {
            PlayerFsm.Singleton.AcquireTrick("Tinsica");
            SaveSystem.WriteRhealTrickLearnedPositionInstance(_positionInstance);
        }
    }
}