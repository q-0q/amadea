using DG.Tweening;
using FMOD.Studio;
using UnityEngine;
using UnityEngine.SceneManagement;
using Util = Code.Misc.Util;

public partial class TestCutsceneFsm
{

    public override void SetupMachine()
    {
        base.SetupMachine();

        Machine.Configure(CutsceneFsmState.Inactive)
            .OnEntry(_ =>
            {
                Time.timeScale = 1f;
                _virtualCamera.Priority = 0;
                _finalVirtualCamera.Priority = 0;
                _mainCanvasGroup.alpha = 0f;
                gondola.transform.position = _endPosition.position;
            })
            .Permit(CutsceneFsmTrigger.StartCutscene, TestCutsceneFsmState.AlignCamera);

        Machine.Configure(TestCutsceneFsmState.AlignCamera)
            .Permit(FsmTrigger.Timeout, TestCutsceneFsmState.ShowText) // ShowText
            .SubstateOf(CutsceneFsmState.Active)
            .OnEntry(_ =>
            {
                PlayerFsm.Singleton.transform.position = _playerTransformOnStart.position;
                PlayerFsm.Singleton.transform.rotation = _playerTransformOnStart.rotation;
                _mainCanvasGroup.alpha = 1f;
                
                _currentTextId = 0;
                _moveCubeForwardShake1 = false;
                _moveCubeForwardShake2 = false;
                
                PlayerCinemachineFreeLook.Singleton.SetAxes(-45f, 0.7f);
            });
        
        Machine.Configure(TestCutsceneFsmState.ShowText)
            .Permit(TestCutsceneFsmTrigger.TextComplete, TestCutsceneFsmState.TextFade)
            .SubstateOf(CutsceneFsmState.Active);
        
        Machine.Configure(TestCutsceneFsmState.TextFade)
            .Permit(FsmTrigger.Timeout, TestCutsceneFsmState.CanvasFade)
            // .Permit(CutsceneFsmTrigger.Skip, TestCutsceneFsmState.Shake2)
            .SubstateOf(CutsceneFsmState.Active)
            .OnEntry(_ =>
        {
            // PlayerFsm.Singleton.Machine.Jump(PlayerFsm.PlayerFsmState.CutsceneIdle);
            _creakEventInstance = FMODUnity.RuntimeManager.CreateInstance(gondolaCreakEventReference);
            FMODUnity.RuntimeManager.AttachInstanceToGameObject(_creakEventInstance, gondola.gameObject);
            _creakEventInstance.start();
        });

        Machine.Configure(TestCutsceneFsmState.CanvasFade)
            .Permit(FsmTrigger.Timeout, TestCutsceneFsmState.PlayerControl)
            .SubstateOf(TestCutsceneFsmState.MoveForward)
            // .Permit(CutsceneFsmTrigger.Skip, TestCutsceneFsmState.Shake2)
            .SubstateOf(CutsceneFsmState.Active)
            .OnEntry(_ =>
            {
                innerCube.DOShakePosition(3.75f, 0.25f);
                innerCube.DOShakeRotation(3.75f, 0.5f);
            });
        
        Machine.Configure(TestCutsceneFsmState.PlayerControl)
            .SubstateOf(TestCutsceneFsmState.MoveForward)
            .Permit(TestCutsceneFsmTrigger.OnTriggersCompleted, TestCutsceneFsmState.InteractableReady1)
            .SubstateOf(CutsceneFsmState.Active)
            .OnEntry(_ =>
            {
                TutorialCanvas.Singleton.ShowTutorialText("Move", "Move");
            });
        
        Machine.Configure(TestCutsceneFsmState.InteractableReady1)
            .SubstateOf(TestCutsceneFsmState.MoveForward)
            .Permit(TestCutsceneFsmTrigger.OnInteracted, TestCutsceneFsmState.InteractableChannel1)
            .SubstateOf(CutsceneFsmState.Active)
            .OnEntry(_ =>
            {
                FMODUnity.RuntimeManager.PlayOneShotAttached(gondolaInteractReadyEventReference, _interactableParticles.gameObject);
                TutorialCanvas.Singleton.HideTutorialText();
                _interactable.SetEnabled(true);
                _interactableParticles.Play();
                _interactable.transform.localPosition = _interactablePosA;
                Util.InvokeSphereEffect(_interactableParticles.transform.position - Vector3.up, Vector3.one * 5f, 1.5f, 1f, 0 );
                innerCube.DOShakePosition(3.75f, 0.25f);
                innerCube.DOShakeRotation(3.75f, 0.5f);
            });
        
        Machine.Configure(TestCutsceneFsmState.InteractableChannel1)
            .SubstateOf(TestCutsceneFsmState.MoveForward)
            .Permit(TestCutsceneFsmTrigger.Timeout, TestCutsceneFsmState.InteractableReady2)
            .SubstateOf(CutsceneFsmState.Active)
            .OnEntry(_ =>
            {
                FMODUnity.RuntimeManager.PlayOneShotAttached(gondolaInteractEventReference, _interactableParticles.gameObject);
                TutorialCanvas.Singleton.ShowTutorialText("Look", "Look");
                _interactable.SetEnabled(false);
                _interactable.transform.localPosition = _interactablePosB;
                Util.InvokeSphereEffect(_interactableParticles.transform.position - Vector3.up, Vector3.one * 5f, 1.5f, 1f, 0 );
            });
        
        Machine.Configure(TestCutsceneFsmState.InteractableReady2)
            .SubstateOf(TestCutsceneFsmState.MoveForward)
            .Permit(TestCutsceneFsmTrigger.OnInteracted, TestCutsceneFsmState.InteractableChannel2)
            .SubstateOf(CutsceneFsmState.Active)
            .OnEntry(_ =>
            {
                FMODUnity.RuntimeManager.PlayOneShotAttached(gondolaInteractReadyEventReference, _interactableParticles.gameObject);
                _interactable.SetEnabled(true);
                _interactableParticles.Play();
                Util.InvokeSphereEffect(_interactableParticles.transform.position - Vector3.up, Vector3.one * 5f, 1.5f, 1f, 0 );
                innerCube.DOShakePosition(3.75f, 0.25f);
                innerCube.DOShakeRotation(3.75f, 0.5f);
            });
        
        Machine.Configure(TestCutsceneFsmState.InteractableChannel2)
            .SubstateOf(TestCutsceneFsmState.MoveForward)
            .Permit(TestCutsceneFsmTrigger.Timeout, TestCutsceneFsmState.InteractableReady3)
            .SubstateOf(CutsceneFsmState.Active)
            .OnEntry(_ =>
            {
                FMODUnity.RuntimeManager.PlayOneShotAttached(gondolaInteractEventReference, _interactableParticles.gameObject);
                TutorialCanvas.Singleton.HideTutorialText();
                _interactable.SetEnabled(false);
                _interactable.transform.localPosition = _interactablePosA;
                Util.InvokeSphereEffect(_interactableParticles.transform.position - Vector3.up, Vector3.one * 5f, 1.5f, 1f, 0 );
            });
        
        Machine.Configure(TestCutsceneFsmState.InteractableReady3)
            .SubstateOf(TestCutsceneFsmState.MoveForward)
            .Permit(TestCutsceneFsmTrigger.OnInteracted, TestCutsceneFsmState.Channel)
            .SubstateOf(CutsceneFsmState.Active)
            .OnEntry(_ =>
            {
                FMODUnity.RuntimeManager.PlayOneShotAttached(gondolaInteractReadyEventReference, _interactableParticles.gameObject);
                _mainCanvasGroup.alpha = 0f;
                _interactable.SetEnabled(true);
                _interactableParticles.Play();
                Util.InvokeSphereEffect(_interactableParticles.transform.position - Vector3.up, Vector3.one * 5f, 1.5f, 1f, 0 );
            });
        
        Machine.Configure(TestCutsceneFsmState.Channel)
            // .SubstateOf(TestCutsceneFsmState.MoveForward)
            .Permit(FsmTrigger.Timeout, TestCutsceneFsmState.ChannelEnd)
            .SubstateOf(CutsceneFsmState.Active)
            .OnEntry(_ =>
            {
                PlayerFsm.Singleton.Machine.Jump(PlayerFsm.PlayerFsmState.CutsceneIdle);

                var gondolaDestination = new Vector3(_endPosition.position.x, gondola.transform.position.y, _endPosition.position.z);
                var delta = gondolaDestination - gondola.transform.position;
                PlayerFsm.Singleton.transform.position += delta;
                gondola.transform.position = gondolaDestination;
                // PlayerFsm.Singleton.ForceParentTransformSync();
                OnIntroCutsceneGondolaTeleported?.Invoke(delta);
                
                
                FMODUnity.RuntimeManager.PlayOneShotAttached(gondolaInteractEventReference, _interactableParticles.gameObject);
                FMODUnity.RuntimeManager.PlayOneShotAttached(gondolaInteractChannelEventReference, _interactableParticles.gameObject);
                OnChannelStarted?.Invoke();
                foreach (Transform child in _backgroundParent.transform)
                {
                    GameObject.Destroy(child.gameObject);
                }
                _interactable.SetEnabled(false);
                _channelCamera.Priority = 20;
                
            })
            .OnExit(_ =>
            {
                Util.InvokeSphereEffect(_interactableParticles.transform.position - Vector3.up, Vector3.one * 5f, 1.5f, 1f, 0 );
                _channelCamera.Priority = -20;
            });
        
        Machine.Configure(TestCutsceneFsmState.ChannelEnd)
            .Permit(FsmTrigger.Timeout, TestCutsceneFsmState.Shake1)
            .SubstateOf(CutsceneFsmState.Active)
            .OnEntry(_ =>
            {
                _interactable.SetEnabled(false);
                _interactableParticles.Stop();
                PlayerFsm.Singleton.Machine.Jump(PlayerFsm.PlayerFsmState.CutsceneWary);
                
                FMODUnity.RuntimeManager.PlayOneShotAttached(gondolaInteractEventReference, _interactableParticles.gameObject);
                
                armVibrator.DOShakePosition(2f, 0.0025f, 20);
                _virtualCamera.Priority = 20;
                _creakEventInstance.stop(STOP_MODE.ALLOWFADEOUT);
                FMODUnity.RuntimeManager.PlayOneShotAttached(gondolaMinorBangEventReference, gondola.gameObject);
                innerCube.DOShakePosition(0.75f, 0.5f);
                innerCube.DOShakeRotation(3.75f, 0.5f);
            });
        
        Machine.Configure(TestCutsceneFsmState.Shake1)
            .Permit(FsmTrigger.Timeout, TestCutsceneFsmState.MoveCubeDown1)
            .SubstateOf(CutsceneFsmState.Active)
            .OnEntry(_ =>
            {
                _interactable.SetEnabled(false);
                _interactableParticles.Stop();
                PlayerFsm.Singleton.Machine.Jump(PlayerFsm.PlayerFsmState.CutsceneWary);

                armVibrator.DOShakePosition(2f, 0.0025f, 20);
                _virtualCamera.Priority = 20;
                _creakEventInstance.stop(STOP_MODE.ALLOWFADEOUT);
                FMODUnity.RuntimeManager.PlayOneShotAttached(gondolaMinorBangEventReference, gondola.gameObject);
                FMODUnity.RuntimeManager.PlayOneShotAttached(gondolaGroanEventReference, gondola.gameObject);
                innerCube.DOShakePosition(0.75f, 0.5f);
                innerCube.DOShakeRotation(3.75f, 0.5f);
            });

        Machine.Configure(TestCutsceneFsmState.MoveCubeDown1)
            .Permit(FsmTrigger.Timeout, TestCutsceneFsmState.WaitForInput)
            .SubstateOf(CutsceneFsmState.Active)
            .OnEntry(_ =>
            {
                armVibrator.DOShakePosition(1.5f, 0.005f, 20);
                FMODUnity.RuntimeManager.PlayOneShotAttached(gondolaBreakEventReference, gondola.gameObject);
                
                innerCube.DOShakePosition(0.75f, 0.5f);
            });
        
        Machine.Configure(TestCutsceneFsmState.WaitForInput)
            .Permit(TestCutsceneFsmTrigger.PlayerInputJump, TestCutsceneFsmState.WaitForJumpsquat)
            .SubstateOf(CutsceneFsmState.Active)
            .OnEntry(_ =>
            {
                PlayerFsm.Singleton.SetMomentum(0f);
                TutorialCanvas.Singleton.ShowTutorialText("Jump", "Jump");
            });
        
        Machine.Configure(TestCutsceneFsmState.WaitForJumpsquat)
            .Permit(TestCutsceneFsmTrigger.PlayerInJumpState, TestCutsceneFsmState.MoveCubeDown2)
            .SubstateOf(CutsceneFsmState.Active)
            .OnEntry(_ =>
            {
                TutorialCanvas.Singleton.HideTutorialText();
                Time.timeScale = 0.5f;
                PlayerFsm.Singleton.Machine.Jump(PlayerFsm.PlayerFsmState.Jumpsquat);
            });

        Machine.Configure(TestCutsceneFsmState.MoveCubeDown2)
            .Permit(FsmTrigger.Timeout, TestCutsceneFsmState.Shake2)
            .SubstateOf(CutsceneFsmState.Active)
            .OnEntry(_ =>
            {
                _finalVirtualCamera.Priority = 20;
                _virtualCamera.Priority = 0;
                Time.timeScale = 0.65f;
            });
        
        Machine.Configure(TestCutsceneFsmState.Shake2)
            .Permit(TestCutsceneFsmTrigger.Timeout, TestCutsceneFsmState.Inactive)
            .SubstateOf(CutsceneFsmState.Active)
            .OnEntry(_ =>
            {
                PlayerCinemachineFreeLook.Singleton.SetAxes(-45f, 0.7f);
                FMODUnity.RuntimeManager.PlayOneShotAttached(gondolaCrashEventReference, gondola.gameObject);

                if (PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.CutsceneIdle))
                {
                    PlayerFsm.Singleton.Machine.Jump(PlayerFsm.PlayerFsmState.HardLand);
                }
                // _finalVirtualCamera.Priority = 0;
                // _virtualCamera.Priority = 20;
                Time.timeScale = 1f;
                _creakEventInstance.stop(STOP_MODE.ALLOWFADEOUT);
                _mainCanvasGroup.alpha = 0f;
                _impactParticles.Play(true);
                innerCube.DOShakePosition(1.5f, 1f);
                gondola.DOShakePosition(1.5f, 1f);
                gondola.transform.position = _endPosition.position;
                FMODUnity.RuntimeManager.StudioSystem.setParameterByName("TimeScale", 1f);
                SaveSystem.WritePersistentEvent(CutscenePersistentEvent);
                SaveSystem.WritePlayerInGamePosition(_endPosition.position + Vector3.up * 5f, "", 0f);
            });
        
