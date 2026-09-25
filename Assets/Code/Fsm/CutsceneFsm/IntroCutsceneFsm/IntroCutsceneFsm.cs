using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Code.Misc;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using Wasp;

public partial class IntroCutsceneFsm : CutsceneFsm
{
    public class IntroCutsceneFsmState : CutsceneFsmState
    {
        public static int AlignCamera;
        public static int ShowText;
        public static int TextFade;
        public static int CanvasFade;
        public static int WaitForMove;
        public static int ForcedMove;
        public static int Hold;
    }

    public class IntroCutsceneFsmTrigger : CutsceneFsm.CutsceneFsmTrigger
    {
        public static int TextComplete;
        public static int OnInteracted;
        public static int OnPlayerMoveInput;
    }

    protected override void OnAwake()
    {
        base.OnAwake();
        // _interactable = GetComponentInChildren<Interactable>();

        _particleSystem = GetComponentInChildren<ParticleSystem>();
        _curvedStarRenderer = _particleSystem.transform.Find("CurvedStar").GetComponent<Renderer>();
        _haloRenderer = _particleSystem.transform.Find("Halo").GetComponent<Renderer>();
        _light = GetComponentInChildren<Light>();
    }

    protected override void OnStart()
    {
        base.OnStart();
        PlayerFsm.Singleton.gameObject.TryGetComponent(out _playerInput);
        InitState = IntroCutsceneFsmState.Inactive;
        transform.Find("IntroCutsceneVirtualCamera").TryGetComponent(out _virtualCamera);
        
        
        transform.Find("IntroCutsceneCanvas").TryGetComponent(out _mainCanvasGroup);
        transform.Find("IntroCutsceneCanvas").Find("TextCanvasGroup").TryGetComponent(out _textCanvasGroup);
        _textTmp = GetComponentInChildren<TextMeshProUGUI>();


        var cameraFollow = FindObjectOfType<CameraFollow>().transform;
        _virtualCamera.Follow = cameraFollow;
        _virtualCamera.LookAt = cameraFollow;
        



    }
    
    public override void OnUpdate()
    {
        base.OnUpdate();

        _particleSystem.transform.position = PlayerFsm.Singleton.transform.position;
        
        if (Machine.IsInState(IntroCutsceneFsmState.ShowText))
        {
            _textClock += Time.deltaTime;
            var text = texts[_currentTextId];
            var textCharId = Math.Min((int)(_textClock / 0.04f), text.Length);
            var newTextStr = text.Substring(0, textCharId) + "<alpha=#00>" + text.Substring(textCharId);
            _textTmp.text = newTextStr;

            if ((int)(_textClock / 0.04f) >= text.Length + 25)
            {
                textAdvanceImage.enabled = true;
                textAdvanceImage.sprite = InputTypeManager.Singleton.GetSpriteForAction("Interact");
            }

            if (_playerInput.actions["Interact"].WasPressedThisFrame())
            {
                if (textCharId < text.Length) _textClock = 100f;
                else if (_currentTextId < texts.Count - 1)
                {
                    textAdvanceImage.enabled = false;
                    _currentTextId++;
                    _textClock = 0f;
                }
                else
                {
                    textAdvanceImage.enabled = false;
                    Machine.Fire(IntroCutsceneFsmTrigger.TextComplete);
                }
            }
        }
        
        if (Machine.IsInState(IntroCutsceneFsmState.TextFade))
        {
            _textCanvasGroup.alpha = Mathf.Lerp(1f, 0.0f, Mathf.InverseLerp(0f, 2f, TimeInCurrentState()));
        }

        if (Machine.IsInState(IntroCutsceneFsmState.CanvasFade))
        {
            _mainCanvasGroup.alpha = Mathf.Lerp(1f, 0.0f, Mathf.InverseLerp(0f, CanvasFadeDuration, TimeInCurrentState()));
            FMODUnity.RuntimeManager.StudioSystem.setParameterByName("RespawnAmbientAmount",
                Mathf.InverseLerp(0, 7f, TimeInCurrentState()));
        }
        
    }

    protected override void OnStartComplete()
    {
        base.OnStartComplete();
        if (SaveSystem.GetPersistentEventCompleted(CutscenePersistentEvent))
        {
            Machine.Jump(CutsceneFsmState.Inactive);
            _particleSystem.Stop();
            _particleSystem.Clear();
            _light.enabled = false;
            _curvedStarRenderer.enabled = false;
            _haloRenderer.enabled = false;
            return;
        }
        Machine.Fire(CutsceneFsmTrigger.StartCutscene);
    }

    protected override void OnStateChanged(TriggerParams triggerParams)
    {
        base.OnStateChanged(triggerParams);
    }

    private void OnEnable()
    {
        // _interactable.OnInteracted += OnInteracted;
    }

    private void OnDisable()
    {
        // _interactable.OnInteracted -= OnInteracted;
    }
}
