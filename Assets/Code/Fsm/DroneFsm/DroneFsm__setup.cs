using System.Collections;
using Code.Misc;
using DG.Tweening;
using UnityEngine;

public partial class DroneFsm
{

    public override void SetupMachine()
    {
        base.SetupMachine();

        Machine.Configure(DroneFsmState.Idle)
            .PermitIf(DroneFsmTrigger.StationInteract, DroneFsmState.Deploying, _ => !IsAnyDroneActive)
            .OnEntry(_ =>
            {
                transform.position = _station.GetDronePosition().position;
                transform.rotation = _station.GetDronePosition().rotation;
                // _lights.SetActive(false);
            });
        
        Machine.Configure(DroneFsmState.Deploying)
            .Permit(DroneFsmTrigger.Timeout, DroneFsmState.Ready)
            .OnEntry(_ =>
            {
                _bobClock = 0f;
                IsAnyDroneActive = true;
                _previousTargetPosition = GetTargetFollowPosition();
            })
            .OnExit(_ =>
            {
                TutorialCanvas.Singleton.ShowTutorialText("Pulse drone", "Interact");
            });

        Machine.Configure(DroneFsmState.Ready)
            .Permit(DroneFsmTrigger.StationInteract, DroneFsmState.Storing)
            .Permit(DroneFsmTrigger.Pulse, DroneFsmState.Pulsing)
            .OnEntry(_ =>
            {
                // _lights.SetActive(true);
            });
        
        Machine.Configure(DroneFsmState.Pulsing)
            .Permit(DroneFsmTrigger.StationInteract, DroneFsmState.Storing)
            .Permit(DroneFsmTrigger.Timeout, DroneFsmState.Ready)
            .OnEntry(_ =>
            {
                _vibrator.DOComplete();
                _vibrator.DOPunchRotation(new Vector3(0f, 0f, 10f), 0.5f, 10, 1f);
                // _pulseParticles.Play();
                Util.InvokeSphereEffect(transform.position - Vector3.up, Vector3.one * 12f, 1.25f, 0.8f, -3f);
                OnDronePulsed?.Invoke(transform.position);
                if(TutorialCanvas.Singleton.GetCurrentAction() == "Interact") TutorialCanvas.Singleton.HideTutorialText();
  
            });
        
        Machine.Configure(DroneFsmState.Storing)
            .Permit(DroneFsmTrigger.Timeout, DroneFsmState.Idle)
            .OnEntry(_ =>
            {
                IsAnyDroneActive = false;
                if(TutorialCanvas.Singleton.GetCurrentAction() == "Interact") TutorialCanvas.Singleton.HideTutorialText();
            });

    }
    
    public override void SetupStateMaps()
    {
        base.SetupStateMaps();
        
        
    }
}