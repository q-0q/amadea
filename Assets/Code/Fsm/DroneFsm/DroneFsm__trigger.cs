using Unity.VisualScripting;
using UnityEngine;

public partial class DroneFsm
{

    public override void OnFireTriggers()
    {
        base.OnFireTriggers();


        if (PlayerFsm.Singleton.currentInteractable == null || !PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.Interactable) && !PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.Dialogue) && !PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.WalkToPosition))
        {
            if (!PhotoManager.Singleton.IsActive() && !GameMenu.Singleton.IsMenuOpen())
            {
                if (_playerInput.actions["Interact"].WasPressedThisFrame())
                {
                    Machine.Fire(DroneFsmTrigger.Pulse);
                }
            }
        }

    }
    
    
}