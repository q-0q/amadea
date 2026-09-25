using System.Collections;
using DG.Tweening;
using FMOD.Studio;
using UnityEngine;
using UnityEngine.SceneManagement;
using Util = Code.Misc.Util;

public partial class IntroCutsceneFsm
{

    public override void SetupMachine()
    {
        base.SetupMachine();

        Machine.Configure(CutsceneFsmState.Inactive)
            .OnEntry(_ =>
            {
                Time.timeScale = 1f;
                _virtualCamera.Priority = -20;
                _mainCanvasGroup.alpha = 0f;
            })
            .Permit(CutsceneFsmTrigger.StartCutscene, IntroCutsceneFsmState.AlignCamera);

        Machine.Configure(IntroCutsceneFsmState.AlignCamera)
            .Permit(FsmTrigger.Timeout, IntroCutsceneFsmState.ShowText) // ShowText
            .SubstateOf(CutsceneFsmState.Active)
            .OnEntry(_ =>
            {
                _mainCanvasGroup.alpha = 1f;
                _currentTextId = 0;
                var find = transform.Find("PlayerInitialPosition");

                PlayerFsm.Singleton.transform.position = find.position;
                PlayerFsm.Singleton.transform.rotation = find.rotation;
                Shader.SetGlobalFloat("_PlayerEvaporateClip", 1f);
                PlayerCinemachineFreeLook.Singleton.SetAxes(-135f, 0.5f);
                _particleSystem.Play();
                _light.enabled = true;
                _virtualCamera.Priority = 50;
            });
        
        Machine.Configure(IntroCutsceneFsmState.ShowText)
            .Permit(IntroCutsceneFsmTrigger.TextComplete, IntroCutsceneFsmState.TextFade)
            .SubstateOf(CutsceneFsmState.Active);
        
        Machine.Configure(IntroCutsceneFsmState.TextFade)
            .Permit(FsmTrigger.Timeout, IntroCutsceneFsmState.CanvasFade)
            // .Permit(CutsceneFsmTrigger.Skip, IntroCutsceneFsmState.Shake2)
            .SubstateOf(CutsceneFsmState.Active)
            .OnEntry(_ =>
        {
            // PlayerFsm.Singleton.Machine.Jump(PlayerFsm.PlayerFsmState.CutsceneIdle);
        });

        Machine.Configure(IntroCutsceneFsmState.CanvasFade)
            .Permit(FsmTrigger.Timeout, IntroCutsceneFsmState.Hold)
            // .Permit(CutsceneFsmTrigger.Skip, IntroCutsceneFsmState.Shake2)
            .SubstateOf(CutsceneFsmState.Active)
            .OnEntry(_ =>
            {
                
            });
        
        Machine.Configure(IntroCutsceneFsmState.Hold)
            .Permit(FsmTrigger.Timeout, IntroCutsceneFsmState.WaitForMove)
            // .Permit(CutsceneFsmTrigger.Skip, IntroCutsceneFsmState.Shake2)
            .SubstateOf(CutsceneFsmState.Active)
            .OnEntry(_ =>
            {
                
            });
        
        Machine.Configure(IntroCutsceneFsmState.WaitForMove)
            .Permit(IntroCutsceneFsmTrigger.OnPlayerMoveInput, IntroCutsceneFsmState.Inactive)
            // .Permit(CutsceneFsmTrigger.Skip, IntroCutsceneFsmState.Shake2)
            .SubstateOf(CutsceneFsmState.Active)
            .OnExit(_ =>
            {

                SaveSystem.WritePersistentEvent(CutscenePersistentEvent);
                SaveSystem.WritePlayerInGamePosition(PlayerFsm.Singleton.transform.position, "", PlayerFsm.Singleton.transform.rotation.eulerAngles.y);
                IEnumerator Coroutine()
                {
                    var t = 0f;
                    var d = 4f;
                    while (t < d)
                    {
                        Shader.SetGlobalFloat("_PlayerEvaporateClip", 1f - Util.SmoothLerp01(t / d));
                        Shader.SetGlobalFloat("_PlayerTintWeight", 1f - Util.SmoothLerp01(t / d));
                        _curvedStarRenderer.material.SetFloat("_Clip", Util.SmoothLerp01(t / d));
                        _haloRenderer.material.SetFloat("_Clip", Mathf.Lerp(0.5f, 3f, Util.SmoothLerp01(t / d)));
                        t += Time.deltaTime;
                        yield return null;
                    }
                    
                    _curvedStarRenderer.enabled = false;
                    _haloRenderer.enabled = false;
                    _particleSystem.Stop();
                    _light.enabled = false;
                }
                
                IEnumerator SoundCoroutine()
                {
                    var t = 0f;
                    var d = 7f;
                    while (t < d)
                    {
                        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("RespawnAmbientAmount",
                            1f - (t / d));
                        t += Time.deltaTime;
                        yield return null;
                    }
                }
                
                _virtualCamera.Priority = -50;
                StartCoroutine(SoundCoroutine());
                StartCoroutine(Coroutine());
            })
            .OnEntry(_ =>
            {
                
            });
        

    }

    public override void SetupStateMaps()
    {
        base.SetupStateMaps();
        
        StateMapConfig.Duration.Add(IntroCutsceneFsmState.TextFade, 4f);
        StateMapConfig.Duration.Add(IntroCutsceneFsmState.Hold, 1.5f);
        
        StateMapConfig.Duration.Add(IntroCutsceneFsmState.CanvasFade, CanvasFadeDuration);
        StateMapConfig.CutscenePlayerDisabled.Add(IntroCutsceneFsmState.WaitForMove, false);
        StateMapConfig.CutscenePlayerDisabled.Add(IntroCutsceneFsmState.Hold, false);
        StateMapConfig.CutsceneHideTutorialCanvas.Add(IntroCutsceneFsmState.WaitForMove, false);
    }
}