        // Machine.Configure(TestCutsceneFsmState.FinalCamera)
        //     .Permit(TestCutsceneFsmTrigger.Timeout, TestCutsceneFsmState.Inactive)
        //     .SubstateOf(CutsceneFsmState.Active)
        //     .OnEntry(_ =>
        //     {
        //         _virtualCamera.Priority = 0;
        //         _finalVirtualCamera.Priority = 20;
        //
        //     });

    }

    public override void SetupStateMaps()
    {
        base.SetupStateMaps();
        
        StateMapConfig.Duration.Add(TestCutsceneFsmState.TextFade, 4f);
        StateMapConfig.Duration.Add(TestCutsceneFsmState.CanvasFade, CanvasFadeDuration);
        StateMapConfig.Duration.Add(TestCutsceneFsmState.Shake1, 3f);
        StateMapConfig.Duration.Add(TestCutsceneFsmState.Shake2, 3f);
        StateMapConfig.Duration.Add(TestCutsceneFsmState.MoveCubeDown1, 0.725f);
        StateMapConfig.Duration.Add(TestCutsceneFsmState.MoveCubeDown2, 0.725f);
        StateMapConfig.Duration.Add(TestCutsceneFsmState.PlayerControl, 4f);
        StateMapConfig.Duration.Add(TestCutsceneFsmState.Channel, 3.5f);
        StateMapConfig.Duration.Add(TestCutsceneFsmState.ChannelEnd, 2f);
        
        StateMapConfig.CutscenePlayerDisabled.Add(TestCutsceneFsmState.PlayerControl, false);
        StateMapConfig.CutscenePlayerDisabled.Add(TestCutsceneFsmState.InteractableReady1, false);
        StateMapConfig.CutscenePlayerDisabled.Add(TestCutsceneFsmState.InteractableChannel1, false);
        StateMapConfig.CutscenePlayerDisabled.Add(TestCutsceneFsmState.InteractableReady2, false);
        StateMapConfig.CutscenePlayerDisabled.Add(TestCutsceneFsmState.InteractableChannel2, false);
        StateMapConfig.CutscenePlayerDisabled.Add(TestCutsceneFsmState.InteractableReady3, false);
        // StateMapConfig.CutscenePlayerDisabled.Add(TestCutsceneFsmState.MoveCubeDown2, t);
        StateMapConfig.CutscenePlayerDisabled.Add(TestCutsceneFsmState.Shake2, false);
        StateMapConfig.CutscenePlayerDisabled.Add(TestCutsceneFsmState.FinalCamera, false);
        
        StateMapConfig.CutsceneJumpDisabled.Add(TestCutsceneFsmState.Shake2, false);
        StateMapConfig.CutsceneJumpDisabled.Add(TestCutsceneFsmState.FinalCamera, false);
        
        StateMapConfig.CutsceneCameraDisabled.Add(TestCutsceneFsmState.InteractableChannel1, false);
        StateMapConfig.CutsceneCameraDisabled.Add(TestCutsceneFsmState.InteractableReady2, false);
        StateMapConfig.CutsceneCameraDisabled.Add(TestCutsceneFsmState.InteractableChannel2, false);
        StateMapConfig.CutsceneCameraDisabled.Add(TestCutsceneFsmState.InteractableReady3, false);
        
        StateMapConfig.CutsceneHardLand.Add(TestCutsceneFsmState.MoveCubeDown2, true);
        StateMapConfig.CutsceneHardLand.Add(TestCutsceneFsmState.Shake2, true);
        
        StateMapConfig.AnimationTrigger.Add(TestCutsceneFsmState.MoveForward, "MoveForward");
        StateMapConfig.AnimationTrigger.Add(TestCutsceneFsmState.Shake1, "BreakStartup");
        StateMapConfig.AnimationTrigger.Add(TestCutsceneFsmState.MoveCubeDown1, "Break");
        
        StateMapConfig.IsAbstract.Add(TestCutsceneFsmState.MoveForward, true);
    }
}