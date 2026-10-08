using DG.Tweening;
using FMOD.Studio;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class SavapheSimpleFsm
{

    public override void SetupMachine()
    {
        base.SetupMachine();

        Machine.Configure(CutsceneFsmState.Inactive);

        Machine.Configure(SavapheSimpleFsmState.NotRung)
            .SubstateOf(CutsceneFsmState.Inactive)
            .Permit(SavapheSimpleFsmTrigger.BellRung, SavapheSimpleFsmState.Rung);


        Machine.Configure(SavapheSimpleFsmState.Rung)
            .SubstateOf(CutsceneFsmState.Inactive)
            .OnEntry(_ =>
            {
                GetComponentInChildren<DialogueController>().currentDialogueIndex = 2;
            });
        
    }

    public override void SetupStateMaps()
    {
        base.SetupStateMaps();
        StateMapConfig.AnimationTrigger.Add(SavapheSimpleFsmState.NotRung, "NotCrossedIdle");
        StateMapConfig.AnimationTrigger.Add(SavapheSimpleFsmState.Rung, "NotCrossedDialogue");
    }
}